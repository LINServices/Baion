using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence;

namespace Baion.Orchestrator.Services.Implementations;

internal class ScriptService(IRepository<Script> scripts, IScriptQueries queries, IUnitOfWork unitOfWork) : IScriptService
{
    public async Task<Result<ScriptSummary>> CreateAsync(CreateScriptRequest request, CancellationToken cancellationToken)
    {
        if (Validate(request.Name, request.Content, request.DefaultTimeoutSeconds) is Error error)
        {
            return Result<ScriptSummary>.Failure(error);
        }

        var script = new Script
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Content = request.Content,
            Checksum = ComputeChecksum(request.Content),
            Runtime = request.Runtime,
            DefaultTimeoutSeconds = request.DefaultTimeoutSeconds
        };

        await scripts.AddAsync(script);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ScriptSummary>.Success(ToSummary(script));
    }

    public async Task<Result<ScriptSummary>> UpdateAsync(Guid scriptId, UpdateScriptRequest request, CancellationToken cancellationToken)
    {
        if (Validate(request.Name, request.Content, request.DefaultTimeoutSeconds) is Error error)
        {
            return Result<ScriptSummary>.Failure(error);
        }

        var script = await scripts.GetByIdAsync(scriptId);

        if (script is null)
        {
            return Result<ScriptSummary>.Failure(Error.NotFound("script.not_found", "El script no existe."));
        }

        script.Name = request.Name.Trim();
        script.Description = request.Description?.Trim();
        script.Runtime = request.Runtime;
        script.DefaultTimeoutSeconds = request.DefaultTimeoutSeconds;

        // La versión y el checksum solo se mueven si cambia el contenido: el agente cachea por checksum
        // y no tiene sentido invalidarlo por retocar el nombre o el timeout.
        if (!string.Equals(script.Content, request.Content, StringComparison.Ordinal))
        {
            script.Content = request.Content;
            script.Checksum = ComputeChecksum(request.Content);
            script.Version++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ScriptSummary>.Success(ToSummary(script));
    }

    public async Task<Result<ScriptSummary>> GetAsync(Guid scriptId)
    {
        var script = await scripts.GetByIdAsync(scriptId);

        return script is null
            ? Result<ScriptSummary>.Failure(Error.NotFound("script.not_found", "El script no existe."))
            : Result<ScriptSummary>.Success(ToSummary(script));
    }

    public async Task<PagedResult<ScriptListItem>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken) => await queries.ListScriptsAsync(search, page, pageSize, cancellationToken);

    public async Task<Result<ScriptDetail>> GetDetailAsync(Guid scriptId, CancellationToken cancellationToken)
    {
        var script = await queries.GetScriptDetailAsync(scriptId, cancellationToken);

        return script is null
            ? Result<ScriptDetail>.Failure(Error.NotFound("script.not_found", "El script no existe."))
            : Result<ScriptDetail>.Success(script);
    }

    /// <summary>Reglas comunes al alta y a la edición; devuelve el primer <see cref="Error"/> o null si todo cuadra.</summary>
    private static Error? Validate(string name, string content, int defaultTimeoutSeconds)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("script.name_required", "El nombre del script es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Error.Validation("script.content_required", "El contenido del script es obligatorio.");
        }

        if (defaultTimeoutSeconds <= 0)
        {
            return Error.Validation("script.timeout_invalid", "El timeout por defecto debe ser mayor que cero.");
        }

        return null;
    }

    private static ScriptSummary ToSummary(Script script) => new(script.Id, script.Name, script.Runtime, script.Version, script.Checksum, script.DefaultTimeoutSeconds);

    /// <summary>SHA-256 del contenido en hexadecimal. El agente lo recalcula y rechaza si no cuadra.</summary>
    public static string ComputeChecksum(string content) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}

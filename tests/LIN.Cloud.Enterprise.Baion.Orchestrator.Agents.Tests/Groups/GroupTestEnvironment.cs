using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Support;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Groups;

// Andamiaje común de las pruebas de grupos, lotes y tareas: tenants, datos sembrados directamente
// en la base y clientes HTTP con el token de un usuario del rol que pida la prueba.
internal sealed class GroupTestEnvironment(OrchestratorFactory factory)
{
    public const string AdminRole = "Admin";

    public const string ViewerRole = "Viewer";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<Guid> CreateTenantAsync()
    {
        var tenant = new Tenant { Name = "Grupos", Slug = $"grupos-{Guid.NewGuid():N}", IdentityMode = IdentityMode.SelfManaged };

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant.Id;
    }

    public async Task<string> IssueEnrollmentTokenAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var emitido = await scope.ServiceProvider.GetRequiredService<IEnrollmentTokenService>().CreateAsync(new CreateEnrollmentTokenRequest("Grupos", ServerKind.Vps, null, null), CancellationToken.None);

        Assert.True(emitido.IsSuccess, emitido.Error?.Message);
        return emitido.Value!.Token;
    }

    public async Task<FakeAgent> ConnectAgentAsync(Guid tenantId, ServerPlatform platform = ServerPlatform.Linux)
    {
        return await FakeAgent.ConnectAsync(factory, await IssueEnrollmentTokenAsync(tenantId), platform);
    }

    // Servidor sembrado sin agente: sirve de miembro de grupo cuando la prueba no necesita que responda.
    public async Task<Guid> CreateServerAsync(Guid tenantId, ServerPlatform platform = ServerPlatform.Linux, ServerStatus status = ServerStatus.Offline)
    {
        return (await CreateServersAsync(tenantId, 1, platform, status)).Single();
    }

    public async Task<IReadOnlyList<Guid>> CreateServersAsync(Guid tenantId, int count, ServerPlatform platform = ServerPlatform.Linux, ServerStatus status = ServerStatus.Offline)
    {
        var servers = Enumerable.Range(0, count).Select(_ => new Server
        {
            Name = $"srv-{Guid.NewGuid():N}",
            Hostname = "10.0.0.1",
            Kind = ServerKind.Vps,
            Platform = platform,
            Status = status,
            MachineId = Guid.NewGuid().ToString("N")
        }).ToList();

        await using var scope = factory.CreateTenantScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Servers.AddRange(servers);
        await context.SaveChangesAsync();

        return servers.Select(server => server.Id).ToList();
    }

    public async Task DisableServerAsync(Guid tenantId, Guid serverId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);

        await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Servers
            .Where(server => server.Id == serverId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(server => server.Status, ServerStatus.Disabled));
    }

    public async Task<Guid> CreateScriptAsync(Guid tenantId, ScriptRuntime runtime = ScriptRuntime.Bash)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateScriptRequest($"script-{Guid.NewGuid():N}", null, "echo grupo", runtime, 60);
        var creado = await scope.ServiceProvider.GetRequiredService<IScriptService>().CreateAsync(request, CancellationToken.None);

        Assert.True(creado.IsSuccess, creado.Error?.Message);
        return creado.Value!.Id;
    }

    public async Task DeactivateScriptAsync(Guid tenantId, Guid scriptId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);

        await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Scripts
            .Where(script => script.Id == scriptId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(script => script.IsActive, false));
    }

    public async Task<Guid> CreateGroupAsync(Guid tenantId, IReadOnlyList<Guid> serverIds, string? name = null)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        var grupo = new ServerGroup { Name = name ?? $"grupo-{Guid.NewGuid():N}" };
        context.ServerGroups.Add(grupo);

        foreach (var serverId in serverIds)
            context.ServerGroupMembers.Add(new ServerGroupMember { ServerGroup = grupo, ServerId = serverId });

        await context.SaveChangesAsync();

        return grupo.Id;
    }

    public async Task<Guid> CreateChainAsync(Guid tenantId, IReadOnlyList<Guid> scriptIds)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var pasos = scriptIds.Select((scriptId, indice) => new CreateScriptChainStepRequest(scriptId, indice + 1, ChainFailurePolicy.StopChain, null)).ToList();
        var creada = await scope.ServiceProvider.GetRequiredService<IScriptChainService>().CreateAsync(new CreateScriptChainRequest($"cadena-{Guid.NewGuid():N}", null, pasos), CancellationToken.None);

        Assert.True(creada.IsSuccess, creada.Error?.Message);
        return creada.Value!.Id;
    }

    public async Task<ScheduledTaskSummary> CreateTaskAsync(Guid tenantId, Guid? scriptId, Guid? serverGroupId = null, Guid? scriptChainId = null, Guid? serverId = null, string cron = "0 3 * * *")
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var request = new CreateScheduledTaskRequest($"tarea-{Guid.NewGuid():N}", cron, "UTC", scriptId, scriptChainId, serverId, serverGroupId, ExecutionMode.Attached, 60);
        var creada = await scope.ServiceProvider.GetRequiredService<IScheduledTaskService>().CreateAsync(request, CancellationToken.None);

        Assert.True(creada.IsSuccess, creada.Error?.Message);
        return creada.Value!;
    }

    public async Task<Guid> CreateBatchAsync(Guid tenantId, Guid groupId, Guid scriptId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        var batch = new ExecutionBatch { ServerGroupId = groupId, ServerGroupName = "lote-previo", ScriptId = scriptId, ScriptName = "script-previo", Mode = ExecutionMode.Attached };
        context.ExecutionBatches.Add(batch);
        await context.SaveChangesAsync();

        return batch.Id;
    }

    public async Task<List<ScriptExecution>> GetExecutionsAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ScriptExecutions.AsNoTracking().ToListAsync();
    }

    public async Task<int> CountBatchesAsync(Guid tenantId)
    {
        await using var scope = factory.CreateTenantScope(tenantId);
        return await scope.ServiceProvider.GetRequiredService<BaionDbContext>().ExecutionBatches.CountAsync();
    }

    public HttpClient CreateAnonymousClient()
    {
        return factory.CreateClient();
    }

    // Cliente con un token válido del tenant; los roles deciden si pasa los endpoints de Admin.
    public HttpClient CreateClient(Guid tenantId, string email, params string[] roles)
    {
        var user = new AuthenticatedUser(Guid.CreateVersion7(), email, "Usuario de pruebas", Guid.NewGuid(), roles);
        var token = factory.Services.GetRequiredService<ITokenService>().Issue(tenantId, user);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    public HttpClient CreateAdminClient(Guid tenantId)
    {
        return CreateClient(tenantId, $"admin-{Guid.NewGuid():N}@baion.local", AdminRole);
    }

    public static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        return problem?.Title;
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Baion.Cliente.Web.Models;
using Microsoft.Extensions.Logging;

namespace Baion.Cliente.Web.Services.Implementations;

/// <summary>
/// El token se adjunta petición a petición en lugar de con un <c>DelegatingHandler</c>: los manejadores de
/// <c>IHttpClientFactory</c> viven en su propio ámbito, distinto del circuito de Blazor, y desde ahí no se
/// ve el usuario de la sesión.
/// </summary>
internal class BaionApiClient(HttpClient http, IAccessTokenProvider tokens, ILogger<BaionApiClient> logger) : IBaionApiClient
{
    public async Task<ApiResult<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        return await SendAsync<AuthenticationResult>(message, authenticated: false, cancellationToken);
    }

    public async Task<ApiResult<DashboardSummary>> GetDashboardSummaryAsync(CancellationToken cancellationToken) => await GetAsync<DashboardSummary>("api/dashboard/summary", cancellationToken);

    public async Task<ApiResult<IReadOnlyList<ServerSummary>>> GetServersAsync(CancellationToken cancellationToken) => await GetAsync<IReadOnlyList<ServerSummary>>("api/servers", cancellationToken);

    public async Task<ApiResult<ServerDetail>> GetServerAsync(Guid serverId, CancellationToken cancellationToken) => await GetAsync<ServerDetail>($"api/servers/{serverId}", cancellationToken);

    public async Task<ApiResult<PagedResult<MetricReading>>> GetServerMetricsAsync(Guid serverId, DateTimeOffset? since, DateTimeOffset? until, int page, int pageSize, CancellationToken cancellationToken)
    {
        var filtros = new (string, string?)[]
        {
            ("since", since?.ToString("O", CultureInfo.InvariantCulture)),
            ("until", until?.ToString("O", CultureInfo.InvariantCulture)),
            ("page", Numero(page)),
            ("pageSize", Numero(pageSize))
        };

        return await GetAsync<PagedResult<MetricReading>>($"api/servers/{serverId}/metrics" + QueryString(filtros), cancellationToken);
    }

    public async Task<ApiResult<ServerSummary>> DisableServerAsync(Guid serverId, CancellationToken cancellationToken) => await PostAsync<ServerSummary>($"api/servers/{serverId}/disable", cancellationToken);

    public async Task<ApiResult<ServerSummary>> EnableServerAsync(Guid serverId, CancellationToken cancellationToken) => await PostAsync<ServerSummary>($"api/servers/{serverId}/enable", cancellationToken);

    public async Task<ApiResult<PagedResult<ScriptListItem>>> GetScriptsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var ruta = "api/scripts" + QueryString([("search", search), ("page", Numero(page)), ("pageSize", Numero(pageSize))]);
        return await GetAsync<PagedResult<ScriptListItem>>(ruta, cancellationToken);
    }

    public async Task<ApiResult<ScriptDetail>> GetScriptAsync(Guid scriptId, CancellationToken cancellationToken) => await GetAsync<ScriptDetail>($"api/scripts/{scriptId}", cancellationToken);

    public async Task<ApiResult<ScriptDetail>> CreateScriptAsync(CreateScriptRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/scripts")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        return await SendAsync<ScriptDetail>(message, authenticated: true, cancellationToken);
    }

    public async Task<ApiResult<ScriptSummary>> UpdateScriptAsync(Guid scriptId, UpdateScriptRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, $"api/scripts/{scriptId}")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        return await SendAsync<ScriptSummary>(message, authenticated: true, cancellationToken);
    }

    public async Task<ApiResult<EnrollmentTokenIssued>> CreateEnrollmentTokenAsync(CreateEnrollmentTokenRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/agents/enrollment-tokens")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        return await SendAsync<EnrollmentTokenIssued>(message, authenticated: true, cancellationToken);
    }

    public async Task<ApiResult<IReadOnlyList<EnrollmentTokenSummary>>> GetEnrollmentTokensAsync(CancellationToken cancellationToken) => await GetAsync<IReadOnlyList<EnrollmentTokenSummary>>("api/agents/enrollment-tokens", cancellationToken);

    public async Task<ApiResult<bool>> RevokeEnrollmentTokenAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Delete, $"api/agents/enrollment-tokens/{tokenId}");
        var token = await tokens.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            return ApiResult<bool>.Failure("No hay sesión iniciada.");
        }

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await http.SendAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // La revocación responde 204 sin cuerpo, así que no hay nada que deserializar.
                return ApiResult<bool>.Success(true);
            }

            var (mensaje, codigo) = await DescribeAsync(response, cancellationToken);
            return ApiResult<bool>.Failure(mensaje, codigo);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("Falló la revocación del token {TokenId}: {Motivo}", tokenId, exception.Message);
            return ApiResult<bool>.Failure(UnreachableMessage);
        }
    }

    public async Task<ApiResult<ScriptExecutionDispatched>> DispatchAsync(DispatchScriptRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/executions")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        return await SendAsync<ScriptExecutionDispatched>(message, authenticated: true, cancellationToken);
    }

    public async Task<ApiResult<PagedResult<ScriptExecutionListItem>>> GetExecutionsAsync(Guid? serverId, Guid? scriptId, string? status, DateTimeOffset? since, int page, int pageSize, CancellationToken cancellationToken)
    {
        var filtros = new (string, string?)[]
        {
            ("serverId", serverId?.ToString()),
            ("scriptId", scriptId?.ToString()),
            ("status", status),
            ("since", since?.ToString("O", CultureInfo.InvariantCulture)),
            ("page", Numero(page)),
            ("pageSize", Numero(pageSize))
        };

        return await GetAsync<PagedResult<ScriptExecutionListItem>>("api/executions" + QueryString(filtros), cancellationToken);
    }

    public async Task<ApiResult<ScriptExecutionDetail>> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken) => await GetAsync<ScriptExecutionDetail>($"api/executions/{executionId}", cancellationToken);

    private async Task<ApiResult<TValue>> GetAsync<TValue>(string path, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, path);
        return await SendAsync<TValue>(message, authenticated: true, cancellationToken);
    }

    /// <summary>Acción sin cuerpo sobre un recurso; la respuesta trae el recurso ya actualizado.</summary>
    private async Task<ApiResult<TValue>> PostAsync<TValue>(string path, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, path);
        return await SendAsync<TValue>(message, authenticated: true, cancellationToken);
    }

    private async Task<ApiResult<TValue>> SendAsync<TValue>(HttpRequestMessage message, bool authenticated, CancellationToken cancellationToken)
    {
        if (authenticated)
        {
            var token = await tokens.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                return ApiResult<TValue>.Failure("No hay sesión iniciada.");
            }

            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            using var response = await http.SendAsync(message, cancellationToken);
            return await ReadAsync<TValue>(response, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("Falló la llamada a {Ruta}: {Motivo}", message.RequestUri, exception.Message);
            return ApiResult<TValue>.Failure(UnreachableMessage);
        }
    }

    private static async Task<ApiResult<TValue>> ReadAsync<TValue>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var (mensaje, codigo) = await DescribeAsync(response, cancellationToken);
            return ApiResult<TValue>.Failure(mensaje, codigo);
        }

        var value = await response.Content.ReadFromJsonAsync<TValue>(JsonOptions, cancellationToken);

        return value is null
            ? ApiResult<TValue>.Failure("El orquestador devolvió una respuesta vacía.")
            : ApiResult<TValue>.Success(value);
    }

    /// <summary>
    /// Traduce el error de la API a un mensaje legible y, si la API lo nombra en el <c>title</c> del
    /// <c>ProblemDetails</c>, devuelve también ese código para que quien llame lo lleve a un campo.
    /// </summary>
    private static async Task<(string Mensaje, string? Codigo)> DescribeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized)
        {
            return ("La sesión no es válida o expiró.", null);
        }

        if (response.StatusCode is HttpStatusCode.Forbidden)
        {
            return ("La cuenta no tiene permisos para esta operación.", null);
        }

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(JsonOptions, cancellationToken);
            var codigo = string.IsNullOrWhiteSpace(problem?.Title) ? null : problem!.Title;

            if (!string.IsNullOrWhiteSpace(problem?.Detail))
            {
                return (problem.Detail, codigo);
            }

            if (codigo is not null)
            {
                return ($"El orquestador respondió {(int)response.StatusCode}.", codigo);
            }
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // La respuesta no era el ProblemDetails esperado; se cae al mensaje genérico.
        }

        return ($"El orquestador respondió {(int)response.StatusCode}.", null);
    }

    /// <summary>Arma la cadena de consulta descartando los filtros que vienen vacíos.</summary>
    private static string QueryString((string Nombre, string? Valor)[] parametros)
    {
        var partes = parametros
            .Where(parametro => !string.IsNullOrWhiteSpace(parametro.Valor))
            .Select(parametro => $"{parametro.Nombre}={Uri.EscapeDataString(parametro.Valor!)}")
            .ToArray();

        return partes.Length == 0 ? string.Empty : "?" + string.Join('&', partes);
    }

    private static string Numero(int valor) => valor.ToString(CultureInfo.InvariantCulture);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string UnreachableMessage = "No se pudo contactar con el orquestador.";
}

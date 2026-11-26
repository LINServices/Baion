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
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Auth;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Common;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Dashboard;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Enrollment;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Executions;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scheduling;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scripts;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.ServerGroups;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Servers;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.SystemServices;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Results;
using Microsoft.Extensions.Logging;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Services.Implementations;

// El token se adjunta petición a petición en lugar de con un DelegatingHandler: los manejadores de
// IHttpClientFactory viven en su propio ámbito y desde ahí no se ve el usuario de la sesión.
internal class BaionApiClient(HttpClient http, IAccessTokenProvider tokens, ILogger<BaionApiClient> logger) : IBaionApiClient
{
    private const string UnreachableMessage = "No se pudo contactar con el orquestador.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ApiResult<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        return await SendAsync<AuthenticationResult>(message, authenticated: false, cancellationToken);
    }

    public async Task<ApiResult<DashboardSummary>> GetDashboardSummaryAsync(CancellationToken cancellationToken)
    {
        return await GetAsync<DashboardSummary>("api/dashboard/summary", cancellationToken);
    }

    public async Task<ApiResult<IReadOnlyList<ServerSummary>>> GetServersAsync(CancellationToken cancellationToken)
    {
        return await GetAsync<IReadOnlyList<ServerSummary>>("api/servers", cancellationToken);
    }

    public async Task<ApiResult<ServerDetail>> GetServerAsync(Guid serverId, CancellationToken cancellationToken)
    {
        return await GetAsync<ServerDetail>($"api/servers/{serverId}", cancellationToken);
    }

    public async Task<ApiResult<PagedResult<MetricReading>>> GetServerMetricsAsync(Guid serverId, DateTimeOffset? since, DateTimeOffset? until, int page, int pageSize, CancellationToken cancellationToken)
    {
        var filtros = new (string, string?)[]
        {
            ("since", since?.ToString("O", CultureInfo.InvariantCulture)),
            ("until", until?.ToString("O", CultureInfo.InvariantCulture)),
            ("page", Numero(page)),
            ("pageSize", Numero(pageSize))
        };

        return await GetAsync<PagedResult<MetricReading>>($"api/servers/{serverId}/metrics{QueryString(filtros)}", cancellationToken);
    }

    public async Task<ApiResult<ServerSummary>> DisableServerAsync(Guid serverId, CancellationToken cancellationToken)
    {
        return await PostAsync<ServerSummary>($"api/servers/{serverId}/disable", cancellationToken);
    }

    public async Task<ApiResult<ServerSummary>> EnableServerAsync(Guid serverId, CancellationToken cancellationToken)
    {
        return await PostAsync<ServerSummary>($"api/servers/{serverId}/enable", cancellationToken);
    }

    public async Task<ApiResult<IReadOnlyList<ServiceSummary>>> GetServerServicesAsync(Guid serverId, string? filter, CancellationToken cancellationToken)
    {
        return await GetAsync<IReadOnlyList<ServiceSummary>>($"api/servers/{serverId}/services{QueryString([("filter", filter)])}", cancellationToken);
    }

    // El identificador del servicio va como un solo segmento de ruta: en Linux son nombres como
    // «nginx.service», con puntos, así que se escapa el valor entero.
    public async Task<ApiResult<ServiceDetail>> GetServerServiceAsync(Guid serverId, string serviceId, CancellationToken cancellationToken)
    {
        return await GetAsync<ServiceDetail>($"api/servers/{serverId}/services/{Uri.EscapeDataString(serviceId)}", cancellationToken);
    }

    public async Task<ApiResult<ServiceLogPage>> GetServerServiceLogsAsync(Guid serverId, string serviceId, int maxLines, DateTimeOffset? since, CancellationToken cancellationToken)
    {
        var filtros = new (string, string?)[]
        {
            ("maxLines", Numero(maxLines)),
            ("since", since?.ToString("O", CultureInfo.InvariantCulture))
        };

        return await GetAsync<ServiceLogPage>($"api/servers/{serverId}/services/{Uri.EscapeDataString(serviceId)}/logs{QueryString(filtros)}", cancellationToken);
    }

    public async Task<ApiResult<ServiceDetail>> ControlServerServiceAsync(Guid serverId, string serviceId, string action, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/servers/{serverId}/services/{Uri.EscapeDataString(serviceId)}/control")
        {
            Content = JsonContent.Create(new ControlServiceRequest(action), options: JsonOptions)
        };

        return await SendAsync<ServiceDetail>(message, authenticated: true, cancellationToken);
    }

    public async Task<ApiResult<PagedResult<ScriptListItem>>> GetScriptsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var ruta = $"api/scripts{QueryString([("search", search), ("page", Numero(page)), ("pageSize", Numero(pageSize))])}";
        return await GetAsync<PagedResult<ScriptListItem>>(ruta, cancellationToken);
    }

    public async Task<ApiResult<ScriptDetail>> GetScriptAsync(Guid scriptId, CancellationToken cancellationToken)
    {
        return await GetAsync<ScriptDetail>($"api/scripts/{scriptId}", cancellationToken);
    }

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

    public async Task<ApiResult<IReadOnlyList<EnrollmentTokenSummary>>> GetEnrollmentTokensAsync(CancellationToken cancellationToken)
    {
        return await GetAsync<IReadOnlyList<EnrollmentTokenSummary>>("api/agents/enrollment-tokens", cancellationToken);
    }

    public async Task<ApiResult<bool>> RevokeEnrollmentTokenAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Delete, $"api/agents/enrollment-tokens/{tokenId}");
        var token = await tokens.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
            return ApiResult<bool>.Failure("No hay sesión iniciada.");

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await http.SendAsync(message, cancellationToken);

            // La revocación responde 204 sin cuerpo, así que no hay nada que deserializar.
            if (response.IsSuccessStatusCode)
                return ApiResult<bool>.Success(true);

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

        return await GetAsync<PagedResult<ScriptExecutionListItem>>($"api/executions{QueryString(filtros)}", cancellationToken);
    }

    public async Task<ApiResult<ScriptExecutionDetail>> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken)
    {
        return await GetAsync<ScriptExecutionDetail>($"api/executions/{executionId}", cancellationToken);
    }

    public async Task<ApiResult<IReadOnlyList<ServerGroupSummary>>> GetServerGroupsAsync(CancellationToken cancellationToken)
    {
        return await GetAsync<IReadOnlyList<ServerGroupSummary>>("api/server-groups", cancellationToken);
    }

    public async Task<ApiResult<ServerGroupDetail>> GetServerGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await GetAsync<ServerGroupDetail>($"api/server-groups/{groupId}", cancellationToken);
    }

    public async Task<ApiResult<ServerGroupDetail>> CreateServerGroupAsync(CreateServerGroupRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/server-groups")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        return await SendAsync<ServerGroupDetail>(message, authenticated: true, cancellationToken);
    }

    public async Task<ApiResult<ServerGroupDetail>> UpdateServerGroupAsync(Guid groupId, UpdateServerGroupRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, $"api/server-groups/{groupId}")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        return await SendAsync<ServerGroupDetail>(message, authenticated: true, cancellationToken);
    }

    public async Task<ApiResult<bool>> DeleteServerGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Delete, $"api/server-groups/{groupId}");
        var token = await tokens.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
            return ApiResult<bool>.Failure("No hay sesión iniciada.");

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await http.SendAsync(message, cancellationToken);

            // El borrado responde 204 sin cuerpo, así que no hay nada que deserializar.
            if (response.IsSuccessStatusCode)
                return ApiResult<bool>.Success(true);

            var (mensaje, codigo) = await DescribeAsync(response, cancellationToken);
            return ApiResult<bool>.Failure(mensaje, codigo);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("Falló el borrado del grupo {GroupId}: {Motivo}", groupId, exception.Message);
            return ApiResult<bool>.Failure(UnreachableMessage);
        }
    }

    public async Task<ApiResult<DispatchGroupResult>> DispatchGroupAsync(DispatchGroupRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/executions/batches")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        return await SendAsync<DispatchGroupResult>(message, authenticated: true, cancellationToken);
    }

    public async Task<ApiResult<ExecutionBatchDetail>> GetExecutionBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        return await GetAsync<ExecutionBatchDetail>($"api/executions/batches/{batchId}", cancellationToken);
    }

    public async Task<ApiResult<IReadOnlyList<ScheduledTaskListItem>>> GetScheduledTasksAsync(CancellationToken cancellationToken)
    {
        return await GetAsync<IReadOnlyList<ScheduledTaskListItem>>("api/scheduled-tasks", cancellationToken);
    }

    public async Task<ApiResult<ScheduledTaskSummary>> CreateScheduledTaskAsync(CreateScheduledTaskRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/scheduled-tasks")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        return await SendAsync<ScheduledTaskSummary>(message, authenticated: true, cancellationToken);
    }

    public async Task<ApiResult<ScheduledTaskSummary>> EnableScheduledTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return await PostAsync<ScheduledTaskSummary>($"api/scheduled-tasks/{taskId}/enable", cancellationToken);
    }

    public async Task<ApiResult<ScheduledTaskSummary>> DisableScheduledTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return await PostAsync<ScheduledTaskSummary>($"api/scheduled-tasks/{taskId}/disable", cancellationToken);
    }

    public async Task<ApiResult<ScheduledTaskTriggered>> TriggerScheduledTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return await PostAsync<ScheduledTaskTriggered>($"api/scheduled-tasks/{taskId}/runs", cancellationToken);
    }

    public async Task<ApiResult<ScheduledTaskSummary>> GetScheduledTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return await GetAsync<ScheduledTaskSummary>($"api/scheduled-tasks/{taskId}", cancellationToken);
    }

    public async Task<ApiResult<ScheduledTaskSummary>> UpdateScheduledTaskAsync(Guid taskId, UpdateScheduledTaskRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, $"api/scheduled-tasks/{taskId}")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        return await SendAsync<ScheduledTaskSummary>(message, authenticated: true, cancellationToken);
    }

    public async Task<ApiResult<bool>> DeleteScheduledTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Delete, $"api/scheduled-tasks/{taskId}");
        var token = await tokens.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
            return ApiResult<bool>.Failure("No hay sesión iniciada.");

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await http.SendAsync(message, cancellationToken);

            // El borrado responde 204 sin cuerpo, así que no hay nada que deserializar.
            if (response.IsSuccessStatusCode)
                return ApiResult<bool>.Success(true);

            var (mensaje, codigo) = await DescribeAsync(response, cancellationToken);
            return ApiResult<bool>.Failure(mensaje, codigo);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("Falló el borrado de la tarea programada {TaskId}: {Motivo}", taskId, exception.Message);
            return ApiResult<bool>.Failure(UnreachableMessage);
        }
    }

    public async Task<ApiResult<IReadOnlyList<ScheduledTaskExecutionItem>>> GetScheduledTaskExecutionsAsync(Guid taskId, int take, CancellationToken cancellationToken)
    {
        return await GetAsync<IReadOnlyList<ScheduledTaskExecutionItem>>($"api/scheduled-tasks/{taskId}/executions?take={take}", cancellationToken);
    }

    private async Task<ApiResult<TValue>> GetAsync<TValue>(string path, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, path);
        return await SendAsync<TValue>(message, authenticated: true, cancellationToken);
    }

    private async Task<ApiResult<TValue>> PostAsync<TValue>(string path, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, path);
        return await SendAsync<TValue>(message, authenticated: true, cancellationToken);
    }

    private async Task<ApiResult<TValue>> SendAsync<TValue>(HttpRequestMessage message, bool authenticated, CancellationToken cancellationToken)
    {
        if (authenticated && !await TryAuthorizeAsync(message))
            return ApiResult<TValue>.Failure("No hay sesión iniciada.");

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

    // Adjunta el token de la sesión; devuelve false si no hay sesión.
    private async Task<bool> TryAuthorizeAsync(HttpRequestMessage message)
    {
        var token = await tokens.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
            return false;

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return true;
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

    // Traduce el error de la API a un mensaje legible y, si la API lo nombra en el title del
    // ProblemDetails, devuelve también ese código para que quien llame lo lleve a un campo.
    private static async Task<(string Mensaje, string? Codigo)> DescribeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized)
            return ("La sesión no es válida o expiró.", null);

        if (response.StatusCode is HttpStatusCode.Forbidden)
            return ("La cuenta no tiene permisos para esta operación.", null);

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(JsonOptions, cancellationToken);
            var codigo = string.IsNullOrWhiteSpace(problem?.Title) ? null : problem!.Title;

            if (!string.IsNullOrWhiteSpace(problem?.Detail))
                return (problem.Detail, codigo);

            if (codigo is not null)
                return ($"El orquestador respondió {(int)response.StatusCode}.", codigo);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // La respuesta no era el ProblemDetails esperado; se cae al mensaje genérico.
        }

        return ($"El orquestador respondió {(int)response.StatusCode}.", null);
    }

    private static string QueryString((string Nombre, string? Valor)[] parametros)
    {
        var partes = parametros
            .Where(parametro => !string.IsNullOrWhiteSpace(parametro.Valor))
            .Select(parametro => $"{parametro.Nombre}={Uri.EscapeDataString(parametro.Valor!)}")
            .ToArray();

        return partes.Length == 0 ? string.Empty : $"?{string.Join('&', partes)}";
    }

    private static string Numero(int valor)
    {
        return valor.ToString(CultureInfo.InvariantCulture);
    }
}

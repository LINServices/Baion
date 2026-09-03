using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Baion.Orchestrator.Presentacion.Controllers;

/// <summary>
/// Punto de entrada del canal con los agentes. Las credenciales se validan <b>antes</b> de aceptar el socket,
/// de modo que un agente sin permiso recibe un 401 normal y no llega a establecer la conexión.
/// </summary>
[ApiController]
[Route(BaionProtocol.WebSocketPath)]
public class AgentSocketController(IAgentEnrollmentService enrollmentService, IAgentConnectionHandler connectionHandler, IHostApplicationLifetime lifetime) : ControllerBase
{
    /// <summary>Acepta la conexión WebSocket de un agente tras validar su token de instalación o su credencial.</summary>
    [HttpGet]
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var credentials = new AgentCredentials(ReadHeader(BaionProtocol.EnrollmentTokenHeader), ReadHeader(BaionProtocol.AgentTokenHeader));
        var resolved = await enrollmentService.ResolveCredentialsAsync(credentials, cancellationToken);

        if (resolved is not { IsSuccess: true, Value: AgentCredentialContext context })
        {
            Response.StatusCode = resolved.Error?.Kind is ErrorKind.Forbidden ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized;
            return;
        }

        // La parada del host tiene que cerrar el bucle: RequestAborted solo salta si es el agente quien se va.
        using var connectionToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.ApplicationStopping);
        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        await connectionHandler.HandleAsync(socket, context, connectionToken.Token);
    }

    private string? ReadHeader(string name) => Request.Headers.TryGetValue(name, out var values) ? values.ToString() : null;
}

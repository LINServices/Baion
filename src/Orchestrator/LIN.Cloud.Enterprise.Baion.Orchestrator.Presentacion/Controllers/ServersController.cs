using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Constants;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/servers")]
[Authorize]
public class ServersController(IServerService servers) : ControllerBase
{
    /// <summary>
    /// Lista los servidores gestionados del tenant actual.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        return Ok(await servers.GetAllAsync(cancellationToken));
    }

    /// <summary>
    /// Obtiene un servidor con su última lectura de métricas.
    /// </summary>
    [HttpGet("{serverId:guid}")]
    public async Task<IActionResult> GetAsync(Guid serverId, CancellationToken cancellationToken)
    {
        return (await servers.GetAsync(serverId, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Desactiva el servidor y corta la conexión de su agente en el acto.
    /// </summary>
    [HttpPost("{serverId:guid}/disable")]
    [Authorize(Roles = AuthorizationRoles.Admin)]
    public async Task<IActionResult> DisableAsync(Guid serverId, CancellationToken cancellationToken)
    {
        return (await servers.DisableAsync(serverId, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Reactiva el servidor para que su agente pueda volver a conectarse.
    /// </summary>
    [HttpPost("{serverId:guid}/enable")]
    [Authorize(Roles = AuthorizationRoles.Admin)]
    public async Task<IActionResult> EnableAsync(Guid serverId, CancellationToken cancellationToken)
    {
        return (await servers.EnableAsync(serverId, cancellationToken)).ToActionResult();
    }
}

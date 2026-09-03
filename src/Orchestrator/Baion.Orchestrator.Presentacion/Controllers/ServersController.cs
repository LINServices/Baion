using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/servers")]
[Authorize]
public class ServersController(IServerService servers) : ControllerBase
{
    /// <summary>Lista los servidores gestionados del tenant actual.</summary>
    [HttpGet]
    public async Task<IActionResult> ListarAsync(CancellationToken cancellationToken) => Ok(await servers.GetAllAsync(cancellationToken));

    /// <summary>Obtiene un servidor con su última lectura de métricas.</summary>
    [HttpGet("{serverId:guid}")]
    public async Task<IActionResult> ObtenerAsync(Guid serverId, CancellationToken cancellationToken) => (await servers.GetAsync(serverId, cancellationToken)).ToActionResult();

    /// <summary>Desactiva el servidor y corta la conexión de su agente en el acto.</summary>
    [HttpPost("{serverId:guid}/disable")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> DesactivarAsync(Guid serverId, CancellationToken cancellationToken) => (await servers.DisableAsync(serverId, cancellationToken)).ToActionResult();

    /// <summary>Reactiva el servidor para que su agente pueda volver a conectarse.</summary>
    [HttpPost("{serverId:guid}/enable")]
    [Authorize(Roles = AdminRole)]
    public async Task<IActionResult> ReactivarAsync(Guid serverId, CancellationToken cancellationToken) => (await servers.EnableAsync(serverId, cancellationToken)).ToActionResult();

    private const string AdminRole = "Admin";
}

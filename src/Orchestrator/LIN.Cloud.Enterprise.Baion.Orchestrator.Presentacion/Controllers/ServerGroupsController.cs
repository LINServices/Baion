using System;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Constants;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/server-groups")]
[Authorize]
public class ServerGroupsController(IServerGroupService groups) : ControllerBase
{
    /// <summary>
    /// Lista los grupos de servidores del tenant actual con el recuento de miembros por plataforma.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        return (await groups.ListAsync(cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Obtiene un grupo con sus miembros.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return (await groups.GetAsync(id, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Crea un grupo de servidores.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = AuthorizationRoles.Admin)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateServerGroupRequest request, CancellationToken cancellationToken)
    {
        return (await groups.CreateAsync(request, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Actualiza un grupo y reemplaza por completo sus miembros.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AuthorizationRoles.Admin)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateServerGroupRequest request, CancellationToken cancellationToken)
    {
        return (await groups.UpdateAsync(id, request, cancellationToken)).ToActionResult();
    }

    /// <summary>
    /// Elimina un grupo sin tocar sus servidores. Falla si una tarea programada lo usa.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AuthorizationRoles.Admin)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return (await groups.DeleteAsync(id, cancellationToken)).ToActionResult();
    }
}

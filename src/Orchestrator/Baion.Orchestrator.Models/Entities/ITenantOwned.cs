using System;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Marca una fila como perteneciente a un tenant; habilita el filtro global y el sellado automático.</summary>
public interface ITenantOwned
{
    /// <summary>Tenant dueño de la fila.</summary>
    Guid TenantId { get; set; }
}

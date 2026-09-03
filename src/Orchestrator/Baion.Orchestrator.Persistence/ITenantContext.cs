using System;

namespace Baion.Orchestrator.Persistence;

/// <summary>Tenant al que pertenece la operación en curso. Es la única fuente del filtro multi-tenant.</summary>
public interface ITenantContext
{
    /// <summary>Tenant activo, o null mientras no se resuelva. Con null el filtro global no devuelve filas.</summary>
    Guid? TenantId { get; }

    /// <summary>Fija el tenant del scope actual. Falla si ya se fijó uno distinto.</summary>
    void SetTenant(Guid tenantId);
}

using System;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;

namespace Baion.Orchestrator.Persistence;

/// <summary>Acceso a la tabla de tenants, la única no sujeta al filtro multi-tenant.</summary>
public interface ITenantRepository
{
    /// <summary>Obtiene un tenant por su identificador.</summary>
    Task<Tenant?> GetByIdAsync(Guid id);

    /// <summary>Obtiene un tenant por su slug.</summary>
    Task<Tenant?> GetBySlugAsync(string slug);

    /// <summary>Registra un tenant nuevo.</summary>
    Task AddAsync(Tenant tenant);
}

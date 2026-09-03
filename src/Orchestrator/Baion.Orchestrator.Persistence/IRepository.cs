using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;

namespace Baion.Orchestrator.Persistence;

/// <summary>Acceso genérico a una entidad, siempre acotado al tenant del scope actual.</summary>
public interface IRepository<TEntity> where TEntity : Entity
{
    /// <summary>Obtiene una entidad por su identificador dentro del tenant actual.</summary>
    Task<TEntity?> GetByIdAsync(Guid id);

    /// <summary>Lista las entidades del tenant actual.</summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Marca una entidad para inserción; el tenant se sella al guardar.</summary>
    Task AddAsync(TEntity entity);

    /// <summary>Marca una entidad como modificada.</summary>
    void Update(TEntity entity);

    /// <summary>Marca una entidad para eliminación.</summary>
    void Remove(TEntity entity);
}

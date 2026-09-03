using System;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Base de las entidades con identificador propio y marcas de auditoría.</summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

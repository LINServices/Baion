using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

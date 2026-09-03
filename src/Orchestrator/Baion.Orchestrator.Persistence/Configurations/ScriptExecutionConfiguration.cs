using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class ScriptExecutionConfiguration : TenantEntityConfiguration<ScriptExecution>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ScriptExecution> builder)
    {
        builder.Ignore(execution => execution.IsFinished);
        builder.Property(execution => execution.ErrorMessage).HasMaxLength(2000);

        builder.HasOne(execution => execution.Server).WithMany(server => server.Executions).HasForeignKey(execution => execution.ServerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(execution => execution.Script).WithMany(script => script.Executions).HasForeignKey(execution => execution.ScriptId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(execution => execution.ScriptChainStep).WithMany().HasForeignKey(execution => execution.ScriptChainStepId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(execution => execution.ScheduledTask).WithMany().HasForeignKey(execution => execution.ScheduledTaskId).OnDelete(DeleteBehavior.NoAction);

        // Barrido de entregas pendientes: solo interesan las que llevan plazo de entrega.
        builder.HasIndex(execution => new { execution.Status, execution.DispatchDeadline }).HasFilter("[dispatch_deadline] IS NOT NULL");

        builder.HasIndex(execution => new { execution.TenantId, execution.ServerId, execution.QueuedAt });
        builder.HasIndex(execution => new { execution.TenantId, execution.Status });
        builder.HasIndex(execution => execution.ChainRunId).HasFilter("[chain_run_id] IS NOT NULL");

        // Un recorrido no puede tener dos ejecuciones del mismo paso: es lo que hace idempotente el avance
        // de la cadena si el desenlace de un paso llegara a procesarse dos veces.
        builder.HasIndex(execution => new { execution.ChainRunId, execution.ScriptChainStepId }).IsUnique().HasFilter("[chain_run_id] IS NOT NULL AND [script_chain_step_id] IS NOT NULL");
    }
}

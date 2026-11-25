using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Configurations;

internal class ExecutionBatchConfiguration : TenantEntityConfiguration<ExecutionBatch>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ExecutionBatch> builder)
    {
        builder.Property(batch => batch.ServerGroupName).IsRequired().HasMaxLength(200);
        builder.Property(batch => batch.ScriptName).IsRequired().HasMaxLength(200);
        builder.Property(batch => batch.RequestedBy).HasMaxLength(320);

        // Si el grupo se elimina, el lote sobrevive con el nombre que guardó al lanzarse.
        builder.HasOne(batch => batch.ServerGroup).WithMany().HasForeignKey(batch => batch.ServerGroupId).OnDelete(DeleteBehavior.SetNull);

        // Sin cascada: borrar un script no debe arrastrar el historial de sus lotes.
        builder.HasOne(batch => batch.Script).WithMany().HasForeignKey(batch => batch.ScriptId).OnDelete(DeleteBehavior.NoAction);

        // Sin cascada: las ejecuciones ya cuelgan en cascada del servidor, y un segundo camino lo rechaza SQL Server.
        builder.HasMany(batch => batch.Executions).WithOne(execution => execution.Batch).HasForeignKey(execution => execution.BatchId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(batch => new { batch.TenantId, batch.CreatedAt });
    }
}

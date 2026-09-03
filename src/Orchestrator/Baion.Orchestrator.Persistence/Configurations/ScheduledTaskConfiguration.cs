using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class ScheduledTaskConfiguration : TenantEntityConfiguration<ScheduledTask>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ScheduledTask> builder)
    {
        builder.Property(task => task.Name).IsRequired().HasMaxLength(200);
        builder.Property(task => task.CronExpression).IsRequired().HasMaxLength(100);
        builder.Property(task => task.TimeZoneId).IsRequired().HasMaxLength(100);

        builder.HasOne(task => task.Script).WithMany().HasForeignKey(task => task.ScriptId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(task => task.ScriptChain).WithMany().HasForeignKey(task => task.ScriptChainId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(task => task.Server).WithMany().HasForeignKey(task => task.ServerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(task => task.ServerGroup).WithMany().HasForeignKey(task => task.ServerGroupId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(task => new { task.TenantId, task.Name }).IsUnique();
        // Es el índice del barrido del planificador: tareas activas cuyo próximo disparo ya venció.
        builder.HasIndex(task => new { task.IsEnabled, task.NextRunAt });

        // El destino y la carga son excluyentes: exactamente una columna de cada par lleva valor.
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_scheduled_tasks_target", "(CASE WHEN [server_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [server_group_id] IS NULL THEN 0 ELSE 1 END) = 1");
            table.HasCheckConstraint("ck_scheduled_tasks_payload", "(CASE WHEN [script_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [script_chain_id] IS NULL THEN 0 ELSE 1 END) = 1");
        });
    }
}

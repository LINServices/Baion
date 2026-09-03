using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class ScriptChainStepConfiguration : TenantEntityConfiguration<ScriptChainStep>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ScriptChainStep> builder)
    {
        // "order" es palabra reservada en T-SQL; se mapea explícito para no depender del entrecomillado.
        builder.Property(step => step.Order).HasColumnName("step_order");

        builder.HasOne(step => step.Script).WithMany().HasForeignKey(step => step.ScriptId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(step => new { step.ScriptChainId, step.Order }).IsUnique();
    }
}

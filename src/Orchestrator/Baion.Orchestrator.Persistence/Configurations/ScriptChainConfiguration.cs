using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class ScriptChainConfiguration : TenantEntityConfiguration<ScriptChain>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ScriptChain> builder)
    {
        builder.Property(chain => chain.Name).IsRequired().HasMaxLength(200);
        builder.Property(chain => chain.Description).HasMaxLength(1000);

        builder.HasIndex(chain => new { chain.TenantId, chain.Name }).IsUnique();
        builder.HasMany(chain => chain.Steps).WithOne(step => step.ScriptChain).HasForeignKey(step => step.ScriptChainId).OnDelete(DeleteBehavior.Cascade);
    }
}

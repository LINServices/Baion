using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class RoleConfiguration : TenantEntityConfiguration<Role>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Role> builder)
    {
        builder.Property(role => role.Name).IsRequired().HasMaxLength(100);
        builder.Property(role => role.NormalizedName).IsRequired().HasMaxLength(100);
        builder.Property(role => role.Description).HasMaxLength(500);

        builder.HasIndex(role => new { role.TenantId, role.NormalizedName }).IsUnique();
    }
}

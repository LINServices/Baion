using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class ServerGroupConfiguration : TenantEntityConfiguration<ServerGroup>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ServerGroup> builder)
    {
        builder.Property(group => group.Name).IsRequired().HasMaxLength(200);
        builder.Property(group => group.Description).HasMaxLength(1000);

        builder.HasIndex(group => new { group.TenantId, group.Name }).IsUnique();
        builder.HasMany(group => group.Members).WithOne(member => member.ServerGroup).HasForeignKey(member => member.ServerGroupId).OnDelete(DeleteBehavior.Cascade);
    }
}

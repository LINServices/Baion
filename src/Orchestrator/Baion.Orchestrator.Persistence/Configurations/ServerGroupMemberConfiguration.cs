using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class ServerGroupMemberConfiguration : TenantEntityConfiguration<ServerGroupMember>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ServerGroupMember> builder)
    {
        builder.HasOne(member => member.Server).WithMany(server => server.GroupMemberships).HasForeignKey(member => member.ServerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(member => new { member.ServerGroupId, member.ServerId }).IsUnique();
    }
}

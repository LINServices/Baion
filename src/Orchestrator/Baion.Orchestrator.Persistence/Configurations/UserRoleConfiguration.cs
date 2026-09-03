using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class UserRoleConfiguration : TenantEntityConfiguration<UserRole>
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasOne(userRole => userRole.User).WithMany(user => user.UserRoles).HasForeignKey(userRole => userRole.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(userRole => userRole.Role).WithMany(role => role.UserRoles).HasForeignKey(userRole => userRole.RoleId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(userRole => new { userRole.UserId, userRole.RoleId }).IsUnique();
    }
}

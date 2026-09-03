using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class UserConfiguration : TenantEntityConfiguration<User>
{
    protected override void ConfigureEntity(EntityTypeBuilder<User> builder)
    {
        builder.Property(user => user.Email).IsRequired().HasMaxLength(256);
        builder.Property(user => user.NormalizedEmail).IsRequired().HasMaxLength(256);
        builder.Property(user => user.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(user => user.PasswordHash).IsRequired().HasMaxLength(512).IsUnicode(false);

        builder.HasIndex(user => new { user.TenantId, user.NormalizedEmail }).IsUnique();
    }
}

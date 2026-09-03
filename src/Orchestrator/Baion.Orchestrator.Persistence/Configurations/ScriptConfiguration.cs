using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class ScriptConfiguration : TenantEntityConfiguration<Script>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Script> builder)
    {
        builder.Property(script => script.Name).IsRequired().HasMaxLength(200);
        builder.Property(script => script.Description).HasMaxLength(1000);
        builder.Property(script => script.Content).IsRequired();

        // SHA-256 en hexadecimal: siempre 64 caracteres ASCII.
        builder.Property(script => script.Checksum).IsRequired().HasMaxLength(64).IsFixedLength().IsUnicode(false);

        builder.HasIndex(script => new { script.TenantId, script.Name }).IsUnique();
    }
}

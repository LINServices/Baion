using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class EnrollmentTokenConfiguration : TenantEntityConfiguration<EnrollmentToken>
{
    protected override void ConfigureEntity(EntityTypeBuilder<EnrollmentToken> builder)
    {
        builder.Property(token => token.Name).IsRequired().HasMaxLength(200);
        builder.Property(token => token.TokenHash).IsRequired().HasMaxLength(64).IsFixedLength().IsUnicode(false);

        // Único global, no por tenant: el token llega en una cabecera y es lo que resuelve de qué tenant es el agente.
        builder.HasIndex(token => token.TokenHash).IsUnique();
    }
}

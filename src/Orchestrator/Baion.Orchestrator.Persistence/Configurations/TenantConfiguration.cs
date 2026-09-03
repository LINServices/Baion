using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Id).ValueGeneratedNever();
        builder.Property(tenant => tenant.Name).IsRequired().HasMaxLength(200);
        builder.Property(tenant => tenant.Slug).IsRequired().HasMaxLength(100);
        builder.Property(tenant => tenant.ExternalTenantId).HasMaxLength(200);

        builder.HasIndex(tenant => tenant.Slug).IsUnique();
        builder.HasIndex(tenant => tenant.ExternalTenantId).HasFilter("[external_tenant_id] IS NOT NULL");
    }
}

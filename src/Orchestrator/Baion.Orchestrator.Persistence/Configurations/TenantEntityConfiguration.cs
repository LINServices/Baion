using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

/// <summary>Configuración común a toda entidad aislada por tenant: clave, y FK al tenant sin cascada.</summary>
internal abstract class TenantEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : TenantEntity
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();

        // Sin cascada: borrar un tenant es una operación deliberada, no un efecto colateral,
        // y además evita los múltiples caminos de cascada que SQL Server rechaza.
        builder.HasOne(entity => entity.Tenant).WithMany().HasForeignKey(entity => entity.TenantId).OnDelete(DeleteBehavior.NoAction);

        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}

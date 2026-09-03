using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class MetricConfiguration : IEntityTypeConfiguration<Metric>
{
    public void Configure(EntityTypeBuilder<Metric> builder)
    {
        // Tabla de series temporales: la clave sustituta queda no agrupada y el índice agrupado
        // sigue el orden real de lectura (por servidor y por tiempo).
        builder.HasKey(metric => metric.Id).IsClustered(false);
        builder.Property(metric => metric.Id).ValueGeneratedOnAdd();

        builder.HasOne(metric => metric.Server).WithMany().HasForeignKey(metric => metric.ServerId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(metric => new { metric.ServerId, metric.CapturedAt }).IsClustered();
        builder.HasIndex(metric => new { metric.TenantId, metric.CapturedAt });

        // Los volúmenes se leen siempre junto a su muestra, nunca por separado: van como JSON
        // en la misma fila para no duplicar el volumen de inserciones ni partir la tabla.
        builder.OwnsMany(metric => metric.Disks, disks =>
        {
            disks.ToJson("disks");
            disks.Property(disk => disk.Name).HasJsonPropertyName("name");
            disks.Property(disk => disk.MountPoint).HasJsonPropertyName("mountPoint");
            disks.Property(disk => disk.TotalBytes).HasJsonPropertyName("totalBytes");
            disks.Property(disk => disk.AvailableBytes).HasJsonPropertyName("availableBytes");
        });
    }
}

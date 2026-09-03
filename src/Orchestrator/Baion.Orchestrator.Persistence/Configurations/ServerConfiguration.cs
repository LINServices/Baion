using Baion.Orchestrator.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baion.Orchestrator.Persistence.Configurations;

internal class ServerConfiguration : TenantEntityConfiguration<Server>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Server> builder)
    {
        builder.Property(server => server.Name).IsRequired().HasMaxLength(200);
        builder.Property(server => server.Hostname).IsRequired().HasMaxLength(255);
        builder.Property(server => server.AgentVersion).HasMaxLength(50);
        builder.Property(server => server.RuntimeIdentifier).HasMaxLength(50);
        builder.Property(server => server.OrchestratorInstanceId).HasMaxLength(100);

        builder.Property(server => server.MachineId).IsRequired().HasMaxLength(128);
        builder.Property(server => server.AgentTokenHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);

        builder.HasIndex(server => new { server.TenantId, server.Name }).IsUnique();
        builder.HasIndex(server => new { server.TenantId, server.MachineId }).IsUnique();

        // Único global: la credencial llega en una cabecera y resuelve por sí sola tenant y servidor.
        builder.HasIndex(server => server.AgentTokenHash).IsUnique().HasFilter("[agent_token_hash] IS NOT NULL");
        builder.HasIndex(server => new { server.TenantId, server.Status });

        // Permite a una instancia recuperar sus agentes tras un reinicio sin escanear la tabla.
        builder.HasIndex(server => server.OrchestratorInstanceId).HasFilter("[orchestrator_instance_id] IS NOT NULL");
    }
}

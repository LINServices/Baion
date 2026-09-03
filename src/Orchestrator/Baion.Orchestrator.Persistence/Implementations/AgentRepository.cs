using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Baion.Orchestrator.Persistence.Implementations;

internal class AgentRepository(BaionDbContext context) : IAgentRepository
{
    public async Task<EnrollmentToken?> FindEnrollmentTokenAsync(string tokenHash, CancellationToken cancellationToken) => await context.EnrollmentTokens
        .IgnoreQueryFilters()
        .Include(token => token.Tenant)
        .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<EnrollmentToken?> FindEnrollmentTokenByIdAsync(Guid tokenId, CancellationToken cancellationToken) => await context.EnrollmentTokens
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(token => token.Id == tokenId, cancellationToken);

    public async Task<Server?> FindByAgentTokenAsync(string tokenHash, CancellationToken cancellationToken) => await context.Servers
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(server => server.AgentTokenHash == tokenHash, cancellationToken);

    public async Task<Server?> FindByMachineIdAsync(Guid tenantId, string machineId, CancellationToken cancellationToken) => await context.Servers
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(server => server.TenantId == tenantId && server.MachineId == machineId, cancellationToken);

    public async Task<Server?> FindByIdAsync(Guid tenantId, Guid serverId, CancellationToken cancellationToken) => await context.Servers
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(server => server.TenantId == tenantId && server.Id == serverId, cancellationToken);

    public async Task<bool> NameExistsAsync(Guid tenantId, string name, CancellationToken cancellationToken) => await context.Servers
        .IgnoreQueryFilters()
        .AnyAsync(server => server.TenantId == tenantId && server.Name == name, cancellationToken);

    public async Task AddAsync(Server server) => await context.Servers.AddAsync(server);

    public async Task MarkDisconnectedAsync(Guid tenantId, Guid serverId, DateTimeOffset lastSeenAt, CancellationToken cancellationToken) => await context.Servers
        .IgnoreQueryFilters()
        .Where(server => server.TenantId == tenantId && server.Id == serverId)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(server => server.Status, server => server.Status == ServerStatus.Disabled ? ServerStatus.Disabled : ServerStatus.Offline)
            .SetProperty(server => server.OrchestratorInstanceId, (string?)null)
            .SetProperty(server => server.ConnectedAt, (DateTimeOffset?)null)
            .SetProperty(server => server.LastSeenAt, lastSeenAt), cancellationToken);

    // ExecuteUpdate no pasa por el change tracker ni por los interceptores, que es lo que se quiere:
    // esta operación es de instancia, no de tenant, y no hay ninguno resuelto al arrancar.
    public async Task<int> ReleaseInstanceServersAsync(string orchestratorInstanceId, CancellationToken cancellationToken) => await context.Servers
        .IgnoreQueryFilters()
        .Where(server => server.OrchestratorInstanceId == orchestratorInstanceId)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(server => server.Status, server => server.Status == ServerStatus.Disabled ? ServerStatus.Disabled : ServerStatus.Offline)
            .SetProperty(server => server.OrchestratorInstanceId, (string?)null)
            .SetProperty(server => server.ConnectedAt, (DateTimeOffset?)null), cancellationToken);
}

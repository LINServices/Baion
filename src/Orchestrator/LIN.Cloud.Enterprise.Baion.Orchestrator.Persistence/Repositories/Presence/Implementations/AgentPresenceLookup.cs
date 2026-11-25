using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Messaging;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Implementations;

// El registro de presencia distribuido son las columnas que el propio handshake ya mantiene en
// servers: qué instancia tiene el socket y cuándo se supo de él por última vez. No hace falta
// un almacén aparte, y la frescura de last_seen_at hace de TTL: si una instancia se cayó sin
// limpiar, su marca envejece y el servidor deja de considerarse alcanzable.
internal class AgentPresenceLookup(BaionDbContext context, IOptions<PresenceOptions> options, TimeProvider timeProvider) : IAgentPresenceLookup
{
    public async Task<string?> ResolveInstanceAsync(Guid serverId, CancellationToken cancellationToken)
    {
        var limite = timeProvider.GetUtcNow().AddSeconds(-Math.Max(options.Value.TimeToLiveSeconds, 1));

        return await context.Servers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(server => server.Id == serverId && server.Status == ServerStatus.Online && server.LastSeenAt >= limite)
            .Select(server => server.OrchestratorInstanceId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

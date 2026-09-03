using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Messaging;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Persistence.Implementations;

/// <summary>
/// El registro de presencia distribuido son las columnas que el propio handshake ya mantiene en
/// <c>servers</c>: qué instancia tiene el socket y cuándo se supo de él por última vez. No hace falta
/// un almacén aparte, y la frescura de <c>last_seen_at</c> hace de TTL: si una instancia se cayó sin
/// limpiar, su marca envejece y el servidor deja de considerarse alcanzable.
/// </summary>
internal class AgentPresenceLookup(BaionDbContext context, IOptions<PresenceOptions> options, TimeProvider timeProvider) : IAgentPresenceLookup
{
    public async Task<string?> ResolveInstanceAsync(Guid serverId, CancellationToken cancellationToken)
    {
        var limite = timeProvider.GetUtcNow().AddSeconds(-Math.Max(options.Value.TimeToLiveSeconds, 1));

        return await context.Servers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(server => server.Id == serverId && server.Status == ServerStatus.Online && server.OrchestratorInstanceId != null && server.LastSeenAt >= limite)
            .Select(server => server.OrchestratorInstanceId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

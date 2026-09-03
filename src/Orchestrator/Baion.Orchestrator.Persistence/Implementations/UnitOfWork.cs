using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Persistence.Context;

namespace Baion.Orchestrator.Persistence.Implementations;

internal class UnitOfWork(BaionDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken) => await context.SaveChangesAsync(cancellationToken);
}

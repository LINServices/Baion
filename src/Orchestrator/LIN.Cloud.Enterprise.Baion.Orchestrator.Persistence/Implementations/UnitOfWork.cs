using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Implementations;

internal class UnitOfWork(BaionDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }
}

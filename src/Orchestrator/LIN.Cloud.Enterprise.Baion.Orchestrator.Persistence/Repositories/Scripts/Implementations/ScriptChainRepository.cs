using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Implementations;

internal class ScriptChainRepository(BaionDbContext context) : IScriptChainRepository
{
    public async Task<ScriptChain?> GetWithStepsAsync(Guid chainId, CancellationToken cancellationToken)
    {
        return await context.ScriptChains
            .Include(chain => chain.Steps.OrderBy(step => step.Order))
            .ThenInclude(step => step.Script)
            .FirstOrDefaultAsync(chain => chain.Id == chainId, cancellationToken);
    }

    public async Task<ScriptChainStep?> GetStepAsync(Guid stepId, CancellationToken cancellationToken)
    {
        return await context.ScriptChainSteps
            .Include(step => step.ScriptChain)
            .ThenInclude(chain => chain.Steps.OrderBy(candidate => candidate.Order))
            .ThenInclude(step => step.Script)
            .FirstOrDefaultAsync(step => step.Id == stepId, cancellationToken);
    }

    public async Task AddAsync(ScriptChain chain)
    {
        await context.ScriptChains.AddAsync(chain);
    }
}

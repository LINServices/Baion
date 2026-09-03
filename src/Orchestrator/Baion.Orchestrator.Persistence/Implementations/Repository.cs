using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Baion.Orchestrator.Persistence.Implementations;

internal class Repository<TEntity>(BaionDbContext context) : IRepository<TEntity> where TEntity : Entity
{
    // FirstOrDefaultAsync y no FindAsync: Find puede resolver desde el caché local y saltarse el filtro de tenant.
    public async Task<TEntity?> GetByIdAsync(Guid id) => await context.Set<TEntity>().FirstOrDefaultAsync(entity => entity.Id == id);

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken) => await context.Set<TEntity>().ToListAsync(cancellationToken);

    public async Task AddAsync(TEntity entity) => await context.Set<TEntity>().AddAsync(entity);

    public void Update(TEntity entity) => context.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => context.Set<TEntity>().Remove(entity);
}

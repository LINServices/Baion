using System;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Baion.Orchestrator.Persistence.Implementations;

internal class TenantRepository(BaionDbContext context) : ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(Guid id) => await context.Tenants.FirstOrDefaultAsync(tenant => tenant.Id == id);

    public async Task<Tenant?> GetBySlugAsync(string slug) => await context.Tenants.FirstOrDefaultAsync(tenant => tenant.Slug == slug);

    public async Task AddAsync(Tenant tenant) => await context.Tenants.AddAsync(tenant);
}

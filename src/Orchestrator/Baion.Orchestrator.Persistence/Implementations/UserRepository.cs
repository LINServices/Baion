using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Baion.Orchestrator.Persistence.Implementations;

internal class UserRepository(BaionDbContext context) : IUserRepository
{
    public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => await context.Users
        .Include(user => user.UserRoles)
        .ThenInclude(userRole => userRole.Role)
        .FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail) => await context.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail);

    public async Task<IReadOnlyList<Role>> GetRolesByNormalizedNamesAsync(IReadOnlyCollection<string> normalizedNames, CancellationToken cancellationToken) => await context.Roles
        .Where(role => normalizedNames.Contains(role.NormalizedName))
        .ToListAsync(cancellationToken);

    public async Task AddAsync(User user) => await context.Users.AddAsync(user);
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;

namespace Baion.Orchestrator.Persistence;

/// <summary>Acceso a usuarios y roles del tenant actual.</summary>
public interface IUserRepository
{
    /// <summary>Obtiene un usuario por su email normalizado, con sus roles cargados.</summary>
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Indica si el tenant actual ya tiene un usuario con ese email normalizado.</summary>
    Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail);

    /// <summary>Obtiene los roles del tenant actual cuyos nombres normalizados estén en la lista.</summary>
    Task<IReadOnlyList<Role>> GetRolesByNormalizedNamesAsync(IReadOnlyCollection<string> normalizedNames, CancellationToken cancellationToken);

    /// <summary>Marca un usuario para inserción.</summary>
    Task AddAsync(User user);
}

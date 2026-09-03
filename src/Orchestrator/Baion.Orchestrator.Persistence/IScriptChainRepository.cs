using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Entities;

namespace Baion.Orchestrator.Persistence;

/// <summary>Acceso a las cadenas de scripts del tenant actual.</summary>
public interface IScriptChainRepository
{
    /// <summary>Obtiene una cadena con sus pasos ordenados y el script de cada uno.</summary>
    Task<ScriptChain?> GetWithStepsAsync(Guid chainId, CancellationToken cancellationToken);

    /// <summary>Obtiene un paso con su cadena y su script.</summary>
    Task<ScriptChainStep?> GetStepAsync(Guid stepId, CancellationToken cancellationToken);

    /// <summary>Marca una cadena para inserción, junto con sus pasos.</summary>
    Task AddAsync(ScriptChain chain);
}

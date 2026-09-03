using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Services;

/// <summary>Emisión y revocación de tokens de instalación del tenant actual.</summary>
public interface IEnrollmentTokenService
{
    /// <summary>Emite un token de instalación. El valor en claro se devuelve una única vez.</summary>
    Task<Result<EnrollmentTokenIssued>> CreateAsync(CreateEnrollmentTokenRequest request, CancellationToken cancellationToken);

    /// <summary>Lista los tokens de instalación del tenant, del más reciente al más antiguo.</summary>
    Task<IReadOnlyList<EnrollmentTokenSummary>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Revoca un token de instalación para que no admita más enrolamientos.</summary>
    Task<Result> RevokeAsync(Guid tokenId, CancellationToken cancellationToken);
}

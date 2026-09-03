using System;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Results;

namespace Baion.Orchestrator.Identity;

/// <summary>Emite y valida los tokens de acceso de Baion, sea cual sea el proveedor que verificó las credenciales.</summary>
public interface ITokenService
{
    /// <summary>Emite el token de acceso para una identidad ya verificada.</summary>
    AccessToken Issue(Guid tenantId, AuthenticatedUser user);

    /// <summary>Valida firma, emisor, audiencia y vigencia de un token emitido por Baion.</summary>
    Task<Result<BaionPrincipal>> ValidateAsync(string token);
}

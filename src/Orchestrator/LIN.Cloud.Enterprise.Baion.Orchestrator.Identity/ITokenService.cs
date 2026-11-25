using System;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Results;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;

/// <summary>
/// Emite y valida los tokens de acceso de Baion, sea cual sea el proveedor que verificó las credenciales.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Emite el token de acceso para una identidad ya verificada.
    /// </summary>
    AccessToken Issue(Guid tenantId, AuthenticatedUser user);

    /// <summary>
    /// Valida firma, emisor, audiencia y vigencia de un token emitido por Baion.
    /// </summary>
    Task<Result<BaionPrincipal>> ValidateAsync(string token);
}

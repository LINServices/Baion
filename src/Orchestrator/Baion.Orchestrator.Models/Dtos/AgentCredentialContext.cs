using System;

namespace Baion.Orchestrator.Models.Dtos;

/// <summary>Resultado de validar las credenciales del agente, antes de aceptar el socket.</summary>
/// <param name="TenantId">Tenant al que pertenece quien se conecta.</param>
/// <param name="ServerId">Servidor ya enrolado, o null si esta conexión es un enrolamiento inicial.</param>
/// <param name="EnrollmentTokenId">Token de instalación usado, cuando se trata de un enrolamiento.</param>
public record AgentCredentialContext(Guid TenantId, Guid? ServerId, Guid? EnrollmentTokenId);

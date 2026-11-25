using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Resultado de validar las credenciales del agente, antes de aceptar el socket.
// TenantId: Tenant al que pertenece quien se conecta.
// ServerId: Servidor ya enrolado, o null si esta conexión es un enrolamiento inicial.
// EnrollmentTokenId: Token de instalación usado, cuando se trata de un enrolamiento.
public record AgentCredentialContext(Guid TenantId, Guid? ServerId, Guid? EnrollmentTokenId);

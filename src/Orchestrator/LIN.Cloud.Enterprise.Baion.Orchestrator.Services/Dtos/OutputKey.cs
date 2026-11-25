using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

// Clave de acumulación de salida: el tenant forma parte de ella porque decide el scope de escritura.
internal readonly record struct OutputKey(Guid TenantId, Guid ExecutionId, OutputStream Stream);

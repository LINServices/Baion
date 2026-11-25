using System;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record StartChainRequest(Guid ScriptChainId, Guid ServerId);

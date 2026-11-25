using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

public record CreateScriptChainRequest(string Name, string? Description, IReadOnlyList<CreateScriptChainStepRequest> Steps);

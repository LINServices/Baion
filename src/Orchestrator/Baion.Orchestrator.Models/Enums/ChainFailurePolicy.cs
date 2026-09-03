namespace Baion.Orchestrator.Models.Enums;

/// <summary>Qué hacer con la cadena cuando un paso termina en fallo.</summary>
public enum ChainFailurePolicy
{
    StopChain = 1,
    ContinueNext = 2
}

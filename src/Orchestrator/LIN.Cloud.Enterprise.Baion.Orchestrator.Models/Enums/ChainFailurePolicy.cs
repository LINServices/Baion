namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Enums;

// Qué hacer con la cadena cuando un paso termina en fallo.
public enum ChainFailurePolicy
{
    StopChain = 1,
    ContinueNext = 2
}

namespace Baion.Contracts.Messages;

/// <summary>Señal periódica de vida del agente. La caída del socket ya indica desconexión; esto refresca el estado.</summary>
public record HeartbeatMessage(int RunningExecutions) : AgentToServerMessage
{
    public const string TypeDiscriminator = "heartbeat";
}

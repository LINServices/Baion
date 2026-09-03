using System;
using Baion.Contracts.Enums;

namespace Baion.Contracts.Messages;

/// <summary>
/// Fragmento de salida de una ejecución. El output viaja troceado según sale del proceso, de modo que
/// ni el agente ni el orquestador tienen que acumularlo entero en memoria.
/// </summary>
/// <param name="Sequence">Orden dentro del flujo; permite recomponer aunque lleguen reordenados.</param>
public record ScriptOutputChunkMessage(Guid ExecutionId, OutputStream Stream, long Sequence, string Content) : AgentToServerMessage
{
    public const string TypeDiscriminator = "script-output";
}

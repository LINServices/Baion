using System;

namespace Baion.Contracts.Messages;

/// <summary>
/// Marca las respuestas del agente a una petición correlacionada del orquestador. El orquestador casa
/// cada respuesta con su petición por <see cref="RequestId"/>, sin importar de qué tipo concreto sea.
/// </summary>
public interface IAgentQueryReply
{
    /// <summary>Identificador de la petición que originó esta respuesta.</summary>
    Guid RequestId { get; }
}

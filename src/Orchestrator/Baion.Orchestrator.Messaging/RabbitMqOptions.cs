namespace Baion.Orchestrator.Messaging;

/// <summary>Conexión y nombres de los exchanges que usa el orquestador.</summary>
public class RabbitMqOptions
{
    /// <summary>
    /// Con RabbitMQ apagado el orquestador funciona igual, pero solo alcanza a los agentes conectados a
    /// su propio proceso. Es el modo de una sola instancia.
    /// </summary>
    public bool Enabled { get; set; }

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    /// <summary>Exchange de tipo topic por el que viajan los comandos dirigidos a un agente concreto.</summary>
    public string CommandExchange { get; set; } = "baion.agent.commands";

    /// <summary>Exchange de tipo fanout por el que se anuncian los cambios de presencia.</summary>
    public string PresenceExchange { get; set; } = "baion.agent.presence";

    /// <summary>Sección de configuración de la que se enlazan estas opciones.</summary>
    public const string SectionName = "RabbitMq";
}

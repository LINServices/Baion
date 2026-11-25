namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    // Con RabbitMQ apagado el orquestador funciona igual, pero solo alcanza a los agentes conectados a
    // su propio proceso. Es el modo de una sola instancia.
    public bool Enabled { get; set; }

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    // Exchange de tipo topic por el que viajan los comandos dirigidos a un agente concreto.
    public string CommandExchange { get; set; } = "baion.agent.commands";

    // Exchange de tipo fanout por el que se anuncian los cambios de presencia.
    public string PresenceExchange { get; set; } = "baion.agent.presence";
}

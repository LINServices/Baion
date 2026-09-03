using System;
using Baion.Contracts.Enums;

namespace Baion.Orchestrator.Models.Entities;

/// <summary>Disparo por cron de un script o una cadena sobre un servidor o un grupo.</summary>
public class ScheduledTask : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string CronExpression { get; set; } = string.Empty;

    /// <summary>Zona horaria IANA con la que se evalúa <see cref="CronExpression"/>.</summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>Script a ejecutar. Excluyente con <see cref="ScriptChainId"/>.</summary>
    public Guid? ScriptId { get; set; }

    public Script? Script { get; set; }

    /// <summary>Cadena a ejecutar. Excluyente con <see cref="ScriptId"/>.</summary>
    public Guid? ScriptChainId { get; set; }

    public ScriptChain? ScriptChain { get; set; }

    /// <summary>Servidor destino. Excluyente con <see cref="ServerGroupId"/>.</summary>
    public Guid? ServerId { get; set; }

    public Server? Server { get; set; }

    /// <summary>Grupo destino. Excluyente con <see cref="ServerId"/>.</summary>
    public Guid? ServerGroupId { get; set; }

    public ServerGroup? ServerGroup { get; set; }

    public ExecutionMode Mode { get; set; } = ExecutionMode.Attached;

    /// <summary>
    /// Margen que se le da al agente para reconectarse cuando está fuera de línea en el momento del disparo.
    /// Con cero, la ejecución se marca fallida en el acto en lugar de esperar.
    /// </summary>
    public int OfflineGraceSeconds { get; set; } = 300;

    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset? LastRunAt { get; set; }

    public DateTimeOffset? NextRunAt { get; set; }
}

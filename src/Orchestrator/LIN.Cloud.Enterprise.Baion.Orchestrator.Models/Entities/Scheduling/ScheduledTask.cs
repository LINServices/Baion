using System;
using LIN.Cloud.Enterprise.Baion.Contracts.Enums;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Entities;

public class ScheduledTask : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string CronExpression { get; set; } = string.Empty;

    // Zona horaria IANA con la que se evalúa CronExpression.
    public string TimeZoneId { get; set; } = "UTC";

    // Script a ejecutar. Excluyente con ScriptChainId.
    public Guid? ScriptId { get; set; }

    public Script? Script { get; set; }

    // Cadena a ejecutar. Excluyente con ScriptId.
    public Guid? ScriptChainId { get; set; }

    public ScriptChain? ScriptChain { get; set; }

    // Servidor destino. Excluyente con ServerGroupId.
    public Guid? ServerId { get; set; }

    public Server? Server { get; set; }

    // Grupo destino. Excluyente con ServerId.
    public Guid? ServerGroupId { get; set; }

    public ServerGroup? ServerGroup { get; set; }

    public ExecutionMode Mode { get; set; } = ExecutionMode.Attached;

    // Margen que se le da al agente para reconectarse cuando está fuera de línea en el momento del disparo.
    // Con cero, la ejecución se marca fallida en el acto en lugar de esperar.
    public int OfflineGraceSeconds { get; set; } = 300;

    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset? LastRunAt { get; set; }

    public DateTimeOffset? NextRunAt { get; set; }
}

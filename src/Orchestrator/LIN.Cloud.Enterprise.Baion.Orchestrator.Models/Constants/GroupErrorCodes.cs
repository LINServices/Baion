namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Models.Dtos;

// Códigos de error de grupos de servidores y lotes de ejecución; un único lugar para que servicio, pruebas y panel no diverjan.
public static class GroupErrorCodes
{
    public const string NameRequired = "group.name_required";

    public const string NameTooLong = "group.name_too_long";

    public const string DescriptionTooLong = "group.description_too_long";

    public const string NameTaken = "group.name_taken";

    public const string NotFound = "group.not_found";

    public const string ServerNotFound = "group.server_not_found";

    public const string InUseByScheduledTasks = "group.in_use_by_scheduled_tasks";

    public const string BatchGroupEmpty = "batch.group_empty";

    public const string BatchGroupTooLarge = "batch.group_too_large";

    public const string BatchNoCompatibleServers = "batch.no_compatible_servers";

    public const string BatchNotFound = "batch.not_found";

    public const string TimeoutInvalid = "execution.timeout_invalid";

    public const string ScriptNotFound = "script.not_found";

    public const string AgentNotConnected = "agent.not_connected";
}

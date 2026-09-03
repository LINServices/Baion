namespace Baion.Cliente.Web.Components.Shared;

/// <summary>Lectura de los estados de ejecución que llegan de la API como texto camelCase.</summary>
public static class ExecutionStatus
{
    /// <summary>Estados en los que la ejecución todavía puede cambiar por su cuenta.</summary>
    public static bool IsLive(string status) => status is "pending" or "dispatched" or "running";
}

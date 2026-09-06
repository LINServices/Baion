namespace Baion.Cliente.Web.Components.Shared;

/// <summary>
/// Lectura de los estados de un servicio del sistema, que llegan de la API como texto camelCase, y de los
/// códigos de error de las consultas al agente. Se traduce aquí y nunca en el marcado para que un mismo
/// estado se lea igual en la tabla y en la ficha de detalle.
/// </summary>
public static class ServiceStates
{
    /// <summary>Etiqueta legible del estado de ejecución.</summary>
    public static string StateLabel(string state) => state switch
    {
        "running" => "En marcha",
        "stopped" => "Parado",
        "starting" => "Arrancando",
        "stopping" => "Deteniéndose",
        "failed" => "Con fallo",
        _ => "Desconocido"
    };

    /// <summary>Etiqueta legible del modo de arranque configurado.</summary>
    public static string StartupLabel(string startupMode) => startupMode switch
    {
        "automatic" => "Automático",
        "manual" => "Manual",
        "disabled" => "Deshabilitado",
        _ => "Desconocido"
    };

    /// <summary>Estados en los que el servicio todavía puede cambiar por su cuenta.</summary>
    public static bool IsTransitional(string state) => state is "starting" or "stopping";

    /// <summary>
    /// Mensaje para el usuario a partir del código del <c>ProblemDetails</c>. Los errores de transporte con
    /// el agente son los que más se van a ver mientras el servidor no responde, así que se nombran claros.
    /// </summary>
    public static string? FriendlyError(string? code, string? fallback) => code switch
    {
        "agent.not_reachable" => "El agente del servidor no está conectado.",
        "agent.query_timeout" => "El agente no respondió a tiempo; vuelve a intentarlo en unos segundos.",
        "service.not_found" => "El servicio ya no existe en el servidor.",
        "server.disabled" => "El servidor está desactivado y no admite acciones de control.",
        _ => fallback
    };
}

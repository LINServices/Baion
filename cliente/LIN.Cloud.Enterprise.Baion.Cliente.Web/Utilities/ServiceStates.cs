namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Utilities;

public static class ServiceStates
{
    public static string StateLabel(string state)
    {
        return state switch
        {
            "running" => "En marcha",
            "stopped" => "Parado",
            "starting" => "Arrancando",
            "stopping" => "Deteniéndose",
            "failed" => "Con fallo",
            _ => "Desconocido"
        };
    }

    public static string StartupLabel(string startupMode)
    {
        return startupMode switch
        {
            "automatic" => "Automático",
            "manual" => "Manual",
            "disabled" => "Deshabilitado",
            _ => "Desconocido"
        };
    }

    public static bool IsTransitional(string state)
    {
        return state is "starting" or "stopping";
    }

    public static string? FriendlyError(string? code, string? fallback)
    {
        return code switch
        {
            "agent.not_reachable" => "El agente del servidor no está conectado.",
            "agent.query_timeout" => "El agente no respondió a tiempo; vuelve a intentarlo en unos segundos.",
            "service.not_found" => "El servicio ya no existe en el servidor.",
            "server.disabled" => "El servidor está desactivado y no admite acciones de control.",
            _ => fallback
        };
    }
}

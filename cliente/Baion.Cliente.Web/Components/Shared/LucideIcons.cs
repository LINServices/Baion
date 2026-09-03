using System.Collections.Generic;

namespace Baion.Cliente.Web.Components.Shared;

/// <summary>
/// Trazos de los iconos que usa el panel, tomados del set Lucide. Se guardan aquí y no como archivos
/// SVG porque van embebidos en el marcado: así heredan <c>currentColor</c> y el grosor de trazo que
/// fija <see cref="Icon"/>, y no hay una petición por icono.
/// </summary>
/// <remarks>
/// El sistema de diseño admite un solo set por proyecto. Si hace falta un icono nuevo, se copia su
/// contenido de lucide.dev tal cual (viewBox de 24, sin atributos de color ni de grosor).
/// </remarks>
public static class LucideIcons
{
    /// <summary>Contenido del icono, o el de <c>circle-help</c> si el nombre no está en el set.</summary>
    public static string Paths(string nombre) => Set.TryGetValue(nombre, out var trazos) ? trazos : Set["circle-help"];

    private static readonly Dictionary<string, string> Set = new()
    {
        ["layout-dashboard"] = """<rect width="7" height="9" x="3" y="3" rx="1"/><rect width="7" height="5" x="14" y="3" rx="1"/><rect width="7" height="9" x="14" y="12" rx="1"/><rect width="7" height="5" x="3" y="16" rx="1"/>""",
        ["server"] = """<rect width="20" height="8" x="2" y="2" rx="2"/><rect width="20" height="8" x="2" y="14" rx="2"/><path d="M6 6h.01"/><path d="M6 18h.01"/>""",
        ["terminal"] = """<path d="m4 17 6-6-6-6"/><path d="M12 19h8"/>""",
        ["history"] = """<path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"/><path d="M3 3v5h5"/><path d="M12 7v5l4 2"/>""",
        ["activity"] = """<path d="M22 12h-4l-3 9L9 3l-3 9H2"/>""",
        ["arrow-up-right"] = """<path d="M7 7h10v10"/><path d="M7 17 17 7"/>""",
        ["chevron-down"] = """<path d="m6 9 6 6 6-6"/>""",
        ["chevron-left"] = """<path d="m15 18-6-6 6-6"/>""",
        ["chevron-right"] = """<path d="m9 18 6-6-6-6"/>""",
        ["circle-alert"] = """<circle cx="12" cy="12" r="10"/><path d="M12 8v4"/><path d="M12 16h.01"/>""",
        ["circle-check"] = """<circle cx="12" cy="12" r="10"/><path d="m9 12 2 2 4-4"/>""",
        ["circle-help"] = """<circle cx="12" cy="12" r="10"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/><path d="M12 17h.01"/>""",
        ["clock"] = """<circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/>""",
        ["cpu"] = """<rect width="16" height="16" x="4" y="4" rx="2"/><rect width="6" height="6" x="9" y="9" rx="1"/><path d="M15 2v2"/><path d="M15 20v2"/><path d="M2 15h2"/><path d="M2 9h2"/><path d="M20 15h2"/><path d="M20 9h2"/><path d="M9 2v2"/><path d="M9 20v2"/>""",
        ["file-code"] = """<path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z"/><path d="M14 2v4a2 2 0 0 0 2 2h4"/><path d="m10 13-2 2 2 2"/><path d="m14 17 2-2-2-2"/>""",
        ["gauge"] = """<path d="m12 14 4-4"/><path d="M3.34 19a10 10 0 1 1 17.32 0"/>""",
        ["hard-drive"] = """<line x1="22" x2="2" y1="12" y2="12"/><path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"/><line x1="6" x2="6.01" y1="16" y2="16"/><line x1="10" x2="10.01" y1="16" y2="16"/>""",
        ["memory-stick"] = """<path d="M6 19v-3"/><path d="M10 19v-3"/><path d="M14 19v-3"/><path d="M18 19v-3"/><path d="M8 11V9"/><path d="M16 11V9"/><path d="M12 11V9"/><path d="M2 15h20"/><path d="M2 7a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v1.1a2 2 0 0 0 0 3.837V17a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-5.1a2 2 0 0 0 0-3.837Z"/>""",
        ["inbox"] = """<path d="M22 12h-6l-2 3h-4l-2-3H2"/><path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"/>""",
        ["key-round"] = """<path d="M2.59 17.41A2 2 0 0 0 2 18.83V21a1 1 0 0 0 1 1h3a1 1 0 0 0 1-1v-1a1 1 0 0 1 1-1h1a1 1 0 0 0 1-1v-1a1 1 0 0 1 1-1h.17a2 2 0 0 0 1.42-.59l.81-.81a6.5 6.5 0 1 0-4-4z"/><path d="M16.5 7.5h.01"/>""",
        ["log-out"] = """<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><path d="m16 17 5-5-5-5"/><path d="M21 12H9"/>""",
        ["plus"] = """<path d="M5 12h14"/><path d="M12 5v14"/>""",
        ["play"] = """<path d="M6 4.5v15l13-7.5z"/>""",
        ["refresh-cw"] = """<path d="M21 12a9 9 0 0 0-9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"/><path d="M3 3v5h5"/><path d="M3 12a9 9 0 0 0 9 9 9.75 9.75 0 0 0 6.74-2.74L21 16"/><path d="M16 16h5v5"/>""",
        ["search"] = """<circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/>""",
        ["sliders-horizontal"] = """<path d="M21 4h-7"/><path d="M10 4H3"/><path d="M21 12h-9"/><path d="M8 12H3"/><path d="M21 20h-5"/><path d="M12 20H3"/><circle cx="12" cy="4" r="2"/><circle cx="10" cy="12" r="2"/><circle cx="14" cy="20" r="2"/>""",
        ["sparkles"] = """<path d="M9.94 15.5A2 2 0 0 0 8.5 14.06l-6.14-1.58a.5.5 0 0 1 0-.96L8.5 9.94A2 2 0 0 0 9.94 8.5l1.58-6.14a.5.5 0 0 1 .96 0L14.06 8.5A2 2 0 0 0 15.5 9.94l6.14 1.58a.5.5 0 0 1 0 .96L15.5 14.06a2 2 0 0 0-1.44 1.44l-1.58 6.14a.5.5 0 0 1-.96 0z"/><path d="M20 3v4"/><path d="M22 5h-4"/><path d="M4 17v2"/><path d="M5 18H3"/>""",
        ["x"] = """<path d="M18 6 6 18"/><path d="m6 6 12 12"/>"""
    };
}

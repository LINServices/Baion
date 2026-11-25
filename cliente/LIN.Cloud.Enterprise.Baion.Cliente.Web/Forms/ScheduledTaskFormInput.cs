using System.ComponentModel.DataAnnotations;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Forms;

// Se valida en el navegador lo evidente; el resto lo comprueba el orquestador igualmente.
public sealed class ScheduledTaskFormInput
{
    [Required(ErrorMessage = "Indica un nombre.")]
    [StringLength(200, ErrorMessage = "El nombre no puede pasar de 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica una expresión cron.")]
    public string CronExpression { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica una zona horaria.")]
    public string TimeZoneId { get; set; } = "UTC";

    [Required(ErrorMessage = "Elige un script.")]
    public string ScriptId { get; set; } = string.Empty;

    // Destino codificado como «s:guid» (servidor) o «g:guid» (grupo), el valor del InputSelect con optgroup.
    [Required(ErrorMessage = "Elige un destino.")]
    public string Target { get; set; } = string.Empty;

    public string Mode { get; set; } = "attached";

    [Range(0, 86_400, ErrorMessage = "La espera debe estar entre 0 segundos y 24 horas.")]
    public int OfflineGraceSeconds { get; set; }
}

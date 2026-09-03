using System.ComponentModel.DataAnnotations;

namespace Baion.Cliente.Web.Components.Shared;

/// <summary>
/// Campos del formulario de script, compartidos por el alta y la edición. Se valida en el navegador lo
/// evidente; el resto lo comprueba el orquestador igualmente.
/// </summary>
public sealed class ScriptFormEntrada
{
    [Required(ErrorMessage = "Indica un nombre.")]
    [StringLength(200, ErrorMessage = "El nombre no puede pasar de 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción no puede pasar de 1000 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "El script no puede estar vacío.")]
    public string Content { get; set; } = string.Empty;

    public string Runtime { get; set; } = "bash";

    [Range(1, 86_400, ErrorMessage = "El timeout debe estar entre 1 segundo y 24 horas.")]
    public int DefaultTimeoutSeconds { get; set; } = 300;
}

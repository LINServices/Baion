using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Forms;

// Se valida en el navegador lo evidente; el resto lo comprueba el orquestador igualmente.
public sealed class ServerGroupFormInput
{
    [Required(ErrorMessage = "Indica un nombre.")]
    [StringLength(200, ErrorMessage = "El nombre no puede pasar de 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción no puede pasar de 1000 caracteres.")]
    public string? Description { get; set; }

    public HashSet<Guid> ServerIds { get; set; } = [];
}

using System.Threading.Tasks;
using Baion.Cliente.Web.Models;

namespace Baion.Cliente.Web.Services;

/// <summary>Arranca y cierra la sesión del panel, que se persiste en el navegador.</summary>
public interface IBaionSession
{
    /// <summary>Guarda la autenticación recién obtenida y avisa a quien observa el estado de sesión.</summary>
    Task IniciarSesionAsync(AuthenticationResult autenticacion);

    /// <summary>Borra la sesión del navegador y deja al usuario como anónimo.</summary>
    Task CerrarSesionAsync();
}

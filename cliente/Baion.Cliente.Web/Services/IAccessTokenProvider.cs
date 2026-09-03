using System.Threading.Tasks;

namespace Baion.Cliente.Web.Services;

/// <summary>Token de acceso del usuario de la sesión en curso.</summary>
public interface IAccessTokenProvider
{
    /// <summary>Devuelve el token, o null si no hay sesión iniciada.</summary>
    Task<string?> GetTokenAsync();
}

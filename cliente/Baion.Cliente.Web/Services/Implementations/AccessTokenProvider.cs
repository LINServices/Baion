using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Baion.Cliente.Web.Services.Implementations;

/// <summary>
/// Saca el token de los claims del usuario autenticado, que <see cref="BaionAuthenticationStateProvider"/>
/// arma a partir de la sesión guardada en el navegador.
/// </summary>
internal class AccessTokenProvider(AuthenticationStateProvider authenticationStateProvider) : IAccessTokenProvider
{
    public async Task<string?> GetTokenAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.Claims.FirstOrDefault(claim => claim.Type == BaionClaims.AccessToken)?.Value;
    }
}

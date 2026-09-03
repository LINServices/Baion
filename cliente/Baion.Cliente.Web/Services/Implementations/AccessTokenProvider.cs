using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Baion.Cliente.Web.Services.Implementations;

/// <summary>
/// Saca el token de los claims del usuario del circuito. Se lee de ahí y no del <c>HttpContext</c> porque
/// un circuito de Blazor vive mucho más que la petición que lo creó.
/// </summary>
internal class AccessTokenProvider(AuthenticationStateProvider authenticationStateProvider) : IAccessTokenProvider
{
    public async Task<string?> GetTokenAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.Claims.FirstOrDefault(claim => claim.Type == BaionClaims.AccessToken)?.Value;
    }
}

using System.Linq;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Constants;
using Microsoft.AspNetCore.Components.Authorization;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Services.Implementations;

internal class AccessTokenProvider(AuthenticationStateProvider authenticationStateProvider) : IAccessTokenProvider
{
    public async Task<string?> GetTokenAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.Claims.FirstOrDefault(claim => claim.Type == BaionClaims.AccessToken)?.Value;
    }
}

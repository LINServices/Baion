using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Constants;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Services.Implementations;

// La sesión vive en localStorage porque el panel es WebAssembly y no hay servidor donde guardarla.
// Dentro viaja el token de la API, que aquí el navegador sí puede leer. Se restaura al arrancar
// y se descarta sola cuando el token ha caducado.
internal sealed class BaionAuthenticationStateProvider(IJSRuntime js) : AuthenticationStateProvider, IBaionSession
{
    private const string StorageKey = "baion.session";

    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private AuthenticationResult? _session;
    private bool _read;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await GetSessionAsync();

        return session is null
            ? Anonymous
            : new AuthenticationState(new ClaimsPrincipal(BuildIdentity(session)));
    }

    public async Task SignInAsync(AuthenticationResult authentication)
    {
        _session = authentication;
        _read = true;

        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(authentication, JsonOptions));

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(BuildIdentity(authentication)))));
    }

    public async Task SignOutAsync()
    {
        await ClearAsync();

        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private async Task<AuthenticationResult?> GetSessionAsync()
    {
        if (!_read)
        {
            _session = await ReadAsync();
            _read = true;
        }

        // La cookie no sobrevivía al token; aquí tampoco: una sesión caducada no vale de nada.
        if (_session is not null && _session.ExpiresAt <= DateTimeOffset.UtcNow)
            await ClearAsync();

        return _session;
    }

    private async Task<AuthenticationResult?> ReadAsync()
    {
        var raw = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);

        if (string.IsNullOrWhiteSpace(raw))
            return null;

        try
        {
            return JsonSerializer.Deserialize<AuthenticationResult>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task ClearAsync()
    {
        _session = null;
        _read = true;

        await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    private static ClaimsIdentity BuildIdentity(AuthenticationResult authentication)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, authentication.UserId.ToString()),
            new(ClaimTypes.Name, authentication.Email),
            new(BaionClaims.AccessToken, authentication.AccessToken),
            new(BaionClaims.TenantId, authentication.TenantId.ToString()),
            new(BaionClaims.ExpiresAt, authentication.ExpiresAt.ToString("O")),
            .. authentication.Roles.Select(role => new Claim(ClaimTypes.Role, role))
        ];

        return new ClaimsIdentity(claims, authenticationType: "baion");
    }
}

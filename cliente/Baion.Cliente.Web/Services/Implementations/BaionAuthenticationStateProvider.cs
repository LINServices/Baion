using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Baion.Cliente.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Baion.Cliente.Web.Services.Implementations;

/// <summary>
/// La sesión del panel vive en <c>localStorage</c> porque el panel es WebAssembly y no hay servidor donde
/// guardarla. Dentro viaja el token de la API, que aquí el navegador sí puede leer. Se restaura al arrancar
/// y se descarta sola cuando el token ha caducado.
/// </summary>
internal sealed class BaionAuthenticationStateProvider(IJSRuntime js) : AuthenticationStateProvider, IBaionSession
{
    private const string StorageKey = "baion.session";

    private static readonly AuthenticationState Anonimo = new(new ClaimsPrincipal(new ClaimsIdentity()));
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private AuthenticationResult? _sesion;
    private bool _leida;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var sesion = await ObtenerSesionAsync();

        return sesion is null
            ? Anonimo
            : new AuthenticationState(new ClaimsPrincipal(ConstruirIdentidad(sesion)));
    }

    public async Task IniciarSesionAsync(AuthenticationResult autenticacion)
    {
        _sesion = autenticacion;
        _leida = true;

        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(autenticacion, JsonOptions));

        NotifyAuthenticationStateChanged(Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(ConstruirIdentidad(autenticacion)))));
    }

    public async Task CerrarSesionAsync()
    {
        await BorrarAsync();

        NotifyAuthenticationStateChanged(Task.FromResult(Anonimo));
    }

    private async Task<AuthenticationResult?> ObtenerSesionAsync()
    {
        if (!_leida)
        {
            _sesion = await LeerAsync();
            _leida = true;
        }

        if (_sesion is not null && _sesion.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            // La cookie no sobrevivía al token; aquí tampoco: una sesión caducada no vale de nada.
            await BorrarAsync();
        }

        return _sesion;
    }

    private async Task<AuthenticationResult?> LeerAsync()
    {
        var crudo = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);

        if (string.IsNullOrWhiteSpace(crudo))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AuthenticationResult>(crudo, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task BorrarAsync()
    {
        _sesion = null;
        _leida = true;

        await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    private static ClaimsIdentity ConstruirIdentidad(AuthenticationResult autenticacion)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, autenticacion.UserId.ToString()),
            new(ClaimTypes.Name, autenticacion.Email),
            new(BaionClaims.AccessToken, autenticacion.AccessToken),
            new(BaionClaims.TenantId, autenticacion.TenantId.ToString()),
            new(BaionClaims.ExpiresAt, autenticacion.ExpiresAt.ToString("O"))
        };

        foreach (var rol in autenticacion.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, rol));
        }

        return new ClaimsIdentity(claims, authenticationType: "baion");
    }
}

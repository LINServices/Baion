using System;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Models.Dtos;
using Baion.Orchestrator.Models.Entities;
using Baion.Orchestrator.Models.Enums;
using Baion.Orchestrator.Models.Results;
using Baion.Orchestrator.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Baion.Orchestrator.Identity.Tests;

public class LoginSelfManagedTests(IdentityDatabaseFixture fixture) : IClassFixture<IdentityDatabaseFixture>
{
    [Fact]
    public async Task Login_ConCredencialesCorrectas_EmiteTokenValido()
    {
        var tenant = await CrearTenantConUsuarioAsync();

        await using var scope = fixture.CreateScope();
        var resultado = await scope.ServiceProvider.GetRequiredService<IAuthenticationService>().LoginAsync(new LoginRequest(tenant.Slug, Email, Password), CancellationToken.None);

        Assert.True(resultado.IsSuccess, resultado.Error?.Message);
        var autenticacion = resultado.Value!;
        Assert.Equal("Bearer", autenticacion.TokenType);
        Assert.Equal(tenant.Id, autenticacion.TenantId);
        Assert.Contains("Admin", autenticacion.Roles);
        Assert.True(autenticacion.ExpiresAt > DateTimeOffset.UtcNow);

        var principal = await scope.ServiceProvider.GetRequiredService<ITokenService>().ValidateAsync(autenticacion.AccessToken);

        Assert.True(principal.IsSuccess, principal.Error?.Message);
        Assert.Equal(tenant.Id, principal.Value!.TenantId);
        Assert.Equal(autenticacion.UserId, principal.Value.UserId);
        Assert.Equal(Email, principal.Value.Email);
        Assert.True(principal.Value.IsInRole("admin"));
    }

    [Fact]
    public async Task Login_ConContrasenaIncorrecta_DevuelveNoAutorizado()
    {
        var tenant = await CrearTenantConUsuarioAsync();

        var resultado = await LoginAsync(tenant.Slug, Email, "contrasena-que-no-es");

        Assert.True(resultado.IsFailure);
        Assert.Equal(ErrorKind.Unauthorized, resultado.Error!.Kind);
    }

    [Fact]
    public async Task Login_ConTenantInexistente_DevuelveElMismoErrorQueUnaCredencialInvalida()
    {
        var resultado = await LoginAsync($"tenant-que-no-existe-{Guid.NewGuid():N}", Email, Password);

        Assert.True(resultado.IsFailure);
        Assert.Equal(ErrorKind.Unauthorized, resultado.Error!.Kind);
        Assert.Equal("auth.invalid_credentials", resultado.Error.Code);
    }

    [Fact]
    public async Task Login_TrasSuperarLosIntentosFallidos_BloqueaLaCuenta()
    {
        var tenant = await CrearTenantConUsuarioAsync();

        for (var intento = 0; intento < 3; intento++)
        {
            await LoginAsync(tenant.Slug, Email, "contrasena-que-no-es");
        }

        var bloqueado = await LoginAsync(tenant.Slug, Email, Password);

        Assert.True(bloqueado.IsFailure);
        Assert.Equal(ErrorKind.Forbidden, bloqueado.Error!.Kind);
        Assert.Equal("auth.locked_out", bloqueado.Error.Code);
    }

    [Fact]
    public async Task Login_EnTenantEnModoLin_IndicaQueLaIntegracionNoEstaLista()
    {
        var tenant = await CrearTenantAsync(IdentityMode.Lin);

        var resultado = await LoginAsync(tenant.Slug, Email, Password);

        Assert.True(resultado.IsFailure);
        Assert.Equal("identity.lin_unavailable", resultado.Error!.Code);
    }

    [Fact]
    public async Task CreateUser_ConEmailRepetidoEnElMismoTenant_DevuelveConflicto()
    {
        var tenant = await CrearTenantConUsuarioAsync();

        await using var scope = fixture.CreateScope();
        var resultado = await scope.ServiceProvider.GetRequiredService<IUserProvisioningService>().CreateUserAsync(tenant.Id, new CreateUserRequest(Email, "Duplicado", Password, []), CancellationToken.None);

        Assert.True(resultado.IsFailure);
        Assert.Equal(ErrorKind.Conflict, resultado.Error!.Kind);
    }

    [Fact]
    public async Task CreateUser_ConContrasenaCorta_DevuelveErrorDeValidacion()
    {
        var tenant = await CrearTenantAsync(IdentityMode.SelfManaged);

        await using var scope = fixture.CreateScope();
        var resultado = await scope.ServiceProvider.GetRequiredService<IUserProvisioningService>().CreateUserAsync(tenant.Id, new CreateUserRequest(Email, "Corto", "corta", []), CancellationToken.None);

        Assert.True(resultado.IsFailure);
        Assert.Equal(ErrorKind.Validation, resultado.Error!.Kind);
    }

    [Fact]
    public async Task ValidateAsync_ConTokenManipulado_DevuelveNoAutorizado()
    {
        var tenant = await CrearTenantConUsuarioAsync();
        var autenticacion = (await LoginAsync(tenant.Slug, Email, Password)).Value!;

        await using var scope = fixture.CreateScope();
        var manipulado = autenticacion.AccessToken[..^4] + "abcd";
        var principal = await scope.ServiceProvider.GetRequiredService<ITokenService>().ValidateAsync(manipulado);

        Assert.True(principal.IsFailure);
        Assert.Equal(ErrorKind.Unauthorized, principal.Error!.Kind);
    }

    private async Task<Result<AuthenticationResult>> LoginAsync(string slug, string email, string password)
    {
        await using var scope = fixture.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAuthenticationService>().LoginAsync(new LoginRequest(slug, email, password), CancellationToken.None);
    }

    private async Task<Tenant> CrearTenantAsync(IdentityMode mode)
    {
        var tenant = new Tenant { Name = "Acme", Slug = $"acme-{Guid.NewGuid():N}", IdentityMode = mode };

        await using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        return tenant;
    }

    private async Task<Tenant> CrearTenantConUsuarioAsync()
    {
        var tenant = await CrearTenantAsync(IdentityMode.SelfManaged);

        await using var scope = fixture.CreateScope();
        var alta = await scope.ServiceProvider.GetRequiredService<IUserProvisioningService>().CreateUserAsync(tenant.Id, new CreateUserRequest(Email, "Operador Acme", Password, ["Admin"]), CancellationToken.None);

        Assert.True(alta.IsSuccess, alta.Error?.Message);

        return tenant;
    }

    private const string Email = "operador@acme.test";

    private const string Password = "contrasena-de-prueba-larga";
}

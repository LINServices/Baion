using Baion.Orchestrator.Identity;
using Baion.Orchestrator.Messaging;
using Baion.Orchestrator.Persistence;
using Baion.Orchestrator.Persistence.Context;
using Baion.Orchestrator.Presentacion;
using Baion.Orchestrator.Presentacion.Middleware;
using Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPersistence(builder.Configuration)
    .AddIdentityProvider(builder.Configuration)
    .AddMessaging(builder.Configuration)
    .AddServices(builder.Configuration)
    .AddPresentacion(builder.Configuration);

var app = builder.Build();

// El orquestador se publica tras el gateway en la ruta /baion. UsePathBase es aditivo: si el gateway
// no recorta el prefijo, la app sigue respondiendo tanto en /baion/... como en la raíz (local y tests).
// Se puede cambiar o vaciar con Orchestrator:PathBase.
var pathBase = app.Configuration["Orchestrator:PathBase"] ?? "/baion";
if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase(pathBase);
}

app.UseWebSockets();
app.UseAuthentication();

// Va entre autenticación y autorización: necesita el token ya validado y debe ejecutarse
// antes de que cualquier endpoint consulte datos filtrados por tenant.
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>Punto de entrada expuesto para que las pruebas de integración puedan levantar la aplicación.</summary>
public partial class Program;

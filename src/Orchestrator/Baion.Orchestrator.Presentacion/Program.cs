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

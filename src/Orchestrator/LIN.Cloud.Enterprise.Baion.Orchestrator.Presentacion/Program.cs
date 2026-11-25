using LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Messaging;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Middleware;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Http.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPersistence(builder.Configuration)
    .AddIdentityProvider(builder.Configuration)
    .AddMessaging(builder.Configuration)
    .AddServices(builder.Configuration)
    .AddLINHttp()
    .AddPresentacion(builder.Configuration);

var app = builder.Build();

var service = app.Services.CreateScope().ServiceProvider.GetRequiredService<BaionDbContext>();
var created = await service.Database.EnsureCreatedAsync();

app.UseLINHttp(useGateway: true);
app.UseWebSockets();
app.UseAuthentication();

// Va entre autenticación y autorización: necesita el token ya validado y debe ejecutarse
// antes de que cualquier endpoint consulte datos filtrados por tenant.
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Punto de entrada expuesto para que las pruebas de integración puedan levantar la aplicación.
/// </summary>
public partial class Program;

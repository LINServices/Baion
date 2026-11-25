using System;
using Http.Extensions;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Messaging;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Context;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Middleware;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPersistence(builder.Configuration)
    .AddIdentityProvider(builder.Configuration)
    .AddMessaging(builder.Configuration)
    .AddServices(builder.Configuration)
    .AddLINHttp()
    .AddPresentacion(builder.Configuration);

WebApplication app = builder.Build();

try
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<BaionDbContext>().Database.MigrateAsync();
}
catch (Exception)
{
}

app.UseLINHttp(useGateway: true);
app.UseWebSockets();
app.UseAuthentication();

// Va entre autenticación y autorización: necesita el token ya validado y debe ejecutarse
// antes de que cualquier endpoint consulte datos filtrados por tenant.
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Punto de entrada expuesto para que las pruebas de integración puedan levantar la aplicación.
public partial class Program;

using LIN.Cloud.Enterprise.Baion.Agent.Core;
using LIN.Cloud.Enterprise.Baion.Agent.Execution;
using LIN.Cloud.Enterprise.Baion.Agent.Metrics;
using LIN.Cloud.Enterprise.Baion.Agent.Services;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Ambas llamadas son inocuas fuera de su plataforma: el mismo binario sirve como servicio de Windows
// y como unidad de systemd, y sigue ejecutándose en primer plano cuando se lanza a mano.
builder.Services.AddWindowsService(options => options.ServiceName = "Baion Agent");
builder.Services.AddSystemd();

builder.Services
    .AddAgentCore(builder.Configuration)
    .AddScriptExecution(builder.Configuration)
    .AddMetricsCollection(builder.Configuration)
    .AddServiceInspection(builder.Configuration);

var host = builder.Build();

host.Run();

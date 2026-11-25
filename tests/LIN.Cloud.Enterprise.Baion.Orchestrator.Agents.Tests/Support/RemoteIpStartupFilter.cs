using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Agents.Tests.Support;

// Fija una IP de origen en cada petición antes de que corra el resto del pipeline.
internal class RemoteIpStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use((context, siguiente) =>
            {
                context.Connection.RemoteIpAddress ??= IPAddress.Loopback;
                return siguiente(context);
            });

            next(app);
        };
    }
}

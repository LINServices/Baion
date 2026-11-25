using System;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;
using LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence;
using Microsoft.AspNetCore.Http;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Middleware;

// Traslada el tenant del token al ITenantContext del scope de la petición. Sin esto, una
// petición autenticada no vería ninguna fila: el filtro global no devuelve nada mientras no haya tenant.
internal class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var claim = context.User.FindFirst(BaionClaims.TenantId);

        if (claim is not null && Guid.TryParse(claim.Value, out var tenantId))
            tenantContext.SetTenant(tenantId);

        await next(context);
    }
}

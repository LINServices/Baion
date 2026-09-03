using System;
using System.Threading.Tasks;
using Baion.Cliente.Web;
using Baion.Cliente.Web.Components;
using Baion.Cliente.Web.Services;
using Baion.Cliente.Web.Services.Implementations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddOptions<BaionApiOptions>().Bind(builder.Configuration.GetSection(BaionApiOptions.SectionName));

// El panel guarda su sesión en una cookie cifrada y HttpOnly; dentro viaja el token de la API.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "baion.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IAccessTokenProvider, AccessTokenProvider>();

builder.Services.AddHttpClient<IBaionApiClient, BaionApiClient>((provider, client) =>
{
    var settings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<BaionApiOptions>>().Value;

    if (string.IsNullOrWhiteSpace(settings.BaseAddress))
    {
        throw new InvalidOperationException($"Falta '{BaionApiOptions.SectionName}:BaseAddress': el panel no sabe dónde está el orquestador.");
    }

    client.BaseAddress = new Uri(settings.BaseAddress.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Max(settings.TimeoutSeconds, 1));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Cerrar sesión es una operación de la petición, no del circuito: necesita el HttpContext para
// borrar la cookie, así que va como endpoint y no como componente.
app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.Run();

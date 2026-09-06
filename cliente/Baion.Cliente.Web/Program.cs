using System;
using System.Threading.Tasks;
using Baion.Cliente.Web;
using Baion.Cliente.Web.Components;
using Baion.Cliente.Web.Services;
using Baion.Cliente.Web.Services.Implementations;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddOptions<BaionApiOptions>().Bind(builder.Configuration.GetSection(BaionApiOptions.SectionName));

// La sesión vive en el navegador (localStorage): al ser WebAssembly no hay servidor donde guardarla.
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<BaionAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<BaionAuthenticationStateProvider>());
builder.Services.AddScoped<IBaionSession>(provider => provider.GetRequiredService<BaionAuthenticationStateProvider>());
builder.Services.AddScoped<IAccessTokenProvider, AccessTokenProvider>();

builder.Services.AddHttpClient<IBaionApiClient, BaionApiClient>((provider, client) =>
{
    var settings = provider.GetRequiredService<IOptions<BaionApiOptions>>().Value;

    if (string.IsNullOrWhiteSpace(settings.BaseAddress))
    {
        throw new InvalidOperationException($"Falta '{BaionApiOptions.SectionName}:BaseAddress': el panel no sabe dónde está el orquestador.");
    }

    client.BaseAddress = new Uri(settings.BaseAddress.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Max(settings.TimeoutSeconds, 1));
});

await builder.Build().RunAsync();

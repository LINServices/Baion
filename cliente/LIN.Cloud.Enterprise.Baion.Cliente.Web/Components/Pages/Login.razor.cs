using System.Threading;
using System.Threading.Tasks;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Auth;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.Services;
using Microsoft.AspNetCore.Components;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Components.Pages;

public partial class Login
{
    private string _tenantSlug = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _loadingMessage = "Iniciando sesión";
    private string _errorMessage = string.Empty;
    private string _errorVisibility = "hidden";
    private bool _isLoggingIn;
    private bool _isAnimating;

    [Inject]
    private IBaionApiClient Api { get; set; } = null!;

    [Inject]
    private IBaionSession Sesion { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "volverA")]
    private string? VolverA { get; set; }

    private void ShowControls()
    {
        _isLoggingIn = false;
        StateHasChanged();
    }

    private void HideControls()
    {
        _isLoggingIn = true;
        StateHasChanged();
    }

    private void HideError()
    {
        _errorVisibility = "hidden";
        StateHasChanged();
    }

    private void ShowError(string message)
    {
        _errorVisibility = "visible";
        _errorMessage = message;
        StateHasChanged();
    }

    private async Task StartAuthenticationAsync()
    {
        _loadingMessage = "Iniciando sesión";
        HideControls();
        HideError();

        if (string.IsNullOrWhiteSpace(_tenantSlug) || string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_password))
        {
            ShowControls();
            ShowError("Completa todos los campos");
            return;
        }

        await AttemptLoginAsync();
    }

    private async Task AttemptLoginAsync()
    {
        var login = await Api.LoginAsync(new LoginRequest(_tenantSlug, _username, _password), CancellationToken.None);

        if (login is not { IsSuccess: true, Value: AuthenticationResult autenticacion })
        {
            ShowControls();
            ShowError(login.ErrorMessage ?? "Inténtalo más tarde");
            return;
        }

        await Sesion.SignInAsync(autenticacion);
        await CompleteLoginAsync();
    }

    private async Task CompleteLoginAsync()
    {
        _isLoggingIn = false;
        _isAnimating = true;
        StateHasChanged();

        await Task.Delay(1100);

        NavigationManager.NavigateTo(string.IsNullOrWhiteSpace(VolverA) ? "/" : $"/{VolverA}");
    }
}

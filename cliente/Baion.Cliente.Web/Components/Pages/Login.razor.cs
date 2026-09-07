using System.Threading;
using System.Threading.Tasks;
using Baion.Cliente.Web.Models;
using Baion.Cliente.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Baion.Cliente.Web.Components.Pages;

/// <summary>
/// Pantalla de inicio de sesión del panel. Misma estructura visual que el login de LIN Console,
/// pero autenticando contra el orquestador con <see cref="IBaionApiClient.LoginAsync"/> y
/// persistiendo la sesión en el navegador con <see cref="IBaionSession"/>.
/// </summary>
public partial class Login
{
    /// <summary>Cliente HTTP del orquestador.</summary>
    [Inject]
    private IBaionApiClient Api { get; set; } = null!;

    /// <summary>Sesión del panel, persistida en el navegador.</summary>
    [Inject]
    private IBaionSession Sesion { get; set; } = null!;

    /// <summary>Gestor de navegación.</summary>
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    /// <summary>Ruta a la que volver tras entrar; llega como query string.</summary>
    [SupplyParameterFromQuery(Name = "volverA")]
    private string? VolverA { get; set; }

    /// <summary>Slug de la organización (tenant) ingresado.</summary>
    private string _tenantSlug = string.Empty;

    /// <summary>Correo ingresado.</summary>
    private string _username = string.Empty;

    /// <summary>Contraseña ingresada.</summary>
    private string _password = string.Empty;

    /// <summary>Mensaje mostrado durante procesos de carga.</summary>
    private string _loadingMessage = "Iniciando sesión";

    /// <summary>Mensaje de error a mostrar en la UI.</summary>
    private string _errorMessage = string.Empty;

    /// <summary>Indica si hay un proceso de inicio de sesión activo.</summary>
    private bool _isLoggingIn;

    /// <summary>Indica si se debe mostrar la animación de éxito.</summary>
    private bool _isAnimating;

    /// <summary>Visibilidad del mensaje de error.</summary>
    private string _errorVisibility = "hidden";

    /// <summary>
    /// Hace visibles los controles de entrada.
    /// </summary>
    private void ShowControls()
    {
        _isLoggingIn = false;
        StateHasChanged();
    }

    /// <summary>
    /// Oculta los controles de entrada.
    /// </summary>
    private void HideControls()
    {
        _isLoggingIn = true;
        StateHasChanged();
    }

    /// <summary>
    /// Oculta el mensaje de error.
    /// </summary>
    private void HideError()
    {
        _errorVisibility = "hidden";
        StateHasChanged();
    }

    /// <summary>
    /// Muestra un mensaje de error en la UI.
    /// </summary>
    /// <param name="message">Mensaje a mostrar.</param>
    private void ShowError(string message)
    {
        _errorVisibility = "visible";
        _errorMessage = message;
        StateHasChanged();
    }

    /// <summary>
    /// Inicia el proceso de autenticación: valida la entrada y llama al orquestador.
    /// </summary>
    private async Task StartAuthenticationAsync()
    {
        _loadingMessage = "Iniciando sesión";
        HideControls();
        HideError();

        // Validar información de entrada.
        if (string.IsNullOrWhiteSpace(_tenantSlug) || string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_password))
        {
            ShowControls();
            ShowError("Completa todos los campos");
            return;
        }

        await AttemptLoginAsync();
    }

    /// <summary>
    /// Intenta iniciar sesión contra el orquestador con las credenciales ingresadas.
    /// </summary>
    private async Task AttemptLoginAsync()
    {
        var login = await Api.LoginAsync(new LoginRequest(_tenantSlug, _username, _password), CancellationToken.None);

        if (login is not { IsSuccess: true, Value: AuthenticationResult autenticacion })
        {
            ShowControls();
            ShowError(login.ErrorMessage ?? "Inténtalo más tarde");
            return;
        }

        await Sesion.IniciarSesionAsync(autenticacion);
        await CompleteLoginAsync();
    }

    /// <summary>
    /// Finaliza un login exitoso: muestra la animación de éxito y navega al destino.
    /// </summary>
    private async Task CompleteLoginAsync()
    {
        _isLoggingIn = false;
        _isAnimating = true;
        StateHasChanged();

        await Task.Delay(1100);

        NavigationManager.NavigateTo(string.IsNullOrWhiteSpace(VolverA) ? "/" : $"/{VolverA}");
    }
}

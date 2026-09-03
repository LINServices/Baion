namespace Baion.Orchestrator.Identity;

/// <summary>Desenlace de verificar una contraseña contra su hash.</summary>
public enum PasswordVerification
{
    Failed = 0,
    Succeeded = 1,

    /// <summary>La contraseña es correcta pero el hash usa parámetros antiguos y conviene regenerarlo.</summary>
    SucceededNeedsRehash = 2
}

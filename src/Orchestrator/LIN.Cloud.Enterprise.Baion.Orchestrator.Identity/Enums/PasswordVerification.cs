namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Identity;

public enum PasswordVerification
{
    Failed = 0,
    Succeeded = 1,

    // La contraseña es correcta pero el hash usa parámetros antiguos y conviene regenerarlo.
    SucceededNeedsRehash = 2
}

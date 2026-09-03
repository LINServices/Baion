namespace Baion.Orchestrator.Identity;

/// <summary>Calcula y verifica hashes de contraseña.</summary>
public interface IPasswordHasher
{
    /// <summary>Calcula el hash de una contraseña en claro.</summary>
    string Hash(string password);

    /// <summary>Verifica una contraseña contra su hash.</summary>
    PasswordVerification Verify(string hash, string password);
}

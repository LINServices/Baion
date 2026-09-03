using Microsoft.AspNetCore.Identity;

namespace Baion.Orchestrator.Identity.Implementations;

/// <summary>
/// Envuelve el hasher de ASP.NET Core Identity (PBKDF2-HMAC-SHA512, formato versionado) para no
/// arrastrar ese tipo fuera de esta capa y poder cambiarlo sin tocar a quien lo consume.
/// </summary>
internal class IdentityPasswordHasher : IPasswordHasher
{
    // El hasher ignora el usuario que recibe; se le pasa un objeto fijo.
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(HashSubject, password);

    public PasswordVerification Verify(string hash, string password) => _hasher.VerifyHashedPassword(HashSubject, hash, password) switch
    {
        PasswordVerificationResult.Success => PasswordVerification.Succeeded,
        PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SucceededNeedsRehash,
        _ => PasswordVerification.Failed
    };

    private static readonly object HashSubject = new();
}

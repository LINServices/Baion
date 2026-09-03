using System;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Baion.Orchestrator.Services;

/// <summary>
/// Genera y resume las credenciales de agente. Son aleatorias de 256 bits, así que un SHA-256 directo
/// basta para guardarlas: no hay espacio de búsqueda que valga un ataque de diccionario, y permite
/// buscarlas por índice, cosa que un hash con sal no permitiría.
/// </summary>
public static class AgentTokens
{
    /// <summary>Genera un token aleatorio de 256 bits en base64url.</summary>
    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));

    /// <summary>Calcula el SHA-256 del token, en hexadecimal minúscula.</summary>
    public static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private const int TokenBytes = 32;
}

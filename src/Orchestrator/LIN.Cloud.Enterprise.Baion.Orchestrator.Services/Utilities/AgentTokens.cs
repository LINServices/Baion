using System;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Services;

// Credenciales de agente: aleatorias de 256 bits, así que un SHA-256 directo basta para guardarlas. No hay
// espacio de búsqueda que valga un ataque de diccionario, y permite buscarlas por índice, cosa que un
// hash con sal no permitiría.
public static class AgentTokens
{
    private const int TokenBytes = 32;

    public static string Generate()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));
    }

    public static string Hash(string token)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}

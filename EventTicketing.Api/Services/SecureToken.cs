using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace EventTicketing.Api.Services;

/// <summary>Random bearer tokens that are stored only as their SHA-256 hash.</summary>
public static class SecureToken
{
    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));

    public static string Hash(string rawToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}

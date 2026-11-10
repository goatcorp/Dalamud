using System.Security.Cryptography;
using System.Text;

namespace Dalamud.Utility;

/// <summary>
/// Utility functions for hashing.
/// </summary>
public static class Hash
{
    /// <summary>
    /// Get the SHA-256 hash of a string.
    /// </summary>
    /// <param name="text">The string to hash.</param>
    /// <returns>The computed hash.</returns>
    internal static string GetStringSha256Hash(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var strUtf8Len = Encoding.UTF8.GetByteCount(text);
        var strBytes = strUtf8Len <= 512 ? stackalloc byte[512] : new byte[strUtf8Len];
        Encoding.UTF8.GetBytes(text, strBytes);

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(strBytes[..strUtf8Len], hash);
        return Convert.ToHexString(hash);
    }
}

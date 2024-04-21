using System.Security.Cryptography;

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
        return string.IsNullOrEmpty(text) ? string.Empty : GetSha256Hash(System.Text.Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// Get the SHA-256 hash of a byte array.
    /// </summary>
    /// <param name="buffer">The byte array to hash.</param>
    /// <returns>The computed hash.</returns>
    internal static string GetSha256Hash(byte[] buffer)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(buffer);
        return ByteArrayToString(hash);
    }

    private static string ByteArrayToString(byte[] ba) => BitConverter.ToString(ba).Replace("-", string.Empty);
}

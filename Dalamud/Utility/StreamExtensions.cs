using System.IO;

namespace Dalamud.Utility;

/// <summary>Extension method for streams.</summary>
internal static class StreamExtensions
{
    /// <summary>Clears a stream = truncates the stream and seeks to the beginning.</summary>
    /// <param name="stream">The stream to clear.</param>
    public static void Clear(this Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        stream.SetLength(0);
    }

    /// <summary>Gets the underlying data span of a memory stream.</summary>
    /// <param name="memoryStream">The memory stream.</param>
    /// <returns>The data span.</returns>
    public static Span<byte> GetDataSpan(this MemoryStream memoryStream) =>
        memoryStream.GetBuffer().AsSpan(0, unchecked((int)memoryStream.Length));

    /// <summary>Gets the underlying data memory of a memory stream.</summary>
    /// <param name="memoryStream">The memory stream.</param>
    /// <returns>The data memory.</returns>
    public static Memory<byte> GetDataMemory(this MemoryStream memoryStream) =>
        memoryStream.GetBuffer().AsMemory(0, unchecked((int)memoryStream.Length));
}

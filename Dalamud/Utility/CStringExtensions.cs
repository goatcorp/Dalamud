using InteropGenerator.Runtime;

using Lumina.Text.ReadOnly;

namespace Dalamud.Utility;

/// <summary>
/// A set of helpful utilities for working with <see cref="CStringPointer"/>s from ClientStructs.
/// </summary>
public static class CStringExtensions
{
    /// <summary>
    /// Convert a CStringPointer to a ReadOnlySeStringSpan.
    /// </summary>
    /// <param name="ptr">The pointer to convert.</param>
    /// <returns>A span.</returns>
    public static unsafe ReadOnlySeStringSpan AsReadOnlySeStringSpan(this CStringPointer ptr)
    {
        return ptr.AsSpan();
    }

    /// <summary>
    /// Extract text from this CStringPointer following <see cref="ReadOnlySeStringSpan.ExtractText()"/>'s rules. Only
    /// useful for SeStrings.
    /// </summary>
    /// <param name="ptr">The CStringPointer to process.</param>
    /// <returns>Extracted text.</returns>
    public static string ExtractText(this CStringPointer ptr)
    {
        return ptr.AsReadOnlySeStringSpan().ExtractText();
    }
}

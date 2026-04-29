using Dalamud.Game.Text.SeStringHandling;

using InteropGenerator.Runtime;

using Lumina.Text.ReadOnly;

namespace Dalamud.Utility;

/// <summary>
/// A set of helpful utilities for working with <see cref="CStringPointer"/>s from ClientStructs.
/// </summary>
/// <remarks>
/// WARNING: Will break if a custom ClientStructs is used. These are here for CONVENIENCE ONLY!.
/// </remarks>
public static class CStringExtensions
{
    /// <summary>
    /// Convert a CStringPointer to a ReadOnlySeStringSpan.
    /// </summary>
    /// <param name="ptr">The pointer to convert.</param>
    /// <returns>A span.</returns>
    public static ReadOnlySeStringSpan AsReadOnlySeStringSpan(this CStringPointer ptr)
    {
        return ptr.AsSpan();
    }

    /// <summary>
    /// Convert a CStringPointer to a Dalamud SeString.
    /// </summary>
    /// <param name="ptr">The pointer to convert.</param>
    /// <returns>A Dalamud-flavored SeString.</returns>
    public static SeString AsDalamudSeString(this CStringPointer ptr)
    {
        return ptr.AsReadOnlySeStringSpan().ToDalamudString();
    }

    /// <summary>
    /// Get a new ReadOnlySeString that's a <em>copy</em> of the text in this CStringPointer.
    /// </summary>
    /// <remarks>
    /// This should be functionally identical to <see cref="AsReadOnlySeStringSpan"/>, but exists
    /// for convenience in places that already expect ReadOnlySeString as a type (and where a copy is desired).
    /// </remarks>
    /// <param name="ptr">The pointer to copy.</param>
    /// <returns>A new Lumina ReadOnlySeString.</returns>
    public static ReadOnlySeString AsReadOnlySeString(this CStringPointer ptr)
    {
        return new ReadOnlySeString(ptr.AsSpan());
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

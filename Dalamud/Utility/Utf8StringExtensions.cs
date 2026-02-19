using Dalamud.Game.Text.SeStringHandling;

using FFXIVClientStructs.FFXIV.Client.System.String;

using Lumina.Text.ReadOnly;

namespace Dalamud.Utility;

/// <summary>
/// A set of helpful utilities for working with <see cref="Utf8String"/>s from ClientStructs.
/// </summary>
/// <remarks>
/// WARNING: Will break if a custom ClientStructs is used. These are here for CONVENIENCE ONLY!.
/// </remarks>
public static class Utf8StringExtensions
{
    /// <summary>
    /// Convert a Utf8String to a ReadOnlySeStringSpan.
    /// </summary>
    /// <param name="str">The Utf8String to convert.</param>
    /// <returns>A span.</returns>
    public static ReadOnlySeStringSpan AsReadOnlySeStringSpan(this Utf8String str)
    {
        return str.AsSpan();
    }

    /// <summary>
    /// Convert a Utf8String to a Dalamud SeString.
    /// </summary>
    /// <param name="str">The Utf8String to convert.</param>
    /// <returns>A Dalamud-flavored SeString.</returns>
    public static SeString AsDalamudSeString(this Utf8String str)
    {
        return str.AsReadOnlySeStringSpan().ToDalamudString();
    }

    /// <summary>
    /// Get a new ReadOnlySeString that's a <em>copy</em> of the text in this Utf8String.
    /// </summary>
    /// <remarks>
    /// This should be functionally identical to <see cref="AsReadOnlySeStringSpan"/>, but exists
    /// for convenience in places that already expect ReadOnlySeString as a type (and where a copy is desired).
    /// </remarks>
    /// <param name="str">The Utf8String to copy.</param>
    /// <returns>A new Lumina ReadOnlySeString.</returns>
    public static ReadOnlySeString AsReadOnlySeString(this Utf8String str)
    {
        return new ReadOnlySeString(str.AsSpan().ToArray());
    }

    /// <summary>
    /// Extract text from this Utf8String following <see cref="ReadOnlySeStringSpan.ExtractText()"/>'s rules.
    /// </summary>
    /// <param name="str">The Utf8String to process.</param>
    /// <returns>Extracted text.</returns>
    public static string ExtractText(this Utf8String str)
    {
        return str.AsReadOnlySeStringSpan().ExtractText();
    }
}

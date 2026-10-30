using FFXIVClientStructs.FFXIV.Client.System.String;

using Lumina.Text.ReadOnly;

namespace Dalamud.Utility;

#pragma warning disable SA1101

/// <summary>
/// A set of helpful utilities for working with <see cref="Utf8String"/>s from ClientStructs.
/// </summary>
/// <remarks>
/// WARNING: Will break if a custom ClientStructs is used. These are here for CONVENIENCE ONLY.
/// </remarks>
public static class Utf8StringExtensions
{
    extension(ref Utf8String str)
    {
        /// <summary>
        /// Convert a Utf8String to a ReadOnlySeStringSpan.
        /// </summary>
        /// <returns>A span.</returns>
        public ReadOnlySeStringSpan AsReadOnlySeStringSpan()
        {
            return str.AsSpan();
        }

        /// <summary>
        /// Get a new ReadOnlySeString that's a <em>copy</em> of the text in this Utf8String.
        /// </summary>
        /// <remarks>
        /// This should be functionally identical to <see cref="AsReadOnlySeStringSpan"/>, but exists
        /// for convenience in places that already expect ReadOnlySeString as a type (and where a copy is desired).
        /// </remarks>
        /// <returns>A new Lumina ReadOnlySeString.</returns>
        public ReadOnlySeString AsReadOnlySeString()
        {
            return new ReadOnlySeString(str.AsSpan());
        }

        /// <summary>
        /// Extract text from this Utf8String following <see cref="ReadOnlySeStringSpan.ExtractText()"/>'s rules.
        /// </summary>
        /// <returns>Extracted text.</returns>
        public string ExtractText()
        {
            return str.AsReadOnlySeStringSpan().ExtractText();
        }
    }
}

using System.Diagnostics.CodeAnalysis;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts;

/// <summary>Members of <see cref="ImFontPtr"/> for Dalamud's fonts.</summary>
/// <remarks>For use from the main thread only.</remarks>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101", Justification = "Extension receivers are not members")]
public static unsafe class ImFontExtensions
{
    /// <param name="font">The font.</param>
    extension(ImFontPtr font)
    {
        /// <summary>Gets the style of the font.</summary>
        /// <remarks>A null font, no font specified, and a font not of this frame are taken as the current font.
        /// </remarks>
        public TextStyle Style =>
            FontImpl.FromNative(font.IsNull ? ImGui.GetFont().Handle : font.Handle)?.Style ?? default;

        /// <summary>Gets the Dalamud font behind this ImGui font, such as <see cref="ImGui.GetFont"/>.</summary>
        /// <value>The font, or <see langword="null"/> if this font is null or not Dalamud's.</value>
        /// <remarks>It never loads a font. To keep a font alive, keep its <see cref="TextStyle"/> or the returned
        /// <see cref="ImFontRef"/>.</remarks>
        public ImFontRef? Ref => Service<FontResolver>.Get().Find(font);

        /// <summary>Pushes this font as it is, at its own size.</summary>
        /// <returns>Calls <see cref="ImGui.PopFont"/> on <see cref="IDisposable.Dispose"/>.</returns>
        /// <remarks>A null font, no font specified, pushes the current font at the current size.</remarks>
        /// <seealso cref="TextStyle.PushFamily"/>
        public ImPopFontDisposable Push()
        {
            ImGui.PushFont(font);
            return new ImPopFontDisposable(font: true);
        }

        /// <summary>Tests whether a font has a glyph for a codepoint, from its own glyphs or its fallbacks.</summary>
        /// <param name="codepoint">The codepoint.</param>
        /// <returns>Whether the glyph exists.</returns>
        public bool IsGlyphInFont(uint codepoint)
        {
            ThreadSafety.AssertMainThread();
            return (font.IsNull ? ImGui.Font : font).GetGlyphIndex(codepoint) != 0;
        }
    }
}

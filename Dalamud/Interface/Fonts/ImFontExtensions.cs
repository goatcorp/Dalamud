using System.Diagnostics.CodeAnalysis;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal;

namespace Dalamud.Interface.Fonts;

/// <summary>Members of <see cref="ImFontPtr"/> for Dalamud's fonts.</summary>
/// <remarks>
/// <para>For use from the main thread only.</para>
/// <para>The ways to push a font compare as follows.</para>
/// <list type="table">
/// <listheader><term>Push</term><description>Size, and what it pushes</description></listheader>
/// <item><term><see cref="ImGui.PushFont(ImFontPtr)"/>, <see cref="Utility.Raii.ImRaii.PushFont"/>,
/// <see cref="ImFontExtensions.extension(ImFontPtr).Push"/></term><description>The font at the size of its style. A style without a
/// size keeps the current size. The pointer is valid only during the frame it was obtained. A stale non-default pointer
/// pushes the current font.</description></item>
/// <item><term><see cref="ImGuiTextStyleExtensions.extension(ImGui).PushFontSize(float, float?)"/>,
/// <see cref="ImGuiTextStyleExtensions.extension(ImGui).PushFontScale"/></term><description>The current font at the
/// size or scale given.</description></item>
/// <item><term><see cref="ImFontRef.Push"/></term><description>The font at the size of its style, as above. It may be
/// kept across frames, and keeps the font loaded.</description></item>
/// <item><term><see cref="TextStyle.Push"/></term><description>The font of the style, loading its family if needed.
/// A style without a size keeps the current size, and one without a line height the current line height. It may be
/// kept across frames.</description></item>
/// <item><term><see cref="TextStyle.PushFamily"/>, <see cref="ImGuiTextStyleExtensions"/></term><description>The
/// current font with fields of its style changed. <see cref="TextStyle.PushFamily"/> keeps the current size, and the
/// size pushers of <see cref="ImGuiTextStyleExtensions"/> change it.</description></item>
/// </list>
/// </remarks>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101", Justification = "Extension receivers are not members")]
public static unsafe class ImFontExtensions
{
    /// <param name="font">The font.</param>
    extension(ImFontPtr font)
    {
        /// <summary>Gets the style of the font.</summary>
        /// <remarks>A null or stale non-default <see cref="ImFontPtr"/> resolves to the current font, and a stale
        /// default-font pointer resolves to the default font. Off the main thread, returns
        /// <see cref="TextStyle.Default"/>.</remarks>
        public TextStyle Style =>
            FontImpl.FromNative(font.IsNull ? ImGui.GetFont().Handle : font.Handle)?.Style ?? default;

        /// <summary>Gets the Dalamud font behind this ImGui font, such as <see cref="ImGui.GetFont"/>.</summary>
        /// <value>The font, or <see langword="null"/> if this font is null, not Dalamud's, or not of this frame, or if
        /// read off the main thread.</value>
        /// <remarks>It never loads a font. To keep a font alive, keep its <see cref="TextStyle"/> or the returned
        /// <see cref="ImFontRef"/>.</remarks>
        public ImFontRef? Ref => Service<FontResolver>.Get().Find(font);

        /// <summary>Pushes this font as it is, at its own size.</summary>
        /// <returns>Calls <see cref="ImGui.PopFont"/> on <see cref="IDisposable.Dispose"/>.</returns>
        /// <remarks>A null <see cref="ImFontPtr"/> pushes the current font at the current size.</remarks>
        /// <seealso cref="TextStyle.PushFamily"/>
        public ImPopFontDisposable Push()
        {
            ImGui.PushFont(font);
            return new(font: true);
        }
    }
}

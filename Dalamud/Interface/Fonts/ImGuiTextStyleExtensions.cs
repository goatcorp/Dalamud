using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal;
using Dalamud.Interface.Fonts.Internal.Markup;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts;

/// <summary>Pushes the current font in another style.</summary>
/// <remarks>Main thread only, as ImGui is. Off the main thread, the pushes throw
/// <see cref="InvalidOperationException"/>.</remarks>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101", Justification = "Extension receivers are not members")]
public static class ImGuiTextStyleExtensions
{
    extension(ImGui)
    {
        /// <summary>Gets the current font.</summary>
        public static ImFontPtr Font => ImGui.GetFont();

        /// <summary>Gets the style of the current font.</summary>
        public static TextStyle TextStyle => ImGui.Font.Style;

        /// <summary>Pushes the current style with another family, keeping the size.</summary>
        /// <param name="family">The family.</param>
        /// <returns>A disposable object that pops the font.</returns>
        /// <seealso cref="ImGui.PopFont"/>
        public static ImPopFontDisposable PushFamily(IFontFamilyId family) =>
            PushChange(TextStyleRange.FamilyId(0, 0, family));

        /// <summary>Pushes the current style with the family of a face, keeping the size.</summary>
        /// <param name="face">The face.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        [OverloadResolutionPriority(1)]
        public static ImPopFontDisposable PushFamily(IFontFaceId face) => ImGui.PushFamily(face.Family);

        /// <summary>Pushes the current font at another size.</summary>
        /// <param name="sizePx">The glyph size in pixels before global scaling.</param>
        /// <param name="lineHeight">The line height as a multiple of the glyph size, or null to inherit the current
        /// line height.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        public static ImPopFontDisposable PushFontSize(float sizePx, float? lineHeight = null) =>
            ImGui.PushFontSize(
                StyleLength.Px(sizePx),
                lineHeight is { } lh ? StyleLength.Em(lh) : default);

        /// <summary>Pushes the current font at another size.</summary>
        /// <param name="size">The glyph size, as <see cref="TextStyle.Size"/>.</param>
        /// <param name="lineHeight">The line height, as <see cref="TextStyle.LineHeight"/>.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        public static ImPopFontDisposable PushFontSize(StyleLength size, StyleLength lineHeight = default) =>
            (ImGui.TextStyle with { Size = size, LineHeight = lineHeight }).Push();

        /// <summary>Pushes the current font at a ratio of the current size.</summary>
        /// <param name="ratio">The ratio, such as 1.5 for 150%.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        public static ImPopFontDisposable PushFontScale(float ratio) => ImGui.PushFontSize(StyleLength.Em(ratio));

        /// <summary>Pushes the current font with a text color.</summary>
        /// <param name="color">The color, or 0 for the color text is drawn with.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        public static ImPopFontDisposable PushTextColor(ImTextColor color) =>
            PushChange(TextStyleRange.Color(0, 0, color));

        /// <summary>Pushes the current font with an edge around the text.</summary>
        /// <param name="color">The color, or alpha 0 for none.</param>
        /// <param name="width">The width, or <see cref="StyleUnit.Default"/> for 1/16 em.</param>
        /// <param name="softness">How soft the edge is, as <see cref="TextEdge.Softness"/>.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        /// <remarks>Values not given are those of the current edge, or of a black default one if there is none.
        /// </remarks>
        public static ImPopFontDisposable PushTextEdge(
            ImTextColor? color = null, StyleLength? width = null, float? softness = null)
        {
            var current = ImGui.TextStyle.Edge;
            var on = current.IsOn;
            return PushChange(
                TextStyleRange.Edge(
                    0,
                    0,
                    new(color ?? (on ? current.Color : 0xFF000000), width ?? current.Width)
                    {
                        Softness = softness ?? current.Softness,
                    }));
        }

        /// <summary>Pushes the current font with a shadow under the text.</summary>
        /// <param name="color">The color, or alpha 0 for none.</param>
        /// <param name="offset">The offset, or 0 for none.</param>
        /// <param name="unit">The unit of <paramref name="offset"/>.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        public static ImPopFontDisposable PushTextShadow(
            ImTextColor? color = null, Vector2? offset = null, StyleUnit unit = StyleUnit.Em)
        {
            var current = ImGui.TextStyle.Shadow;
            var on = current.IsOn;
            return PushChange(
                TextStyleRange.Shadow(
                    0,
                    0,
                    new(
                        color ?? (on ? current.Color : 0xFF000000),
                        offset ?? (on ? current.Offset : TextShadow.DefaultOffset),
                        offset is null && on ? current.Unit : unit)));
        }

        /// <summary>Pushes the current font with <see cref="TextStyle.Background"/> set.</summary>
        /// <param name="color">The color, or alpha 0 for none.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        public static ImPopFontDisposable PushTextBackground(ImTextColor color) =>
            PushChange(TextStyleRange.Background(0, 0, color));

        /// <summary>Pushes the current font with lines under, over, or through the text.</summary>
        /// <param name="positions">Where the lines are, or <see cref="TextLinePosition.None"/> for none.</param>
        /// <param name="style">How the lines are drawn, or <see cref="TextLineStyle.None"/> for none.</param>
        /// <param name="color">The color, or 0 for the color of the text.</param>
        /// <param name="thickness">The thickness in pixels, or 0 for what the font says.</param>
        /// <param name="count">The number of lines at each position.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        /// <remarks>Null arguments keep the current lines' values. A style or a color shows nothing while there are no
        /// lines.</remarks>
        /// <seealso cref="TextStyle.Lines"/>
        public static ImPopFontDisposable PushTextLines(
            TextLinePosition? positions = null, TextLineStyle? style = null, ImTextColor? color = null,
            float? thickness = null, byte? count = null)
        {
            var current = ImGui.TextStyle.Lines;
            return PushChange(
                TextStyleRange.Lines(
                    0,
                    0,
                    new(
                        positions ?? current.Positions,
                        style ?? (current.IsOn ? current.Style : TextLineStyle.Solid),
                        color ?? current.Color,
                        thickness ?? current.Thickness,
                        count ?? Math.Max(current.Count, (byte)1))));
        }

        /// <summary>Pushes the current font with underlined text, or without the underline.</summary>
        /// <param name="color">The color of all the lines, 0 for the color of the text, or null to keep that of the
        /// current lines. The lines of a style share one color.</param>
        /// <param name="on">Whether to add the line, or to remove it.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        public static ImPopFontDisposable PushTextUnderline(ImTextColor? color = null, bool on = true) =>
            PushLinesAlso(TextLinePosition.Under, color, on);

        /// <summary>Pushes the current font with overlined text, or without the overline.</summary>
        /// <inheritdoc cref="PushTextUnderline"/>
        public static ImPopFontDisposable PushTextOverline(ImTextColor? color = null, bool on = true) =>
            PushLinesAlso(TextLinePosition.Over, color, on);

        /// <summary>Pushes the current font with struck-through text, or without the line through it.</summary>
        /// <inheritdoc cref="PushTextUnderline"/>
        public static ImPopFontDisposable PushTextStrikethrough(ImTextColor? color = null, bool on = true) =>
            PushLinesAlso(TextLinePosition.Through, color, on);

        /// <summary>Pushes the current font with a kind of decoration as <see cref="TextStyle.ExtraDecoration"/>,
        /// or without the decorations of that kind.</summary>
        /// <param name="id">The kind of decoration, built in or from <see cref="IUiBuilder.RegisterTextDecoration"/>.
        /// </param>
        /// <param name="context">The context, such as a color, or null to keep that of the current one if it is of the
        /// same kind.</param>
        /// <param name="data">The data, or null to keep that of the current one if it is of the same kind.</param>
        /// <param name="on">Whether to add the decoration, or to remove it.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        /// <remarks>Removing <see cref="TextDecorationId.Background"/> or <see cref="TextDecorationId.Line"/> also
        /// clears <see cref="TextStyle.Background"/> or <see cref="TextStyle.Lines"/>.</remarks>
        public static ImPopFontDisposable PushTextDecoration(
            TextDecorationId id, uint? context = null, ulong? data = null, bool on = true)
        {
            if (!on)
                return ImGui.TextStyle.WithoutDecoration(id).Push();
            var current = ImGui.TextStyle.ExtraDecoration;
            if (current.Id != id)
                current = default;
            return PushChange(
                TextStyleRange.ExtraDecoration(0, 0, new(id, context ?? current.Context, data ?? current.Data)));
        }

        /// <summary>Pushes the bold form of the current font.</summary>
        /// <param name="bold">Whether bold.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        /// <remarks>The font's bold faces where it has them, emboldened where not.</remarks>
        public static ImPopFontDisposable PushTextBold(bool bold = true) => PushChange(TextStyleRange.Bold(0, 0, bold));

        /// <summary>Pushes the italic form of the current font.</summary>
        /// <param name="italic">Whether italic.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId)" path="/returns"/>
        /// <remarks>The font's italic faces where it has them, slanted where not.</remarks>
        public static ImPopFontDisposable PushTextItalic(bool italic = true) =>
            PushChange(TextStyleRange.Italic(0, 0, italic));

        /// <summary>Enters a scope where all text is of a markup.</summary>
        /// <param name="markup">The markup.</param>
        /// <param name="callback">Optional callback.</param>
        /// <param name="inherit">Whether the callbacks pushed earlier are still asked.</param>
        /// <returns>A disposable object that exits the scope. It is <see langword="default"/> if nothing was pushed.
        /// </returns>
        /// <seealso cref="ImGuiTextExtensions.extension(ImGui).PushTextCallback"/>
        public static ImPopFontDisposable PushTextMarkup(
            TextMarkup markup, ITextMarkupCallback? callback = null, bool inherit = true)
        {
            ThreadSafety.AssertMainThread();
            var pushFont = ImGui.TextStyle.Markup != markup;
            if (pushFont)
                Service<FontResolver>.Get().PushMarkup(markup);
            var pushCallback = callback is not null || !inherit;
            if (pushCallback)
                TextCallbackStack.Push(callback, inherit);
            return new(font: pushFont, textCallback: pushCallback);
        }
    }

    private static ImPopFontDisposable PushChange(in TextStyleRange change)
    {
        Service<FontResolver>.Get().PushChange(change);
        return new ImPopFontDisposable(font: true);
    }

    private static ImPopFontDisposable PushLinesAlso(TextLinePosition position, ImTextColor? color, bool on)
    {
        var positions = ImGui.TextStyle.Lines.Positions;
        return ImGui.PushTextLines(on ? positions | position : positions & ~position, color: color);
    }
}

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
        /// <param name="fallback">
        /// <inheritdoc cref="TextStyle.From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/>
        /// </param>
        /// <returns>A disposable object that pops the font.</returns>
        /// <seealso cref="ImGui.PopFont"/>
        public static ImPopFontDisposable PushFamily(IFontFamilyId family, bool fallback = true) =>
            PushChange(TextStyleRange.FamilyId(0, 0, TextStyle.WithFallback(family, fallback)));

        /// <summary>Pushes the current style with the family of a face, in the weight, slant, and stretch of the
        /// face, keeping the size.</summary>
        /// <param name="face">The face.</param>
        /// <param name="fallback">
        /// <inheritdoc cref="TextStyle.From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/>
        /// </param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
        /// <remarks>The weight and stretch the face specifies replace the current ones. The current
        /// <see cref="TextStyle.Bold"/> and <see cref="TextStyle.Italic"/> are kept, and an italic face makes the text
        /// italic.</remarks>
        [OverloadResolutionPriority(1)]
        public static ImPopFontDisposable PushFamily(IFontFaceId face, bool fallback = true)
        {
            var style = face.Style;
            var family = TextStyle.WithFallback(face.Family, fallback);
            if (style == default)
                return PushChange(TextStyleRange.FamilyId(0, 0, family));

            // Apply all face fields in one style change to avoid creating intermediate FontImpl entries.
            var current = ImGui.TextStyle;
            return PushDerived(
                current with
                {
                    FamilyId = family,
                    Weight = style.Weight != 0 ? style.Weight : current.Weight,
                    Italic = current.Italic || style.Italicized,
                    Stretch = style.Stretch != 0 ? style.Stretch : current.Stretch,
                });
        }

        /// <summary>Pushes the current font at another size.</summary>
        /// <param name="sizePx">The glyph size in pixels before global scaling.</param>
        /// <param name="lineHeight">The line height as a multiple of the glyph size, or null to inherit the current
        /// line height.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
        public static ImPopFontDisposable PushFontSize(float sizePx, float? lineHeight = null) =>
            ImGui.PushFontSize(
                StyleLength.Px(sizePx),
                lineHeight is { } lh ? StyleLength.Em(lh) : default);

        /// <summary>Pushes the current font at another size.</summary>
        /// <param name="size">The glyph size, as <see cref="TextStyle.Size"/>.</param>
        /// <param name="lineHeight">The line height, as <see cref="TextStyle.LineHeight"/>.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
        public static ImPopFontDisposable PushFontSize(StyleLength size, StyleLength lineHeight = default) =>
            (ImGui.TextStyle with { Size = size, LineHeight = lineHeight }).Push();

        /// <summary>Pushes the current font at a ratio of the current size.</summary>
        /// <param name="ratio">The ratio, such as 1.5 for 150%.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
        public static ImPopFontDisposable PushFontScale(float ratio) => ImGui.PushFontSize(StyleLength.Em(ratio));

        /// <summary>Pushes the current font with a text color.</summary>
        /// <param name="color">The text color, or 0 to inherit the draw call's text color.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
        public static ImPopFontDisposable PushTextColor(ImTextColor color) =>
            PushChange(TextStyleRange.Color(0, 0, color));

        /// <summary>Pushes the current font with an edge around the text.</summary>
        /// <param name="color">The color, or alpha 0 for none.</param>
        /// <param name="width">The width, or <see cref="StyleUnit.Default"/> for 1/16 em.</param>
        /// <param name="softness">How soft the edge is, as <see cref="TextEdge.Softness"/>.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
        /// <remarks>Null arguments keep the current edge values, or default to a black edge when none is active.</remarks>
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
        /// <param name="unit">The unit of <paramref name="offset"/>, or <see langword="null"/> to preserve the current
        /// shadow's unit when <paramref name="offset"/> is also <see langword="null"/>, and <see cref="StyleUnit.Em"/>
        /// otherwise.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
        public static ImPopFontDisposable PushTextShadow(
            ImTextColor? color = null, Vector2? offset = null, StyleUnit? unit = null)
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
                        unit ?? (offset is null && on ? current.Unit : StyleUnit.Em))));
        }

        /// <summary>Pushes the current font with <see cref="TextStyle.Background"/> set.</summary>
        /// <param name="color">The color, or alpha 0 for none.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
        public static ImPopFontDisposable PushTextBackground(ImTextColor color) =>
            PushChange(TextStyleRange.Background(0, 0, color));

        /// <summary>Pushes the current font with lines under, over, or through the text.</summary>
        /// <param name="positions">Where the lines are, or <see cref="TextLinePosition.None"/> for none.</param>
        /// <param name="style">How the lines are drawn, or <see cref="TextLineStyle.None"/> for none.</param>
        /// <param name="color">The color, or 0 for the color of the text.</param>
        /// <param name="thickness">The thickness in screen pixels, or 0 for the font's default thickness.</param>
        /// <param name="count">The number of lines at each position.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
        /// <remarks>Null arguments keep the current line values, except when no lines are active, where null
        /// <paramref name="style"/> defaults to <see cref="TextLineStyle.Solid"/> and null <paramref name="count"/>
        /// defaults to <c>1</c>. Setting only style or color has no visible effect without
        /// <paramref name="positions"/>.</remarks>
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
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
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

        /// <summary>Pushes the current font with <see cref="TextStyle.ExtraDecoration"/> set, or cleared when its ID matches.</summary>
        /// <param name="decoration">The decoration to apply, or whose ID to clear.</param>
        /// <param name="on"><see langword="true"/> to set <paramref name="decoration"/>, or <see langword="false"/> to
        /// clear a decoration with the same ID.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
        /// <remarks>Removing <see cref="TextDecorationId.Background"/> or <see cref="TextDecorationId.Line"/> also
        /// clears <see cref="TextStyle.Background"/> or <see cref="TextStyle.Lines"/>.</remarks>
        public static ImPopFontDisposable PushTextDecoration(TextDecoration decoration, bool on = true) =>
            !on
                ? PushDerived(ImGui.TextStyle.WithoutDecoration(decoration.Id))
                : PushChange(TextStyleRange.ExtraDecoration(0, 0, decoration));

        /// <summary>Pushes the bold form of the current font.</summary>
        /// <param name="bold">Whether bold.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
        /// <remarks>The font's bold faces where it has them, emboldened where not.</remarks>
        public static ImPopFontDisposable PushTextBold(bool bold = true) => PushChange(TextStyleRange.Bold(0, 0, bold));

        /// <summary>Pushes the italic form of the current font.</summary>
        /// <param name="italic">Whether italic.</param>
        /// <inheritdoc cref="PushFamily(IFontFamilyId, bool)" path="/returns"/>
        /// <remarks>The font's italic faces where it has them, slanted where not.</remarks>
        public static ImPopFontDisposable PushTextItalic(bool italic = true) =>
            PushChange(TextStyleRange.Italic(0, 0, italic));

        /// <summary>Pushes a scope in which text is parsed using <paramref name="markup"/>.</summary>
        /// <param name="markup">The markup format.</param>
        /// <param name="callback">Optional callback to push alongside the markup.</param>
        /// <param name="inherit">Whether callbacks pushed earlier remain active.</param>
        /// <returns>A disposable scope that restores the previous markup and callback stack.</returns>
        /// <remarks>Links in the label of a widget that takes the mouse are never hovered or clicked, as the widget
        /// is hovered first. Such widgets are
        /// <see cref="ImGui.Selectable(ImU8String, bool, ImGuiSelectableFlags, Vector2)"/>,
        /// <see cref="ImGui.TreeNodeEx(ImU8String, ImGuiTreeNodeFlags, ImU8String)"/> and
        /// <see cref="ImGui.Button(ImU8String, Vector2)"/>.</remarks>
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
        return new(font: true);
    }

    private static ImPopFontDisposable PushDerived(in TextStyle style)
    {
        Service<FontResolver>.Get().PushDerived(style);
        return new(font: true);
    }

    private static ImPopFontDisposable PushLinesAlso(TextLinePosition position, ImTextColor? color, bool on)
    {
        var positions = ImGui.TextStyle.Lines.Positions;
        return ImGui.PushTextLines(on ? positions | position : positions & ~position, color: color);
    }
}

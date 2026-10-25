using System.Collections.Immutable;
using System.Globalization;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>A font family, its size, and how text is drawn with it. Equal styles share one font.</summary>
/// <remarks><ul>
/// <li>Values that are off are stored as their defaults, so that styles that look the same are equal.</li>
/// <li><see cref="Size"/> is the size of the glyphs, as <see cref="SizeBasis"/> measures it. ImGui's font size, such
/// as <see cref="ImGui.GetFontSize"/>, is the line box instead, as <see cref="LineHeight"/> sets it.</li>
/// <li>The default value is the default font at the current size, with no effects.</li>
/// </ul></remarks>
/// <example><code>
/// using (TextStyle.Mono.Push())
///     ImGui.Text("Monospaced"u8);
/// using ((ImGui.TextStyle with { Bold = true, Size = StyleLength.Em(1.5f) }).Push())
///     ImGui.Text("Larger and bold"u8);
/// </code></example>
[JsonConverter(typeof(TextStyleJsonConverter))]
public partial record struct TextStyle
{
    private static readonly DalamudAssetFontId FontAwesome = new(DalamudAsset.FontAwesomeFreeSolid);

    private EquatableArray<FontFeature> features;

    /// <summary>Gets the default font at the current size.</summary>
    public static TextStyle Default => default;

    /// <summary>Gets the <see cref="FontAwesomeIcon"/> font, with each icon advancing 1 em.</summary>
    public static TextStyle Icon { get; } =
        new() { FamilyId = new ChainFamilyId([new(FontAwesome) { Advance = StyleLength.Em(1) }]) };

    /// <summary>Gets the <see cref="FontAwesomeIcon"/> font, with each icon fit into a square of 1 em.</summary>
    public static TextStyle IconFixedWidth { get; } =
        new() { FamilyId = new ChainFamilyId([new(FontAwesome) { FitToSquare = true }]) };

    /// <summary>Gets the Dalamud default monospaced font, with all the system fallback fonts.</summary>
    public static TextStyle Mono { get; } = From(DalamudAsset.InconsolataRegular);

    /// <summary>Gets or sets the font family, or null for the default font.</summary>
    /// <remarks><see cref="DalamudDefaultFontId"/> is stored as null.</remarks>
    public IFontFamilyId? FamilyId
    {
        get;
        set => field = value is DalamudDefaultFontId ? null : value;
    }

    /// <summary>Gets or sets the size of the glyphs, as <see cref="SizeBasis"/> measures it.</summary>
    /// <remarks><see cref="StyleLength.Em"/> is a multiple of the current size, and <see cref="StyleUnit.Default"/> keeps
    /// the current size. Both take the current size as this style's <see cref="SizeBasis"/> measures it.</remarks>
    public StyleLength Size { get; set; }

    /// <summary>Gets or sets what <see cref="Size"/> measures.</summary>
    /// <remarks>Each style reads its own size with its own basis. It is not inherited.</remarks>
    public FontSizeBasis SizeBasis { get; set; }

    /// <summary>Gets or sets the height of a line.</summary>
    /// <remarks><see cref="StyleUnit.Default"/> inherits the line height of the current font. The default font has the
    /// natural line height of each font, unless its configured style sets one. <see cref="StyleLength.Em"/> is a
    /// multiple of what <see cref="Size"/> measures, like a unitless number in CSS. <see cref="StyleLength.Px"/> is a
    /// fixed height. A font pushed over this one with a default line height keeps the multiple or the fixed height.
    /// A value that is not positive and finite is stored as <see cref="StyleUnit.Default"/>.</remarks>
    public StyleLength LineHeight
    {
        get;
        set => field = value.Value is > 0 and < float.PositiveInfinity ? value : default;
    }

    /// <summary>Gets or sets the color of the text. Its alpha is multiplied with the alpha of the color the text is
    /// drawn with. 0 means to use the color the text is drawn with.</summary>
    public ImTextColor Color { get; set; }

    /// <summary>Gets or sets the shadow of the glyphs and their edge.</summary>
    public TextShadow Shadow
    {
        get;
        set => field = value.IsOn ? value : default;
    }

    /// <summary>Gets or sets the edge around the glyphs.</summary>
    public TextEdge Edge
    {
        get;
        set => field = value.IsOn ? value : default;
    }

    /// <summary>Gets or sets the color behind the text, over the height of its line, or alpha 0 for none.</summary>
    public ImTextColor Background
    {
        get;
        set => field = value.Alpha != 0 ? value : default;
    }

    /// <summary>Gets or sets the lines under, over, or through the text.</summary>
    /// <remarks>A count of 0 is stored as 1.</remarks>
    public TextLineDecoration Lines
    {
        get;
        set => field = value.IsOn ? value with { Count = Math.Max(value.Count, (byte)1) } : default;
    }

    /// <summary>Gets or sets one more decoration, drawn after <see cref="Background"/> and <see cref="Lines"/>.
    /// </summary>
    /// <remarks>It can be a built-in one, such as a wavy underline under the solid one of <see cref="Lines"/>, or one
    /// a plugin registered. To draw several, register one decoration that draws them all.</remarks>
    public TextDecoration ExtraDecoration
    {
        get;
        set => field = value.IsNone ? default : value;
    }

    /// <summary>Gets or sets a value indicating whether to use a bolder weight, faux where the font has none.
    /// </summary>
    /// <remarks>The weight is <see cref="Weight"/> or that of the face, made bolder as CSS does. Weights up to 300
    /// become 400, up to 500 become 700, and others become 900.</remarks>
    public bool Bold { get; set; }

    /// <summary>Gets or sets a value indicating whether to use italic faces, faux where the font has none.</summary>
    public bool Italic { get; set; }

    /// <summary>Gets or sets how the text is read.</summary>
    public TextMarkup Markup { get; set; }

    /// <summary>Gets or sets a value indicating whether to draw the glyphs in grayscale even where subpixel text is
    /// on.</summary>
    public bool NoSubpixel { get; set; }

    /// <summary>Gets or sets a value indicating whether to draw the glyphs without fitting them to the pixel grid.
    /// </summary>
    /// <remarks><ul>
    /// <li>Hinted glyphs are drawn as DirectWrite recommends for the font and size, which may be its bitmaps for
    /// small sizes. They are sharper at small sizes.</li>
    /// <li>Unhinted glyphs are drawn from their outlines, smoothed in both directions, without bitmaps. They keep their
    /// designed shapes and change size smoothly.</li>
    /// <li>The layout is the same either way.</li>
    /// </ul></remarks>
    public bool NoHinting { get; set; }

    /// <summary>Gets or sets the weight, from 1 (thinnest) to 1000 (heaviest), or 0 for that of the face.</summary>
    /// <remarks><see cref="FontStyle"/> has the named weights. Values out of range are clamped.</remarks>
    public int Weight
    {
        get;
        set => field = Math.Clamp(value, 0, 1000);
    }

    /// <summary>Gets or sets the stretch, from 1 (narrowest) to 9 (widest), or 0 for that of the face.</summary>
    /// <remarks>Values out of range are clamped.</remarks>
    public int Stretch
    {
        get;
        set => field = Math.Clamp(value, 0, 9);
    }

    /// <summary>Gets or sets the space added after each character.</summary>
    /// <remarks>A value that is not finite is stored as <see cref="StyleUnit.Default"/>.</remarks>
    public StyleLength LetterSpacing
    {
        get;
        set => field = float.IsFinite(value.Value) ? value : default;
    }

    /// <summary>Gets or sets the locale to shape text in, such as <c>"ja-JP"</c>, or null for that of the font.
    /// </summary>
    /// <remarks>An empty name is stored as null. Names are stored in the case .NET gives them.</remarks>
    /// <exception cref="CultureNotFoundException">If the value is not a predefined culture.</exception>
    public string? Locale
    {
        get;
        set => field = string.IsNullOrEmpty(value)
                           ? null
                           : CultureInfo.GetCultureInfo(value, predefinedOnly: true).Name;
    }

    /// <summary>Gets or sets the OpenType features to shape text with, or empty for those of the font.</summary>
    public ImmutableArray<FontFeature> Features
    {
        readonly get => this.features.Array;
        set => this.features = new(value);
    }

    /// <summary>Gets the part of the style that picks the faces of a family, as a key of
    /// <see cref="Internal.Sources.Font.WithStyle"/>.</summary>
    internal readonly TextStyle FaceKey =>
        new()
        {
            Weight = this.Weight,
            Italic = this.Italic,
            Bold = this.Bold,
            Stretch = this.Stretch,
            NoHinting = this.NoHinting,
            Locale = this.Locale,
            features = this.features,
        };

    /// <summary>Gets the font family, with <see cref="DalamudDefaultFontId"/> for the default font.</summary>
    internal readonly IFontFamilyId FamilyOrDefault => this.FamilyId ?? DalamudDefaultFontId.Instance;

    /// <summary>Returns a copy without the decorations of a kind.</summary>
    /// <param name="id">The kind. <see cref="TextDecorationId.Background"/> and
    /// <see cref="TextDecorationId.Line"/> also clear <see cref="Background"/> and <see cref="Lines"/>.</param>
    /// <returns>The copy.</returns>
    public readonly TextStyle WithoutDecoration(TextDecorationId id)
    {
        var style = this;
        if (id == TextDecorationId.Background)
            style.Background = default;
        else if (id == TextDecorationId.Line)
            style.Lines = default;
        if (style.ExtraDecoration.Id == id)
            style.ExtraDecoration = default;
        return style;
    }

    /// <summary>Returns a copy in the weight, slant, and stretch of a face.</summary>
    /// <param name="face">The style of the face.</param>
    /// <returns>The copy.</returns>
    internal readonly TextStyle WithFace(FontStyle face) =>
        this with { Weight = face.Weight, Italic = face.Italicized, Stretch = face.Stretch };

    /// <summary>Returns a copy with the fields that change the layout taken from another style.</summary>
    /// <param name="layout">The style to take them from.</param>
    /// <returns>The copy.</returns>
    internal readonly TextStyle WithLayoutOf(in TextStyle layout) =>
        this with
        {
            FamilyId = layout.FamilyId,
            Size = layout.Size,
            SizeBasis = layout.SizeBasis,
            LineHeight = layout.LineHeight,
            Bold = layout.Bold,
            Italic = layout.Italic,
            Markup = layout.Markup,
            LetterSpacing = layout.LetterSpacing,
            Weight = layout.Weight,
            Stretch = layout.Stretch,
            Locale = layout.Locale,
            features = layout.features,
        };

    /// <summary>Gets the style of a face drawn in this style.</summary>
    /// <param name="face">The style of the face.</param>
    /// <returns>The style, with every part specified.</returns>
    internal readonly FontStyle On(FontStyle face)
    {
        var weight = this.Weight != 0 ? this.Weight : face.EffectiveWeight;
        return new(
            !this.Bold ? weight : weight < 350 ? 400 : weight < 550 ? 700 : Math.Max(weight, 900),
            this.Italic || face.Italicized,
            this.Stretch != 0 ? this.Stretch : face.EffectiveStretch);
    }
}

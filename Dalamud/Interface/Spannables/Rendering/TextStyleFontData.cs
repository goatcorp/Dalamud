using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Spannables.Text;
using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Rendering;

/// <summary>Font related values calculated from <see cref="TextStyle"/>.</summary>
public readonly ref struct TextStyleFontData
{
    /// <summary>The fake bold divisor, for scaling the thickness to the font size.</summary>
    public const float FakeBoldDivisor = 18f;

    /// <summary>The fake italic divisor, for scaling the top displacement to the font size.</summary>
    public const float FakeItalicDivisor = 6f;

    /// <summary>The font being contained.</summary>
    public readonly ImFontPtr Font;

    /// <summary>The scaled font size.</summary>
    public readonly float ScaledFontSize;

    /// <summary>The ascent(-) and descent(+) of the line.</summary>
    public readonly Vector2 BBoxVertical;

    /// <summary>The scaled horizontal offset.</summary>
    public readonly float ScaledHorizontalOffset;

    /// <summary>The additional glyph width introduced by faux bold.</summary>
    public readonly int BoldExtraWidth;

    /// <summary>The scaled text decoration thickness.</summary>
    public readonly float ScaledTextDecorationThickness;

    /// <summary>The scale, relative to <see cref="ImFontPtr.FontSize"/> of <see cref="Font"/>.</summary>
    public readonly float Scale;

    /// <summary>The render scale.</summary>
    public readonly float RenderScale;

    /// <summary>Whether kerning is enabled.</summary>
    private readonly bool useKern;

    /// <summary>Whether fake italic is being used.</summary>
    private readonly bool fakeItalic;

    /// <summary>Whether fake bold is being used.</summary>
    private readonly bool fakeBold;

    /// <summary>The glyphs from fonts.</summary>
    private readonly ReadOnlySpan<ImGuiHelpers.ImFontGlyphReal> glyphs;

    /// <summary>The lookup table from fonts.</summary>
    private readonly ReadOnlySpan<ushort> lookup;

    /// <summary>Initializes a new instance of the <see cref="TextStyleFontData"/> struct.</summary>
    /// <param name="renderScale">The scale applicable for everything.</param>
    /// <param name="style">The span style.</param>
    /// <param name="font">The resolved font.</param>
    /// <param name="intendedFontSize">The intended font size.</param>
    /// <param name="fakeItalic">Whether to use faux italics.</param>
    /// <param name="fakeBold">Whether to use faux bold.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal unsafe TextStyleFontData(
        float renderScale,
        scoped in TextStyle style,
        ImFontPtr font,
        float intendedFontSize,
        bool fakeItalic,
        bool fakeBold)
    {
        this.RenderScale = renderScale;
        this.Font = font;
        this.fakeBold = fakeBold;
        this.fakeItalic = fakeItalic;
        this.useKern = (ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.NoKerning) == 0;

        this.glyphs = new((void*)font.NativePtr->Glyphs.Data, font.NativePtr->Glyphs.Size);
        this.lookup = new((void*)font.NativePtr->IndexLookup.Data, font.NativePtr->IndexLookup.Size);

        this.ScaledFontSize = intendedFontSize;
        this.Scale = this.ScaledFontSize / font.FontSize;

        this.BBoxVertical = new Vector2(-font.Ascent, font.Descent) * this.Scale;
        if (style.LineHeight > 0)
            this.BBoxVertical *= style.LineHeight;

        this.ScaledHorizontalOffset =
            MathF.Round(this.ScaledFontSize * style.HorizontalOffset * renderScale) / renderScale;

        this.BoldExtraWidth =
            this.fakeBold
                ? (int)(MathF.Ceiling((this.ScaledFontSize / FakeBoldDivisor) * renderScale) / renderScale) - 1
                : 0;

        this.ScaledTextDecorationThickness =
            style.TextDecorationThickness > 0
                ? Math.Max(
                    1,
                    MathF.Round(this.ScaledFontSize * style.TextDecorationThickness * renderScale) / renderScale)
                : 0;
    }

    /// <summary>Gets a vector representing the difference of X offset per every Y offset difference.</summary>
    public Vector2 SlopeVector2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this.fakeItalic ? 1f / FakeItalicDivisor : 0f, 1f);
    }

    /// <summary>Gets the effective codepoint, handling the unsupported characters.</summary>
    /// <param name="c">The codepoint.</param>
    /// <returns>The effective codepoint.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEffeciveCodepoint(int c) =>
        c < 0 || c >= this.lookup.Length || this.lookup[c] == ushort.MaxValue
            ? this.Font.FallbackChar
            : c;

    /// <summary>Gets the effectively glyph.</summary>
    /// <param name="c">The codepoint.</param>
    /// <returns>The effective glyph.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly ImGuiHelpers.ImFontGlyphReal GetEffectiveGlyph(int c) =>
        ref this.glyphs[this.lookup[this.GetEffeciveCodepoint(c)]];

    /// <summary>Gets the skew distance at the top.</summary>
    /// <param name="xy0">The XY0 offset of the glyph, relative to the glyph origin.</param>
    /// <returns>The skew distance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetScaledTopSkew(Vector2 xy0) =>
        this.fakeItalic ? (this.ScaledFontSize - xy0.Y) / FakeItalicDivisor : 0f;

    /// <summary>Gets the scaled distance between two characters from kerning.</summary>
    /// <param name="last">The previous codepoint.</param>
    /// <param name="current">The current codepoint.</param>
    /// <returns>The scaled distance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe float GetScaledGap(int last, int current)
    {
        // Both Left and Current must be in UInt16 range. This &-test tests for both parameters at once.
        if (!this.useKern || ((last | current) & 0xFFFF0000) != 0)
            return 0;

        var gap = ImGuiNative.ImFont_GetDistanceAdjustmentForPair(
            this.Font.NativePtr,
            (ushort)last,
            (ushort)current);
        return gap * this.Scale;
    }

    /// <summary>Calculates tab size.</summary>
    /// <param name="tabWidthValue">The TabWidth value.</param>
    /// <returns>The calculated tab size.</returns>
    /// <remarks>See <see cref="TextSpannableBase.Options.TabWidth"/>.</remarks>
    public float CalculateTabSize(float tabWidthValue) => tabWidthValue switch
    {
        0 => this.GetEffectiveGlyph(' ').AdvanceX,
        < 0 => this.GetEffectiveGlyph(' ').AdvanceX * -tabWidthValue * this.Scale,
        _ => tabWidthValue * this.Scale,
    };
}

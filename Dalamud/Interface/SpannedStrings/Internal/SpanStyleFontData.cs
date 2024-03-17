using System.Numerics;

using Dalamud.Interface.SpannedStrings.Styles;
using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings.Internal;

/// <summary>Font related values calculated from <see cref="SpanStyle"/>.</summary>
internal ref struct SpanStyleFontData
{
    /// <summary>Whether kerning is enabled.</summary>
    public readonly bool UseKern;

    /// <summary>The font being contained.</summary>
    public ImFontPtr Font;

    /// <summary>Whether fake italic is being used.</summary>
    public bool FakeItalic;

    /// <summary>The scaled font size.</summary>
    public float ScaledFontSize;

    /// <summary>The ascent(-) and descent(+) of the line.</summary>
    public Vector2 BBoxVertical;

    /// <summary>The hot data from fonts.</summary>
    public ImVectorWrapper<ImGuiHelpers.ImFontGlyphHotDataReal> HotData;

    /// <summary>The glyphs from fonts.</summary>
    public ImVectorWrapper<ImGuiHelpers.ImFontGlyphReal> Glyphs;

    /// <summary>The lookup table from fonts.</summary>
    public ImVectorWrapper<ushort> Lookup;

    /// <summary>The scaled horizontal offset.</summary>
    public float ScaledHorizontalOffset;

    /// <summary>The additional glyph width introduced by faux bold.</summary>
    public int BoldExtraWidth;

    /// <summary>The scale, relative to <see cref="ImFontPtr.FontSize"/> of <see cref="Font"/>.</summary>
    public float Scale;

    private const int FakeBoldDivisor = 18;
    private const int FakeItalicDivisor = 6;

    private readonly float renderScale;

    /// <summary>Initializes a new instance of the <see cref="SpanStyleFontData"/> struct.</summary>
    /// <param name="renderScale">The scale applicable for everything.</param>
    /// <remarks>Values will be mostly empty, until the first call to <see cref="Update"/>.</remarks>
    public SpanStyleFontData(float renderScale)
    {
        this.renderScale = renderScale;
        this.UseKern = (ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.NoKerning) == 0;
    }

    /// <summary>Gets the effective codepoint, handling the unsupported characters.</summary>
    /// <param name="c">The codepoint.</param>
    /// <returns>The effective codepoint.</returns>
    public readonly int GetEffeciveCodepoint(int c) =>
        c >= this.HotData.Length || c >= this.Lookup.Length || this.Lookup[c] == ushort.MaxValue
            ? this.Font.FallbackChar
            : c;

    /// <summary>Gets the skew distance at the top.</summary>
    /// <param name="xy0">The XY0 offset of the glyph, relative to the glyph origin.</param>
    /// <returns>The skew distance.</returns>
    public readonly float GetScaledTopSkew(Vector2 xy0) =>
        this.FakeItalic ? (this.ScaledFontSize - xy0.Y) / FakeItalicDivisor : 0f;

    /// <summary>Gets the scaled distance between two characters from kerning.</summary>
    /// <param name="last">The previous codepoint.</param>
    /// <param name="current">The current codepoint.</param>
    /// <returns>The scaled distance.</returns>
    public readonly unsafe float GetScaledGap(int last, int current)
    {
        if (!this.UseKern
            || last is < 0 or > ushort.MaxValue
            || current is < 0 or > ushort.MaxValue)
            return 0;

        var gap = ImGuiNative.ImFont_GetDistanceAdjustmentForPair(
            this.Font.NativePtr,
            (ushort)last,
            (ushort)current);
        return gap * this.Scale;
    }

    /// <summary>Updates the values from the specified span style.</summary>
    /// <param name="style">The span style.</param>
    public void Update(in SpanStyle style)
    {
        // This function has to be called from the main thread, and the current contract requires that once a font
        // has been pushed, it must stay alive until the end of ImGui render.
        // ImGui.GetFont() will stay alive for the duration we need it to, even if we dispose this here.
        using var fontPopDisposable = style.Font.PushEffectiveFont(
            style.Italic,
            style.Bold,
            out this.FakeItalic,
            out var fakeBold);

        this.Font = ImGui.GetFont();
        this.HotData = this.Font.IndexedHotDataWrapped();
        this.Glyphs = this.Font.GlyphsWrapped();
        this.Lookup = this.Font.IndexLookupWrapped();

        this.ScaledFontSize = style.FontSize;
        if (this.ScaledFontSize <= 0)
            this.ScaledFontSize = this.Font.FontSize;
        this.ScaledFontSize *= this.renderScale;
        this.Scale = this.ScaledFontSize / this.Font.FontSize;

        this.BBoxVertical = new Vector2(-this.Font.Ascent, this.Font.Descent) * this.Scale;
        if (style.LineHeight > 0)
            this.BBoxVertical *= style.LineHeight;

        this.ScaledHorizontalOffset = MathF.Round(this.ScaledFontSize * style.HorizontalOffset);

        this.BoldExtraWidth = fakeBold ? (int)MathF.Ceiling(this.ScaledFontSize / FakeBoldDivisor) - 1 : 0;
    }
}

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;

using ImGuiNET;

using Newtonsoft.Json;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents a user's choice of a single font.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1206:Declaration keywords should follow order",
    Justification = "public required")]
public record SingleFontSpec : IFontSpec
{
    /// <summary>
    /// Gets the font id.
    /// </summary>
    [JsonProperty]
    public required IFontId FontId { get; init; }

    /// <inheritdoc/>
    [JsonProperty]
    public float SizePx { get; init; } = 16;

    /// <inheritdoc/>
    [JsonIgnore]
    public float SizePt
    {
        get => (this.SizePx * 3) / 4;
        init => this.SizePx = (value * 4) / 3;
    }

    /// <inheritdoc/>
    [JsonIgnore]
    public float LineHeightPx => MathF.Round(this.SizePx * this.LineHeight);

    /// <summary>
    /// Gets the line height ratio to the font size.
    /// </summary>
    [JsonProperty]
    public float LineHeight { get; init; } = 1f;

    /// <summary>
    /// Gets the glyph offset in pixels.
    /// </summary>
    [JsonProperty]
    public Vector2 GlyphOffset { get; init; }

    /// <summary>
    /// Gets the glyph extra spacing in pixels.
    /// </summary>
    [JsonProperty]
    public Vector2 GlyphExtraSpacing { get; init; }

    /// <summary>
    /// Gets the glyph ranges.
    /// </summary>
    [JsonProperty]
    public ushort[]? GlyphRanges { get; init; }

    /// <inheritdoc/>
    public IFontHandle CreateFontHandle(IFontAtlas atlas, FontAtlasBuildStepDelegate? callback = null) =>
        atlas.NewDelegateFontHandle(tk =>
        {
            tk.OnPreBuild(e => e.Font = this.AddToBuildToolkit(e));
            callback?.Invoke(tk);
        });

    /// <inheritdoc/>
    public ImFontPtr AddToBuildToolkit(IFontAtlasBuildToolkitPreBuild tk, ImFontPtr mergeFont = default)
    {
        var font = this.FontId.AddToBuildToolkit(
            tk,
            new()
            {
                SizePx = this.SizePx,
                GlyphRanges = this.GlyphRanges,
                MergeFont = mergeFont,
            });

        tk.RegisterPostBuild(
            () =>
            {
                var roundUnit = tk.IsGlobalScaleIgnored(font) ? 1 : 1 / tk.Scale;
                var newAscent = MathF.Round((font.Ascent * this.LineHeight) / roundUnit) * roundUnit;
                var newFontSize = MathF.Round((font.FontSize * this.LineHeight) / roundUnit) * roundUnit;
                var shiftDown = MathF.Round((newFontSize - font.FontSize) / 2f / roundUnit) * roundUnit;

                font.Ascent = newAscent;
                font.FontSize = newFontSize;
                font.Descent = newFontSize - font.Ascent;

                var lookup = new BitArray(ushort.MaxValue + 1, this.GlyphRanges is null);
                if (this.GlyphRanges is not null)
                {
                    for (var i = 0; i < this.GlyphRanges.Length && this.GlyphRanges[i] != 0; i += 2)
                    {
                        var to = (int)this.GlyphRanges[i + 1];
                        for (var j = this.GlyphRanges[i]; j <= to; j++)
                            lookup[j] = true;
                    }
                }

                // `/ roundUnit` = `* scale`
                var dax = MathF.Round(this.GlyphExtraSpacing.X / roundUnit / roundUnit) * roundUnit;
                var dxy0 = this.GlyphOffset / roundUnit;

                dxy0 /= roundUnit;
                dxy0.X = MathF.Round(dxy0.X);
                dxy0.Y = MathF.Round(dxy0.Y);
                dxy0 *= roundUnit;

                dxy0.Y += shiftDown;
                var dxy = new Vector4(dxy0, dxy0.X, dxy0.Y);
                foreach (ref var glyphReal in font.GlyphsWrapped().DataSpan)
                {
                    if (!lookup[glyphReal.Codepoint])
                        continue;

                    glyphReal.XY += dxy;
                    glyphReal.AdvanceX += dax;
                }
            });

        return font;
    }
}

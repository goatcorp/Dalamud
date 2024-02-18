using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

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
    /// Gets the letter spacing in pixels.
    /// </summary>
    [JsonProperty]
    public float LetterSpacing { get; init; }

    /// <summary>
    /// Gets the glyph ranges.
    /// </summary>
    [JsonProperty]
    public ushort[]? GlyphRanges { get; init; }

    /// <inheritdoc/>
    public string ToLocalizedString(string localeCode)
    {
        var sb = new StringBuilder();
        sb.Append(this.FontId.Family.GetLocalizedName(localeCode));
        sb.Append($"({this.FontId.GetLocalizedName(localeCode)}, {this.SizePt}pt");
        if (Math.Abs(this.LineHeight - 1f) > 0.000001f)
            sb.Append($", LH={this.LineHeight:0.##}");
        if (this.GlyphOffset != default)
            sb.Append($", O={this.GlyphOffset.X:0.##},{this.GlyphOffset.Y:0.##}");
        if (this.LetterSpacing != 0f)
            sb.Append($", LS={this.LetterSpacing:0.##}");
        sb.Append(')');
        return sb.ToString();
    }

    /// <inheritdoc/>
    public override string ToString() => this.ToLocalizedString("en");

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
                // Multiplication by scale will be done with global scale, outside of this handling.
                var scale = tk.GetFontScaleMode(font) == FontScaleMode.UndoGlobalScale ? 1 / tk.Scale : 1;
                var roundUnit = tk.GetFontScaleMode(font) == FontScaleMode.SkipHandling ? 1 : 1 / tk.Scale;
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

                var dax = MathF.Round((this.LetterSpacing * scale) / roundUnit) * roundUnit;
                var dxy0 = this.GlyphOffset * scale;
                dxy0 /= roundUnit;
                dxy0 = new(MathF.Round(dxy0.X), MathF.Round(dxy0.Y));
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

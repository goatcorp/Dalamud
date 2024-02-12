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
                GlyphOffset = this.GlyphOffset,
                GlyphExtraSpacing = this.GlyphExtraSpacing,
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
                if (shiftDown != 0f)
                {
                    foreach (ref var glyphReal in font.GlyphsWrapped().DataSpan)
                    {
                        glyphReal.Y0 += shiftDown;
                        glyphReal.Y1 += shiftDown;
                    }
                }
            });

        return font;
    }
}

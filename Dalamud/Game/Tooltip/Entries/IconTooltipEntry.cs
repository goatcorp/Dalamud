using Dalamud.Game.Tooltip.StyleOptions;

using FFXIVClientStructs.FFXIV.Common.Math;

namespace Dalamud.Game.Tooltip.Entries;

/// <summary>
/// Object representing an icon tooltip entry.
/// </summary>
internal class IconTooltipEntry : TooltipEntry
{
    /// <summary>
    /// Gets or sets the iconId.
    /// </summary>
    public uint IconId { get; set; }

    /// <summary>
    /// Gets or sets the image size.
    /// </summary>
    public Vector2 Size { get; set; }

    /// <summary>
    /// Gets or sets the image node options.
    /// </summary>
    public ImageOptions Options { get; set; } = new();
}

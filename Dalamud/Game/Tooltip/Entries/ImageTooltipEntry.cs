using Dalamud.Game.Tooltip.StyleOptions;

using FFXIVClientStructs.FFXIV.Common.Math;

namespace Dalamud.Game.Tooltip.Entries;

/// <summary>
/// Internal class representing the data from a plugin to add an image entry.
/// </summary>
internal class ImageTooltipEntry : TooltipEntry
{
    /// <summary>
    /// Gets or sets the image path.
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the image size.
    /// </summary>
    public Vector2 Size { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the image node options.
    /// </summary>
    public ImageOptions Options { get; set; } = new();
}

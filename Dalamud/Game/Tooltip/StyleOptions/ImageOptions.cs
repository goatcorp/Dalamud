using Dalamud.Game.Tooltip.TooltipArgTypes;
using Dalamud.NativeUi.Nodes;

using FFXIVClientStructs.FFXIV.Common.Math;

namespace Dalamud.Game.Tooltip.StyleOptions;

/// <summary>
/// Object representing various properties of a <see cref="ImageNode"/> for use with <see cref="TooltipArgs.AddImage"/>.
/// </summary>
public class ImageOptions
{
    /// <summary>
    /// Gets or sets the color value of the image. Default is Vector4.One.
    /// </summary>
    /// <remarks>
    /// Expects values between 0.0f and 1.0f.
    /// </remarks>
    public Vector4 Color { get; set; } = Vector4.One;
}

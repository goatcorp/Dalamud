using System.Numerics;

using Dalamud.NativeUi.Extensions;

using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// Conversion class used to convert between a managed object and <see cref="AtkTimelineNodeTint"/>
/// </summary>
/// <remarks>
/// There's some nonsense with how the conversion has to happen that this just solves.
/// </remarks>
internal class NodeTint
{
    /// <summary>
    /// Gets or sets add color.
    /// </summary>
    public Vector3 AddColor { get; set; }

    /// <summary>
    /// Gets or sets multiply color.
    /// </summary>
    public Vector3 MultiplyColor { get; set; }

    /// <summary>
    /// Converts a NodeTint into a usable timeline node tint.
    /// </summary>
    /// <param name="tint">Tint to convert.</param>
    public static implicit operator AtkTimelineNodeTint(NodeTint tint) => new()
    {
        MultiplyRGB = new ByteColor
        {
            R = (byte)tint.MultiplyColor.X, G = (byte)tint.MultiplyColor.Y, B = (byte)tint.MultiplyColor.Z,
        },
        AddRGBBitfield = Convert(tint.AddColor),
    };

    /// <summary>
    /// Converts a timeline node tint into a managed node tint that can be edited.
    /// </summary>
    /// <param name="tint">Tint to convert.</param>
    public static implicit operator NodeTint(AtkTimelineNodeTint tint) => new()
    {
        AddColor = new Vector3(tint.AddR, tint.AddG, tint.AddB), MultiplyColor = tint.MultiplyRGB.ToVector4().AsVector3(),
    };

    private static uint Convert(Vector3 color)
    {
        var red = (short)(color.X + 255);
        var green = (short)(color.Y + 255);
        var blue = (short)(color.Z + 255);

        return (uint)((red & 0x3FF) | ((green & 0xFFF) << 10) | ((blue & 0x3FF) << 22));
    }
}

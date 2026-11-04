using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.SeStrings.Formats;

/// <summary>An entry of a game bitmap font icon table file (.gfd).</summary>
/// <remarks>Positions are on a 512x1024 grid over the icon texture. The icons fill its top third. The HQ icons, at
/// twice the scale, fill the rest, from row 341.</remarks>
[StructLayout(LayoutKind.Sequential, Size = 0x10)]
internal struct GfdEntry
{
    /// <summary>The width of the grid the positions are on.</summary>
    public const float GridWidth = 512f;

    /// <summary>The height of the grid the positions are on.</summary>
    public const float GridHeight = 1024f;

    /// <summary>The row where the HQ icons start, on the half-scale grid of the HQ icons.</summary>
    public const float HqTop = 170.5f;

    /// <summary>ID of the entry.</summary>
    public ushort Id;

    /// <summary>The left offset of the entry.</summary>
    public ushort Left;

    /// <summary>The top offset of the entry.</summary>
    public ushort Top;

    /// <summary>The width of the entry.</summary>
    public ushort Width;

    /// <summary>The height of the entry.</summary>
    public ushort Height;

    /// <summary>Unknown/unused.</summary>
    public ushort Unk0A;

    /// <summary>The ID of the entry to draw instead, or 0.</summary>
    public ushort Redirect;

    /// <summary>Unknown/unused.</summary>
    public ushort Unk0E;

    /// <summary>Gets a value indicating whether this entry is effectively empty.</summary>
    public readonly bool IsEmpty => this.Width == 0 || this.Height == 0;

    /// <summary>Gets the UV0 of this entry.</summary>
    public readonly Vector2 Uv0 => new(this.Left / GridWidth, this.Top / GridHeight);

    /// <summary>Gets the UV1 of this entry.</summary>
    public readonly Vector2 Uv1 => new((this.Left + this.Width) / GridWidth, (this.Top + this.Height) / GridHeight);

    /// <summary>Gets the UV0 of the HQ version of this entry.</summary>
    public readonly Vector2 HqUv0 => new(this.Left / (GridWidth / 2), (this.Top + HqTop) / (GridHeight / 2));

    /// <summary>Gets the UV1 of the HQ version of this entry.</summary>
    public readonly Vector2 HqUv1 =>
        new(
            (this.Left + this.Width) / (GridWidth / 2),
            (this.Top + this.Height + HqTop) / (GridHeight / 2));

    /// <summary>Calculates the size in pixels of a GFD entry when drawn along with a text.</summary>
    /// <param name="fontSize">Font size in pixels.</param>
    /// <param name="useHq">Receives whether to draw the HQ texture.</param>
    /// <returns>Determined size of the GFD entry when drawn.</returns>
    /// <remarks>The entry must not be empty.</remarks>
    public readonly Vector2 CalculateScaledSize(float fontSize, out bool useHq)
    {
        useHq = fontSize > 19;
        var targetHeight = useHq ? fontSize : 20;
        return new(this.Width * (targetHeight / this.Height), targetHeight);
    }
}

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts;

/// <summary>Lines under, over, or through a text, in one style.</summary>
/// <param name="Positions">Where the lines are, or <see cref="TextLinePosition.None"/> for no lines.</param>
/// <param name="Style">How the lines are drawn, or <see cref="TextLineStyle.None"/> for no lines.</param>
/// <param name="Color">The color, or 0 for the color of the text.</param>
/// <param name="Thickness">The thickness of each line in pixels, or 0 for what the font says.</param>
/// <param name="Count">The number of lines at each position. 0 also means 1.</param>
/// <seealso cref="TextStyle.Lines"/>
public readonly record struct TextLineDecoration(
    TextLinePosition Positions,
    TextLineStyle Style = TextLineStyle.Solid,
    ImTextColor Color = default,
    float Thickness = 0,
    byte Count = 1)
{
    /// <summary>Gets a value indicating whether there are lines.</summary>
    public bool IsOn => this.Positions != TextLinePosition.None && this.Style != TextLineStyle.None;

    public static implicit operator TextDecoration(in TextLineDecoration d) => d.ToDecoration();

    /// <summary>Reads the lines of a decoration.</summary>
    /// <param name="decoration">The decoration.</param>
    /// <param name="lines">The lines.</param>
    /// <returns>Whether it is a <see cref="TextDecorationId.Line"/>.</returns>
    public static bool TryRead(in TextDecoration decoration, out TextLineDecoration lines)
    {
        if (decoration.Id != TextDecorationId.Line)
        {
            lines = default;
            return false;
        }

        var data = decoration.Data;
        lines = new(
            (TextLinePosition)(byte)data,
            (TextLineStyle)(byte)(data >> 8),
            decoration.Context,
            BitConverter.UInt32BitsToSingle((uint)(data >> 32)),
            (byte)(data >> 16));
        return true;
    }

    /// <summary>Gets the decoration of these lines, such as for <see cref="TextStyle.ExtraDecoration"/>.</summary>
    /// <returns>The decoration, of <see cref="TextDecorationId.Line"/>.</returns>
    public TextDecoration ToDecoration() =>
        new(
            TextDecorationId.Line,
            this.Color,
            (byte)this.Positions |
            ((ulong)this.Style << 8) |
            ((ulong)Math.Max(this.Count, (byte)1) << 16) |
            ((ulong)BitConverter.SingleToUInt32Bits(this.Thickness) << 32));
}

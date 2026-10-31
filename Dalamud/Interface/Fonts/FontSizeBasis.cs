namespace Dalamud.Interface.Fonts;

/// <summary>What <see cref="TextStyle.Size"/> measures.</summary>
public enum FontSizeBasis : byte
{
    /// <summary><see cref="EmBox"/> for game fonts, and <see cref="LineBox"/> for others.</summary>
    Auto,

    /// <summary>The line box at the natural line height of the font.</summary>
    LineBox,

    /// <summary>The em, as CSS's <c>font-size</c>.</summary>
    EmBox,
}

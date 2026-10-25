using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts;

/// <summary>An edge around the glyphs of a text.</summary>
/// <param name="Color">The color, or alpha 0 for no edge.</param>
/// <param name="Width">The width, or <see cref="StyleUnit.Default"/> for <see cref="DefaultWidth"/> ems.</param>
/// <remarks>The glyph shader draws edges up to 8 texels of the glyph's bitmap wide.</remarks>
/// <seealso cref="TextStyle.Edge"/>
public readonly record struct TextEdge(ImTextColor Color, StyleLength Width = default)
{
    /// <summary>The default width, in ems.</summary>
    public const float DefaultWidth = 1 / 16f;

    /// <summary>Gets how soft the edge is, or 0 for a hard edge.</summary>
    /// <remarks><ul>
    /// <li>Up to 1, it is the fraction of the width the edge fades over, ending at the width.</li>
    /// <li>Above 1, the edge fades from the glyph out to the width times it, within what the shader draws.</li>
    /// </ul></remarks>
    public float Softness { get; init; }

    /// <summary>Gets a value indicating whether there is an edge.</summary>
    public bool IsOn => this.Color.Alpha != 0 && (this.Width.Unit == StyleUnit.Default || this.Width.Value > 0);

    /// <summary>Gets the width in pixels.</summary>
    /// <param name="emPx">The em in pixels.</param>
    /// <param name="globalScale">The global scale.</param>
    /// <returns>The width, or 0 if there is no edge.</returns>
    internal float WidthAt(float emPx, float globalScale) =>
        this.IsOn ? StyleLength.Resolve(this.Width, emPx, globalScale, DefaultWidth * emPx) : 0;
}

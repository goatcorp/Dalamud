using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts;

/// <summary>A shadow under the glyphs and edge of a text.</summary>
/// <param name="Color">The color, or alpha 0 for no shadow.</param>
/// <param name="Offset">The offset, or 0 or a value that is not finite for no shadow.</param>
/// <param name="Unit">The unit of <paramref name="Offset"/>. <see cref="StyleUnit.Default"/> is ems.</param>
/// <seealso cref="TextStyle.Shadow"/>
public readonly record struct TextShadow(ImTextColor Color, Vector2 Offset, StyleUnit Unit = StyleUnit.Em)
{
    /// <summary>Initializes a new instance of the <see cref="TextShadow"/> struct, offset by
    /// <see cref="DefaultOffset"/> ems.</summary>
    /// <param name="color">The color, or alpha 0 for no shadow.</param>
    public TextShadow(ImTextColor color)
        : this(color, DefaultOffset)
    {
    }

    /// <summary>Gets the default offset, in ems.</summary>
    public static Vector2 DefaultOffset => new(0, 1 / 16f);

    /// <summary>Gets a value indicating whether there is a shadow.</summary>
    public bool IsOn =>
        this.Color.Alpha != 0
        && this.Offset != Vector2.Zero
        && float.IsFinite(this.Offset.X)
        && float.IsFinite(this.Offset.Y);

    /// <summary>Gets the offset in pixels.</summary>
    /// <param name="emPx">The em in pixels.</param>
    /// <param name="globalScale">The global scale.</param>
    /// <returns>The offset.</returns>
    internal Vector2 OffsetAt(float emPx, float globalScale) =>
        this.Offset * (this.Unit == StyleUnit.Px ? globalScale : emPx);
}

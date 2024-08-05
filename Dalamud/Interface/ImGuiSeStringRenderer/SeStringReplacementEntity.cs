using System.Numerics;

namespace Dalamud.Interface.ImGuiSeStringRenderer;

/// <summary>Replacement entity to draw instead while rendering a SeString.</summary>
public readonly record struct SeStringReplacementEntity
{
    /// <summary>Initializes a new instance of the <see cref="SeStringReplacementEntity"/> struct.</summary>
    /// <param name="byteLength">Number of bytes taken by this entity. Must be at least 0. If <c>0</c>, then the entity
    /// is considered as empty.</param>
    /// <param name="size">Size of this entity in pixels. Components must be non-negative.</param>
    /// <param name="draw">Draw callback.</param>
    public SeStringReplacementEntity(int byteLength, Vector2 size, DrawDelegate draw)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteLength);
        ArgumentOutOfRangeException.ThrowIfNegative(size.X, nameof(size));
        ArgumentOutOfRangeException.ThrowIfNegative(size.Y, nameof(size));
        ArgumentNullException.ThrowIfNull(draw);
        this.ByteLength = byteLength;
        this.Size = size;
        this.Draw = draw;
    }

    /// <summary>Gets the replacement entity.</summary>
    /// <param name="state">Draw state.</param>
    /// <param name="byteOffset">Byte offset in <see cref="SeStringDrawState.Span"/>.</param>
    /// <returns>Replacement entity definition, or <c>default</c> if none.</returns>
    public delegate SeStringReplacementEntity GetEntityDelegate(scoped in SeStringDrawState state, int byteOffset);

    /// <summary>Draws the replacement entity.</summary>
    /// <param name="state">Draw state.</param>
    /// <param name="byteOffset">Byte offset in <see cref="SeStringDrawState.Span"/>.</param>
    /// <param name="offset">Relative offset in pixels w.r.t. <see cref="SeStringDrawState.ScreenOffset"/>.</param>
    public delegate void DrawDelegate(scoped in SeStringDrawState state, int byteOffset, Vector2 offset);

    /// <summary>Gets the number of bytes taken by this entity.</summary>
    public int ByteLength { get; init; }

    /// <summary>Gets the size of this entity in pixels.</summary>
    public Vector2 Size { get; init; }

    /// <summary>Gets the Draw callback.</summary>
    public DrawDelegate Draw { get; init; }

    /// <summary>Gets a value indicating whether this entity is empty.</summary>
    /// <param name="e">Instance of <see cref="SeStringReplacementEntity"/> to test.</param>
    public static implicit operator bool(in SeStringReplacementEntity e) => e.ByteLength != 0;
}

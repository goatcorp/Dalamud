using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal;

namespace Dalamud.Interface.Fonts;

/// <summary>Where an entity of styled text is.</summary>
public readonly ref struct TextEntityContext
{
    private readonly FontImpl font;

    /// <summary>Initializes a new instance of the <see cref="TextEntityContext"/> struct.</summary>
    /// <param name="markup">The markup of the text.</param>
    /// <param name="source">The text as read.</param>
    /// <param name="offset">The byte offset of the entity in <paramref name="source"/>.</param>
    /// <param name="font">The font of the style at the offset.</param>
    /// <param name="fontSize">The glyph size.</param>
    /// <param name="data">The data of the entity.</param>
    /// <param name="size">The size of the entity in pixels.</param>
    /// <param name="color">The color the text is drawn with.</param>
    internal TextEntityContext(
        TextMarkup markup, ReadOnlySpan<byte> source, int offset, FontImpl font, float fontSize, ulong data,
        Vector2 size, uint color)
    {
        this.Markup = markup;
        this.Source = source;
        this.Offset = offset;
        this.font = font;
        this.FontSize = fontSize;
        this.Data = data;
        this.Size = size;
        this.Color = color;
    }

    /// <summary>Gets the markup of the text.</summary>
    public TextMarkup Markup { get; }

    /// <summary>Gets the text as read.</summary>
    /// <remarks>For <see cref="TextMarkup.MacroString"/>, it is the compiled SeString.</remarks>
    public ReadOnlySpan<byte> Source { get; }

    /// <summary>Gets the byte offset in <see cref="Source"/>.</summary>
    public int Offset { get; }

    /// <summary>Gets the style at <see cref="Offset"/>.</summary>
    public ref readonly TextStyle Style => ref this.font.Style;

    /// <summary>Gets the font of <see cref="Style"/>.</summary>
    public unsafe ImFontPtr Font => this.font.Native;

    /// <summary>Gets the glyph size in pixels.</summary>
    public float FontSize { get; }

    /// <summary>Gets the <see cref="TextEntity.Data"/> of the entity being drawn.</summary>
    /// <remarks>It is 0 in <see cref="ITextMarkupCallback.TryGetEntity"/>, as there is no entity yet.</remarks>
    public ulong Data { get; }

    /// <summary>Gets the size in pixels of the entity being drawn.</summary>
    /// <remarks>A height relative to the line is in pixels here. It is zero in
    /// <see cref="ITextMarkupCallback.TryGetEntity"/>, as there is no entity yet.</remarks>
    /// <seealso cref="TextEntity.Size"/>
    public Vector2 Size { get; }

    /// <summary>Gets the color the text is drawn with.</summary>
    /// <remarks>Its alpha also applies to the colors of styles. Apply it to fade the entity with the text. It is 0 in
    /// <see cref="ITextMarkupCallback.TryGetEntity"/>, as nothing is drawn yet.</remarks>
    public uint Color { get; }
}

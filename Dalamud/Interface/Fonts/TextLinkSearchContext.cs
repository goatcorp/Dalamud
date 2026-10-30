namespace Dalamud.Interface.Fonts;

/// <summary>A piece of styled text to find links in.</summary>
/// <seealso cref="ITextMarkupCallback.FindLinks"/>
public readonly ref struct TextLinkSearchContext
{
    /// <summary>Initializes a new instance of the <see cref="TextLinkSearchContext"/> struct.</summary>
    /// <param name="markup">The markup of the text.</param>
    /// <param name="source">The text as read.</param>
    /// <param name="offset">The byte offset of the piece in <paramref name="source"/>.</param>
    /// <param name="length">The length of the piece in bytes.</param>
    internal TextLinkSearchContext(TextMarkup markup, ReadOnlySpan<byte> source, int offset, int length)
    {
        this.Markup = markup;
        this.Source = source;
        this.Offset = offset;
        this.Text = source.Slice(offset, length);
    }

    /// <summary>Gets the markup of the text.</summary>
    public TextMarkup Markup { get; }

    /// <summary>Gets the text as read.</summary>
    /// <remarks>For <see cref="TextMarkup.MacroString"/>, it is the compiled SeString.</remarks>
    public ReadOnlySpan<byte> Source { get; }

    /// <summary>Gets the byte offset of <see cref="Text"/> in <see cref="Source"/>.</summary>
    public int Offset { get; }

    /// <summary>Gets the piece of text to find links in, in UTF-8.</summary>
    /// <remarks>It is text between the payloads or macros of the markup, outside links the markup makes. For
    /// <see cref="TextMarkup.Plain"/>, it is the whole text.</remarks>
    public ReadOnlySpan<byte> Text { get; }
}

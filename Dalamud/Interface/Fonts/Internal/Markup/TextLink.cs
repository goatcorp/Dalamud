namespace Dalamud.Interface.Fonts.Internal.Markup;

/// <summary>A link in styled text, with byte offsets into <see cref="StyledText.Source"/>.</summary>
/// <param name="Offset">Byte offset in <see cref="StyledText.Source"/> of the opening link payload or matched link text.</param>
/// <param name="Length">Byte length in <see cref="StyledText.Source"/> of the opening link payload or matched link text.</param>
/// <param name="Target">The link target.</param>
/// <param name="Detected">Whether a callback found it with <see cref="ITextMarkupCallback.FindLinks"/>.</param>
/// <seealso cref="StyledText.Links"/>
internal readonly record struct TextLink(int Offset, int Length, ReadOnlyMemory<byte> Target, bool Detected = false)
{
    /// <summary>Makes the context the callbacks get for the link.</summary>
    /// <param name="markup">The markup of the text.</param>
    /// <param name="source">The parsed markup buffer.</param>
    /// <param name="state">The link's interaction state.</param>
    /// <returns>The context.</returns>
    public TextLinkContext ToContext(TextMarkup markup, ReadOnlySpan<byte> source, TextLinkState state) =>
        new(markup, source, this.Offset, this.Length, this.Target.Span, state, this.Detected);
}

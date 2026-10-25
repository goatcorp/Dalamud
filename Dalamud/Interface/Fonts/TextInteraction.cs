namespace Dalamud.Interface.Fonts;

/// <summary>What happened to a link of the styled text drawn last.</summary>
public readonly ref struct TextInteraction
{
    /// <summary>Initializes a new instance of the <see cref="TextInteraction"/> struct.</summary>
    /// <param name="link">The link.</param>
    /// <param name="drawnOffset">The byte offset of what defines the link, in the text as drawn.</param>
    /// <param name="drawnLength">The length of what defines the link, in the text as drawn.</param>
    /// <param name="flags">What happened.</param>
    /// <param name="held">Whether the link is held down.</param>
    internal TextInteraction(
        TextMarkup markup, ReadOnlySpan<byte> source, int offset, int length, int drawnOffset, int drawnLength,
        ReadOnlySpan<byte> target, TextInteractionFlags flags, bool held)
    {
        this.Link = link;
        this.DrawnOffset = drawnOffset;
        this.DrawnLength = drawnLength;
        this.Flags = flags;
        this.Held = held;
    }

    /// <summary>Gets the link, with <see cref="TextLinkContext.State"/> telling whether it is hovered or held.
    /// </summary>
    public TextLinkContext Link { get; }

    /// <summary>Gets what happened.</summary>
    public TextInteractionFlags Flags { get; }

    /// <summary>Gets the byte offset of what defines the link, in the text as drawn.</summary>
    /// <remarks>It differs from <see cref="TextLinkContext.Offset"/> only for <see cref="TextMarkup.MacroString"/>.
    /// </remarks>
    public int DrawnOffset { get; }

    /// <summary>Gets the length of what defines the link in bytes, in the text as drawn.</summary>
    public int DrawnLength { get; }

    /// <summary>Gets the link target.</summary>
    /// <remarks>For SeStrings, it is the link payload.</remarks>
    public ReadOnlySpan<byte> Target { get; }

    /// <summary>Gets what happened.</summary>
    public TextInteractionFlags Flags { get; }

    /// <summary>Gets a value indicating whether the link is held down.</summary>
    public bool Held { get; }
}

namespace Dalamud.Interface.Fonts;

/// <summary>What happened to a link of the styled text drawn last.</summary>
public readonly ref struct TextInteraction
{
    /// <summary>Initializes a new instance of the <see cref="TextInteraction"/> struct.</summary>
    /// <param name="link">The link.</param>
    /// <param name="drawnOffset">The byte offset of what defines the link, in the text as drawn.</param>
    /// <param name="drawnLength">The length of what defines the link, in the text as drawn.</param>
    /// <param name="flags">What happened.</param>
    internal TextInteraction(TextLinkContext link, int drawnOffset, int drawnLength, TextInteractionFlags flags)
    {
        this.Link = link;
        this.DrawnOffset = drawnOffset;
        this.DrawnLength = drawnLength;
        this.Flags = flags;
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
}

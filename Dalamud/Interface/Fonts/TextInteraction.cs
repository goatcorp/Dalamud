namespace Dalamud.Interface.Fonts;

/// <summary>Link interaction details for the most recently drawn styled text.</summary>
public readonly ref struct TextInteraction
{
    /// <summary>Initializes a new instance of the <see cref="TextInteraction"/> struct.</summary>
    /// <param name="link">The link.</param>
    /// <param name="drawnOffset">Byte offset of the link in the caller's input buffer.</param>
    /// <param name="drawnLength">Byte length of the link in the caller's input buffer.</param>
    /// <param name="flags">The interaction flags.</param>
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

    /// <summary>Gets the interaction flags.</summary>
    public TextInteractionFlags Flags { get; }

    /// <summary>Gets the byte offset of the link in the caller's input buffer.</summary>
    /// <remarks>For <see cref="TextMarkup.MacroString"/>, this spans the opening macro tag, whereas
    /// <see cref="TextLinkContext.Offset"/> indexes into the compiled SeString. For <see cref="TextMarkup.SeString"/>,
    /// this spans the opening link payload, and for <see cref="TextLinkContext.Detected"/> links, the matched
    /// text.</remarks>
    public int DrawnOffset { get; }

    /// <summary>Gets the byte length corresponding to <see cref="DrawnOffset"/> in the caller's input buffer.</summary>
    public int DrawnLength { get; }
}

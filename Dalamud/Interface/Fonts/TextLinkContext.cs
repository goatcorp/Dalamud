using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Fonts;

/// <summary>A link of styled text that is currently being dealt with.</summary>
public readonly ref struct TextLinkContext
{
    /// <summary>Initializes a new instance of the <see cref="TextLinkContext"/> struct.</summary>
    /// <param name="markup">The markup of the text.</param>
    /// <param name="source">The text as read.</param>
    /// <param name="offset">The byte offset of what defines the link in <paramref name="source"/>.</param>
    /// <param name="length">The length of what defines the link.</param>
    /// <param name="target">The link target.</param>
    /// <param name="state">What is happening to the link.</param>
    internal TextLinkContext(
        TextMarkup markup, ReadOnlySpan<byte> source, int offset, int length, ReadOnlySpan<byte> target,
        TextLinkState state)
    {
        this.Markup = markup;
        this.Source = source;
        this.Offset = offset;
        this.Length = length;
        this.Target = target;
        this.State = state;
    }

    /// <summary>Gets the markup of the text.</summary>
    public TextMarkup Markup { get; }

    /// <summary>Gets the text as read.</summary>
    /// <remarks>For <see cref="TextMarkup.MacroString"/>, it is the compiled SeString.</remarks>
    public ReadOnlySpan<byte> Source { get; }

    /// <summary>Gets the byte offset of what defines the link in <see cref="Source"/>.</summary>
    public int Offset { get; }

    /// <summary>Gets the length of what defines the link in bytes.</summary>
    public int Length { get; }

    /// <summary>Gets the link target.</summary>
    /// <remarks>For SeStrings, it is the link payload.</remarks>
    public ReadOnlySpan<byte> Target { get; }

    /// <summary>Gets what is happening to the link.</summary>
    public TextLinkState State { get; }

    /// <summary>Gets a value indicating whether the link is held down.</summary>
    public bool Held => this.State == TextLinkState.Held;

    /// <summary>Gets the link payload of a SeString link.</summary>
    /// <param name="payload">The first payload of <see cref="Target"/>.</param>
    /// <returns><see langword="true"/> if the link is of a SeString, or <see langword="false"/> if it is
    /// <see cref="Detected"/> or <see cref="Target"/> has no payload.</returns>
    public bool TryGetSePayload(out ReadOnlySePayloadSpan payload)
    {
        if (!this.Detected)
        {
            var payloads = new ReadOnlySeStringSpan(this.Target).GetEnumerator();
            if (payloads.MoveNext())
            {
                payload = payloads.Current;
                return true;
            }
        }

        payload = default;
        return false;
    }
}

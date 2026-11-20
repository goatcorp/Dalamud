using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Fonts;

/// <summary>Context for a link in styled text during styling, hit-testing, or activation.</summary>
public readonly ref struct TextLinkContext
{
    /// <summary>Initializes a new instance of the <see cref="TextLinkContext"/> struct.</summary>
    /// <param name="markup">The markup of the text.</param>
    /// <param name="source">The parsed markup buffer.</param>
    /// <param name="offset">Byte offset in <paramref name="source"/> of the opening link payload or matched link text.</param>
    /// <param name="length">Byte length in <paramref name="source"/> of the opening link payload or matched link text.</param>
    /// <param name="target">The link target.</param>
    /// <param name="state">The link's interaction state.</param>
    /// <param name="detected">Whether a callback detected the link in text.</param>
    internal TextLinkContext(
        TextMarkup markup, ReadOnlySpan<byte> source, int offset, int length, ReadOnlySpan<byte> target,
        TextLinkState state, bool detected)
    {
        this.Markup = markup;
        this.Source = source;
        this.Offset = offset;
        this.Length = length;
        this.Target = target;
        this.State = state;
        this.Detected = detected;
    }

    /// <summary>Gets the markup of the text.</summary>
    public TextMarkup Markup { get; }

    /// <summary>Gets the parsed markup buffer.</summary>
    /// <remarks>For <see cref="TextMarkup.MacroString"/>, this is the compiled SeString.</remarks>
    public ReadOnlySpan<byte> Source { get; }

    /// <summary>Gets the byte offset in <see cref="Source"/> of the opening link payload, or of the matched text for
    /// <see cref="Detected"/> links.</summary>
    public int Offset { get; }

    /// <summary>Gets the byte length in <see cref="Source"/> of the opening link payload, or of the matched text for
    /// <see cref="Detected"/> links.</summary>
    public int Length { get; }

    /// <summary>Gets the link target.</summary>
    /// <remarks>For SeStrings, it is the link payload. For <see cref="Detected"/> links, it is what the callback
    /// gave.</remarks>
    public ReadOnlySpan<byte> Target { get; }

    /// <summary>Gets the link's current interaction state.</summary>
    public TextLinkState State { get; }

    /// <summary>Gets a value indicating whether the link was detected by a callback rather than defined by markup.</summary>
    /// <seealso cref="ITextMarkupCallback.FindLinks"/>
    public bool Detected { get; }

    /// <summary>Gets a value indicating whether the link is held down.</summary>
    public bool Held => this.State == TextLinkState.Held;

    /// <summary>Gets the link payload of a SeString link.</summary>
    /// <param name="payload">The first payload of <see cref="Target"/>.</param>
    /// <returns><see langword="true"/> if <see cref="Detected"/> is <see langword="false"/> and <see cref="Target"/>
    /// contains a SeString payload, or <see langword="false"/> otherwise.</returns>
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

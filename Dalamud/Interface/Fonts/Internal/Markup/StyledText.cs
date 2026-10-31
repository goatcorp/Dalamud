using System.Collections.Generic;

using Dalamud.Interface.Fonts.Internal.Layout;

namespace Dalamud.Interface.Fonts.Internal.Markup;

/// <summary>Text a styler has read, to lay out.</summary>
/// <remarks>Offsets are into <see cref="Text"/>, unless named otherwise.</remarks>
/// <seealso cref="TextStyler"/>
internal sealed class StyledText
{
    private static readonly Comparer<TextLink> OffsetComparer =
        Comparer<TextLink>.Create((a, b) => a.Offset.CompareTo(b.Offset));

    private byte[] text = new byte[256];
    private int[] sourceOffsets = new int[257];
    private int textLength;

    /// <summary>Gets or sets the text as read.</summary>
    /// <remarks>Layouts keep it, so it must not be written to afterwards.</remarks>
    public byte[] Source { get; set; } = [];

    /// <summary>Gets or sets the offset in the text as drawn of each byte of <see cref="Source"/> and of its end, or
    /// null if <see cref="Source"/> is the text as drawn.</summary>
    public int[]? SourceToDrawn { get; set; }

    /// <summary>Gets or sets the font of text outside runs, or null for that of the layout.</summary>
    public FontImpl? DefaultFont { get; set; }

    /// <summary>Gets the text to display, in UTF-8.</summary>
    public ReadOnlySpan<byte> Text => this.text.AsSpan(0, this.textLength);

    /// <summary>Gets the runs.</summary>
    /// <remarks><see cref="TextRun.UserData"/> is the index into <see cref="Links"/> plus 1, or 0 outside links.
    /// </remarks>
    public List<TextRun> Runs { get; } = [];

    /// <summary>Gets the inline objects, one per U+FFFC in <see cref="Text"/>.</summary>
    /// <remarks><see cref="InlineObject.UserData"/> is as for <see cref="Runs"/>.</remarks>
    public List<InlineObject> Objects { get; } = [];

    /// <summary>Gets what each of <see cref="Objects"/> shows.</summary>
    public List<TextObjectContent> Contents { get; } = [];

    /// <summary>Gets the links.</summary>
    public List<TextLink> Links { get; } = [];

    /// <summary>Clears everything.</summary>
    public void Clear()
    {
        this.textLength = 0;
        this.Source = [];
        this.SourceToDrawn = null;
        this.DefaultFont = null;
        this.Runs.Clear();
        this.Objects.Clear();
        this.Contents.Clear();
        this.Links.Clear();
    }

    /// <summary>Appends text to display.</summary>
    /// <param name="bytes">The text, in UTF-8.</param>
    /// <param name="sourceOffset">The offset in <see cref="Source"/> it came from.</param>
    public void Append(ReadOnlySpan<byte> bytes, int sourceOffset)
    {
        if (this.textLength + bytes.Length > this.text.Length)
        {
            Array.Resize(ref this.text, Math.Max(this.textLength + bytes.Length, this.text.Length * 2));
            Array.Resize(ref this.sourceOffsets, this.text.Length + 1);
        }

        bytes.CopyTo(this.text.AsSpan(this.textLength));
        this.sourceOffsets.AsSpan(this.textLength, bytes.Length).Fill(sourceOffset);
        this.textLength += bytes.Length;
    }

    /// <summary>Appends a piece of <see cref="Source"/> to display as it is.</summary>
    /// <param name="sourceOffset">The offset of the piece in <see cref="Source"/>.</param>
    /// <param name="length">The length of the piece.</param>
    public void AppendSource(int sourceOffset, int length)
    {
        var at = this.textLength;
        this.Append(this.Source.AsSpan(sourceOffset, length), sourceOffset);
        for (var i = 1; i < length; i++)
            this.sourceOffsets[at + i] = sourceOffset + i;
    }

    /// <summary>Asks callbacks for links in a piece of <see cref="Source"/>, and adds them to <see cref="Links"/>.
    /// </summary>
    /// <param name="markup">The markup of the text.</param>
    /// <param name="callbacks">The callbacks, in order.</param>
    /// <param name="sourceOffset">The offset of the piece in <see cref="Source"/>.</param>
    /// <param name="length">The length of the piece.</param>
    /// <returns>The number of links added, in order.</returns>
    /// <remarks>Every callback is asked. Links of earlier callbacks win where they overlap.</remarks>
    public int FindLinks(TextMarkup markup, ITextMarkupCallback[] callbacks, int sourceOffset, int length)
    {
        var first = this.Links.Count;
        if (length == 0)
            return 0;
        var context = new TextLinkSearchContext(markup, this.Source, sourceOffset, length);
        var sorted = true;
        foreach (var callback in callbacks)
        {
            var before = this.Links.Count;
            callback.FindLinks(context, new(this.Links, this.Source, sourceOffset, length, first));
            sorted &= before == first || before == this.Links.Count;
        }

        if (!sorted)
            this.Links.Sort(first, this.Links.Count - first, OffsetComparer);
        return this.Links.Count - first;
    }

    /// <summary>Gets the style to lay out a link with in the style of the text around it, as callbacks say for
    /// <see cref="TextLinkState.Normal"/>.</summary>
    /// <param name="markup">The markup of the text.</param>
    /// <param name="callbacks">The callbacks, in order.</param>
    /// <param name="link">The index into <see cref="Links"/>.</param>
    /// <param name="style">The style of the text around it.</param>
    /// <returns>The style of the link. Only its fields that change the layout come from the callbacks, as the others
    /// are asked for as it is drawn.</returns>
    public TextStyle LinkStyleOf(TextMarkup markup, ITextMarkupCallback[] callbacks, int link, in TextStyle style)
    {
        var l = this.Links[link];
        var context = new TextLinkContext(
            markup, this.Source, l.Offset, l.Length, l.Target.Span, TextLinkState.Normal, l.Detected);
        foreach (var callback in callbacks)
        {
            if (callback.TryGetLinkStyle(context, style, out var s))
                return style.WithLayoutOf(s with { Markup = style.Markup });
        }

        return style;
    }

    /// <summary>Gets the offset in the text as drawn of each byte of <see cref="Text"/> and of its end.</summary>
    /// <returns>The offsets, valid until the next change.</returns>
    /// <remarks>It maps the offsets in place, so call it once, after the text is complete.</remarks>
    public ReadOnlySpan<int> GetDrawnOffsets()
    {
        var offsets = this.sourceOffsets.AsSpan(0, this.textLength + 1);
        offsets[^1] = this.Source.Length;
        if (this.SourceToDrawn is { } toDrawn)
        {
            foreach (ref var offset in offsets)
                offset = toDrawn[offset];
        }

        return offsets;
    }
}

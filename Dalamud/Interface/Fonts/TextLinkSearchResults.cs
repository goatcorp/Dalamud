using System.Collections.Generic;

using Dalamud.Interface.Fonts.Internal.Markup;

namespace Dalamud.Interface.Fonts;

/// <summary>Receives the links a callback finds in a piece of styled text.</summary>
/// <seealso cref="ITextMarkupCallback.FindLinks"/>
public readonly ref struct TextLinkSearchResults
{
    private readonly List<TextLink> links;
    private readonly byte[] source;
    private readonly int offset;
    private readonly int length;
    private readonly int kept;
    private readonly int first;

    /// <summary>Initializes a new instance of the <see cref="TextLinkSearchResults"/> struct.</summary>
    /// <param name="links">The links of the styled text, which found links are added to.</param>
    /// <param name="source">The text as read.</param>
    /// <param name="offset">The byte offset of the piece in <paramref name="source"/>.</param>
    /// <param name="length">The length of the piece in bytes.</param>
    /// <param name="kept">The index into <paramref name="links"/> of the first link that callbacks pushed later found
    /// in the piece. New links must not overlap those.</param>
    internal TextLinkSearchResults(List<TextLink> links, byte[] source, int offset, int length, int kept)
    {
        this.links = links;
        this.source = source;
        this.offset = offset;
        this.length = length;
        this.kept = kept;
        this.first = links.Count;
    }

    /// <summary>Gets the number of links this callback added.</summary>
    public int Count => this.links.Count - this.first;

    /// <summary>Adds a link.</summary>
    /// <param name="offset">The byte offset of the link in <see cref="TextLinkSearchContext.Text"/>.</param>
    /// <param name="length">The length of the link in bytes.</param>
    /// <param name="target">The link target. A slice of <see cref="TextLinkSearchContext.Text"/> is kept as it is,
    /// and anything else is copied.</param>
    /// <returns><see langword="false"/> if the link is left out. That is when it is empty, outside the text, not after
    /// the link added before, or overlapping a link of a callback pushed later.</returns>
    public bool Add(int offset, int length, ReadOnlySpan<byte> target)
    {
        var after = this.Count == 0 ? 0 : this.links[^1].Offset + this.links[^1].Length - this.offset;
        if (length <= 0 || offset < after || offset > this.length - length)
            return false;

        var begin = this.offset + offset;
        for (var i = this.kept; i < this.first; i++)
        {
            if (this.links[i].Offset < begin + length && begin < this.links[i].Offset + this.links[i].Length)
                return false;
        }

        var inSource = this.source.AsSpan().Overlaps(target, out var at) &&
                       at >= 0 &&
                       at <= this.source.Length - target.Length;
        this.links.Add(new(begin, length, inSource ? this.source.AsMemory(at, target.Length) : target.ToArray(), true));
        return true;
    }
}

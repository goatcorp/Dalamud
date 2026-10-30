using Dalamud.Interface.Fonts.Internal.Layout;

namespace Dalamud.Interface.Fonts.Internal.Markup;

/// <summary>Reads plain text, for the links callbacks find in it.</summary>
/// <remarks>It is used only while text callbacks are pushed. Plain text is laid out as it is otherwise.</remarks>
/// <seealso cref="TextLayouter.CreateStyled"/>
internal sealed class PlainTextStyler : TextStyler
{
    /// <inheritdoc/>
    public override TextMarkup Markup => TextMarkup.Plain;

    /// <inheritdoc/>
    protected override void Parse(
        FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text, ITextMarkupCallback[] callbacks, bool nested,
        StyledText output)
    {
        output.Source = [.. text];
        output.AppendSource(0, text.Length);
        var count = output.FindLinks(this.Markup, callbacks, 0, text.Length);
        for (var i = 0; i < count; i++)
        {
            var l = output.Links[i];
            var style = output.LinkStyleOf(this.Markup, callbacks, i, font.Style);
            output.Runs.Add(new(l.Offset, l.Offset + l.Length, font.Resolver.GetImpl(style), (ulong)i + 1));
        }
    }
}

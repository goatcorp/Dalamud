using System.Numerics;
using System.Text;

namespace Dalamud.Interface.ImGuiSeStringRenderer.Internal;

/// <summary>Represents a text fragment in a SeString span.</summary>
/// <param name="From">Starting byte offset (inclusive) in a SeString.</param>
/// <param name="To">Ending byte offset (exclusive) in a SeString.</param>
/// <param name="Link">Byte offset of the link that decorates this text fragment, or <c>-1</c> if none.</param>
/// <param name="Offset">Offset in pixels w.r.t. <see cref="SeStringDrawParams.ScreenOffset"/>.</param>
/// <param name="Entity">Replacement entity, if any.</param>
/// <param name="VisibleWidth">Visible width of this text fragment. This is the width required to draw everything
/// without clipping.</param>
/// <param name="AdvanceWidth">Advance width of this text fragment. This is the width required to add to the cursor
/// to position the next fragment correctly.</param>
/// <param name="AdvanceWidthWithoutSoftHyphen">Same with <paramref name="AdvanceWidth"/>, but trimming all the
/// trailing soft hyphens.</param>
/// <param name="BreakAfter">Whether to insert a line break after this text fragment.</param>
/// <param name="EndsWithSoftHyphen">Whether this text fragment ends with one or more soft hyphens.</param>
/// <param name="FirstRune">First rune in this text fragment.</param>
/// <param name="LastRune">Last rune in this text fragment, for the purpose of calculating kerning distance with
/// the following text fragment in the same line, if any.</param>
internal record struct TextFragment(
    int From,
    int To,
    int Link,
    Vector2 Offset,
    SeStringReplacementEntity Entity,
    float VisibleWidth,
    float AdvanceWidth,
    float AdvanceWidthWithoutSoftHyphen,
    bool BreakAfter,
    bool EndsWithSoftHyphen,
    Rune FirstRune,
    Rune LastRune)
{
    /// <summary>Gets a value indicating whether the fragment ends with a visible soft hyphen.</summary>
    public bool IsSoftHyphenVisible => this.EndsWithSoftHyphen && this.BreakAfter;
}

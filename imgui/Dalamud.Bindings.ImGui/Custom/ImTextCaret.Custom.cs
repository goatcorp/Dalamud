namespace Dalamud.Bindings.ImGui;

/// <summary>A caret position in a text layout returned by <see cref="ImTextLayoutPtr.GetCaret"/>,
/// <see cref="ImTextLayoutPtr.HitTest"/>, or <see cref="ImTextLayoutPtr.Move"/>.</summary>
/// <remarks><ul>
/// <li><see cref="Pos"/> is the top of the caret line relative to the layout origin. <see cref="Length"/> is the line
/// height. <see cref="InlinePos"/> is the x offset within the line.</li>
/// <li><see cref="TextOffset"/> is in code units of the input text (1 per <see langword="char"/> for UTF-16).
/// <see cref="RunIndex"/> is the run at the caret, or before it at the end of the text, or <c>-1</c>.
/// <see cref="ObjectIndex"/> is the inline object at the caret or under the hit-test point, or <c>-1</c>.</li>
/// <li><see cref="UserData"/> is the user data of the run or inline object at the caret, or <c>0</c>. For
/// <see cref="ImTextLayoutPtr.HitTest"/>, it is <c>0</c> when the point lies outside every grapheme and inline object
/// box on that line, even when <see cref="RunIndex"/> or <see cref="ObjectIndex"/> is set for the clamped caret.</li>
/// </ul></remarks>
public partial struct ImTextCaret
{
    /// <summary>Gets the empty caret returned for null layouts or failed queries, with <see cref="RunIndex"/> and
    /// <see cref="ObjectIndex"/> set to <c>-1</c> and all other fields <c>0</c>.</summary>
    public static ImTextCaret None => new() { RunIndex = -1, ObjectIndex = -1 };

    /// <summary>Gets or sets a value indicating whether the caret is at the end of <see cref="LineIndex"/> at a wrap
    /// point, rather than the start of the next line.</summary>
    public bool IsPreviousLine
    {
        readonly get => this.PreviousLine != 0;
        set => this.PreviousLine = value ? (byte)1 : (byte)0;
    }

    /// <summary>Gets or sets a value indicating whether the hit tested point is inside a grapheme's box.</summary>
    public bool IsInside
    {
        readonly get => this.Inside != 0;
        set => this.Inside = value ? (byte)1 : (byte)0;
    }
}

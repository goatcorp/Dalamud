namespace Dalamud.Bindings.ImGui;

/// <summary>A caret position in a text layout, from getting the caret of an offset, hit testing a point, or moving
/// another caret.</summary>
/// <remarks><ul>
/// <li><see cref="Pos"/> is the top of the caret line, relative to the layout origin. <see cref="Length"/> is the
/// length of the caret line, which is the line height. <see cref="InlinePos"/> is the x position within the line.</li>
/// <li><see cref="TextOffset"/> is in bytes of the text as given, so UTF-16 text has 2 per char.
/// <see cref="RunIndex"/> is the run of the grapheme at the caret, or before it at the end of the text, or -1.
/// <see cref="ObjectIndex"/> is the inline object at the caret or under the point, or -1. <see cref="UserData"/> is
/// the user data of the run or object, or 0.</li>
/// </ul></remarks>
public partial struct ImTextCaret
{
    /// <summary>Gets the caret of no place, as a null layout or a failed query gives. Its <see cref="RunIndex"/> and
    /// <see cref="ObjectIndex"/> are -1, and the rest is 0.</summary>
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

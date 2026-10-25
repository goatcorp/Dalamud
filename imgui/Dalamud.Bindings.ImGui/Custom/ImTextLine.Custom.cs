namespace Dalamud.Bindings.ImGui;

/// <summary>A line of a text layout.</summary>
/// <remarks><see cref="TextBegin"/> is the starting byte offset. <see cref="TextEnd"/> is the ending byte offset,
/// excluding the line break and trailing blanks. <see cref="TextNext"/> is the starting byte offset of the next line.
/// <see cref="Baseline"/> is the distance from the top of the line box to the baseline. <see cref="Pos"/> is the
/// top-left of the line box, relative to the layout origin, and <see cref="Size"/> is its size. Byte offsets are of
/// the text as given, so UTF-16 text has 2 per char.</remarks>
public partial struct ImTextLine;

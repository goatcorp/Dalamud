namespace Dalamud.Bindings.ImGui;

/// <summary>A line of a text layout.</summary>
/// <remarks><ul>
/// <li><see cref="TextBegin"/> is the starting offset.</li>
/// <li><see cref="TextEnd"/> is the ending offset, excluding the line break and trailing blanks.</li>
/// <li><see cref="TextNext"/> is the starting offset of the next line.</li>
/// <li><see cref="Baseline"/> is the distance from the top of the line box to the baseline.</li>
/// <li><see cref="Pos"/> is the top-left of the line box, relative to the layout origin, and
/// <see cref="Size"/> is its size. A line moved by alignment starts at its <see cref="Pos"/>.</li>
/// <li>Offsets are in code units of the text as given, so UTF-16 text has 1 per char.</li>
/// </ul></remarks>
public partial struct ImTextLine;

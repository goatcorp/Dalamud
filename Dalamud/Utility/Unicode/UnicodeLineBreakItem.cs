namespace Dalamud.Utility.Unicode;

/// <summary>Describes where to break a line from <see cref="UnicodeLineBreakEnumerator"/>.</summary>
/// <param name="ByteOffset">Byte offset in the original byte span.</param>
/// <param name="Mandatory">Whether there must be a line break.</param>
public record struct UnicodeLineBreakItem(int ByteOffset, bool Mandatory);

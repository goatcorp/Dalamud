namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <summary>A range of attributed text.</summary>
/// <param name="TextBegin">Starting byte offset into the input buffer, in bytes even for UTF-16 text unlike reported
/// code-unit offsets.</param>
/// <param name="TextEnd">Ending offset, in bytes.</param>
/// <param name="Impl">The font of the style of the range, or null for that of the layout.</param>
/// <param name="UserData">Context for hit test returns.</param>
internal readonly record struct TextRun(
    int TextBegin,
    int TextEnd,
    FontImpl? Impl = null,
    ulong UserData = 0);

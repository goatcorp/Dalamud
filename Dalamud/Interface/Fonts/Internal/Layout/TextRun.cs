namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <summary>A range of attributed text.</summary>
/// <param name="TextBegin">Starting byte offset.</param>
/// <param name="TextEnd">Ending byte offset.</param>
/// <param name="Impl">The font of the style of the range, or null for that of the layout.</param>
/// <param name="UserData">Context for hit test returns.</param>
internal readonly record struct TextRun(
    int TextBegin,
    int TextEnd,
    FontImpl? Impl = null,
    ulong UserData = 0);

using Dalamud.Interface.SpannedStrings.Internal;

namespace Dalamud.Interface.SpannedStrings.Enums;

/// <summary>Specifies the vertical alignment of a text in a line consisting of texts of mixed line heights.</summary>
public enum VerticalAlignment : byte
{
    /// <summary>Align to the baseline.</summary>
    [SpannedParseShortName("base")]
    Baseline,

    /// <summary>Align to the top.</summary>
    [SpannedParseShortName("up", "above")]
    Top,

    /// <summary>Align to the middle.</summary>
    [SpannedParseShortName("mid", "center")]
    Middle,

    /// <summary>Align to the bottom.</summary>
    [SpannedParseShortName("down", "below")]
    Bottom,
}

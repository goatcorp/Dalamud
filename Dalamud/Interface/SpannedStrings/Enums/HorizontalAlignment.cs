using Dalamud.Interface.SpannedStrings.Internal;

namespace Dalamud.Interface.SpannedStrings.Enums;

/// <summary>Specifies the horizontal alignment of the text in the line.</summary>
public enum HorizontalAlignment : byte
{
    /// <summary>Align to the left.</summary>
    Left,

    /// <summary>Align to the center.</summary>
    [SpannedParseShortName("middle", "mid")]
    Center,

    /// <summary>Align to the right.</summary>
    Right,
}

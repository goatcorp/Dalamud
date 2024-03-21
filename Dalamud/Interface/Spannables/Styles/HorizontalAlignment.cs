using Dalamud.Interface.Spannables.Internal;

namespace Dalamud.Interface.Spannables.Styles;

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

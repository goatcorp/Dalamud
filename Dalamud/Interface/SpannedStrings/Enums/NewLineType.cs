using Dalamud.Interface.SpannedStrings.Internal;

namespace Dalamud.Interface.SpannedStrings.Enums;

/// <summary>Specifies which line breaks should be honored.</summary>
[Flags]
public enum NewLineType : byte
{
    /// <summary>Not a line break.</summary>
    None = 0,

    /// <summary>A custom line break.</summary>
    [SpannedParseShortName("m")]
    Manual = 1 << 0,

    /// <summary>A carriage return character (<c>\r</c>).</summary>
    [SpannedParseShortName("r")]
    Cr = 1 << 1,

    /// <summary>A line feed character (<c>\n</c>).</summary>
    [SpannedParseShortName("n")]
    Lf = 1 << 2,

    /// <summary>A carriage return character followed by a line feed character (<c>\r\n</c>).</summary>
    [SpannedParseShortName("rn")]
    CrLf = 1 << 3,

    /// <summary>Shortcut for all valid options.</summary>
    All = byte.MaxValue,
}

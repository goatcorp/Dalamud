using Dalamud.Interface.SpannedStrings.Internal;

namespace Dalamud.Interface.SpannedStrings.Enums;

/// <summary>Possible modes for a boolean value that supports toggling.</summary>
public enum BoolOrToggle : byte
{
    /// <summary>Do not change.</summary>
    [SpannedParseShortName("revert")]
    NoChange,

    /// <summary>Change.</summary>
    Change,

    /// <summary>Turn it off.</summary>
    [SpannedParseShortName("false", "no")]
    Off,

    /// <summary>Turn it on.</summary>
    [SpannedParseShortName("true", "yes")]
    On,
}

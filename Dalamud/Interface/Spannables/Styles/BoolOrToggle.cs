using Dalamud.Interface.Spannables.Internal;

namespace Dalamud.Interface.Spannables.Styles;

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

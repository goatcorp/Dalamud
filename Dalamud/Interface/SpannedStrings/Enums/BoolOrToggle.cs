namespace Dalamud.Interface.SpannedStrings.Enums;

/// <summary>Possible modes for a boolean value that supports toggling.</summary>
public enum BoolOrToggle : byte
{
    /// <summary>Do not change.</summary>
    NoChange,

    /// <summary>Change.</summary>
    Change,

    /// <summary>Turn it off.</summary>
    Off,

    /// <summary>Turn it on.</summary>
    On,
}

using System;

namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// Condition flags for the <see cref="PartyFinderGui"/> class.
/// </summary>
[Flags]
public enum ConditionFlags : uint
{
    /// <summary>
    /// No duty condition.
    /// </summary>
    None = 1,

    /// <summary>
    /// The duty complete condition.
    /// </summary>
    DutyComplete = 2,

    /// <summary>
    /// The duty incomplete condition.
    /// </summary>
    DutyIncomplete = 4,
}

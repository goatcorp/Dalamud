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
    None = 1 << 0,

    /// <summary>
    /// The duty complete condition.
    /// </summary>
    DutyComplete = 1 << 1,

    /// <summary>
    /// The duty complete (weekly reward unclaimed) condition. This condition is
    /// only available for savage fights prior to echo release.
    /// </summary>
    DutyCompleteWeeklyRewardUnclaimed = 1 << 3,

    /// <summary>
    /// The duty incomplete condition.
    /// </summary>
    DutyIncomplete = 1 << 2,
}

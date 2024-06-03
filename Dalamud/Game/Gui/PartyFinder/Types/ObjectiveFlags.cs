namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// Objective flags for the <see cref="PartyFinderGui"/> class.
/// </summary>
[Flags]
public enum ObjectiveFlags : uint
{
    /// <summary>
    /// No objective.
    /// </summary>
    None = 0,

    /// <summary>
    /// The duty completion objective.
    /// </summary>
    DutyCompletion = 1,

    /// <summary>
    /// The practice objective.
    /// </summary>
    Practice = 2,

    /// <summary>
    /// The loot objective.
    /// </summary>
    Loot = 4,
}

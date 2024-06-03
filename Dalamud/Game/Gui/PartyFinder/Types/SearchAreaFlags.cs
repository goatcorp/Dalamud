namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// Search area flags for the <see cref="PartyFinderGui"/> class.
/// </summary>
[Flags]
public enum SearchAreaFlags : uint
{
    /// <summary>
    /// Datacenter.
    /// </summary>
    DataCentre = 1 << 0,

    /// <summary>
    /// Private.
    /// </summary>
    Private = 1 << 1,

    /// <summary>
    /// Alliance raid.
    /// </summary>
    AllianceRaid = 1 << 2,

    /// <summary>
    /// World.
    /// </summary>
    World = 1 << 3,

    /// <summary>
    /// One player per job.
    /// </summary>
    OnePlayerPerJob = 1 << 5,
}

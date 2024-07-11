namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// Duty finder settings flags for the <see cref="PartyFinderGui"/> class.
/// </summary>
[Flags]
public enum DutyFinderSettingsFlags : uint
{
    /// <summary>
    /// No duty finder settings.
    /// </summary>
    None = 0,

    /// <summary>
    /// The undersized party setting.
    /// </summary>
    UndersizedParty = 1 << 0,

    /// <summary>
    /// The minimum item level setting.
    /// </summary>
    MinimumItemLevel = 1 << 1,

    /// <summary>
    /// The silence echo setting.
    /// </summary>
    SilenceEcho = 1 << 2,
}

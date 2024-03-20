using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// Extensions for the <see cref="JobFlags"/> enum.
/// </summary>
public static class JobFlagsExtensions
{
    /// <summary>
    /// Get the actual ClassJob from the in-game sheets for this JobFlags.
    /// </summary>
    /// <param name="job">A JobFlags enum member.</param>
    /// <param name="data">A DataManager to get the ClassJob from.</param>
    /// <returns>A ClassJob if found or null if not.</returns>
    public static ClassJob? ClassJob(this JobFlags job, IDataManager data)
    {
        var jobs = data.GetExcelSheet<ClassJob>();

        uint? row = job switch
        {
            JobFlags.Gladiator => 1,
            JobFlags.Pugilist => 2,
            JobFlags.Marauder => 3,
            JobFlags.Lancer => 4,
            JobFlags.Archer => 5,
            JobFlags.Conjurer => 6,
            JobFlags.Thaumaturge => 7,
            JobFlags.Paladin => 19,
            JobFlags.Monk => 20,
            JobFlags.Warrior => 21,
            JobFlags.Dragoon => 22,
            JobFlags.Bard => 23,
            JobFlags.WhiteMage => 24,
            JobFlags.BlackMage => 25,
            JobFlags.Arcanist => 26,
            JobFlags.Summoner => 27,
            JobFlags.Scholar => 28,
            JobFlags.Rogue => 29,
            JobFlags.Ninja => 30,
            JobFlags.Machinist => 31,
            JobFlags.DarkKnight => 32,
            JobFlags.Astrologian => 33,
            JobFlags.Samurai => 34,
            JobFlags.RedMage => 35,
            JobFlags.BlueMage => 36,
            JobFlags.Gunbreaker => 37,
            JobFlags.Dancer => 38,
            JobFlags.Reaper => 39,
            JobFlags.Sage => 40,
            _ => null,
        };

        return row == null ? null : jobs?.GetRow((uint)row);
    }
}

using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Game.Gui.FellowshipFinder.Types
{
    /// <summary>
    /// The different activities that can be specified for a fellowship.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "No")]
    public enum FellowshipFinderActivity
    {
        None = 0,
        RolePlaying = 1,
        PlayerEvents = 2,
        MakingFriends = 3,
        NoviceSupport = 4,
        Casual = 5,
        Hardcore = 6,
        Leveling = 7,
        Battle = 8,
        Crafting = 9,
        Gathering = 10,
        Housing = 11,
        Hunts = 12,
        TreasureHunt = 13,
        Pvp = 14,
        Fishing = 15,
        DomanMahjong = 16,
        Performance = 17,
        Glamours = 18,
        GroupPose = 19,
        TripleTriad = 20,
        Collectables = 21,
        Chatting = 22,
    }
}

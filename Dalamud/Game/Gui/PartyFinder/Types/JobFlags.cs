namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// Job flags for the <see cref="PartyFinder"/> class.
/// </summary>
[Flags]
public enum JobFlags : ulong
{
    /// <summary>
    /// Gladiator (GLD).
    /// </summary>
    Gladiator = 1ul << 1,

    /// <summary>
    /// Pugilist (PGL).
    /// </summary>
    Pugilist = 1ul << 2,

    /// <summary>
    /// Marauder (MRD).
    /// </summary>
    Marauder = 1ul << 3,

    /// <summary>
    /// Lancer (LNC).
    /// </summary>
    Lancer = 1ul << 4,

    /// <summary>
    /// Archer (ARC).
    /// </summary>
    Archer = 1ul << 5,

    /// <summary>
    /// Conjurer (CNJ).
    /// </summary>
    Conjurer = 1ul << 6,

    /// <summary>
    /// Thaumaturge (THM).
    /// </summary>
    Thaumaturge = 1ul << 7,

    /// <summary>
    /// Paladin (PLD).
    /// </summary>
    Paladin = 1ul << 8,

    /// <summary>
    /// Monk (MNK).
    /// </summary>
    Monk = 1ul << 9,

    /// <summary>
    /// Warrior (WAR).
    /// </summary>
    Warrior = 1ul << 10,

    /// <summary>
    /// Dragoon (DRG).
    /// </summary>
    Dragoon = 1ul << 11,

    /// <summary>
    /// Bard (BRD).
    /// </summary>
    Bard = 1ul << 12,

    /// <summary>
    /// White mage (WHM).
    /// </summary>
    WhiteMage = 1ul << 13,

    /// <summary>
    /// Black mage (BLM).
    /// </summary>
    BlackMage = 1ul << 14,

    /// <summary>
    /// Arcanist (ACN).
    /// </summary>
    Arcanist = 1ul << 15,

    /// <summary>
    /// Summoner (SMN).
    /// </summary>
    Summoner = 1ul << 16,

    /// <summary>
    /// Scholar (SCH).
    /// </summary>
    Scholar = 1ul << 17,

    /// <summary>
    /// Rogue (ROG).
    /// </summary>
    Rogue = 1ul << 18,

    /// <summary>
    /// Ninja (NIN).
    /// </summary>
    Ninja = 1ul << 19,

    /// <summary>
    /// Machinist (MCH).
    /// </summary>
    Machinist = 1ul << 20,

    /// <summary>
    /// Dark Knight (DRK).
    /// </summary>
    DarkKnight = 1ul << 21,

    /// <summary>
    /// Astrologian (AST).
    /// </summary>
    Astrologian = 1ul << 22,

    /// <summary>
    /// Samurai (SAM).
    /// </summary>
    Samurai = 1ul << 23,

    /// <summary>
    /// Red mage (RDM).
    /// </summary>
    RedMage = 1ul << 24,

    /// <summary>
    /// Blue mage (BLU).
    /// </summary>
    BlueMage = 1ul << 25,

    /// <summary>
    /// Gunbreaker (GNB).
    /// </summary>
    Gunbreaker = 1ul << 26,

    /// <summary>
    /// Dancer (DNC).
    /// </summary>
    Dancer = 1ul << 27,

    /// <summary>
    /// Reaper (RPR).
    /// </summary>
    Reaper = 1ul << 28,

    /// <summary>
    /// Sage (SGE).
    /// </summary>
    Sage = 1ul << 29,

    /// <summary>
    /// Viper (VPR).
    /// </summary>
    Viper = 1ul << 30,

    /// <summary>
    /// Pictomancer (PCT).
    /// </summary>
    Pictomancer = 1ul << 31,
}

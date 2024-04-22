namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// Job flags for the <see cref="PartyFinder"/> class.
/// </summary>
[Flags]
public enum JobFlags
{
    /// <summary>
    /// Gladiator (GLD).
    /// </summary>
    Gladiator = 1 << 1,

    /// <summary>
    /// Pugilist (PGL).
    /// </summary>
    Pugilist = 1 << 2,

    /// <summary>
    /// Marauder (MRD).
    /// </summary>
    Marauder = 1 << 3,

    /// <summary>
    /// Lancer (LNC).
    /// </summary>
    Lancer = 1 << 4,

    /// <summary>
    /// Archer (ARC).
    /// </summary>
    Archer = 1 << 5,

    /// <summary>
    /// Conjurer (CNJ).
    /// </summary>
    Conjurer = 1 << 6,

    /// <summary>
    /// Thaumaturge (THM).
    /// </summary>
    Thaumaturge = 1 << 7,

    /// <summary>
    /// Paladin (PLD).
    /// </summary>
    Paladin = 1 << 8,

    /// <summary>
    /// Monk (MNK).
    /// </summary>
    Monk = 1 << 9,

    /// <summary>
    /// Warrior (WAR).
    /// </summary>
    Warrior = 1 << 10,

    /// <summary>
    /// Dragoon (DRG).
    /// </summary>
    Dragoon = 1 << 11,

    /// <summary>
    /// Bard (BRD).
    /// </summary>
    Bard = 1 << 12,

    /// <summary>
    /// White mage (WHM).
    /// </summary>
    WhiteMage = 1 << 13,

    /// <summary>
    /// Black mage (BLM).
    /// </summary>
    BlackMage = 1 << 14,

    /// <summary>
    /// Arcanist (ACN).
    /// </summary>
    Arcanist = 1 << 15,

    /// <summary>
    /// Summoner (SMN).
    /// </summary>
    Summoner = 1 << 16,

    /// <summary>
    /// Scholar (SCH).
    /// </summary>
    Scholar = 1 << 17,

    /// <summary>
    /// Rogue (ROG).
    /// </summary>
    Rogue = 1 << 18,

    /// <summary>
    /// Ninja (NIN).
    /// </summary>
    Ninja = 1 << 19,

    /// <summary>
    /// Machinist (MCH).
    /// </summary>
    Machinist = 1 << 20,

    /// <summary>
    /// Dark Knight (DRK).
    /// </summary>
    DarkKnight = 1 << 21,

    /// <summary>
    /// Astrologian (AST).
    /// </summary>
    Astrologian = 1 << 22,

    /// <summary>
    /// Samurai (SAM).
    /// </summary>
    Samurai = 1 << 23,

    /// <summary>
    /// Red mage (RDM).
    /// </summary>
    RedMage = 1 << 24,

    /// <summary>
    /// Blue mage (BLM).
    /// </summary>
    BlueMage = 1 << 25,

    /// <summary>
    /// Gunbreaker (GNB).
    /// </summary>
    Gunbreaker = 1 << 26,

    /// <summary>
    /// Dancer (DNC).
    /// </summary>
    Dancer = 1 << 27,

    /// <summary>
    /// Reaper (RPR).
    /// </summary>
    Reaper = 1 << 28,

    /// <summary>
    /// Sage (SGE).
    /// </summary>
    Sage = 1 << 29,
}

using System;

using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.Internal.Gui.Structs
{
    /// <summary>
    /// Search area flags for the <see cref="PartyFinder"/> class.
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
    }

    /// <summary>
    /// Objective flags for the <see cref="PartyFinder"/> class.
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

    /// <summary>
    /// Condition flags for the <see cref="PartyFinder"/> class.
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

    /// <summary>
    /// Duty finder settings flags for the <see cref="PartyFinder"/> class.
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

    /// <summary>
    /// Loot rule flags for the <see cref="PartyFinder"/> class.
    /// </summary>
    [Flags]
    public enum LootRuleFlags : uint
    {
        /// <summary>
        /// No loot rules.
        /// </summary>
        None = 0,

        /// <summary>
        /// The greed only rule.
        /// </summary>
        GreedOnly = 1,

        /// <summary>
        /// The lootmaster rule.
        /// </summary>
        Lootmaster = 2,
    }

    /// <summary>
    /// Category flags for the <see cref="PartyFinder"/> class.
    /// </summary>
    public enum Category
    {
        /// <summary>
        /// The duty category.
        /// </summary>
        Duty = 0,

        /// <summary>
        /// The quest battle category.
        /// </summary>
        QuestBattles = 1 << 0,

        /// <summary>
        /// The fate category.
        /// </summary>
        Fates = 1 << 1,

        /// <summary>
        /// The treasure hunt category.
        /// </summary>
        TreasureHunt = 1 << 2,

        /// <summary>
        /// The hunt category.
        /// </summary>
        TheHunt = 1 << 3,

        /// <summary>
        /// The gathering forays category.
        /// </summary>
        GatheringForays = 1 << 4,

        /// <summary>
        /// The deep dungeons category.
        /// </summary>
        DeepDungeons = 1 << 5,

        /// <summary>
        /// The adventuring forays category.
        /// </summary>
        AdventuringForays = 1 << 6,
    }

    /// <summary>
    /// Duty type flags for the <see cref="PartyFinder"/> class.
    /// </summary>
    public enum DutyType
    {
        /// <summary>
        /// No duty type.
        /// </summary>
        Other = 0,

        /// <summary>
        /// The roulette duty type.
        /// </summary>
        Roulette = 1 << 0,

        /// <summary>
        /// The normal duty type.
        /// </summary>
        Normal = 1 << 1,
    }

    /// <summary>
    /// Extensions for the <see cref="JobFlags"/> enum.
    /// </summary>
    public static class JobFlagsExt
    {
        /// <summary>
        /// Get the actual ClassJob from the in-game sheets for this JobFlags.
        /// </summary>
        /// <param name="job">A JobFlags enum member.</param>
        /// <param name="data">A DataManager to get the ClassJob from.</param>
        /// <returns>A ClassJob if found or null if not.</returns>
        public static ClassJob ClassJob(this JobFlags job, DataManager data)
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
                _ => null,
            };

            return row == null ? null : jobs.GetRow((uint)row);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Data;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.Internal.Gui.Structs {
    #region Raw structs

    internal static class PartyFinder {
        public static class PacketInfo {
            public static readonly int PacketSize = Marshal.SizeOf<Packet>();
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Packet {
            public readonly int batchNumber;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private readonly byte[] padding1;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public readonly Listing[] listings;
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Listing {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            private readonly byte[] header1;

            internal readonly uint id;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            private readonly byte[] header2;

            internal readonly uint contentIdLower;
            private readonly ushort unknownShort1;
            private readonly ushort unknownShort2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            private readonly byte[] header3;

            internal readonly byte category;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            private readonly byte[] header4;

            internal readonly ushort duty;
            internal readonly byte dutyType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            private readonly byte[] header5;

            internal readonly ushort world;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private readonly byte[] header6;

            internal readonly byte objective;
            internal readonly byte beginnersWelcome;
            internal readonly byte conditions;
            internal readonly byte dutyFinderSettings;
            internal readonly byte lootRules;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            private readonly byte[] header7; // all zero in every pf I've examined

            private readonly uint lastPatchHotfixTimestamp; // last time the servers were restarted?
            internal readonly ushort secondsRemaining;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            private readonly byte[] header8; // 00 00 01 00 00 00 in every pf I've examined

            internal readonly ushort minimumItemLevel;
            internal readonly ushort homeWorld;
            internal readonly ushort currentWorld;

            private readonly byte header9;

            internal readonly byte numSlots;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            private readonly byte[] header10;

            internal readonly byte searchArea;

            private readonly byte header11;

            internal readonly byte numParties;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            private readonly byte[] header12; // 00 00 00 always. maybe numParties is a u32?

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            internal readonly uint[] slots;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            internal readonly byte[] jobsPresent;

            // Note that ByValTStr will not work here because the strings are UTF-8 and there's only a CharSet for UTF-16 in C#.
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            internal readonly byte[] name;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 192)]
            internal readonly byte[] description;

            internal bool IsNull() {
                // a valid party finder must have at least one slot set
                return this.slots.All(slot => slot == 0);
            }
        }
    }

    #endregion

    #region Read-only classes

    public class PartyFinderListing {
        /// <summary>
        /// The ID assigned to this listing by the game's server.
        /// </summary>
        public uint Id { get; }
        /// <summary>
        /// The lower bits of the player's content ID.
        /// </summary>
        public uint ContentIdLower { get; }
        /// <summary>
        /// The name of the player hosting this listing.
        /// </summary>
        public SeString Name { get; }
        /// <summary>
        /// The description of this listing as set by the host. May be multiple lines.
        /// </summary>
        public SeString Description { get; }
        /// <summary>
        /// The world that this listing was created on.
        /// </summary>
        public Lazy<World> World { get; }
        /// <summary>
        /// The home world of the listing's host.
        /// </summary>
        public Lazy<World> HomeWorld { get; }
        /// <summary>
        /// The current world of the listing's host.
        /// </summary>
        public Lazy<World> CurrentWorld { get; }
        /// <summary>
        /// The Party Finder category this listing is listed under.
        /// </summary>
        public Category Category { get; }
        /// <summary>
        /// The row ID of the duty this listing is for. May be 0 for non-duty listings.
        /// </summary>
        public ushort RawDuty { get; }
        /// <summary>
        /// The duty this listing is for. May be null for non-duty listings.
        /// </summary>
        public Lazy<ContentFinderCondition> Duty { get; }
        /// <summary>
        /// The type of duty this listing is for.
        /// </summary>
        public DutyType DutyType { get; }
        /// <summary>
        /// If this listing is beginner-friendly. Shown with a sprout icon in-game.
        /// </summary>
        public bool BeginnersWelcome { get; }
        /// <summary>
        /// How many seconds this listing will continue to be available for. It may end before this time if the party
        /// fills or the host ends it early.
        /// </summary>
        public ushort SecondsRemaining { get; }
        /// <summary>
        /// The minimum item level required to join this listing.
        /// </summary>
        public ushort MinimumItemLevel { get; }
        /// <summary>
        /// The number of parties this listing is recruiting for.
        /// </summary>
        public byte Parties { get; }
        /// <summary>
        /// The number of player slots this listing is recruiting for.
        /// </summary>
        public byte SlotsAvailable { get; }

        /// <summary>
        /// A list of player slots that the Party Finder is accepting.
        /// </summary>
        public IReadOnlyCollection<PartyFinderSlot> Slots => this.slots;

        /// <summary>
        /// The objective of this listing.
        /// </summary>
        public ObjectiveFlags Objective => (ObjectiveFlags) this.objective;

        /// <summary>
        /// The conditions of this listing.
        /// </summary>
        public ConditionFlags Conditions => (ConditionFlags) this.conditions;

        /// <summary>
        /// The Duty Finder settings that will be used for this listing.
        /// </summary>
        public DutyFinderSettingsFlags DutyFinderSettings => (DutyFinderSettingsFlags) this.dutyFinderSettings;

        /// <summary>
        /// The loot rules that will be used for this listing.
        /// </summary>
        public LootRuleFlags LootRules => (LootRuleFlags) this.lootRules;

        /// <summary>
        /// Where this listing is searching. Note that this is also used for denoting alliance raid listings and one
        /// player per job.
        /// </summary>
        public SearchAreaFlags SearchArea => (SearchAreaFlags) this.searchArea;

        /// <summary>
        /// A list of the class/job IDs that are currently present in the party.
        /// </summary>
        public IReadOnlyCollection<byte> RawJobsPresent => this.jobsPresent;
        /// <summary>
        /// A list of the classes/jobs that are currently present in the party.
        /// </summary>
        public IReadOnlyCollection<Lazy<ClassJob>> JobsPresent { get; }

        #region Backing fields

        private readonly byte objective;
        private readonly byte conditions;
        private readonly byte dutyFinderSettings;
        private readonly byte lootRules;
        private readonly byte searchArea;
        private readonly PartyFinderSlot[] slots;
        private readonly byte[] jobsPresent;

        #endregion

        #region Indexers

        public bool this[ObjectiveFlags flag] => this.objective == 0 || (this.objective & (uint) flag) > 0;

        public bool this[ConditionFlags flag] => this.conditions == 0 || (this.conditions & (uint) flag) > 0;

        public bool this[DutyFinderSettingsFlags flag] => this.dutyFinderSettings == 0 || (this.dutyFinderSettings & (uint) flag) > 0;

        public bool this[LootRuleFlags flag] => this.lootRules == 0 || (this.lootRules & (uint) flag) > 0;

        public bool this[SearchAreaFlags flag] => this.searchArea == 0 || (this.searchArea & (uint) flag) > 0;

        #endregion

        internal PartyFinderListing(PartyFinder.Listing listing, DataManager dataManager, SeStringManager seStringManager) {
            this.objective = listing.objective;
            this.conditions = listing.conditions;
            this.dutyFinderSettings = listing.dutyFinderSettings;
            this.lootRules = listing.lootRules;
            this.searchArea = listing.searchArea;
            this.slots = listing.slots.Select(accepting => new PartyFinderSlot(accepting)).ToArray();
            this.jobsPresent = listing.jobsPresent;

            Id = listing.id;
            ContentIdLower = listing.contentIdLower;
            Name = seStringManager.Parse(listing.name.TakeWhile(b => b != 0).ToArray());
            Description = seStringManager.Parse(listing.description.TakeWhile(b => b != 0).ToArray());
            World = new Lazy<World>(() => dataManager.GetExcelSheet<World>().GetRow(listing.world));
            HomeWorld = new Lazy<World>(() => dataManager.GetExcelSheet<World>().GetRow(listing.homeWorld));
            CurrentWorld = new Lazy<World>(() => dataManager.GetExcelSheet<World>().GetRow(listing.currentWorld));
            Category = (Category) listing.category;
            RawDuty = listing.duty;
            Duty = new Lazy<ContentFinderCondition>(() => dataManager.GetExcelSheet<ContentFinderCondition>().GetRow(listing.duty));
            DutyType = (DutyType) listing.dutyType;
            BeginnersWelcome = listing.beginnersWelcome == 1;
            SecondsRemaining = listing.secondsRemaining;
            MinimumItemLevel = listing.minimumItemLevel;
            Parties = listing.numParties;
            SlotsAvailable = listing.numSlots;
            JobsPresent = listing.jobsPresent
                              .Select(id => new Lazy<ClassJob>(() => id == 0
                                                                          ? null
                                                                          : dataManager.GetExcelSheet<ClassJob>().GetRow(id)))
                              .ToArray();
        }
    }

    /// <summary>
    /// A player slot in a Party Finder listing.
    /// </summary>
    public class PartyFinderSlot {
        private readonly uint accepting;
        private JobFlags[] listAccepting;

        /// <summary>
        /// List of jobs that this slot is accepting.
        /// </summary>
        public IReadOnlyCollection<JobFlags> Accepting {
            get {
                if (this.listAccepting != null) {
                    return this.listAccepting;
                }

                this.listAccepting = Enum.GetValues(typeof(JobFlags))
                                          .Cast<JobFlags>()
                                          .Where(flag => this[flag])
                                          .ToArray();

                return this.listAccepting;
            }
        }

        /// <summary>
        /// Tests if this slot is accepting a job.
        /// </summary>
        /// <param name="flag">Job to test</param>
        public bool this[JobFlags flag] => (this.accepting & (uint) flag) > 0;

        internal PartyFinderSlot(uint accepting) {
            this.accepting = accepting;
        }
    }

    [Flags]
    public enum SearchAreaFlags : uint {
        DataCentre = 1 << 0,
        Private = 1 << 1,
        AllianceRaid = 1 << 2,
        World = 1 << 3,
        OnePlayerPerJob = 1 << 5,
    }

    [Flags]
    public enum JobFlags {
        Gladiator = 1 << 1,
        Pugilist = 1 << 2,
        Marauder = 1 << 3,
        Lancer = 1 << 4,
        Archer = 1 << 5,
        Conjurer = 1 << 6,
        Thaumaturge = 1 << 7,
        Paladin = 1 << 8,
        Monk = 1 << 9,
        Warrior = 1 << 10,
        Dragoon = 1 << 11,
        Bard = 1 << 12,
        WhiteMage = 1 << 13,
        BlackMage = 1 << 14,
        Arcanist = 1 << 15,
        Summoner = 1 << 16,
        Scholar = 1 << 17,
        Rogue = 1 << 18,
        Ninja = 1 << 19,
        Machinist = 1 << 20,
        DarkKnight = 1 << 21,
        Astrologian = 1 << 22,
        Samurai = 1 << 23,
        RedMage = 1 << 24,
        BlueMage = 1 << 25,
        Gunbreaker = 1 << 26,
        Dancer = 1 << 27,
    }

    public static class JobFlagsExt {
        /// <summary>
        /// Get the actual ClassJob from the in-game sheets for this JobFlags.
        /// </summary>
        /// <param name="job">A JobFlags enum member</param>
        /// <param name="data">A DataManager to get the ClassJob from</param>
        /// <returns>A ClassJob if found or null if not</returns>
        public static ClassJob ClassJob(this JobFlags job, DataManager data) {
            var jobs = data.GetExcelSheet<ClassJob>();

            uint? row = job switch {
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

            return row == null ? null : jobs.GetRow((uint) row);
        }
    }

    [Flags]
    public enum ObjectiveFlags : uint {
        None = 0,
        DutyCompletion = 1,
        Practice = 2,
        Loot = 4,
    }

    [Flags]
    public enum ConditionFlags : uint {
        None = 1,
        DutyComplete = 2,
        DutyIncomplete = 4,
    }

    [Flags]
    public enum DutyFinderSettingsFlags : uint {
        None = 0,
        UndersizedParty = 1 << 0,
        MinimumItemLevel = 1 << 1,
        SilenceEcho = 1 << 2,
    }

    [Flags]
    public enum LootRuleFlags : uint {
        None = 0,
        GreedOnly = 1,
        Lootmaster = 2,
    }

    public enum Category {
        Duty = 0,
        QuestBattles = 1 << 0,
        Fates = 1 << 1,
        TreasureHunt = 1 << 2,
        TheHunt = 1 << 3,
        GatheringForays = 1 << 4,
        DeepDungeons = 1 << 5,
        AdventuringForays = 1 << 6,
    }

    public enum DutyType {
        Other = 0,
        Roulette = 1 << 0,
        Normal = 1 << 1,
    }

    #endregion
}

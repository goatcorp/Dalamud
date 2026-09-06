using System.Collections.Generic;
using System.Linq;

using Dalamud.Data;

using FFXIVClientStructs.FFXIV.Client.Game.Network;

using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// A interface representing a single listing in party finder.
/// </summary>
public interface IPartyFinderListing
{
    /// <summary>
    /// Gets  the objective of this listing.
    /// </summary>
    ObjectiveFlags Objective { get; }

    /// <summary>
    /// Gets the conditions of this listing.
    /// </summary>
    ConditionFlags Conditions { get; }

    /// <summary>
    /// Gets the Duty Finder settings that will be used for this listing.
    /// </summary>
    DutyFinderSettingsFlags DutyFinderSettings { get; }

    /// <summary>
    /// Gets the loot rules that will be used for this listing.
    /// </summary>
    LootRuleFlags LootRules { get; }

    /// <summary>
    /// Gets where this listing is searching. Note that this is also used for denoting alliance raid listings and one
    /// player per job.
    /// </summary>
    SearchAreaFlags SearchArea { get; }

    /// <summary>
    /// Gets a list of player slots that the Party Finder is accepting.
    /// </summary>
    IReadOnlyCollection<PartyFinderSlot> Slots { get; }

    /// <summary>
    /// Gets a list of the classes/jobs that are currently present in the party.
    /// </summary>
    IReadOnlyCollection<RowRef<ClassJob>> JobsPresent { get; }

    /// <summary>
    /// Gets the ID assigned to this listing by the game's server.
    /// </summary>
    ulong Id { get; }

    /// <summary>
    /// Gets the player's unique content ID.
    /// </summary>
    ulong ContentId { get; }

    /// <summary>
    /// Gets the name of the player hosting this listing.
    /// </summary>
    ReadOnlySeString Name { get; }

    /// <summary>
    /// Gets the description of this listing as set by the host. May be multiple lines.
    /// </summary>
    ReadOnlySeString Description { get; }

    /// <summary>
    /// Gets the world that this listing was created on.
    /// </summary>
    RowRef<World> World { get; }

    /// <summary>
    /// Gets the home world of the listing's host.
    /// </summary>
    RowRef<World> HomeWorld { get; }

    /// <summary>
    /// Gets the current world of the listing's host.
    /// </summary>
    RowRef<World> CurrentWorld { get; }

    /// <summary>
    /// Gets the Party Finder category this listing is listed under.
    /// </summary>
    DutyCategory Category { get; }

    /// <summary>
    /// Gets the row ID of the duty this listing is for. May be 0 for non-duty listings.
    /// </summary>
    ushort RawDuty { get; }

    /// <summary>
    /// Gets the duty this listing is for. May be null for non-duty listings.
    /// </summary>
    RowRef<ContentFinderCondition> Duty { get; }

    /// <summary>
    /// Gets the type of duty this listing is for.
    /// </summary>
    DutyType DutyType { get; }

    /// <summary>
    /// Gets a value indicating whether if this listing is beginner-friendly. Shown with a sprout icon in-game.
    /// </summary>
    bool BeginnersWelcome { get; }

    /// <summary>
    /// Gets how many seconds this listing will continue to be available for. It may end before this time if the party
    /// fills or the host ends it early.
    /// </summary>
    ushort SecondsRemaining { get; }

    /// <summary>
    /// Gets the minimum item level required to join this listing.
    /// </summary>
    ushort MinimumItemLevel { get; }

    /// <summary>
    /// Gets the number of parties this listing is recruiting for.
    /// </summary>
    byte Parties { get; }

    /// <summary>
    /// Gets the number of player slots this listing is recruiting for.
    /// </summary>
    byte SlotsAvailable { get; }

    /// <summary>
    /// Gets the number of player slots filled.
    /// </summary>
    byte SlotsFilled { get; }

    /// <summary>
    /// Gets the time at which the server this listings is on last restarted for a patch/hotfix.
    /// Probably.
    /// </summary>
    int LastPatchHotfixTimestamp { get; }

    /// <summary>
    /// Gets a list of the class/job IDs that are currently present in the party.
    /// </summary>
    IReadOnlyCollection<byte> RawJobsPresent { get; }

    /// <summary>
    /// Check if the given flag is present.
    /// </summary>
    /// <param name="flag">The flag to check for.</param>
    /// <returns>A value indicating whether the flag is present.</returns>
    bool this[ObjectiveFlags flag] { get; }

    /// <summary>
    /// Check if the given flag is present.
    /// </summary>
    /// <param name="flag">The flag to check for.</param>
    /// <returns>A value indicating whether the flag is present.</returns>
    bool this[ConditionFlags flag] { get; }

    /// <summary>
    /// Check if the given flag is present.
    /// </summary>
    /// <param name="flag">The flag to check for.</param>
    /// <returns>A value indicating whether the flag is present.</returns>
    bool this[DutyFinderSettingsFlags flag] { get; }

    /// <summary>
    /// Check if the given flag is present.
    /// </summary>
    /// <param name="flag">The flag to check for.</param>
    /// <returns>A value indicating whether the flag is present.</returns>
    bool this[LootRuleFlags flag] { get; }

    /// <summary>
    /// Check if the given flag is present.
    /// </summary>
    /// <param name="flag">The flag to check for.</param>
    /// <returns>A value indicating whether the flag is present.</returns>
    bool this[SearchAreaFlags flag] { get; }
}

/// <summary>
/// A single listing in party finder.
/// </summary>
internal unsafe class PartyFinderListing : IPartyFinderListing
{
    private readonly CrossRealmListingSegmentPacket.CrossRealmListing* listing;
    private readonly PartyFinderSlot[] slots;
    private readonly byte[] jobsPresent;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyFinderListing"/> class.
    /// </summary>
    /// <param name="listing">The interop listing data.</param>
    internal PartyFinderListing(CrossRealmListingSegmentPacket.CrossRealmListing* listing)
    {
        this.listing = listing;

        this.slots = new PartyFinderSlot[listing->SlotFlags.Length];
        for (var i = 0; i < this.slots.Length; i++)
            this.slots[i] = new PartyFinderSlot(listing->SlotFlags[i]);

        this.jobsPresent = listing->JobsPresent.ToArray();
        this.JobsPresent = this.jobsPresent
                                  .Select(id => LuminaUtils.CreateRef<ClassJob>(id))
                                  .ToArray();
    }

    /// <inheritdoc/>
    public ulong Id => this.listing->ListingId;

    /// <inheritdoc/>
    public ulong ContentId => this.listing->ContentId;

    /// <inheritdoc/>
    public ReadOnlySeString Name => this.listing->Name;

    /// <inheritdoc/>
    public ReadOnlySeString Description => this.listing->Description;

    /// <inheritdoc/>
    public RowRef<World> World => LuminaUtils.CreateRef<World>(this.listing->WorldId);

    /// <inheritdoc/>
    public RowRef<World> HomeWorld => LuminaUtils.CreateRef<World>(this.listing->HomeWorldId);

    /// <inheritdoc/>
    public RowRef<World> CurrentWorld => LuminaUtils.CreateRef<World>(this.listing->CurrentWorldId);

    /// <inheritdoc/>
    public DutyCategory Category => (DutyCategory)this.listing->Category;

    /// <inheritdoc/>
    public ushort RawDuty => this.listing->Duty;

    /// <inheritdoc/>
    public RowRef<ContentFinderCondition> Duty => LuminaUtils.CreateRef<ContentFinderCondition>(this.listing->Duty);

    /// <inheritdoc/>
    public DutyType DutyType => (DutyType)this.listing->DutyType;

    /// <inheritdoc/>
    public bool BeginnersWelcome => this.listing->BeginnersWelcome == 1;

    /// <inheritdoc/>
    public ushort SecondsRemaining => this.listing->TimeLeft;

    /// <inheritdoc/>
    public ushort MinimumItemLevel => this.listing->AvgItemLv;

    /// <inheritdoc/>
    public byte Parties => this.listing->NumberOfParties;

    /// <inheritdoc/>
    public byte SlotsAvailable => this.listing->TotalSlots;

    /// <inheritdoc/>
    public byte SlotsFilled => this.listing->SlotsFilled;

    /// <inheritdoc/>
    public int LastPatchHotfixTimestamp => this.listing->LastPatchHotfixTimestamp;

    /// <inheritdoc/>
    public IReadOnlyCollection<PartyFinderSlot> Slots => this.slots;

    /// <inheritdoc/>
    public ObjectiveFlags Objective => (ObjectiveFlags)this.listing->Objective;

    /// <inheritdoc/>
    public ConditionFlags Conditions => (ConditionFlags)this.listing->CompletionStatus;

    /// <inheritdoc/>
    public DutyFinderSettingsFlags DutyFinderSettings => (DutyFinderSettingsFlags)this.listing->DutyFinderSettings;

    /// <inheritdoc/>
    public LootRuleFlags LootRules => (LootRuleFlags)this.listing->LootRule;

    /// <inheritdoc/>
    public SearchAreaFlags SearchArea => (SearchAreaFlags)this.listing->JoinConditionFlags;

    /// <inheritdoc/>
    public IReadOnlyCollection<byte> RawJobsPresent => this.jobsPresent;

    /// <inheritdoc/>
    public IReadOnlyCollection<RowRef<ClassJob>> JobsPresent { get; }

    #region Indexers

    /// <inheritdoc/>
    public bool this[ObjectiveFlags flag] => this.listing->Objective == 0 || (this.listing->Objective & (byte)flag) != 0;

    /// <inheritdoc/>
    public bool this[ConditionFlags flag] => this.listing->CompletionStatus == 0 || (this.listing->CompletionStatus & (byte)flag) != 0;

    /// <inheritdoc/>
    public bool this[DutyFinderSettingsFlags flag] => this.listing->DutyFinderSettings == 0 || (this.listing->DutyFinderSettings & (byte)flag) != 0;

    /// <inheritdoc/>
    public bool this[LootRuleFlags flag] => this.listing->LootRule == 0 || (this.listing->LootRule & (byte)flag) != 0;

    /// <inheritdoc/>
    public bool this[SearchAreaFlags flag] => this.listing->JoinConditionFlags == 0 || (this.listing->JoinConditionFlags & (byte)flag) != 0;

    #endregion
}

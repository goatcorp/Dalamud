using System.Collections.Generic;
using System.Linq;

using Dalamud.Data;
using Dalamud.Game.Gui.PartyFinder.Internal;
using Dalamud.Game.Text.SeStringHandling;

using Lumina.Excel.GeneratedSheets;

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
    IReadOnlyCollection<Lazy<ClassJob>> JobsPresent { get; }

    /// <summary>
    /// Gets the ID assigned to this listing by the game's server.
    /// </summary>
    uint Id { get; }

    /// <summary>
    /// Gets the lower bits of the player's content ID.
    /// </summary>
    ulong ContentId { get; }

    /// <summary>
    /// Gets the name of the player hosting this listing.
    /// </summary>
    SeString Name { get; }

    /// <summary>
    /// Gets the description of this listing as set by the host. May be multiple lines.
    /// </summary>
    SeString Description { get; }

    /// <summary>
    /// Gets the world that this listing was created on.
    /// </summary>
    Lazy<World> World { get; }

    /// <summary>
    /// Gets the home world of the listing's host.
    /// </summary>
    Lazy<World> HomeWorld { get; }

    /// <summary>
    /// Gets the current world of the listing's host.
    /// </summary>
    Lazy<World> CurrentWorld { get; }

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
    Lazy<ContentFinderCondition> Duty { get; }

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
    uint LastPatchHotfixTimestamp { get; }

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
internal class PartyFinderListing : IPartyFinderListing
{
    private readonly byte objective;
    private readonly byte conditions;
    private readonly byte dutyFinderSettings;
    private readonly byte lootRules;
    private readonly byte searchArea;
    private readonly PartyFinderSlot[] slots;
    private readonly byte[] jobsPresent;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyFinderListing"/> class.
    /// </summary>
    /// <param name="listing">The interop listing data.</param>
    internal PartyFinderListing(PartyFinderPacketListing listing)
    {
        var dataManager = Service<DataManager>.Get();

        this.objective = listing.Objective;
        this.conditions = listing.Conditions;
        this.dutyFinderSettings = listing.DutyFinderSettings;
        this.lootRules = listing.LootRules;
        this.searchArea = listing.SearchArea;
        this.slots = listing.Slots.Select(accepting => new PartyFinderSlot(accepting)).ToArray();
        this.jobsPresent = listing.JobsPresent;

        this.Id = listing.Id;
        this.ContentId = listing.ContentId;
        this.Name = SeString.Parse(listing.Name.TakeWhile(b => b != 0).ToArray());
        this.Description = SeString.Parse(listing.Description.TakeWhile(b => b != 0).ToArray());
        this.World = new Lazy<World>(() => dataManager.GetExcelSheet<World>().GetRow(listing.World));
        this.HomeWorld = new Lazy<World>(() => dataManager.GetExcelSheet<World>().GetRow(listing.HomeWorld));
        this.CurrentWorld = new Lazy<World>(() => dataManager.GetExcelSheet<World>().GetRow(listing.CurrentWorld));
        this.Category = (DutyCategory)listing.Category;
        this.RawDuty = listing.Duty;
        this.Duty = new Lazy<ContentFinderCondition>(() => dataManager.GetExcelSheet<ContentFinderCondition>().GetRow(listing.Duty));
        this.DutyType = (DutyType)listing.DutyType;
        this.BeginnersWelcome = listing.BeginnersWelcome == 1;
        this.SecondsRemaining = listing.SecondsRemaining;
        this.MinimumItemLevel = listing.MinimumItemLevel;
        this.Parties = listing.NumParties;
        this.SlotsAvailable = listing.NumSlots;
        this.SlotsFilled = listing.NumSlotsFilled;
        this.LastPatchHotfixTimestamp = listing.LastPatchHotfixTimestamp;
        this.JobsPresent = listing.JobsPresent
                                  .Select(id => new Lazy<ClassJob>(
                                              () => id == 0
                                                        ? null
                                                        : dataManager.GetExcelSheet<ClassJob>().GetRow(id)))
                                  .ToArray();
    }

    /// <inheritdoc/>
    public uint Id { get; }

    /// <inheritdoc/>
    public ulong ContentId { get; }

    /// <inheritdoc/>
    public SeString Name { get; }

    /// <inheritdoc/>
    public SeString Description { get; }

    /// <inheritdoc/>
    public Lazy<World> World { get; }

    /// <inheritdoc/>
    public Lazy<World> HomeWorld { get; }

    /// <inheritdoc/>
    public Lazy<World> CurrentWorld { get; }

    /// <inheritdoc/>
    public DutyCategory Category { get; }

    /// <inheritdoc/>
    public ushort RawDuty { get; }

    /// <inheritdoc/>
    public Lazy<ContentFinderCondition> Duty { get; }

    /// <inheritdoc/>
    public DutyType DutyType { get; }

    /// <inheritdoc/>
    public bool BeginnersWelcome { get; }

    /// <inheritdoc/>
    public ushort SecondsRemaining { get; }

    /// <inheritdoc/>
    public ushort MinimumItemLevel { get; }

    /// <inheritdoc/>
    public byte Parties { get; }

    /// <inheritdoc/>
    public byte SlotsAvailable { get; }

    /// <inheritdoc/>
    public byte SlotsFilled { get; }

    /// <inheritdoc/>
    public uint LastPatchHotfixTimestamp { get; }

    /// <inheritdoc/>
    public IReadOnlyCollection<PartyFinderSlot> Slots => this.slots;

    /// <inheritdoc/>
    public ObjectiveFlags Objective => (ObjectiveFlags)this.objective;

    /// <inheritdoc/>
    public ConditionFlags Conditions => (ConditionFlags)this.conditions;

    /// <inheritdoc/>
    public DutyFinderSettingsFlags DutyFinderSettings => (DutyFinderSettingsFlags)this.dutyFinderSettings;

    /// <inheritdoc/>
    public LootRuleFlags LootRules => (LootRuleFlags)this.lootRules;

    /// <inheritdoc/>
    public SearchAreaFlags SearchArea => (SearchAreaFlags)this.searchArea;

    /// <inheritdoc/>
    public IReadOnlyCollection<byte> RawJobsPresent => this.jobsPresent;

    /// <inheritdoc/>
    public IReadOnlyCollection<Lazy<ClassJob>> JobsPresent { get; }

    #region Indexers

    /// <inheritdoc/>
    public bool this[ObjectiveFlags flag] => this.objective == 0 || (this.objective & (uint)flag) > 0;

    /// <inheritdoc/>
    public bool this[ConditionFlags flag] => this.conditions == 0 || (this.conditions & (uint)flag) > 0;

    /// <inheritdoc/>
    public bool this[DutyFinderSettingsFlags flag] => this.dutyFinderSettings == 0 || (this.dutyFinderSettings & (uint)flag) > 0;

    /// <inheritdoc/>
    public bool this[LootRuleFlags flag] => this.lootRules == 0 || (this.lootRules & (uint)flag) > 0;

    /// <inheritdoc/>
    public bool this[SearchAreaFlags flag] => this.searchArea == 0 || (this.searchArea & (uint)flag) > 0;

    #endregion
}


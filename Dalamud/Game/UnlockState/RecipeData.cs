using System.Linq;

using CommunityToolkit.HighPerformance;

using Dalamud.Data;
using Dalamud.Game.Gui;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.Interop;

using Lumina.Excel.Sheets;

namespace Dalamud.Game.UnlockState;

/// <summary>
/// Represents recipe-related data for all crafting classes.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal unsafe class RecipeData : IInternalDisposableService
{
    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ClientState.ClientState clientState = Service<ClientState.ClientState>.Get();

    [ServiceManager.ServiceDependency]
    private readonly GameGui gameGui = Service<GameGui>.Get();

    private readonly ushort[] craftTypeLevels;
    private readonly byte[] unlockedNoteBookDivisionsCount;
    private readonly byte[] unlockedSecretNoteBookDivisionsCount;
    private readonly ushort[,] noteBookDivisionIds;
    private byte[]? cachedUnlockedSecretRecipeBooks;
    private byte[]? cachedUnlockLinks;
    private byte[]? cachedCompletedQuests;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecipeData"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    public RecipeData()
    {
        var numCraftTypes = this.dataManager.GetExcelSheet<CraftType>().Count();
        var numSecretNotBookDivisions = this.dataManager.GetExcelSheet<NotebookDivision>().Count(row => row.RowId is >= 1000 and < 2000);

        this.unlockedNoteBookDivisionsCount = new byte[numCraftTypes];
        this.unlockedSecretNoteBookDivisionsCount = new byte[numCraftTypes];
        this.noteBookDivisionIds = new ushort[numCraftTypes, numSecretNotBookDivisions];

        this.craftTypeLevels = new ushort[numCraftTypes];

        this.clientState.Login += this.Update;
        this.clientState.Logout += this.OnLogout;
        this.clientState.LevelChanged += this.OnlevelChanged;
        this.gameGui.AgentUpdate += this.OnAgentUpdate;
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.clientState.Login -= this.Update;
        this.clientState.Logout -= this.OnLogout;
        this.clientState.LevelChanged -= this.OnlevelChanged;
        this.gameGui.AgentUpdate -= this.OnAgentUpdate;
    }

    /// <summary>
    /// Determines whether the specified Recipe is unlocked.
    /// </summary>
    /// <param name="row">The Recipe row to check.</param>
    /// <returns><see langword="true"/> if unlocked; otherwise, <see langword="false"/>.</returns>
    public bool IsRecipeUnlocked(Recipe row)
    {
        // E8 ?? ?? ?? ?? 48 63 76 (2025.09.04)
        var division = row.RecipeNotebookList.RowId != 0 && row.RecipeNotebookList.IsValid
            ? (row.RecipeNotebookList.RowId - 1000) / 8 + 1000
            : ((uint)row.RecipeLevelTable.Value.ClassJobLevel - 1) / 5;

        // E8 ?? ?? ?? ?? 33 ED 84 C0 75 (2025.09.04)
        foreach (var craftTypeRow in this.dataManager.GetExcelSheet<CraftType>())
        {
            var craftType = (byte)craftTypeRow.RowId;

            if (division < this.unlockedNoteBookDivisionsCount[craftType])
                return true;

            if (this.unlockedNoteBookDivisionsCount[craftType] == 0)
                continue;

            if (division is 5000 or 5001)
                return true;

            if (division < 1000)
                continue;

            if (this.unlockedSecretNoteBookDivisionsCount[craftType] == 0)
                continue;

            if (this.noteBookDivisionIds.GetRowSpan(craftType).Contains((ushort)division))
                return true;
        }

        return false;
    }

    private void OnLogout(int type, int code)
    {
        this.cachedUnlockedSecretRecipeBooks = null;
        this.cachedUnlockLinks = null;
        this.cachedCompletedQuests = null;
    }

    private void OnlevelChanged(uint classJobId, uint level)
    {
        if (this.dataManager.GetExcelSheet<ClassJob>().TryGetRow(classJobId, out var classJobRow) &&
            classJobRow.ClassJobCategory.RowId == 33) // Crafter
        {
            this.Update();
        }
    }

    private void OnAgentUpdate(AgentUpdateFlag agentUpdateFlag)
    {
        if (agentUpdateFlag.HasFlag(AgentUpdateFlag.UnlocksUpdate))
            this.Update();
    }

    private void Update()
    {
        // based on Client::Game::UI::RecipeNote.InitializeStructs

        if (!this.clientState.IsLoggedIn || !this.NeedsUpdate())
            return;

        Array.Clear(this.unlockedNoteBookDivisionsCount, 0, this.unlockedNoteBookDivisionsCount.Length);
        Array.Clear(this.unlockedSecretNoteBookDivisionsCount, 0, this.unlockedSecretNoteBookDivisionsCount.Length);
        Array.Clear(this.noteBookDivisionIds, 0, this.noteBookDivisionIds.Length);

        foreach (var craftTypeRow in this.dataManager.GetExcelSheet<CraftType>())
        {
            var craftType = (byte)craftTypeRow.RowId;
            var craftTypeLevel = RecipeNote.Instance()->GetCraftTypeLevel(craftType);
            if (craftTypeLevel == 0)
                continue;

            var noteBookDivisionIndex = -1;

            foreach (var noteBookDivisionRow in this.dataManager.GetExcelSheet<NotebookDivision>())
            {
                if (noteBookDivisionRow.RowId < 1000)
                {
                    if (craftTypeLevel >= noteBookDivisionRow.CraftOpeningLevel)
                        this.unlockedNoteBookDivisionsCount[craftType]++;
                }
                else if (noteBookDivisionRow.RowId < 2000)
                {
                    noteBookDivisionIndex++;

                    if (!noteBookDivisionRow.AllowedCraftTypes[craftType])
                        continue;

                    if (noteBookDivisionRow.GatheringOpeningLevel != byte.MaxValue)
                        continue;

                    if (noteBookDivisionRow.RequiresSecretRecipeBookGroupUnlock)
                    {
                        var secretRecipeBookUnlocked = false;

                        foreach (var secretRecipeBookGroup in noteBookDivisionRow.SecretRecipeBookGroups)
                        {
                            if (secretRecipeBookGroup.RowId == 0 || !secretRecipeBookGroup.IsValid)
                                continue;

                            var bitIndex = secretRecipeBookGroup.Value.SecretRecipeBook[craftType].RowId;
                            if (PlayerState.Instance()->UnlockedSecretRecipeBooksBitArray.Get((int)bitIndex))
                            {
                                secretRecipeBookUnlocked = true;
                                break;
                            }
                        }

                        if (noteBookDivisionRow.CraftOpeningLevel > craftTypeLevel && !secretRecipeBookUnlocked)
                            continue;
                    }
                    else if (craftTypeLevel < noteBookDivisionRow.CraftOpeningLevel)
                    {
                        continue;
                    }
                    else if (noteBookDivisionRow.QuestUnlock.RowId != 0 && !UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(noteBookDivisionRow.QuestUnlock.RowId))
                    {
                        continue;
                    }

                    this.unlockedSecretNoteBookDivisionsCount[craftType]++;
                    this.noteBookDivisionIds[craftType, noteBookDivisionIndex] = (ushort)noteBookDivisionRow.RowId;
                }
            }
        }
    }

    private bool NeedsUpdate()
    {
        var changed = false;

        foreach (var craftTypeRow in this.dataManager.GetExcelSheet<CraftType>())
        {
            var craftType = (byte)craftTypeRow.RowId;
            var craftTypeLevel = RecipeNote.Instance()->GetCraftTypeLevel(craftType);

            if (this.craftTypeLevels[craftType] != craftTypeLevel)
            {
                this.craftTypeLevels[craftType] = craftTypeLevel;
                changed |= true;
            }
        }

        if (this.cachedUnlockedSecretRecipeBooks == null || !PlayerState.Instance()->UnlockedSecretRecipeBooks.SequenceEqual(this.cachedUnlockedSecretRecipeBooks))
        {
            this.cachedUnlockedSecretRecipeBooks = PlayerState.Instance()->UnlockedSecretRecipeBooks.ToArray();
            changed |= true;
        }

        if (this.cachedUnlockLinks == null || !UIState.Instance()->UnlockLinks.SequenceEqual(this.cachedUnlockLinks))
        {
            this.cachedUnlockLinks = UIState.Instance()->UnlockLinks.ToArray();
            changed |= true;
        }

        if (this.cachedCompletedQuests == null || !QuestManager.Instance()->CompletedQuests.SequenceEqual(this.cachedCompletedQuests))
        {
            this.cachedCompletedQuests = QuestManager.Instance()->CompletedQuests.ToArray();
            changed |= true;
        }

        return changed;
    }
}

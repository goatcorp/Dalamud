using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Completion;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Internal;

/// <summary>
/// This class adds Dalamud and plugin commands to the chat box's autocompletion.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class DalamudCompletion : IInternalDisposableService
{
    // 0xFF is a magic group number that causes CompletionModule's internals to treat entries
    // as raw strings instead of as lookups into an EXD sheet
    private const int GroupNumber = 0xFF;

    [ServiceManager.ServiceDependency]
    private readonly CommandManager commandManager = Service<CommandManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    private readonly Dictionary<string, EntryStrings> cachedCommands = [];

    private EntryStrings? dalamudCategory;

    private Hook<AtkTextInput.Delegates.OpenCompletion> openSuggestionsHook;
    private Hook<CompletionModule.Delegates.GetSelection>? getSelectionHook;

    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudCompletion"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    internal DalamudCompletion()
    {
        this.framework.RunOnTick(this.Setup);
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.openSuggestionsHook?.Disable();
        this.openSuggestionsHook?.Dispose();

        this.getSelectionHook?.Disable();
        this.getSelectionHook?.Dispose();

        this.dalamudCategory?.Dispose();

        this.ClearCachedCommands();
    }

    private void Setup()
    {
        var uiModule = UIModule.Instance();
        if (uiModule == null || uiModule->FrameCount == 0)
        {
            this.framework.RunOnTick(this.Setup);
            return;
        }

        this.dalamudCategory = new EntryStrings("【Dalamud】");

        this.openSuggestionsHook = Hook<AtkTextInput.Delegates.OpenCompletion>.FromAddress(
            (nint)AtkTextInput.MemberFunctionPointers.OpenCompletion,
            this.OpenSuggestionsDetour);

        this.getSelectionHook = Hook<CompletionModule.Delegates.GetSelection>.FromAddress(
            (nint)uiModule->CompletionModule.VirtualTable->GetSelection,
            this.GetSelectionDetour);

        this.openSuggestionsHook.Enable();
        this.getSelectionHook.Enable();
    }

    private void OpenSuggestionsDetour(AtkTextInput* thisPtr)
    {
        this.UpdateCompletionData();
        this.openSuggestionsHook!.Original(thisPtr);
    }

    private int GetSelectionDetour(CompletionModule* thisPtr, CategoryData.CompletionDataStruct* dataStructs, int index, Utf8String* outputString, Utf8String* outputDisplayString)
    {
        var ret = this.getSelectionHook!.Original.Invoke(thisPtr, dataStructs, index, outputString, outputDisplayString);
        this.HandleInsert(ret, outputString, outputDisplayString);
        return ret;
    }

    private void UpdateCompletionData()
    {
        if (!this.TryGetActiveTextInput(out var component, out var addon))
        {
            if (this.HasDalamudCategory())
                this.ResetCompletionData();

            return;
        }

        var uiModule = UIModule.Instance();
        if (uiModule == null)
            return;

        this.ResetCompletionData();
        this.ClearCachedCommands();

        var currentText = component->EvaluatedString.StringPtr.ExtractText();

        var commands = this.commandManager.Commands
            .Where(kv => kv.Value.ShowInHelp && (currentText.Length == 0 || kv.Key.StartsWith(currentText)))
            .OrderBy(kv => kv.Key);

        if (!commands.Any())
            return;

        var categoryData = (CategoryData*)IMemorySpace.GetDefaultSpace()->Malloc((ulong)sizeof(CategoryData), 0x08);
        categoryData->Ctor(GroupNumber, 0xFF);

        uiModule->CompletionModule.AddCategoryData(
            GroupNumber,
            this.dalamudCategory!.Display->StringPtr,
            this.dalamudCategory.Match->StringPtr, categoryData);

        foreach (var (cmd, info) in commands)
        {
            if (!this.cachedCommands.TryGetValue(cmd, out var entryString))
                this.cachedCommands.Add(cmd, entryString = new EntryStrings(cmd));

            uiModule->CompletionModule.AddCompletionEntry(
                GroupNumber,
                0xFF,
                entryString.Display->StringPtr,
                entryString.Match->StringPtr,
                0xFF);
        }

        categoryData->SortEntries();
    }

    private void HandleInsert(int ret, Utf8String* outputString, Utf8String* outputDisplayString)
    {
        // -2 means it was a plain text final selection, so it might be ours.
        if (ret != -2 || outputString == null)
            return;

        // Strip out color payloads that we added to the string.
        var txt = outputString->StringPtr.ExtractText();
        if (!this.cachedCommands.ContainsKey(txt))
            return;

        if (!this.TryGetActiveTextInput(out _, out _))
        {
            outputString->Clear();

            if (outputDisplayString != null)
                outputDisplayString->Clear();

            return;
        }

        outputString->SetString(txt + ' ');
    }

    private bool TryGetActiveTextInput(out AtkComponentTextInput* component, out AtkUnitBase* addon)
    {
        component = null;
        addon = null;

        var raptureAtkModule = RaptureAtkModule.Instance();
        if (raptureAtkModule == null)
            return false;

        var textInputEventInterface = raptureAtkModule->TextInput.TargetTextInputEventInterface;
        if (textInputEventInterface == null)
            return false;

        var ownerNode = textInputEventInterface->GetOwnerNode();
        if (ownerNode == null || ownerNode->GetNodeType() != NodeType.Component)
            return false;

        var componentNode = (AtkComponentNode*)ownerNode;
        var componentBase = componentNode->Component;
        if (componentBase == null || componentBase->GetComponentType() != ComponentType.TextInput)
            return false;

        component = (AtkComponentTextInput*)componentBase;

        addon = component->OwnerAddon;

        if (addon == null)
            addon = component->ContainingAddon2;

        if (addon == null)
            addon = RaptureAtkUnitManager.Instance()->GetAddonByNode((AtkResNode*)component->OwnerNode);

        return addon != null && addon->NameString == "ChatLog";
    }

    private bool HasDalamudCategory()
    {
        var uiModule = UIModule.Instance();
        if (uiModule == null)
            return false;

        for (var i = 0; i < uiModule->CompletionModule.CategoryNames.Count; i++)
        {
            if (uiModule->CompletionModule.CategoryNames[i].AsReadOnlySeStringSpan().ContainsText("【Dalamud】"u8))
            {
                return true;
            }
        }

        return false;
    }

    private void ResetCompletionData()
    {
        var uiModule = UIModule.Instance();
        if (uiModule == null)
            return;

        uiModule->CompletionModule.ClearCompletionData();

        // This happens in UIModule.Update. Just repeat it to fill CompletionData back up with defaults.
        uiModule->CompletionModule.Update(
            &uiModule->CompletionSheetName,
            &uiModule->CompletionOpenIconMacro,
            &uiModule->CompletionCloseIconMacro,
            0);
    }

    private void ClearCachedCommands()
    {
        foreach (var entry in this.cachedCommands.Values)
        {
            entry.Dispose();
        }

        this.cachedCommands.Clear();
    }

    private class EntryStrings : IDisposable
    {
        public EntryStrings(string command)
        {
            using var rssb = new RentedSeStringBuilder();

            this.Display = Utf8String.FromSequence(rssb.Builder
                .PushColorType(539)
                .Append(command)
                .PopColorType()
                .GetViewAsSpan());

            this.Match = Utf8String.FromString(command);
        }

        public Utf8String* Display { get; }

        public Utf8String* Match { get; }

        public void Dispose()
        {
            this.Display->Dtor(true);
            this.Match->Dtor(true);
        }
    }
}

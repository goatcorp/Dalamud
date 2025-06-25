using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Completion;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Text;

namespace Dalamud.Game.Internal;

/// <summary>
/// This class adds dalamud and plugin commands to the chat box's autocompletion.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class Completion : IInternalDisposableService
{
    // 0xFF is a magic group number that causes CompletionModule's internals to treat entries
    // as raw strings instead of as lookups into an EXD sheet
    private const int GroupNumber = 0xFF;

    [ServiceManager.ServiceDependency]
    private readonly CommandManager commandManager = Service<CommandManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    private readonly Dictionary<string, EntryStrings> cachedCommands = [];
    private readonly ConcurrentQueue<string> addedCommands = [];

    private EntryStrings? dalamudCategory;

    private Hook<CompletionModule.Delegates.GetSelection>? getSelectionHook;

    // This is marked volatile since we set and check it from different threads. Instead of using a synchronization
    // primitive, a volatile is sufficient since the absolute worst case is that we delay one extra frame to reset
    // the list, which is fine
    private volatile bool needsClear;

    /// <summary>
    /// Initializes a new instance of the <see cref="Completion"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    internal Completion()
    {
        this.commandManager.CommandAdded += this.OnCommandAdded;
        this.commandManager.CommandRemoved += this.OnCommandRemoved;

        this.framework.Update += this.OnFrameworkUpdate;
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.getSelectionHook?.Disable();
        this.getSelectionHook?.Dispose();

        this.framework.Update -= this.OnFrameworkUpdate;
        this.commandManager.CommandAdded -= this.OnCommandAdded;
        this.commandManager.CommandRemoved -= this.OnCommandRemoved;

        this.dalamudCategory?.Dispose();
        this.ClearCachedCommands();
    }

    private static AtkComponentTextInput* GetActiveTextInput()
    {
        var raptureAtkModule = RaptureAtkModule.Instance();
        if (raptureAtkModule == null)
            return null;

        var textInputEventInterface = raptureAtkModule->TextInput.TargetTextInputEventInterface;
        if (textInputEventInterface == null)
            return null;

        var ownerNode = textInputEventInterface->GetOwnerNode();
        if (ownerNode == null || ownerNode->GetNodeType() != NodeType.Component)
            return null;

        var componentNode = (AtkComponentNode*)ownerNode;
        var component = componentNode->Component;
        if (component == null || component->GetComponentType() != ComponentType.TextInput)
            return null;

        return (AtkComponentTextInput*)component;
    }

    private static bool AllowCompletion(string cmd)
    {
        // This is one of our commands, let's see if we should allow this to be completed
        var component = GetActiveTextInput();
        if (component == null) return false;

        // ContainingAddon or ContainingAddon2 aren't always populated, but they
        // seem to be in any case where this is actually a completable AtkComponentTextInput.
        // In the worst case, we can walk the AtkNode tree, but let's try the easy pointers first.
        var addon = component->ContainingAddon;

        if (addon == null)
            addon = component->ContainingAddon2;

        if (addon == null)
            addon = RaptureAtkUnitManager.Instance()->GetAddonByNode((AtkResNode*)component->OwnerNode);

        if (addon == null || addon->NameString != "ChatLog")
        {
            // We don't know what addon is completing or we know it isn't ChatLog.
            // Either way, we should just reject this completion.
            return false;
        }

        // We're in ChatLog, so check if this is the start of the text input.
        // AtkComponentTextInput->UnkText1 is the evaluated version of the current text.
        // If the command starts with that, then either it's empty or a prefix completion.
        // In either case, we're happy to allow completion.
        return cmd.StartsWith(component->UnkText1.StringPtr.ExtractText());
    }

    private void OnCommandAdded(object? sender, CommandManager.CommandEventArgs e)
    {
        if (e.CommandInfo.ShowInHelp)
            this.addedCommands.Enqueue(e.Command);
    }

    private void OnCommandRemoved(object? sender, CommandManager.CommandEventArgs e)
    {
        this.needsClear = true;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var atkModule = RaptureAtkModule.Instance();
        if (atkModule == null)
            return;

        ref var textInput = ref atkModule->TextInput;
        if (textInput.CompletionModule == null)
            return;

        // If CategoryData isn't loaded yet, which should happen in CompletionModule.Update(), bail out.
        if (textInput.CompletionModule->CategoryData.RepresentativePointer == null)
            return;

        // Before we change _anything_ we need to check the state of the UI (if the completion list is open).
        // Changes to the underlying data are extremely unsafe, so we'll just wait until the next frame.
        // Worst case, someone tries to complete a command that _just_ got unloaded so it won't do anything,
        // but that's the same as making a typo, really.
        if (textInput.CompletionDepth > 0)
            return;

        // Create the category for Dalamud commands.
        // This needs to be done here, since we cannot create Utf8Strings before the game
        // has initialized (no allocator set up yet).
        this.dalamudCategory ??= new EntryStrings("【Dalamud】");

        this.LoadCommands(textInput.CompletionModule);
    }

    private CategoryData* EnsureCategoryData(CompletionModule* module)
    {
        if (module == null)
            return null;

        if (this.getSelectionHook == null)
        {
            this.getSelectionHook = Hook<CompletionModule.Delegates.GetSelection>.FromAddress(
                (nint)module->VirtualTable->GetSelection,
                this.GetSelectionDetour);

            this.getSelectionHook.Enable();
        }

        for (var i = 0; i < module->CategoryNames.Count; i++)
        {
            if (module->CategoryNames[i].AsReadOnlySeStringSpan().ContainsText("【Dalamud】"u8))
            {
                return module->CategoryData[i];
            }
        }

        // Create the category since we don't have one
        var categoryData = (CategoryData*)IMemorySpace.GetDefaultSpace()->Malloc((ulong)sizeof(CategoryData), 0x08);
        categoryData->Ctor(GroupNumber, 0xFF);

        module->AddCategoryData(
            GroupNumber,
            this.dalamudCategory!.Display->StringPtr,
            this.dalamudCategory.Match->StringPtr, categoryData);

        return categoryData;
    }

    private void ClearCachedCommands()
    {
        if (this.cachedCommands.Count == 0)
            return;

        foreach (var entry in this.cachedCommands.Values)
        {
            entry.Dispose();
        }

        this.cachedCommands.Clear();
    }

    private void LoadCommands(CompletionModule* completionModule)
    {
        if (completionModule == null)
            return;

        // We want this data populated first
        if (completionModule->CategoryNames.Count == 0)
            return;

        if (this.needsClear && this.cachedCommands.Count > 0)
        {
            this.needsClear = false;
            completionModule->ClearCompletionData();
            this.ClearCachedCommands();
            return;
        }

        var catData = this.EnsureCategoryData(completionModule);
        if (catData == null) return;

        if (catData->CompletionData.Count == 0)
        {
            var inputCommands = this.commandManager.Commands.Where(pair => pair.Value.ShowInHelp);

            foreach (var (cmd, _) in inputCommands)
                AddEntry(cmd);

            catData->SortEntries();

            return;
        }

        var needsSort = false;
        while (this.addedCommands.TryDequeue(out var cmd))
        {
            needsSort = true;
            AddEntry(cmd);
        }

        if (needsSort)
            catData->SortEntries();

        return;

        void AddEntry(string cmd)
        {
            if (this.cachedCommands.ContainsKey(cmd)) return;

            var cmdStr = new EntryStrings(cmd);
            this.cachedCommands.Add(cmd, cmdStr);
            completionModule->AddCompletionEntry(
                GroupNumber,
                0xFF,
                cmdStr.Display->StringPtr,
                cmdStr.Match->StringPtr,
                0xFF);
        }
    }

    private int GetSelectionDetour(CompletionModule* thisPtr, CategoryData.CompletionDataStruct* dataStructs, int index, Utf8String* outputString, Utf8String* outputDisplayString)
    {
        var ret = this.getSelectionHook!.Original.Invoke(thisPtr, dataStructs, index, outputString, outputDisplayString);
        if (ret != -2 || outputString == null)
            return ret;

        // -2 means it was a plain text final selection, so it might be ours.
        // Unfortunately, the code that uses this string mangles the color macro for some reason...
        // We'll just strip those out since we don't need the color in the chatbox.
        var txt = outputString->StringPtr.ExtractText();

        if (!this.cachedCommands.ContainsKey(txt))
            return ret;

        if (!AllowCompletion(txt))
        {
            outputString->Clear();

            if (outputDisplayString != null)
                outputDisplayString->Clear();

            return ret;
        }

        outputString->SetString(txt + " ");
        return ret;
    }

    private class EntryStrings : IDisposable
    {
        public EntryStrings(string command)
        {
            var rssb = SeStringBuilder.SharedPool.Get();

            this.Display = Utf8String.FromSequence(rssb
                .PushColorType(539)
                .Append(command)
                .PopColorType()
                .GetViewAsSpan());

            SeStringBuilder.SharedPool.Return(rssb);

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

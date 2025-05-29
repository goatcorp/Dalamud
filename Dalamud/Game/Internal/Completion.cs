using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Completion;
using FFXIVClientStructs.FFXIV.Component.GUI;

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

    private Hook<CompletionModule.Delegates.GetSelection>? getSelection;

    // This is marked volatile since we set and check it from different threads. Instead of using a synchronization
    // primitive, a volatile is sufficient since the absolute worst case is that we delay one extra frame to reset
    // the list, which is fine
    private volatile bool needsClear;
    private bool disposed;
    private nint wantedVtblPtr;

    /// <summary>
    /// Initializes a new instance of the <see cref="Completion"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    internal Completion()
    {
        this.commandManager.CommandAdded += this.OnCommandAdded;
        this.commandManager.CommandRemoved += this.OnCommandRemoved;

        this.framework.Update += this.OnUpdate;
    }

    /// <summary>Finalizes an instance of the <see cref="Completion"/> class.</summary>
    ~Completion() => this.Dispose(false);

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.Dispose(true);

    private static AtkUnitBase* FindOwningAddon(AtkComponentTextInput* component)
    {
        if (component == null) return null;

        var node = (AtkResNode*)component->OwnerNode;
        if (node == null) return null;

        while (node->ParentNode != null)
            node = node->ParentNode;

        foreach (var addon in RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries)
        {
            if (addon.Value->RootNode == node)
                return addon;
        }

        return null;
    }

    private AtkComponentTextInput* GetActiveTextInput()
    {
        var mod = RaptureAtkModule.Instance();
        if (mod == null) return null;

        var basePtr = mod->TextInput.TargetTextInputEventInterface;
        if (basePtr == null) return null;

        // Once CS has an implementation for multiple inheritance, we can remove this sig from dalamud
        // as well as the nasty pointer arithmetic below. But for now, we need to do this manually.
        // The AtkTextInputEventInterface* is the secondary base class for AtkComponentTextInput*
        // so the pointer is sizeof(AtkComponentInputBase) into the object. We verify that we're looking
        // at the object we think we are by confirming the pointed-to vtbl matches the known secondary vtbl for
        // AtkComponentTextInput, and if it does, we can shift the pointer back to get the start of our text input
        if (this.wantedVtblPtr == 0)
        {
            this.wantedVtblPtr =
                Service<TargetSigScanner>.Get().GetStaticAddressFromSig(
                    "48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 8B 48 68",
                    4);
        }

        var vtblPtr = *(nint*)basePtr;
        if (vtblPtr != this.wantedVtblPtr) return null;

        // This needs to be updated if the layout/base order of AtkComponentTextInput changes
        return (AtkComponentTextInput*)((AtkComponentInputBase*)basePtr - 1);
    }

    private bool AllowCompletion(string cmd)
    {
        // this is one of our commands, let's see if we should allow this to be completed
        var component = this.GetActiveTextInput();

        // ContainingAddon or ContainingAddon2 aren't always populated, but they
        // seem to be in any case where this is actually a completable AtkComponentTextInput
        // In the worst case, we can walk the AtkNode tree- but let's try the easy pointers first
        var addon = component->ContainingAddon;
        if (addon == null) addon = component->ContainingAddon2;
        if (addon == null) addon = FindOwningAddon(component);

        if (addon == null || addon->NameString != "ChatLog")
        {
            // we don't know what addon is completing, or we know it isn't ChatLog
            // either way, we should just reject this completion
            return false;
        }

        // We're in ChatLog, so check if this is the start of the text input
        // AtkComponentTextInput->UnkText1 is the evaluated version of the current text
        // so if the command starts with that, then either it's empty or a prefix completion.
        // In either case, we're happy to allow completion.
        return cmd.StartsWith(component->UnkText1.StringPtr.ExtractText());
    }

    private void Dispose(bool disposing)
    {
        if (this.disposed)
            return;

        if (disposing)
        {
            this.getSelection?.Disable();
            this.getSelection?.Dispose();
            this.framework.Update -= this.OnUpdate;
            this.commandManager.CommandAdded -= this.OnCommandAdded;
            this.commandManager.CommandRemoved -= this.OnCommandRemoved;

            this.dalamudCategory?.Dispose();
            this.ClearCachedCommands();
        }

        this.disposed = true;
    }

    private void OnCommandAdded(object? sender, CommandManager.CommandEventArgs e)
    {
        if (e.CommandInfo.ShowInHelp)
            this.addedCommands.Enqueue(e.Command);
    }

    private void OnCommandRemoved(object? sender, CommandManager.CommandEventArgs e) => this.needsClear = true;

    private void OnUpdate(IFramework fw)
    {
        var atkModule = RaptureAtkModule.Instance();
        if (atkModule == null) return;

        var textInput = &atkModule->TextInput;

        if (textInput->CompletionModule == null) return;

        // Before we change _anything_ we need to check the state of the UI- if the completion list is open
        // changes to the underlying data are extremely unsafe, so we'll just wait until the next frame
        // worst case, someone tries to complete a command that _just_ got unloaded so it won't do anything
        // but that's the same as making a typo, really
        if (textInput->CompletionDepth > 0) return;

        // Create the category for Dalamud commands.
        // This needs to be done here, since we cannot create Utf8Strings before the game
        // has initialized (no allocator set up yet).
        this.dalamudCategory ??= new EntryStrings("【Dalamud】");

        this.LoadCommands(textInput->CompletionModule);
    }

    private CategoryData* EnsureCategoryData(CompletionModule* module)
    {
        if (module == null) return null;

        if (this.getSelection == null)
        {
            this.getSelection = Hook<CompletionModule.Delegates.GetSelection>.FromAddress(
                (IntPtr)module->VirtualTable->GetSelection,
                this.GetSelectionDetour);
            this.getSelection.Enable();
        }

        for (var i = 0; i < module->CategoryNames.Count; i++)
        {
            if (module->CategoryNames[i].ExtractText() == "【Dalamud】")
            {
                return module->CategoryData[i];
            }
        }

        // Create the category since we don't have one
        var categoryData = (CategoryData*)Memory.MemoryHelper.GameAllocateDefault((ulong)sizeof(CategoryData));
        categoryData->Ctor(GroupNumber, 0xFF);
        module->AddCategoryData(GroupNumber, this.dalamudCategory!.Display->StringPtr,
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
        if (completionModule == null) return;
        if (completionModule->CategoryNames.Count == 0) return; // We want this data populated first

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
            var inputCommands = this.commandManager.Commands.Where(pair => pair.Value.ShowInHelp).OrderBy(pair => pair.Key);
            foreach (var (cmd, _) in inputCommands)
                AddEntry(cmd);

            return;
        }

        while (this.addedCommands.TryDequeue(out var cmd))
            AddEntry(cmd);

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
        var ret = this.getSelection!.Original.Invoke(thisPtr, dataStructs, index, outputString, outputDisplayString);
        if (ret != -2 || outputString == null) return ret;

        // -2 means it was a plain text final selection, so it might be ours
        // Unfortunately, the code that uses this string mangles the color macro for some reason...
        // We'll just strip those out since we don't need the color in the chatbox
        var txt = outputString->StringPtr.ExtractText();
        if (!this.cachedCommands.ContainsKey(txt))
            return ret;

        if (!this.AllowCompletion(txt))
        {
            outputString->Clear();
            if (outputDisplayString != null) outputDisplayString->Clear();
            return ret;
        }

        outputString->SetString(txt + " ");
        return ret;
    }

    private class EntryStrings(string command) : IDisposable
    {
        public Utf8String* Display { get; } =
            Utf8String.FromSequence(new SeStringBuilder().AddUiForeground(command, 539).Encode());

        public Utf8String* Match { get; } = Utf8String.FromString(command);

        public void Dispose()
        {
            this.Display->Dtor(true);
            this.Match->Dtor(true);
        }
    }
}

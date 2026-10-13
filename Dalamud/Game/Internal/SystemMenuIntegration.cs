using CheapLoc;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Hooking;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace Dalamud.Game.Internal;

/// <summary>
/// This class implements in-game Dalamud options in the in-game System menu.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class SystemMenuIntegration : IInternalDisposableService
{
    private const int SystemMenuPluginsId = 69420;
    private const int SystemMenuSettingsId = 69421;
    private const int MainCrossPluginsId = 69422;
    private const int MainCrossSettingsId = 69423;

    private static readonly ModuleLog Log = ModuleLog.Create<SystemMenuIntegration>();

    private readonly Hook<AgentHUD.Delegates.OpenSystemMenu> hookAgentHudOpenSystemMenu;
    private readonly Hook<UIModule.Delegates.ExecuteMainCommand> hookUiModuleExecuteMainCommand; // TODO: Make this into events in Framework.Gui

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    // [ServiceManager.ServiceDependency]
    // private readonly ContextMenu contextMenu = Service<ContextMenu>.Get();

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycle = Service<AddonLifecycle>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Localization localization = Service<Localization>.Get();

    private AddonLifecycleEventListener mainCrossPostSetupListener;
    private AddonLifecycleEventListener mainCrossPostReceiveEventListener;

    [ServiceManager.ServiceConstructor]
    private SystemMenuIntegration()
    {
        this.hookAgentHudOpenSystemMenu = Hook<AgentHUD.Delegates.OpenSystemMenu>.FromAddress(AgentHUD.Addresses.OpenSystemMenu.Value, this.AgentHudOpenSystemMenuDetour);
        this.hookUiModuleExecuteMainCommand = Hook<UIModule.Delegates.ExecuteMainCommand>.FromAddress((nint)UIModule.StaticVirtualTablePointer->ExecuteMainCommand, this.UiModuleExecuteMainCommandDetour);

        // this.contextMenu.ContextMenuOpened += this.ContextMenuOnContextMenuOpened;
        this.localization.LocalizationChanged += this.OnLocalizationChanged;

        this.hookAgentHudOpenSystemMenu.Enable();
        this.hookUiModuleExecuteMainCommand.Enable();

        this.mainCrossPostSetupListener = new AddonLifecycleEventListener(AddonEvent.PostSetup, "_MainCross", this.OnMainCrossPostSetup);
        this.mainCrossPostReceiveEventListener = new AddonLifecycleEventListener(AddonEvent.PostReceiveEvent, "_MainCross", this.OnMainCrossPostReceiveEvent);

        this.addonLifecycle.RegisterListener(this.mainCrossPostSetupListener);
        this.addonLifecycle.RegisterListener(this.mainCrossPostReceiveEventListener);
    }

    private string LocDalamudPlugins => Loc.Localize("SystemMenuPlugins", "Dalamud Plugins");

    private string LocDalamudSettings => Loc.Localize("SystemMenuSettings", "Dalamud Settings");

    private string LocDalamudPluginsDescription => Loc.Localize("SystemMenuPluginsDescription", "Opens the Dalamud Plugin Installer.");

    private string LocDalamudSettingsDescription => Loc.Localize("SystemMenuSettingsDescription", "Opens the Dalamud Settings.");

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.hookAgentHudOpenSystemMenu.Dispose();
        this.hookUiModuleExecuteMainCommand.Dispose();

        // this.contextMenu.ContextMenuOpened -= this.ContextMenuOnContextMenuOpened;
        this.localization.LocalizationChanged -= this.OnLocalizationChanged;

        this.addonLifecycle.UnregisterListener(this.mainCrossPostSetupListener);
        this.addonLifecycle.UnregisterListener(this.mainCrossPostReceiveEventListener);
    }

    private static T* NewDynamicArray<T>(int length) where T : unmanaged, ICreatable<T>
    {
        var ptr = (nint)IMemorySpace.GetUISpace()->Malloc((ulong)(nint.Size + sizeof(T) * length), 0);

        *(nint*)ptr = length;

        var elementsPtr = (T*)(ptr + nint.Size);

        for (var i = 0; i < length; i++)
            elementsPtr[i].Ctor();

        return elementsPtr;
    }

    private static T* EnlargeArray<T>(T* oldArray, int oldLength, int length) where T : unmanaged
    {
        var oldSize = (ulong)(sizeof(T) * oldLength);
        var newSize = (ulong)(sizeof(T) * length);

        var newArray = (T*)IMemorySpace.GetUISpace()->Malloc(newSize, 0);

        Buffer.MemoryCopy(oldArray, newArray, newSize, oldSize);

        IMemorySpace.GetDefaultSpace()->AlignedFree(oldArray);

        return newArray;
    }

    private void OnLocalizationChanged(string langCode)
    {
        var gameGui = Service<GameGui>.GetNullable();
        if (gameGui == null)
            return;

        var addon = gameGui.GetAddonByName<AddonMainCross>("_MainCross"u8);
        if (addon == null)
            return;

        foreach (ref var category in addon->CategoryData)
        {
            foreach (ref var item in category.Commands)
            {
                switch (item.RowId)
                {
                    case MainCrossPluginsId:
                        this.SetMainCrossItemText(ref item, this.LocDalamudPlugins);
                        break;
                    case MainCrossSettingsId:
                        this.SetMainCrossItemText(ref item, this.LocDalamudSettings);
                        break;
                }
            }
        }

        addon->RefreshCurrentItemNodes();
        addon->UpdateHelpText(false);
    }

    private void OnMainCrossPostSetup(AddonEvent type, AddonArgs args)
    {
        if (!this.configuration.DoButtonsSystemMenu)
            return;

        var addon = (AddonMainCross*)args.Addon.Address;

        var oldItemNodeCount = addon->ItemMaxCount;
        var newItemNodeCount = oldItemNodeCount + 2;

        // Resize CurrentItemTweens
        addon->CurrentItemTweens->Dtor(3); // delete[]
        addon->CurrentItemTweens = NewDynamicArray<AtkSimpleTween>(newItemNodeCount); // new T[n]

        // Resize CurrentItemNodes
        addon->CurrentItemNodes = EnlargeArray(addon->CurrentItemNodes, oldItemNodeCount, newItemNodeCount);

        // Add Dalamud commands (in reverse order so it's simpler)
        var lastCategoryIndex = addon->CategoryData.Length - 1;
        var category = addon->CategoryData.GetPointer(lastCategoryIndex);

        var newItem = default(AddonMainCross.CommandData);
        newItem.Label.Ctor();
        newItem.LabelWithPatchMark.Ctor();

        newItem.RowId = MainCrossSettingsId;
        newItem.IconId = 14;
        newItem.SortId = category->Commands.First->SortId - 1;
        newItem.IsEnabled = true;
        newItem.IsUnseen = false;
        this.SetMainCrossItemText(ref newItem, this.LocDalamudSettings);
        AddonMainCross.StdVectorCommandDataInsert(&category->Commands, category->Commands.First, &newItem);

        newItem.RowId = MainCrossPluginsId;
        newItem.IconId = 14;
        newItem.SortId = category->Commands.First->SortId - 1;
        newItem.IsEnabled = true;
        newItem.IsUnseen = false;
        this.SetMainCrossItemText(ref newItem, this.LocDalamudPlugins);
        AddonMainCross.StdVectorCommandDataInsert(&category->Commands, category->Commands.First, &newItem);

        newItem.Label.Dtor(false);
        newItem.LabelWithPatchMark.Dtor(false);

        // Create new ItemNodes
        for (var colIdx = 0; colIdx < addon->ColumnNodes.Length; colIdx++)
        {
            ref var column = ref addon->ColumnNodes[colIdx];

            column.ItemNodes = EnlargeArray(column.ItemNodes, oldItemNodeCount, newItemNodeCount);

            ref var uldManager = ref column.ComponentNode->GetComponent()->UldManager;
            var baseNodeId = uldManager.SearchNodeById(2)->GetBaseNodeId();
            uldManager.DuplicateComponentNode(baseNodeId, 2, (uint)oldItemNodeCount); // duplicate 2 new nodes

            for (var entryIdx = oldItemNodeCount; entryIdx < newItemNodeCount; entryIdx++)
            {
                ref var entryNode = ref column.ItemNodes[entryIdx];

                entryNode.Node = (AtkComponentNode*)uldManager.GetDuplicatedNode(baseNodeId, (uint)entryIdx, 0);
                entryNode.Component = uldManager.GetDuplicatedNode(baseNodeId, (uint)entryIdx, 0)->GetAsAtkComponentButton();
                entryNode.TextNode = entryNode.Component->GetTextNodeById(2);
            }
        }

        // Update length
        addon->ItemMaxCount = newItemNodeCount;

        // Push down default selection for category
        addon->DefaultCategoryItemIndexes[lastCategoryIndex] += 2;

        // Trigger refresh
        addon->RefreshCurrentItemNodes();
    }

    private void OnMainCrossPostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonReceiveEventArgs { AtkEventType: AddonEventType.InputReceived } receiveEventArgs)
            return;

        var inputData = (AtkEventData.AtkInputData*)receiveEventArgs.AtkEventData;
        var inputId = (InputId)inputData->InputId;
        if (inputId is not (InputId.LEFT or InputId.RIGHT or InputId.UP or InputId.DOWN))
            return;

        this.UpdateMainCrossHelpText((AddonMainCross*)args.Addon.Address);
    }

    private void SetMainCrossItemText(ref AddonMainCross.CommandData item, string text)
    {
        using var rssb = new RentedSeStringBuilder();

        rssb
            .PushColorType(539)
            .PushEdgeColorBgra(0, 0, 0, 255)
            .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
            .PopEdgeColor()
            .PopColorType()
            .Append(text);

        item.Label.SetString(rssb.GetViewAsSpan());
        item.LabelWithPatchMark.SetString(rssb.GetViewAsSpan());
    }

    private void UpdateMainCrossHelpText(AddonMainCross* addon)
    {
        switch (addon->CategoryData[addon->SelectedCategory].Commands[addon->SelectedItem].RowId)
        {
            case MainCrossPluginsId:
                SetHelpText(this.LocDalamudPluginsDescription);
                break;

            case MainCrossSettingsId:
                SetHelpText(this.LocDalamudSettingsDescription);
                break;
        }

        void SetHelpText(string text)
        {
            var componentNode = (AtkComponentNode*)addon->GetNodeById(5);
            if (componentNode == null) return;

            var textNode = componentNode->Component->GetTextNodeById(3);
            if (textNode == null) return;

            textNode->SetText(text);
        }
    }

    /*
    private void ContextMenuOnContextMenuOpened(ContextMenuOpenedArgs args)
    {
        var systemText = Service<DataManager>.GetNullable()?.GetExcelSheet<Addon>()?.GetRow(1059)?.Text?.RawString; // "System"
        var interfaceManager = Service<InterfaceManager>.GetNullable();

        if (systemText == null || interfaceManager == null)
            return;

        if (args.Title == systemText && this.configuration.DoButtonsSystemMenu && interfaceManager.IsDispatchingEvents)
        {
            var dalamudInterface = Service<DalamudInterface>.Get();

            args.Items.Insert(0, new CustomContextMenuItem(this.LocDalamudSettings, selectedArgs =>
            {
                dalamudInterface.ToggleSettingsWindow();
            }));

            args.Items.Insert(0, new CustomContextMenuItem(this.LocDalamudPlugins, selectedArgs =>
            {
                dalamudInterface.TogglePluginInstallerWindow();
            }));
        }
    }
    */

    private void AgentHudOpenSystemMenuDetour(AgentHUD* thisPtr, AtkValue* atkValueArgs, uint menuSize)
    {
        if (WindowSystem.ShouldInhibitAtkCloseEvents && this.configuration.IsFocusManagementEnabled)
        {
            Log.Verbose($"Cancelling OpenSystemMenu due to WindowSystem {WindowSystem.FocusedWindowSystemNamespace}");
            return;
        }

        var interfaceManager = Service<InterfaceManager>.GetNullable();
        if (interfaceManager == null)
        {
            this.hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize);
            return;
        }

        if (!this.configuration.DoButtonsSystemMenu || !interfaceManager.IsDispatchingEvents)
        {
            this.hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize);
            return;
        }

        const int maxEntries = 20; // the hardcoded amount of maximum entries
        const int startIndex = 5; // the offset at which entries start
        const int offset = 2; // the amount of entries we want to inject

        var newMenuSize = (int)menuSize + offset;
        if (newMenuSize >= maxEntries)
        {
            this.hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize);
            return;
        }

        using var values = new RentedAtkValues(startIndex + (maxEntries * 2));

        // copy beginning of AtkValues
        for (var i = 0; i < startIndex; i++)
            values[i].Copy(&atkValueArgs[i]);

        // copy entries, but shifted
        for (var i = startIndex; i < startIndex + menuSize; i++)
        {
            values[i + offset].Copy(&atkValueArgs[i]);
            values[i + offset + maxEntries].Copy(&atkValueArgs[i + maxEntries]);
        }

        // set new menu size
        values[3].SetInt(newMenuSize);

        // set our new entries to dummy commands
        const int color = 539;
        using var rssb = new RentedSeStringBuilder();
        var entryIndex = startIndex;

        values[entryIndex].SetInt(SystemMenuPluginsId);
        values[entryIndex + maxEntries].SetManagedString(rssb.Builder
            .PushColorType(color)
            .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
            .PopColorType()
            .Append(this.LocDalamudPlugins)
            .GetViewAsSpan());

        rssb.Builder.Clear();
        entryIndex++;

        values[entryIndex].SetInt(SystemMenuSettingsId);
        values[entryIndex + maxEntries].SetManagedString(rssb.Builder
            .PushColorType(color)
            .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
            .PopColorType()
            .Append(this.LocDalamudSettings)
            .GetViewAsSpan());

        this.hookAgentHudOpenSystemMenu.Original(thisPtr, values, (uint)newMenuSize);
    }

    private void UiModuleExecuteMainCommandDetour(UIModule* thisPtr, uint commandId)
    {
        switch (commandId)
        {
            case SystemMenuPluginsId:
                Service<DalamudInterface>.GetNullable()?.OpenPluginInstaller();
                break;
            case SystemMenuSettingsId:
                Service<DalamudInterface>.GetNullable()?.OpenSettings();
                break;
            case MainCrossPluginsId:
                Service<GamepadState>.GetNullable()?.EnableGamepadNav = true;
                Service<DalamudInterface>.GetNullable()?.OpenPluginInstaller();
                break;
            case MainCrossSettingsId:
                Service<GamepadState>.GetNullable()?.EnableGamepadNav = true;
                Service<DalamudInterface>.GetNullable()?.OpenSettings();
                break;
            default:
                this.hookUiModuleExecuteMainCommand.Original(thisPtr, commandId);
                break;
        }
    }
}

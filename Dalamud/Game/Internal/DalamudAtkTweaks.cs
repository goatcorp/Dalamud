using CheapLoc;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Text;
using Dalamud.Hooking;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Dalamud.Game.Internal;

/// <summary>
/// This class implements in-game Dalamud options in the in-game System menu.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class DalamudAtkTweaks : IInternalDisposableService
{
    private static readonly ModuleLog Log = new("DalamudAtkTweaks");

    private readonly Hook<AgentHUD.Delegates.OpenSystemMenu> hookAgentHudOpenSystemMenu;

    // TODO: Make this into events in Framework.Gui
    private readonly Hook<UIModule.Delegates.ExecuteMainCommand> hookUiModuleExecuteMainCommand;

    private readonly Hook<AtkUnitBase.Delegates.ReceiveGlobalEvent> hookAtkUnitBaseReceiveGlobalEvent;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    // [ServiceManager.ServiceDependency]
    // private readonly ContextMenu contextMenu = Service<ContextMenu>.Get();

    private readonly string locDalamudPlugins;
    private readonly string locDalamudSettings;

    private bool disposed = false;

    [ServiceManager.ServiceConstructor]
    private DalamudAtkTweaks(TargetSigScanner sigScanner)
    {
        this.hookAgentHudOpenSystemMenu = Hook<AgentHUD.Delegates.OpenSystemMenu>.FromAddress(AgentHUD.Addresses.OpenSystemMenu.Value, this.AgentHudOpenSystemMenuDetour);
        this.hookUiModuleExecuteMainCommand = Hook<UIModule.Delegates.ExecuteMainCommand>.FromAddress((nint)UIModule.StaticVirtualTablePointer->ExecuteMainCommand, this.UiModuleExecuteMainCommandDetour);
        this.hookAtkUnitBaseReceiveGlobalEvent = Hook<AtkUnitBase.Delegates.ReceiveGlobalEvent>.FromAddress((nint)AtkUnitBase.StaticVirtualTablePointer->ReceiveGlobalEvent, this.AtkUnitBaseReceiveGlobalEventDetour);

        this.locDalamudPlugins = Loc.Localize("SystemMenuPlugins", "Dalamud Plugins");
        this.locDalamudSettings = Loc.Localize("SystemMenuSettings", "Dalamud Settings");

        // this.contextMenu.ContextMenuOpened += this.ContextMenuOnContextMenuOpened;

        this.hookAgentHudOpenSystemMenu.Enable();
        this.hookUiModuleExecuteMainCommand.Enable();
        this.hookAtkUnitBaseReceiveGlobalEvent.Enable();
    }

    /// <summary>Finalizes an instance of the <see cref="DalamudAtkTweaks"/> class.</summary>
    ~DalamudAtkTweaks() => this.Dispose(false);

    private delegate void AgentHudOpenSystemMenuPrototype(AgentHUD* thisPtr, AtkValue* atkValueArgs, uint menuSize);

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.Dispose(true);

    private void Dispose(bool disposing)
    {
        if (this.disposed)
            return;

        if (disposing)
        {
            this.hookAgentHudOpenSystemMenu.Dispose();
            this.hookUiModuleExecuteMainCommand.Dispose();
            this.hookAtkUnitBaseReceiveGlobalEvent.Dispose();

            // this.contextMenu.ContextMenuOpened -= this.ContextMenuOnContextMenuOpened;
        }

        this.disposed = true;
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

            args.Items.Insert(0, new CustomContextMenuItem(this.locDalamudSettings, selectedArgs =>
            {
                dalamudInterface.ToggleSettingsWindow();
            }));

            args.Items.Insert(0, new CustomContextMenuItem(this.locDalamudPlugins, selectedArgs =>
            {
                dalamudInterface.TogglePluginInstallerWindow();
            }));
        }
    }
    */

    private void AtkUnitBaseReceiveGlobalEventDetour(AtkUnitBase* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        // 3 == Close
        if (eventType == AtkEventType.InputReceived && WindowSystem.ShouldInhibitAtkCloseEvents && atkEventData != null && *(int*)atkEventData == 3 && this.configuration.IsFocusManagementEnabled)
        {
            Log.Verbose($"Cancelling global event SendHotkey command due to WindowSystem {WindowSystem.FocusedWindowSystemNamespace}");
            return;
        }

        this.hookAtkUnitBaseReceiveGlobalEvent.Original(thisPtr, eventType, eventParam, atkEvent, atkEventData);
    }

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

        // the max size (hardcoded) is 0x12/18, but the system menu currently uses 0xC/12
        // this is a just in case that doesnt really matter
        // see if we can add 2 entries
        if (menuSize >= 0x12)
        {
            this.hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize);
            return;
        }

        // atkValueArgs is actually an array of AtkValues used as args. all their UI code works like this.
        // in this case, menu size is stored in atkValueArgs[4], and the next 17 slots are the MainCommand
        // the 17 slots after that, if they exist, are the entry names, but they are otherwise pulled from MainCommand EXD
        // reference the original function for more details :)

        // step 1) move all the current menu items down so we can put Dalamud at the top like it deserves
        (&atkValueArgs[menuSize + 5])->ChangeType(ValueType.Int); // currently this value has no type, set it to int
        (&atkValueArgs[menuSize + 5 + 1])->ChangeType(ValueType.Int);

        for (var i = menuSize + 2; i > 1; i--)
        {
            var curEntry = &atkValueArgs[i + 5 - 2];
            var nextEntry = &atkValueArgs[i + 5];

            nextEntry->Int = curEntry->Int;
        }

        // step 2) set our new entries to dummy commands
        var firstEntry = &atkValueArgs[5];
        firstEntry->Int = 69420;
        var secondEntry = &atkValueArgs[6];
        secondEntry->Int = 69421;

        // step 3) create strings for them
        // since the game first checks for strings in the AtkValue argument before pulling them from the exd, if we create strings we dont have to worry
        // about hooking the exd reader, thank god
        var firstStringEntry = &atkValueArgs[5 + 18];
        firstStringEntry->ChangeType(ValueType.String);

        var secondStringEntry = &atkValueArgs[6 + 18];
        secondStringEntry->ChangeType(ValueType.String);

        const int color = 539;

        using var rssb = new RentedSeStringBuilder();

        firstStringEntry->SetManagedString(rssb.Builder
            .PushColorType(color)
            .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
            .PopColorType()
            .Append(this.locDalamudPlugins)
            .GetViewAsSpan());

        rssb.Builder.Clear();
        secondStringEntry->SetManagedString(rssb.Builder
            .PushColorType(color)
            .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
            .PopColorType()
            .Append(this.locDalamudSettings)
            .GetViewAsSpan());

        // open menu with new size
        var sizeEntry = &atkValueArgs[4];
        sizeEntry->UInt = menuSize + 2;

        this.hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize + 2);
    }

    private unsafe void UiModuleExecuteMainCommandDetour(UIModule* thisPtr, uint commandId)
    {
        var dalamudInterface = Service<DalamudInterface>.GetNullable();

        switch (commandId)
        {
            case 69420:
                dalamudInterface?.OpenPluginInstaller();
                break;
            case 69421:
                dalamudInterface?.OpenSettings();
                break;
            default:
                this.hookUiModuleExecuteMainCommand.Original(thisPtr, commandId);
                break;
        }
    }
}

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CheapLoc;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Agent;
using Dalamud.Game.Agent.AgentArgTypes;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Hooking;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Internal;

/// <summary>
/// This class implements in-game Dalamud options in the in-game System menu.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class DalamudAtkTweaks : IInternalDisposableService
{
    private static readonly ModuleLog Log = ModuleLog.Create<DalamudAtkTweaks>();

    // private readonly Hook<AgentHUD.Delegates.OpenSystemMenu> hookAgentHudOpenSystemMenu;

    // TODO: Make this into events in Framework.Gui
    private readonly Hook<UIModule.Delegates.ExecuteMainCommand> hookUiModuleExecuteMainCommand;

    private readonly Hook<AtkUnitBase.Delegates.ReceiveGlobalEvent> hookAtkUnitBaseReceiveGlobalEvent;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceDependency]
    private readonly AgentLifecycle agentLifecycle = Service<AgentLifecycle>.Get();

    // [ServiceManager.ServiceDependency]
    // private readonly ContextMenu contextMenu = Service<ContextMenu>.Get();

    private readonly AgentLifecycleEventListener agentLobbyPreEventListener;
    private Task lobbyProfileApplyTask = Task.CompletedTask;

    private bool disposed = false;

    [ServiceManager.ServiceConstructor]
    private DalamudAtkTweaks(TargetSigScanner sigScanner)
    {
        // NOTE: Disabled in 7.5, too many entries in the original menu
        // this.hookAgentHudOpenSystemMenu = Hook<AgentHUD.Delegates.OpenSystemMenu>.FromAddress(AgentHUD.Addresses.OpenSystemMenu.Value, this.AgentHudOpenSystemMenuDetour);
        this.hookUiModuleExecuteMainCommand = Hook<UIModule.Delegates.ExecuteMainCommand>.FromAddress((nint)UIModule.StaticVirtualTablePointer->ExecuteMainCommand, this.UiModuleExecuteMainCommandDetour);
        this.hookAtkUnitBaseReceiveGlobalEvent = Hook<AtkUnitBase.Delegates.ReceiveGlobalEvent>.FromAddress((nint)AtkUnitBase.StaticVirtualTablePointer->ReceiveGlobalEvent, this.AtkUnitBaseReceiveGlobalEventDetour);

        // this.contextMenu.ContextMenuOpened += this.ContextMenuOnContextMenuOpened;

        this.agentLobbyPreEventListener = new AgentLifecycleEventListener(AgentEvent.PreReceiveEvent, Agent.AgentId.Lobby, this.AgentLobbyPreReceiveEvent);

        this.agentLifecycle.RegisterListener(this.agentLobbyPreEventListener);

        // this.hookAgentHudOpenSystemMenu.Enable();
        this.hookUiModuleExecuteMainCommand.Enable();
        this.hookAtkUnitBaseReceiveGlobalEvent.Enable();
    }

    /// <summary>Finalizes an instance of the <see cref="DalamudAtkTweaks"/> class.</summary>
    ~DalamudAtkTweaks() => this.Dispose(false);

    private string LocDalamudPlugins => Loc.Localize("SystemMenuPlugins", "Dalamud Plugins");

    private string LocDalamudSettings => Loc.Localize("SystemMenuSettings", "Dalamud Settings");

    private string LocDalamudLoadingPluginsForCharacter => Loc.Localize("LoadingPluginsForCharacter", "Loading plugins for this character...");

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.Dispose(true);

    private void Dispose(bool disposing)
    {
        if (this.disposed)
            return;

        if (disposing)
        {
            this.agentLifecycle.UnregisterListener(this.agentLobbyPreEventListener);

            // this.hookAgentHudOpenSystemMenu.Dispose();
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

    private void AgentLobbyPreReceiveEvent(AgentEvent type, AgentArgs args)
    {
        var profileManager = Service<ProfileManager>.Get();

        // Don't do anything if no profiles have character-specific plugin enabling, since in that case we won't be
        // doing any loading on login and thus don't need to delay the login prompt
        if (profileManager.Profiles.All(x => x.Model is ProfileModelV1 { EnableForCharacters: false }))
            return;

        const int loginEventKind = 0x03;
        const int recursionSentinel = 69420;

        if (args is not AgentReceiveEventArgs receiveEventArgs)
            return;

        if (receiveEventArgs.EventKind is not loginEventKind)
            return;

        if (!receiveEventArgs.AtkValueEnumerable.Any())
            return;

        if (!receiveEventArgs.AtkValueEnumerable.ElementAt(0).TryGet(out int eventValue))
            return;

        // Prevent recursion from our own injected event
        if (receiveEventArgs.AtkValueEnumerable.Count() == 2 &&
            receiveEventArgs.AtkValueEnumerable.ElementAt(1).TryGet(out int eventValue2) && eventValue2 == recursionSentinel)
        {
            Log.Verbose("Prevent recursion (eventValue {EventValue})", eventValue);
            return;
        }

        var waitingForProfileLoad = !this.lobbyProfileApplyTask.IsCompleted;

        if (!waitingForProfileLoad && eventValue != 0)
        {
            Log.Verbose("Dismissing login prompt (eventValue {EventValue})", eventValue);
            return;
        }

        args.PreventOriginal();

        if (!waitingForProfileLoad && eventValue == 0)
        {
            var addonSelectYesno = Service<GameGui>.Get().GetAddonByName<AddonSelectYesno>("SelectYesno");

            using var rssb = new RentedSeStringBuilder();

            addonSelectYesno->PromptText->SetText(rssb.Builder
                .PushColorType(539)
                .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
                .PopColorType()
                .Append(this.LocDalamudLoadingPluginsForCharacter)
                .GetViewAsSpan());

            addonSelectYesno->YesButton->SetEnabledState(false);
            addonSelectYesno->NoButton->SetEnabledState(false);
            addonSelectYesno->ShouldFireCallbackAndHideOrClose = true;

            var cts = new CancellationTokenSource();
            cts.CancelAfter(60000);
            var delayTask = Task.Delay(30000, cts.Token); // Just in case something goes wrong, we don't want to leave the player stuck on this screen forever
            var applyTask = Task.Run(() => profileManager.ApplyAllWantStatesAsync("Login start"), cts.Token);

            this.lobbyProfileApplyTask = Task.WhenAny(delayTask, applyTask).ContinueWith(_ =>
            {
                Service<Framework>.Get().Run(() =>
                {
                    addonSelectYesno->ShouldFireCallbackAndHideOrClose = false;
                    addonSelectYesno->YesButton->SetEnabledState(true);
                    addonSelectYesno->NoButton->SetEnabledState(true);
                    addonSelectYesno->Close(false);

                    var dummyRet = stackalloc AtkValue[1];
                    dummyRet->Type = AtkValueType.Undefined;
                    dummyRet->Int = recursionSentinel;

                    var okAtkValue = stackalloc AtkValue[2];
                    okAtkValue[0].Type = AtkValueType.Int;
                    okAtkValue[0].Int = 0;
                    okAtkValue[1].Type = AtkValueType.Int;
                    okAtkValue[1].Int = recursionSentinel;

                    AgentLobby.Instance()->ReceiveEvent(
                        dummyRet,
                        okAtkValue,
                        2,
                        loginEventKind);
                });
            }, cts.Token);
        }
    }

    private void AtkUnitBaseReceiveGlobalEventDetour(AtkUnitBase* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        // 3 == Close
        if (eventType == AtkEventType.InputReceived && WindowSystem.ShouldInhibitAtkCloseEvents && atkEventData != null && atkEventData->InputData.InputId == 3 && this.configuration.IsFocusManagementEnabled)
        {
            Log.Verbose($"Cancelling global event SendHotkey command due to WindowSystem {WindowSystem.FocusedWindowSystemNamespace}");
            return;
        }

        this.hookAtkUnitBaseReceiveGlobalEvent.Original(thisPtr, eventType, eventParam, atkEvent, atkEventData);
    }

    /*
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
        for (var i = menuSize + 2; i > 1; i--)
        {
            ref var curEntry = ref atkValueArgs[i + 5 - 2];
            ref var nextEntry = ref atkValueArgs[i + 5];
            nextEntry.SetInt(curEntry.Int);
        }

        // step 2) set our new entries to dummy commands
        const int color = 539;
        using var rssb = new RentedSeStringBuilder();

        atkValueArgs[5].SetInt(69420);
        atkValueArgs[5 + 18].SetManagedString(rssb.Builder
            .PushColorType(color)
            .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
            .PopColorType()
            .Append(this.LocDalamudPlugins)
            .GetViewAsSpan());

        rssb.Builder.Clear();

        atkValueArgs[6].SetInt(69421);
        atkValueArgs[6 + 18].SetManagedString(rssb.Builder
            .PushColorType(color)
            .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
            .PopColorType()
            .Append(this.LocDalamudSettings)
            .GetViewAsSpan());

        // open menu with new size
        atkValueArgs[4].SetUInt(menuSize + 2);

        this.hookAgentHudOpenSystemMenu.Original(thisPtr, atkValueArgs, menuSize + 2);
    }
    */

    private void UiModuleExecuteMainCommandDetour(UIModule* thisPtr, uint commandId)
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

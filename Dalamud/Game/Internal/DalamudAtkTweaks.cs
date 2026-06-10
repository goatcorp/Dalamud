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
using FFXIVClientStructs.Interop;

namespace Dalamud.Game.Internal;

/// <summary>
/// This class implements in-game Dalamud options in the in-game System menu.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class DalamudAtkTweaks : IInternalDisposableService
{
    private static readonly ModuleLog Log = ModuleLog.Create<DalamudAtkTweaks>();

    private readonly Hook<AgentHUD.Delegates.OpenSystemMenu> hookAgentHudOpenSystemMenu;
    private readonly Hook<UIModule.Delegates.ExecuteMainCommand> hookUiModuleExecuteMainCommand; // TODO: Make this into events in Framework.Gui
    private readonly Hook<AtkUnitBase.Delegates.ReceiveGlobalEvent> hookAtkUnitBaseReceiveGlobalEvent;
    private readonly Hook<RaptureAtkUnitManager.Delegates.GetAddonCollision> hookGetAddonCollision;

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
        this.hookAgentHudOpenSystemMenu = Hook<AgentHUD.Delegates.OpenSystemMenu>.FromAddress(AgentHUD.Addresses.OpenSystemMenu.Value, this.AgentHudOpenSystemMenuDetour);
        this.hookUiModuleExecuteMainCommand = Hook<UIModule.Delegates.ExecuteMainCommand>.FromAddress((nint)UIModule.StaticVirtualTablePointer->ExecuteMainCommand, this.UiModuleExecuteMainCommandDetour);
        this.hookAtkUnitBaseReceiveGlobalEvent = Hook<AtkUnitBase.Delegates.ReceiveGlobalEvent>.FromAddress((nint)AtkUnitBase.StaticVirtualTablePointer->ReceiveGlobalEvent, this.AtkUnitBaseReceiveGlobalEventDetour);
        this.hookGetAddonCollision = Hook<RaptureAtkUnitManager.Delegates.GetAddonCollision>.FromAddress((nint)RaptureAtkUnitManager.StaticVirtualTablePointer->GetAddonCollision, this.RaptureAtkUnitManagerGetAddonCollisionDetour);

        // this.contextMenu.ContextMenuOpened += this.ContextMenuOnContextMenuOpened;

        this.agentLobbyPreEventListener = new AgentLifecycleEventListener(AgentEvent.PreReceiveEvent, Agent.AgentId.Lobby, this.AgentLobbyPreReceiveEvent);

        this.agentLifecycle.RegisterListener(this.agentLobbyPreEventListener);

        this.hookAgentHudOpenSystemMenu.Enable();
        this.hookUiModuleExecuteMainCommand.Enable();
        this.hookAtkUnitBaseReceiveGlobalEvent.Enable();
        this.hookGetAddonCollision.Enable();
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

            this.hookAgentHudOpenSystemMenu.Dispose();
            this.hookUiModuleExecuteMainCommand.Dispose();
            this.hookAtkUnitBaseReceiveGlobalEvent.Dispose();
            this.hookGetAddonCollision.Dispose();

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

    private void RaptureAtkUnitManagerGetAddonCollisionDetour(RaptureAtkUnitManager* thisPtr, AddonCollision* collisionInfo, short x, short y)
    {
        if (WindowSystem.ShouldInhibitAtkCollisions)
        {
            if (collisionInfo != null)
            {
                collisionInfo->UnitBase = null;
                collisionInfo->CollisionNode = null;
            }

            return;
        }

        this.hookGetAddonCollision.Original(thisPtr, collisionInfo, x, y);
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

        values[entryIndex].SetInt(69420);
        values[entryIndex + maxEntries].SetManagedString(rssb.Builder
            .PushColorType(color)
            .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
            .PopColorType()
            .Append(this.LocDalamudPlugins)
            .GetViewAsSpan());

        rssb.Builder.Clear();
        entryIndex++;

        values[entryIndex].SetInt(69421);
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

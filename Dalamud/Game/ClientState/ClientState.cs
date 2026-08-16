using System.Linq;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Application.Network;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Network;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Lumina.Excel.Sheets;

using Action = System.Action;
using CSUIState = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState;

namespace Dalamud.Game.ClientState;

/// <summary>
/// This class represents the state of the game client at the time of access.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal unsafe sealed class ClientState : IInternalDisposableService, IClientState
{
    private static readonly ModuleLog Log = ModuleLog.Create<ClientState>();

    [ServiceManager.ServiceDependency]
    private readonly GameLifecycle gameLifecycle = Service<GameLifecycle>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ObjectTable objectTable = Service<ObjectTable>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ChatGui chatGui = Service<ChatGui>.Get();

    private readonly Hook<UIModule.Delegates.HandlePacket> uiModuleHandlePacketHook;
    private readonly Hook<PacketDispatcher.Delegates.HandleContentsFinderNotificationPacket> cfPopHook;

    private Hook<LogoutCallbackInterface.Delegates.OnLogout>? onLogoutHook;
    private bool initialized;
    private bool lastConditionNone = true;

    [ServiceManager.ServiceConstructor]
    private ClientState(Dalamud dalamud)
    {
        Log.Verbose("===== C L I E N T  S T A T E =====");

        this.ClientLanguage = (ClientLanguage)dalamud.StartInfo.Language;

        this.uiModuleHandlePacketHook = Hook<UIModule.Delegates.HandlePacket>.FromAddress(
            (nint)UIModule.StaticVirtualTablePointer->HandlePacket,
            this.UIModuleHandlePacketDetour);

        this.cfPopHook = Hook<PacketDispatcher.Delegates.HandleContentsFinderNotificationPacket>.FromAddress(
            PacketDispatcher.Addresses.HandleContentsFinderNotificationPacket.Value,
            this.HandleContentsFinderNotificationPacketDetour);

        this.uiModuleHandlePacketHook.Enable();
        this.cfPopHook.Enable();

        this.framework.RunOnTick(this.Setup);
    }

    private delegate void SetCurrentInstanceDelegate(NetworkModuleProxy* thisPtr, short instanceId);

    /// <inheritdoc/>
    public event Action<ZoneInitEventArgs>? ZoneInit;

    /// <inheritdoc/>
    public event Action<uint>? TerritoryChanged;

    /// <inheritdoc/>
    public event Action<uint>? MapIdChanged;

    /// <inheritdoc/>
    public event Action<uint>? InstanceChanged;

    /// <inheritdoc/>
    public event IClientState.ClassJobChangeDelegate? ClassJobChanged;

    /// <inheritdoc/>
    public event IClientState.LevelChangeDelegate? LevelChanged;

    /// <inheritdoc/>
    public event Action? Login;

    /// <inheritdoc/>
    public event IClientState.LogoutDelegate? Logout;

    /// <inheritdoc/>
    public event Action? EnterPvP;

    /// <inheritdoc/>
    public event Action? LeavePvP;

    /// <inheritdoc/>
    public event Action<ContentFinderCondition>? CfPop;

    /// <inheritdoc/>
    public ClientLanguage ClientLanguage { get; }

    /// <inheritdoc/>
    public uint TerritoryType
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;

                if (this.initialized)
                {
                    Log.Debug("TerritoryType changed: {0}", value);
                    this.TerritoryChanged?.InvokeSafely(value);
                }
            }
        }
    }

    /// <inheritdoc/>
    public uint MapId
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;

                if (this.initialized)
                {
                    Log.Debug("MapId changed: {0}", value);
                    this.MapIdChanged?.InvokeSafely(value);
                }
            }
        }
    }

    /// <inheritdoc/>
    public uint Instance
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;

                if (this.initialized)
                {
                    Log.Debug("Instance changed: {0}", value);
                    this.InstanceChanged?.InvokeSafely(value);
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool IsLoggedIn
    {
        get
        {
            var agentLobby = AgentLobby.Instance();
            return agentLobby != null && agentLobby->IsLoggedIn;
        }
    }

    /// <inheritdoc/>
    public bool IsPvP
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;

                if (this.initialized)
                {
                    if (value)
                    {
                        Log.Debug("EnterPvP");
                        this.EnterPvP?.InvokeSafely();
                    }
                    else
                    {
                        Log.Debug("LeavePvP");
                        this.LeavePvP?.InvokeSafely();
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool IsPvPExcludingDen => this.IsPvP && this.TerritoryType != 250;

    /// <inheritdoc />
    public bool IsGPosing => GameMain.IsInGPose();

    /// <inheritdoc/>
    public bool IsClientIdle(out ConditionFlag blockingFlag)
    {
        blockingFlag = 0;
        if (!this.IsLoggedIn)
            return true;

        var condition = Service<Conditions.Condition>.GetNullable();

        var blockingConditions = condition.AsReadOnlySet().Except([
            ConditionFlag.NormalConditions,
            ConditionFlag.Emoting,
            ConditionFlag.Jumping,
            ConditionFlag.Mounted,
            ConditionFlag.InFlight,
            ConditionFlag.Swimming,
            ConditionFlag.Diving,
            ConditionFlag.UsingFashionAccessory,
            ConditionFlag.OnFreeTrial]);

        blockingFlag = blockingConditions.FirstOrDefault();
        return blockingFlag == 0;
    }

    /// <inheritdoc/>
    public bool IsClientIdle() => this.IsClientIdle(out _);

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        this.uiModuleHandlePacketHook.Dispose();
        this.cfPopHook.Dispose();
        this.onLogoutHook?.Dispose();

        this.framework.Update -= this.OnFrameworkUpdate;
    }

    private void Setup()
    {
        this.onLogoutHook = Hook<LogoutCallbackInterface.Delegates.OnLogout>.FromAddress((nint)AgentLobby.Instance()->LogoutCallbackInterface.VirtualTable->OnLogout, this.OnLogoutDetour);
        this.onLogoutHook.Enable();

        this.IsPvP = GameMain.IsInPvPArea();
        this.TerritoryType = GameMain.Instance()->CurrentTerritoryTypeId;
        this.MapId = AgentMap.Instance()->CurrentMapId;
        this.Instance = CSUIState.Instance()->PublicInstance.InstanceId;

        this.initialized = true;

        this.framework.Update += this.OnFrameworkUpdate;
    }

    private void UIModuleHandlePacketDetour(
        UIModule* thisPtr, UIModulePacketType type, uint uintParam, void* packet)
    {
        this.uiModuleHandlePacketHook.Original(thisPtr, type, uintParam, packet);

        switch (type)
        {
            case UIModulePacketType.ClassJobChange:
            {
                var classJobId = uintParam;

                foreach (var action in Delegate.EnumerateInvocationList(this.ClassJobChanged))
                {
                    try
                    {
                        action(classJobId);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Exception during raise of {handler}", action.Method);
                    }
                }

                break;
            }

            case UIModulePacketType.LevelChange:
            {
                var classJobId = *(uint*)packet;
                var level = *(ushort*)((nint)packet + 4);

                foreach (var action in Delegate.EnumerateInvocationList(this.LevelChanged))
                {
                    try
                    {
                        action(classJobId, level);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Exception during raise of {handler}", action.Method);
                    }
                }

                break;
            }

            case UIModulePacketType.ZoneInit:
            {
                var eventArgs = ZoneInitEventArgs.Read((ZoneInitPacket*)packet);
                Log.Debug($"ZoneInit: {eventArgs}");
                this.ZoneInit?.InvokeSafely(eventArgs);
                this.TerritoryType = eventArgs.TerritoryType.RowId;
                this.Instance = eventArgs.Instance;
                this.IsPvP = eventArgs.TerritoryType.Value.IsPvpZone;
                break;
            }
        }
    }

    private void HandleContentsFinderNotificationPacketDetour(ContentsFinderNotificationPacket* packet)
    {
        this.cfPopHook.OriginalDisposeSafe(packet);

        try
        {
            if (packet->QueueState != ContentsFinderQueueState.Ready)
                return;

            if (this.configuration.DutyFinderTaskbarFlash)
                Util.FlashWindow();

            var cfcId = packet->ContentFinderConditionId;
            var cfCondition = LuminaUtils.CreateRef<ContentFinderCondition>(cfcId);

            if (!cfCondition.IsValid)
            {
                Log.Error("CFC key {cfcId} not found", cfcId);
                return;
            }

            var cfcName = cfCondition.Value.Name.ToDalamudString();
            if (cfcName.Payloads.Count == 0)
                cfcName = "Duty Roulette";

            Task.Run(() =>
            {
                if (this.configuration.DutyFinderChatMessage)
                {
                    var b = new SeStringBuilder();
                    b.Append("Duty pop: ");
                    b.Append(cfcName);
                    this.chatGui.Print(b.Build());
                }

                this.CfPop.InvokeSafely(cfCondition.Value);
            }).ContinueWith(
                task => Log.Error(task.Exception, "CfPop.Invoke failed"),
                TaskContinuationOptions.OnlyOnFaulted);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CfPopDetour threw an exception");
        }
    }

    private void OnFrameworkUpdate(IFramework frameworkArg)
    {
        this.MapId = AgentMap.Instance()->CurrentMapId;

        var condition = Service<Conditions.Condition>.GetNullable();
        var gameGui = Service<GameGui>.GetNullable();
        var data = Service<DataManager>.GetNullable();

        if (condition == null || gameGui == null || data == null)
            return;

        if (condition.Any() && this.lastConditionNone && this.objectTable.LocalPlayer != null)
        {
            Log.Debug("Is login");
            this.lastConditionNone = false;
            this.Login?.InvokeSafely();
            gameGui.ResetUiHideState();

            this.gameLifecycle.ResetLogout();
        }
    }

    private void OnLogoutDetour(LogoutCallbackInterface* thisPtr, LogoutCallbackInterface.LogoutParams* logoutParams)
    {
        var gameGui = Service<GameGui>.GetNullable();

        if (logoutParams != null)
        {
            try
            {
                var type = logoutParams->Type;
                var code = logoutParams->Code;

                Log.Debug("Logout: Type {type}, Code {code}", type, code);

                foreach (var action in Delegate.EnumerateInvocationList(this.Logout))
                {
                    try
                    {
                        action(type, code);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Exception during raise of {handler}", action.Method);
                    }
                }

                gameGui?.ResetUiHideState();
                this.lastConditionNone = true; // unblock login flag

                this.gameLifecycle.SetLogout();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during OnLogoutDetour");
            }
        }

        this.onLogoutHook!.Original(thisPtr, logoutParams);
    }

    private void NetworkHandlersOnCfPop(ContentFinderCondition e)
    {
        this.CfPop?.InvokeSafely(e);
    }
}

/// <summary>
/// Plugin-scoped version of a GameConfig service.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IClientState>]
#pragma warning restore SA1015
internal class ClientStatePluginScoped : IInternalDisposableService, IClientState
{
    [ServiceManager.ServiceDependency]
    private readonly ClientState clientStateService = Service<ClientState>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientStatePluginScoped"/> class.
    /// </summary>
    internal ClientStatePluginScoped()
    {
        this.clientStateService.ZoneInit += this.ZoneInitForward;
        this.clientStateService.TerritoryChanged += this.TerritoryChangedForward;
        this.clientStateService.MapIdChanged += this.MapIdChangedForward;
        this.clientStateService.InstanceChanged += this.InstanceChangedForward;
        this.clientStateService.ClassJobChanged += this.ClassJobChangedForward;
        this.clientStateService.LevelChanged += this.LevelChangedForward;
        this.clientStateService.Login += this.LoginForward;
        this.clientStateService.Logout += this.LogoutForward;
        this.clientStateService.EnterPvP += this.EnterPvPForward;
        this.clientStateService.LeavePvP += this.ExitPvPForward;
        this.clientStateService.CfPop += this.ContentFinderPopForward;
    }

    /// <inheritdoc/>
    public event Action<ZoneInitEventArgs>? ZoneInit;

    /// <inheritdoc/>
    public event Action<uint>? TerritoryChanged;

    /// <inheritdoc/>
    public event Action<uint>? MapIdChanged;

    /// <inheritdoc/>
    public event Action<uint>? InstanceChanged;

    /// <inheritdoc/>
    public event IClientState.ClassJobChangeDelegate? ClassJobChanged;

    /// <inheritdoc/>
    public event IClientState.LevelChangeDelegate? LevelChanged;

    /// <inheritdoc/>
    public event Action? Login;

    /// <inheritdoc/>
    public event IClientState.LogoutDelegate? Logout;

    /// <inheritdoc/>
    public event Action? EnterPvP;

    /// <inheritdoc/>
    public event Action? LeavePvP;

    /// <inheritdoc/>
    public event Action<ContentFinderCondition>? CfPop;

    /// <inheritdoc/>
    public ClientLanguage ClientLanguage => this.clientStateService.ClientLanguage;

    /// <inheritdoc/>
    public uint TerritoryType => this.clientStateService.TerritoryType;

    /// <inheritdoc/>
    public uint MapId => this.clientStateService.MapId;

    /// <inheritdoc/>
    public uint Instance => this.clientStateService.Instance;

    /// <inheritdoc/>
    public bool IsLoggedIn => this.clientStateService.IsLoggedIn;

    /// <inheritdoc/>
    public bool IsPvP => this.clientStateService.IsPvP;

    /// <inheritdoc/>
    public bool IsPvPExcludingDen => this.clientStateService.IsPvPExcludingDen;

    /// <inheritdoc/>
    public bool IsGPosing => this.clientStateService.IsGPosing;

    /// <inheritdoc/>
    public bool IsClientIdle(out ConditionFlag blockingFlag) => this.clientStateService.IsClientIdle(out blockingFlag);

    /// <inheritdoc/>
    public bool IsClientIdle() => this.clientStateService.IsClientIdle();

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.clientStateService.ZoneInit -= this.ZoneInitForward;
        this.clientStateService.TerritoryChanged -= this.TerritoryChangedForward;
        this.clientStateService.MapIdChanged -= this.MapIdChangedForward;
        this.clientStateService.InstanceChanged -= this.InstanceChangedForward;
        this.clientStateService.ClassJobChanged -= this.ClassJobChangedForward;
        this.clientStateService.LevelChanged -= this.LevelChangedForward;
        this.clientStateService.Login -= this.LoginForward;
        this.clientStateService.Logout -= this.LogoutForward;
        this.clientStateService.EnterPvP -= this.EnterPvPForward;
        this.clientStateService.LeavePvP -= this.ExitPvPForward;
        this.clientStateService.CfPop -= this.ContentFinderPopForward;

        this.ZoneInit = null;
        this.TerritoryChanged = null;
        this.MapIdChanged = null;
        this.InstanceChanged = null;
        this.ClassJobChanged = null;
        this.LevelChanged = null;
        this.Login = null;
        this.Logout = null;
        this.EnterPvP = null;
        this.LeavePvP = null;
        this.CfPop = null;
    }

    private void ZoneInitForward(ZoneInitEventArgs eventArgs) => this.ZoneInit?.Invoke(eventArgs);

    private void TerritoryChangedForward(uint territoryId) => this.TerritoryChanged?.Invoke(territoryId);

    private void MapIdChangedForward(uint mapId) => this.MapIdChanged?.Invoke(mapId);

    private void InstanceChangedForward(uint instanceId) => this.InstanceChanged?.Invoke(instanceId);

    private void ClassJobChangedForward(uint classJobId) => this.ClassJobChanged?.Invoke(classJobId);

    private void LevelChangedForward(uint classJobId, uint level) => this.LevelChanged?.Invoke(classJobId, level);

    private void LoginForward() => this.Login?.Invoke();

    private void LogoutForward(int type, int code) => this.Logout?.Invoke(type, code);

    private void EnterPvPForward() => this.EnterPvP?.Invoke();

    private void ExitPvPForward() => this.LeavePvP?.Invoke();

    private void ContentFinderPopForward(ContentFinderCondition cfc) => this.CfPop?.Invoke(cfc);
}

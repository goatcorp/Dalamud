using System.Linq;

using Dalamud.Data;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui;
using Dalamud.Game.Network.Internal;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Application.Network;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Lumina.Excel.Sheets;

using Action = System.Action;

namespace Dalamud.Game.ClientState;

/// <summary>
/// This class represents the state of the game client at the time of access.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed class ClientState : IInternalDisposableService, IClientState
{
    private static readonly ModuleLog Log = new("ClientState");

    private readonly GameLifecycle lifecycle;
    private readonly ClientStateAddressResolver address;
    private readonly Hook<EventFramework.Delegates.SetTerritoryTypeId> setupTerritoryTypeHook;
    private readonly Hook<UIModule.Delegates.HandlePacket> uiModuleHandlePacketHook;
    private readonly Hook<LogoutCallbackInterface.Delegates.OnLogout> onLogoutHook;

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();
    
    [ServiceManager.ServiceDependency]
    private readonly NetworkHandlers networkHandlers = Service<NetworkHandlers>.Get();
    
    private bool lastConditionNone = true;


    [ServiceManager.ServiceConstructor]
    private unsafe ClientState(TargetSigScanner sigScanner, Dalamud dalamud, GameLifecycle lifecycle)
    {
        this.lifecycle = lifecycle;
        this.address = new ClientStateAddressResolver();
        this.address.Setup(sigScanner);

        Log.Verbose("===== C L I E N T  S T A T E =====");

        this.ClientLanguage = (ClientLanguage)dalamud.StartInfo.Language;

        var setTerritoryTypeAddr = EventFramework.Addresses.SetTerritoryTypeId.Value;
        Log.Verbose($"SetupTerritoryType address {Util.DescribeAddress(setTerritoryTypeAddr)}");

        this.setupTerritoryTypeHook = Hook<EventFramework.Delegates.SetTerritoryTypeId>.FromAddress(setTerritoryTypeAddr, this.SetupTerritoryTypeDetour);
        this.uiModuleHandlePacketHook = Hook<UIModule.Delegates.HandlePacket>.FromAddress((nint)UIModule.StaticVirtualTablePointer->HandlePacket, this.UIModuleHandlePacketDetour);
        this.onLogoutHook = Hook<LogoutCallbackInterface.Delegates.OnLogout>.FromAddress((nint)LogoutCallbackInterface.StaticVirtualTablePointer->OnLogout, this.OnLogoutDetour);

        this.framework.Update += this.FrameworkOnOnUpdateEvent;
        this.networkHandlers.CfPop += this.NetworkHandlersOnCfPop;

        this.setupTerritoryTypeHook.Enable();
        this.uiModuleHandlePacketHook.Enable();
        this.onLogoutHook.Enable();
    }

    private unsafe delegate void ProcessPacketPlayerSetupDelegate(nint a1, nint packet);

    /// <inheritdoc/>
    public event Action<ushort>? TerritoryChanged;

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
    public ushort TerritoryType { get; private set; }

    /// <inheritdoc/>
    public unsafe uint MapId
    {
        get
        {
            var agentMap = AgentMap.Instance();
            return agentMap != null ? agentMap->CurrentMapId : 0;
        }
    }

    /// <inheritdoc/>
    public IPlayerCharacter? LocalPlayer => Service<ObjectTable>.GetNullable()?[0] as IPlayerCharacter;

    /// <inheritdoc/>
    public unsafe ulong LocalContentId => PlayerState.Instance()->ContentId;

    /// <inheritdoc/>
    public unsafe bool IsLoggedIn
    {
        get
        {
            var agentLobby = AgentLobby.Instance();
            return agentLobby != null && agentLobby->IsLoggedIn;
        }
    }

    /// <inheritdoc/>
    public bool IsPvP { get; private set; }

    /// <inheritdoc/>
    public bool IsPvPExcludingDen { get; private set; }

    /// <inheritdoc />
    public bool IsGPosing => GameMain.IsInGPose();

    /// <summary>
    /// Gets client state address resolver.
    /// </summary>
    internal ClientStateAddressResolver AddressResolver => this.address;

    /// <inheritdoc/>
    public bool IsClientIdle(out ConditionFlag blockingFlag)
    {
        blockingFlag = 0;
        if (this.LocalPlayer is null) return true;

        var condition = Service<Conditions.Condition>.GetNullable();

        var blockingConditions = condition.AsReadOnlySet().Except([
            ConditionFlag.NormalConditions,
            ConditionFlag.Jumping,
            ConditionFlag.Mounted,
            ConditionFlag.UsingParasol]);

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
        this.setupTerritoryTypeHook.Dispose();
        this.uiModuleHandlePacketHook.Dispose();
        this.onLogoutHook.Dispose();

        this.framework.Update -= this.FrameworkOnOnUpdateEvent; 
        this.networkHandlers.CfPop -= this.NetworkHandlersOnCfPop;
    }

    private unsafe void SetupTerritoryTypeDetour(EventFramework* eventFramework, ushort territoryType)
    {
        Log.Debug("TerritoryType changed: {0}", territoryType);

        this.TerritoryType = territoryType;
        this.TerritoryChanged?.InvokeSafely(territoryType);

        var rowRef = LuminaUtils.CreateRef<TerritoryType>(territoryType);
        if (rowRef.IsValid)
        {
            var isPvP = rowRef.Value.IsPvpZone;
            if (isPvP != this.IsPvP)
            {
                this.IsPvP = isPvP;
                this.IsPvPExcludingDen = this.IsPvP && this.TerritoryType != 250;

                if (this.IsPvP)
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

        this.setupTerritoryTypeHook.Original(eventFramework, territoryType);
    }

    private unsafe void UIModuleHandlePacketDetour(UIModule* thisPtr, UIModulePacketType type, uint uintParam, void* packet)
    {
        this.uiModuleHandlePacketHook.Original(thisPtr, type, uintParam, packet);

        switch (type)
        {
            case UIModulePacketType.ClassJobChange when this.ClassJobChanged is { } callback:
                {
                    var classJobId = uintParam;

                    foreach (var action in callback.GetInvocationList().Cast<IClientState.ClassJobChangeDelegate>())
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

            case UIModulePacketType.LevelChange when this.LevelChanged is { } callback:
                {
                    var classJobId = *(uint*)packet;
                    var level = *(ushort*)((nint)packet + 4);

                    foreach (var action in callback.GetInvocationList().Cast<IClientState.LevelChangeDelegate>())
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
        }
    }

    private void FrameworkOnOnUpdateEvent(IFramework framework1)
    {
        var condition = Service<Conditions.Condition>.GetNullable();
        var gameGui = Service<GameGui>.GetNullable();
        var data = Service<DataManager>.GetNullable();

        if (condition == null || gameGui == null || data == null)
            return;

        if (condition.Any() && this.lastConditionNone && this.LocalPlayer != null)
        {
            Log.Debug("Is login");
            this.lastConditionNone = false;
            this.Login?.InvokeSafely();
            gameGui.ResetUiHideState();

            this.lifecycle.ResetLogout();
        }
    }

    private unsafe void OnLogoutDetour(LogoutCallbackInterface* thisPtr, LogoutCallbackInterface.LogoutParams* logoutParams)
    {
        var gameGui = Service<GameGui>.GetNullable();

        if (logoutParams != null)
        {
            try
            {
                var type = logoutParams->Type;
                var code = logoutParams->Code;

                Log.Debug("Logout: Type {type}, Code {code}", type, code);

                if (this.Logout is { } callback)
                {
                    foreach (var action in callback.GetInvocationList().Cast<IClientState.LogoutDelegate>())
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
                }

                gameGui?.ResetUiHideState();
                this.lastConditionNone = true; // unblock login flag

                this.lifecycle.SetLogout();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during OnLogoutDetour");
            }
        }

        this.onLogoutHook.Original(thisPtr, logoutParams);
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
        this.clientStateService.TerritoryChanged += this.TerritoryChangedForward;
        this.clientStateService.ClassJobChanged += this.ClassJobChangedForward;
        this.clientStateService.LevelChanged += this.LevelChangedForward;
        this.clientStateService.Login += this.LoginForward;
        this.clientStateService.Logout += this.LogoutForward;
        this.clientStateService.EnterPvP += this.EnterPvPForward;
        this.clientStateService.LeavePvP += this.ExitPvPForward;
        this.clientStateService.CfPop += this.ContentFinderPopForward;
    }

    /// <inheritdoc/>
    public event Action<ushort>? TerritoryChanged;

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
    public ushort TerritoryType => this.clientStateService.TerritoryType;

    /// <inheritdoc/>
    public uint MapId => this.clientStateService.MapId;

    /// <inheritdoc/>
    public IPlayerCharacter? LocalPlayer => this.clientStateService.LocalPlayer;

    /// <inheritdoc/>
    public ulong LocalContentId => this.clientStateService.LocalContentId;

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
        this.clientStateService.TerritoryChanged -= this.TerritoryChangedForward;
        this.clientStateService.ClassJobChanged -= this.ClassJobChangedForward;
        this.clientStateService.LevelChanged -= this.LevelChangedForward;
        this.clientStateService.Login -= this.LoginForward;
        this.clientStateService.Logout -= this.LogoutForward;
        this.clientStateService.EnterPvP -= this.EnterPvPForward;
        this.clientStateService.LeavePvP -= this.ExitPvPForward;
        this.clientStateService.CfPop -= this.ContentFinderPopForward;

        this.TerritoryChanged = null;
        this.Login = null;
        this.Logout = null;
        this.EnterPvP = null;
        this.LeavePvP = null;
        this.CfPop = null;
    }

    private void TerritoryChangedForward(ushort territoryId) => this.TerritoryChanged?.Invoke(territoryId);

    private void ClassJobChangedForward(uint classJobId) => this.ClassJobChanged?.Invoke(classJobId);

    private void LevelChangedForward(uint classJobId, uint level) => this.LevelChanged?.Invoke(classJobId, level);

    private void LoginForward() => this.Login?.Invoke();

    private void LogoutForward(int type, int code) => this.Logout?.Invoke(type, code);

    private void EnterPvPForward() => this.EnterPvP?.Invoke();

    private void ExitPvPForward() => this.LeavePvP?.Invoke();

    private void ContentFinderPopForward(ContentFinderCondition cfc) => this.CfPop?.Invoke(cfc);
}

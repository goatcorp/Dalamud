using System.Runtime.InteropServices;

using Dalamud.Data;
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
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Lumina.Excel.GeneratedSheets;

using Action = System.Action;

namespace Dalamud.Game.ClientState;

/// <summary>
/// This class represents the state of the game client at the time of access.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal sealed class ClientState : IInternalDisposableService, IClientState
{
    private static readonly ModuleLog Log = new("ClientState");
    
    private readonly GameLifecycle lifecycle;
    private readonly ClientStateAddressResolver address;
    private readonly Hook<SetupTerritoryTypeDelegate> setupTerritoryTypeHook;

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly NetworkHandlers networkHandlers = Service<NetworkHandlers>.Get();

    private bool lastConditionNone = true;
    private bool lastFramePvP;

    [ServiceManager.ServiceConstructor]
    private ClientState(TargetSigScanner sigScanner, Dalamud dalamud, GameLifecycle lifecycle)
    {
        this.lifecycle = lifecycle;
        this.address = new ClientStateAddressResolver();
        this.address.Setup(sigScanner);

        Log.Verbose("===== C L I E N T  S T A T E =====");

        this.ClientLanguage = (ClientLanguage)dalamud.StartInfo.Language;

        Log.Verbose($"SetupTerritoryType address 0x{this.address.SetupTerritoryType.ToInt64():X}");

        this.setupTerritoryTypeHook = Hook<SetupTerritoryTypeDelegate>.FromAddress(this.address.SetupTerritoryType, this.SetupTerritoryTypeDetour);

        this.framework.Update += this.FrameworkOnOnUpdateEvent;

        this.networkHandlers.CfPop += this.NetworkHandlersOnCfPop;

        this.setupTerritoryTypeHook.Enable();
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr SetupTerritoryTypeDelegate(IntPtr manager, ushort terriType);

    /// <inheritdoc/>
    public event Action<ushort>? TerritoryChanged;

    /// <inheritdoc/>
    public event Action? Login;

    /// <inheritdoc/>
    public event Action? Logout;

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
            return agentMap != null ? AgentMap.Instance()->CurrentMapId : 0;
        }
    }

    /// <inheritdoc/>
    public PlayerCharacter? LocalPlayer => Service<ObjectTable>.GetNullable()?[0] as PlayerCharacter;

    /// <inheritdoc/>
    public ulong LocalContentId => (ulong)Marshal.ReadInt64(this.address.LocalContentId);

    /// <inheritdoc/>
    public bool IsLoggedIn { get; private set; }

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

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        this.setupTerritoryTypeHook.Dispose();
        this.framework.Update -= this.FrameworkOnOnUpdateEvent;
        this.networkHandlers.CfPop -= this.NetworkHandlersOnCfPop;
    }

    private IntPtr SetupTerritoryTypeDetour(IntPtr manager, ushort terriType)
    {
        this.TerritoryType = terriType;
        this.TerritoryChanged?.InvokeSafely(terriType);

        Log.Debug("TerritoryType changed: {0}", terriType);

        return this.setupTerritoryTypeHook.Original(manager, terriType);
    }

    private void NetworkHandlersOnCfPop(ContentFinderCondition e)
    {
        this.CfPop?.InvokeSafely(e);
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
            this.IsLoggedIn = true;
            this.Login?.InvokeSafely();
            gameGui.ResetUiHideState();

            this.lifecycle.ResetLogout();
        }

        if (!condition.Any() && this.lastConditionNone == false)
        {
            Log.Debug("Is logout");
            this.lastConditionNone = true;
            this.IsLoggedIn = false;
            this.Logout?.InvokeSafely();
            gameGui.ResetUiHideState();

            this.lifecycle.SetLogout();
        }

        this.IsPvP = GameMain.IsInPvPArea();
        this.IsPvPExcludingDen = this.IsPvP && this.TerritoryType != 250;

        if (this.IsPvP != this.lastFramePvP)
        {
            this.lastFramePvP = this.IsPvP;

            if (this.IsPvP)
            {
                this.EnterPvP?.InvokeSafely();
            }
            else
            {
                this.LeavePvP?.InvokeSafely();
            }
        }
    }
}

/// <summary>
/// Plugin-scoped version of a GameConfig service.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
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
        this.clientStateService.Login += this.LoginForward;
        this.clientStateService.Logout += this.LogoutForward;
        this.clientStateService.EnterPvP += this.EnterPvPForward;
        this.clientStateService.LeavePvP += this.ExitPvPForward;
        this.clientStateService.CfPop += this.ContentFinderPopForward;
    }
    
    /// <inheritdoc/>
    public event Action<ushort>? TerritoryChanged;
    
    /// <inheritdoc/>
    public event Action? Login;
    
    /// <inheritdoc/>
    public event Action? Logout;
    
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
    public PlayerCharacter? LocalPlayer => this.clientStateService.LocalPlayer;

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
    void IInternalDisposableService.DisposeService()
    {
        this.clientStateService.TerritoryChanged -= this.TerritoryChangedForward;
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
    
    private void LoginForward() => this.Login?.Invoke();
    
    private void LogoutForward() => this.Logout?.Invoke();
    
    private void EnterPvPForward() => this.EnterPvP?.Invoke();
    
    private void ExitPvPForward() => this.LeavePvP?.Invoke();

    private void ContentFinderPopForward(ContentFinderCondition cfc) => this.CfPop?.Invoke(cfc);
}

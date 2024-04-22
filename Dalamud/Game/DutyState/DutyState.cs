using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

namespace Dalamud.Game.DutyState;

/// <summary>
/// This class represents the state of the currently occupied duty.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal unsafe class DutyState : IInternalDisposableService, IDutyState
{
    private readonly DutyStateAddressResolver address;
    private readonly Hook<SetupContentDirectNetworkMessageDelegate> contentDirectorNetworkMessageHook;

    [ServiceManager.ServiceDependency]
    private readonly Condition condition = Service<Condition>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ClientState.ClientState clientState = Service<ClientState.ClientState>.Get();

    [ServiceManager.ServiceConstructor]
    private DutyState(TargetSigScanner sigScanner)
    {
        this.address = new DutyStateAddressResolver();
        this.address.Setup(sigScanner);

        this.contentDirectorNetworkMessageHook = Hook<SetupContentDirectNetworkMessageDelegate>.FromAddress(this.address.ContentDirectorNetworkMessage, this.ContentDirectorNetworkMessageDetour);

        this.framework.Update += this.FrameworkOnUpdateEvent;
        this.clientState.TerritoryChanged += this.TerritoryOnChangedEvent;

        this.contentDirectorNetworkMessageHook.Enable();
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate byte SetupContentDirectNetworkMessageDelegate(IntPtr a1, IntPtr a2, ushort* a3);

    /// <inheritdoc/>
    public event EventHandler<ushort>? DutyStarted;

    /// <inheritdoc/>
    public event EventHandler<ushort>? DutyWiped;
    
    /// <inheritdoc/>
    public event EventHandler<ushort>? DutyRecommenced;
    
    /// <inheritdoc/>
    public event EventHandler<ushort>? DutyCompleted;
    
    /// <inheritdoc/>
    public bool IsDutyStarted { get; private set; }

    private bool CompletedThisTerritory { get; set; }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.contentDirectorNetworkMessageHook.Dispose();
        this.framework.Update -= this.FrameworkOnUpdateEvent;
        this.clientState.TerritoryChanged -= this.TerritoryOnChangedEvent;
    }

    private byte ContentDirectorNetworkMessageDetour(IntPtr a1, IntPtr a2, ushort* a3)
    {
        var category = *a3;
        var type = *(uint*)(a3 + 4);

        // DirectorUpdate Category
        if (category == 0x6D)
        {
            switch (type)
            {
                // Duty Commenced
                case 0x4000_0001:
                    this.IsDutyStarted = true;
                    this.DutyStarted?.Invoke(this, this.clientState.TerritoryType);
                    break;

                // Party Wipe
                case 0x4000_0005:
                    this.IsDutyStarted = false;
                    this.DutyWiped?.Invoke(this, this.clientState.TerritoryType);
                    break;

                // Duty Recommence
                case 0x4000_0006:
                    this.IsDutyStarted = true;
                    this.DutyRecommenced?.Invoke(this, this.clientState.TerritoryType);
                    break;

                // Duty Completed Flytext Shown
                case 0x4000_0002 when !this.CompletedThisTerritory:
                    this.IsDutyStarted = false;
                    this.CompletedThisTerritory = true;
                    this.DutyCompleted?.Invoke(this, this.clientState.TerritoryType);
                    break;

                // Duty Completed
                case 0x4000_0003 when !this.CompletedThisTerritory:
                    this.IsDutyStarted = false;
                    this.CompletedThisTerritory = true;
                    this.DutyCompleted?.Invoke(this, this.clientState.TerritoryType);
                    break;
            }
        }

        return this.contentDirectorNetworkMessageHook.Original(a1, a2, a3);
    }

    private void TerritoryOnChangedEvent(ushort territoryId)
    {
        if (this.IsDutyStarted)
        {
            this.IsDutyStarted = false;
        }

        this.CompletedThisTerritory = false;
    }

    /// <summary>
    /// Fallback event handler in the case that we missed the duty started event.
    /// Joining a duty in progress, or disconnecting and reconnecting will cause the player to miss the event.
    /// </summary>
    /// <param name="framework1">Framework reference.</param>
    private void FrameworkOnUpdateEvent(IFramework framework1)
    {
        // If the duty hasn't been started, and has not been completed yet this territory
        if (!this.IsDutyStarted && !this.CompletedThisTerritory)
        {
            // If the player is in a duty, and got into combat, we need to set the duty stated value
            if (this.IsBoundByDuty() && this.IsInCombat())
            {
                this.IsDutyStarted = true;
            }
        }

        // If the player is no longer bound by duty but we missed the event somehow, set it to false
        else if (!this.IsBoundByDuty() && this.IsDutyStarted)
        {
            this.IsDutyStarted = false;
        }
    }

    private bool IsBoundByDuty()
        => this.condition.Any(ConditionFlag.BoundByDuty,
                              ConditionFlag.BoundByDuty56,
                              ConditionFlag.BoundByDuty95);
    
    private bool IsInCombat()        
        => this.condition.Any(ConditionFlag.InCombat);
}

/// <summary>
/// Plugin scoped version of DutyState.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IDutyState>]
#pragma warning restore SA1015
internal class DutyStatePluginScoped : IInternalDisposableService, IDutyState
{
    [ServiceManager.ServiceDependency]
    private readonly DutyState dutyStateService = Service<DutyState>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="DutyStatePluginScoped"/> class.
    /// </summary>
    internal DutyStatePluginScoped()
    {
        this.dutyStateService.DutyStarted += this.DutyStartedForward;
        this.dutyStateService.DutyWiped += this.DutyWipedForward;
        this.dutyStateService.DutyRecommenced += this.DutyRecommencedForward;
        this.dutyStateService.DutyCompleted += this.DutyCompletedForward;
    }

    /// <inheritdoc/>
    public event EventHandler<ushort>? DutyStarted;
    
    /// <inheritdoc/>
    public event EventHandler<ushort>? DutyWiped;
    
    /// <inheritdoc/>
    public event EventHandler<ushort>? DutyRecommenced;
    
    /// <inheritdoc/>
    public event EventHandler<ushort>? DutyCompleted;
    
    /// <inheritdoc/>
    public bool IsDutyStarted => this.dutyStateService.IsDutyStarted;
    
    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.dutyStateService.DutyStarted -= this.DutyStartedForward;
        this.dutyStateService.DutyWiped -= this.DutyWipedForward;
        this.dutyStateService.DutyRecommenced -= this.DutyRecommencedForward;
        this.dutyStateService.DutyCompleted -= this.DutyCompletedForward;

        this.DutyStarted = null;
        this.DutyWiped = null;
        this.DutyRecommenced = null;
        this.DutyCompleted = null;
    }

    private void DutyStartedForward(object sender, ushort territoryId) => this.DutyStarted?.Invoke(sender, territoryId);
    
    private void DutyWipedForward(object sender, ushort territoryId) => this.DutyWiped?.Invoke(sender, territoryId);
    
    private void DutyRecommencedForward(object sender, ushort territoryId) => this.DutyRecommenced?.Invoke(sender, territoryId);
    
    private void DutyCompletedForward(object sender, ushort territoryId) => this.DutyCompleted?.Invoke(sender, territoryId);
}

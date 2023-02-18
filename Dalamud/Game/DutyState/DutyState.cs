using System;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Utility;

namespace Dalamud.Game.DutyState;

/// <summary>
/// This class represents the state of the currently occupied duty.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
public unsafe class DutyState : IDisposable, IServiceType
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
    private DutyState(SigScanner sigScanner)
    {
        this.address = new DutyStateAddressResolver();
        this.address.Setup(sigScanner);

        this.contentDirectorNetworkMessageHook = Hook<SetupContentDirectNetworkMessageDelegate>.FromAddress(this.address.ContentDirectorNetworkMessage, this.ContentDirectorNetworkMessageDetour);

        this.framework.Update += this.FrameworkOnUpdateEvent;
        this.clientState.TerritoryChanged += this.TerritoryOnChangedEvent;
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate byte SetupContentDirectNetworkMessageDelegate(IntPtr a1, IntPtr a2, ushort* a3);

    /// <summary>
    /// Event that gets fired when the duty starts. Triggers when the "Duty Start"
    /// message displays, and on the remove of the ring at duty's spawn.
    /// </summary>
    public event EventHandler<ushort> DutyStarted;

    /// <summary>
    /// Event that gets fired when everyone in the party dies and the screen fades to black.
    /// </summary>
    public event EventHandler<ushort> DutyWiped;

    /// <summary>
    /// Event that gets fired when the "Duty Recommence" message displays,
    /// and on the remove the the ring at duty's spawn.
    /// </summary>
    public event EventHandler<ushort> DutyRecommenced;

    /// <summary>
    /// Event that gets fired when the duty is completed successfully.
    /// </summary>
    public event EventHandler<ushort> DutyCompleted;

    /// <summary>
    /// Gets a value indicating whether the current duty has been started.
    /// </summary>
    public bool IsDutyStarted { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current duty has been completed or not.
    /// Prevents DutyStarted from triggering if combat is entered after receiving a duty complete network event.
    /// </summary>
    private bool CompletedThisTerritory { get; set; }

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    void IDisposable.Dispose()
    {
        this.contentDirectorNetworkMessageHook.Dispose();
        this.framework.Update -= this.FrameworkOnUpdateEvent;
        this.clientState.TerritoryChanged -= this.TerritoryOnChangedEvent;
    }

    [ServiceManager.CallWhenServicesReady]
    private void ContinueConstruction()
    {
        this.contentDirectorNetworkMessageHook.Enable();
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
                    this.DutyStarted.InvokeSafely(this, this.clientState.TerritoryType);
                    break;

                // Party Wipe
                case 0x4000_0005:
                    this.IsDutyStarted = false;
                    this.DutyWiped.InvokeSafely(this, this.clientState.TerritoryType);
                    break;

                // Duty Recommence
                case 0x4000_0006:
                    this.IsDutyStarted = true;
                    this.DutyRecommenced.InvokeSafely(this, this.clientState.TerritoryType);
                    break;

                // Duty Completed Flytext Shown
                case 0x4000_0002 when !this.CompletedThisTerritory:
                    this.IsDutyStarted = false;
                    this.CompletedThisTerritory = true;
                    this.DutyCompleted.InvokeSafely(this, this.clientState.TerritoryType);
                    break;

                // Duty Completed
                case 0x4000_0003 when !this.CompletedThisTerritory:
                    this.IsDutyStarted = false;
                    this.CompletedThisTerritory = true;
                    this.DutyCompleted.InvokeSafely(this, this.clientState.TerritoryType);
                    break;
            }
        }

        return this.contentDirectorNetworkMessageHook.Original(a1, a2, a3);
    }

    private void TerritoryOnChangedEvent(object? sender, ushort e)
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
    private void FrameworkOnUpdateEvent(Framework framework1)
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

            // Could potentially add a call to DutyCompleted here since this
            // should only be reached if we are actually no longer in a duty, and missed the network event.
        }
    }

    private bool IsBoundByDuty()
    {
        return this.condition[ConditionFlag.BoundByDuty] ||
               this.condition[ConditionFlag.BoundByDuty56] ||
               this.condition[ConditionFlag.BoundByDuty95];
    }

    private bool IsInCombat() => this.condition[ConditionFlag.InCombat];
}

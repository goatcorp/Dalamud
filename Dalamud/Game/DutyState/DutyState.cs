using Dalamud.Data;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dalamud.Game.DutyState;

/// <summary>
/// This class represents the state of the currently occupied duty.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal unsafe class DutyState : IInternalDisposableService, IDutyState
{
    private static readonly ModuleLog Log = ModuleLog.Create<DutyState>();

    private readonly Hook<PacketDispatcher.Delegates.HandleActorControlPacket> handleActorControlPacketHook;

    [ServiceManager.ServiceDependency]
    private readonly ClientState.Conditions.Condition condition = Service<ClientState.Conditions.Condition>.Get();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ClientState.ClientState clientState = Service<ClientState.ClientState>.Get();

    [ServiceManager.ServiceConstructor]
    private DutyState()
    {
        this.handleActorControlPacketHook = Hook<PacketDispatcher.Delegates.HandleActorControlPacket>.FromAddress(
            (nint)PacketDispatcher.MemberFunctionPointers.HandleActorControlPacket,
            this.HandleActorControlPacketDetour);

        this.framework.Update += this.FrameworkOnUpdateEvent;
        this.clientState.TerritoryChanged += this.TerritoryOnChangedEvent;

        this.handleActorControlPacketHook.Enable();
    }

    /// <inheritdoc/>
    public event IDutyState.DutyStartedDelegate? DutyStarted;

    /// <inheritdoc/>
    public event IDutyState.DutyWipedDelegate? DutyWiped;

    /// <inheritdoc/>
    public event IDutyState.DutyRecommencedDelegate? DutyRecommenced;

    /// <inheritdoc/>
    public event IDutyState.DutyCompletedDelegate? DutyCompleted;

    /// <inheritdoc/>
    public RowRef<ContentFinderCondition> ContentFinderCondition => LuminaUtils.CreateRef<ContentFinderCondition>(GameMain.Instance()->CurrentContentFinderConditionId);

    /// <inheritdoc/>
    public bool IsDutyStarted { get; private set; }

    private bool CompletedThisTerritory { get; set; }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.handleActorControlPacketHook.Dispose();
        this.framework.Update -= this.FrameworkOnUpdateEvent;
        this.clientState.TerritoryChanged -= this.TerritoryOnChangedEvent;
    }

    private void HandleActorControlPacketDetour(uint entityId, uint category, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, uint arg6, uint arg7, uint arg8, GameObjectId targetId, bool isRecorded)
    {
        if (category == 0x6D)
        {
            switch (arg2)
            {
                // Duty Commenced
                case 0x4000_0001:
                    {
                        this.IsDutyStarted = true;

                        var args = this.CreateEventArgs(arg1);

                        foreach (var action in Delegate.EnumerateInvocationList(this.DutyStarted))
                        {
                            try
                            {
                                action(args);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Exception during raise of {handler}", action.Method);
                            }
                        }
                    }

                    break;

                // Party Wipe
                case 0x4000_0005:
                    {
                        this.IsDutyStarted = false;

                        var args = this.CreateEventArgs(arg1);

                        foreach (var action in Delegate.EnumerateInvocationList(this.DutyWiped))
                        {
                            try
                            {
                                action(args);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Exception during raise of {handler}", action.Method);
                            }
                        }
                    }

                    break;

                // Duty Recommence
                case 0x4000_0006:
                    {
                        this.IsDutyStarted = true;

                        var args = this.CreateEventArgs(arg1);

                        foreach (var action in Delegate.EnumerateInvocationList(this.DutyRecommenced))
                        {
                            try
                            {
                                action(args);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Exception during raise of {handler}", action.Method);
                            }
                        }
                    }

                    break;

                // Duty Completed Flytext Shown
                case 0x4000_0002 when !this.CompletedThisTerritory:
                    {
                        this.IsDutyStarted = false;
                        this.CompletedThisTerritory = true;

                        var args = this.CreateEventArgs(arg1);

                        foreach (var action in Delegate.EnumerateInvocationList(this.DutyCompleted))
                        {
                            try
                            {
                                action(args);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Exception during raise of {handler}", action.Method);
                            }
                        }
                    }

                    break;

                // Duty Completed
                case 0x4000_0003 when !this.CompletedThisTerritory:
                    {
                        this.IsDutyStarted = false;
                        this.CompletedThisTerritory = true;

                        var args = this.CreateEventArgs(arg1);

                        foreach (var action in Delegate.EnumerateInvocationList(this.DutyCompleted))
                        {
                            try
                            {
                                action(args);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Exception during raise of {handler}", action.Method);
                            }
                        }
                    }

                    break;
            }
        }

        this.handleActorControlPacketHook.Original(entityId, category, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, targetId, isRecorded);
    }

    private DutyStateEventArgs CreateEventArgs(uint eventHandlerId)
    {
        return new DutyStateEventArgs()
        {
            TerritoryType = LuminaUtils.CreateRef<TerritoryType>(this.clientState.TerritoryType),
            ContentFinderCondition = this.ContentFinderCondition,
            EventHandlerId = eventHandlerId,
        };
    }

    private void TerritoryOnChangedEvent(uint territoryId)
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
    public event IDutyState.DutyStartedDelegate? DutyStarted;

    /// <inheritdoc/>
    public event IDutyState.DutyWipedDelegate? DutyWiped;

    /// <inheritdoc/>
    public event IDutyState.DutyRecommencedDelegate? DutyRecommenced;

    /// <inheritdoc/>
    public event IDutyState.DutyCompletedDelegate? DutyCompleted;

    /// <inheritdoc/>
    public RowRef<ContentFinderCondition> ContentFinderCondition => this.dutyStateService.ContentFinderCondition;

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

    private void DutyStartedForward(IDutyStateEventArgs args) => this.DutyStarted?.Invoke(args);

    private void DutyWipedForward(IDutyStateEventArgs args) => this.DutyWiped?.Invoke(args);

    private void DutyRecommencedForward(IDutyStateEventArgs args) => this.DutyRecommenced?.Invoke(args);

    private void DutyCompletedForward(IDutyStateEventArgs args) => this.DutyCompleted?.Invoke(args);
}

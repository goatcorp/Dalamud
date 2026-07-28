using Dalamud.Configuration.Internal;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Internal;

/// <summary>
/// Integrates Dalamuds window system into the games input and collision handling.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class WindowSystemIntegration : IInternalDisposableService
{
    private static readonly ModuleLog Log = ModuleLog.Create<WindowSystemIntegration>();

    private readonly Hook<AtkUnitBase.Delegates.ReceiveGlobalEvent> hookAtkUnitBaseReceiveGlobalEvent;
    private readonly Hook<RaptureAtkUnitManager.Delegates.GetAddonCollision> hookGetAddonCollision;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private bool disposed = false;

    [ServiceManager.ServiceConstructor]
    private WindowSystemIntegration()
    {
        this.hookAtkUnitBaseReceiveGlobalEvent = Hook<AtkUnitBase.Delegates.ReceiveGlobalEvent>.FromAddress((nint)AtkUnitBase.StaticVirtualTablePointer->ReceiveGlobalEvent, this.AtkUnitBaseReceiveGlobalEventDetour);
        this.hookGetAddonCollision = Hook<RaptureAtkUnitManager.Delegates.GetAddonCollision>.FromAddress((nint)RaptureAtkUnitManager.StaticVirtualTablePointer->GetAddonCollision, this.RaptureAtkUnitManagerGetAddonCollisionDetour);

        this.hookAtkUnitBaseReceiveGlobalEvent.Enable();
        this.hookGetAddonCollision.Enable();
    }

    /// <summary>Finalizes an instance of the <see cref="WindowSystemIntegration"/> class.</summary>
    ~WindowSystemIntegration() => this.Dispose(false);

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.Dispose(true);

    private void Dispose(bool disposing)
    {
        if (this.disposed)
            return;

        if (disposing)
        {
            this.hookAtkUnitBaseReceiveGlobalEvent.Dispose();
            this.hookGetAddonCollision.Dispose();
        }

        this.disposed = true;
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
        if (WindowSystem.ShouldInhibitAtkCollisions && !UIModule.Instance()->IsPadModeEnabled())
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
}

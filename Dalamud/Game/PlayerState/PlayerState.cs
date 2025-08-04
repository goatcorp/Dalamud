using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Data;
using Dalamud.Hooking;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Dalamud.Game.PlayerState;

/// <summary>
/// This class represents the state of the local player.
/// </summary>
internal unsafe partial class PlayerState : IInternalDisposableService, IPlayerState
{
    private static readonly ModuleLog Log = new("PlayerState");

    private readonly PlayerStateAddressResolver address;
    private readonly Hook<UIModule.Delegates.HandlePacket> uiModuleHandlePacketHook;
    private readonly Hook<PerformMateriaActionMigrationDelegate> performMateriaActionMigrationDelegateHook;
    private readonly ConcurrentDictionary<Type, HashSet<uint>> cachedUnlockedRowIds = [];

    [ServiceManager.ServiceDependency]
    private readonly DataManager dataManager = Service<DataManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ClientState.ClientState clientState = Service<ClientState.ClientState>.Get();

    [ServiceManager.ServiceConstructor]
    private PlayerState(TargetSigScanner sigScanner)
    {
        this.address = new PlayerStateAddressResolver();
        this.address.Setup(sigScanner);

        this.clientState.Login += this.OnLogin;
        this.clientState.Logout += this.OnLogout;

        this.uiModuleHandlePacketHook = Hook<UIModule.Delegates.HandlePacket>.FromAddress(
            (nint)UIModule.StaticVirtualTablePointer->HandlePacket,
            this.UIModuleHandlePacketDetour);

        this.performMateriaActionMigrationDelegateHook = Hook<PerformMateriaActionMigrationDelegate>.FromAddress(
            this.address.PerformMateriaActionMigration,
            this.PerformMateriaActionMigrationDetour);

        this.uiModuleHandlePacketHook.Enable();
        this.performMateriaActionMigrationDelegateHook.Enable();
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void PerformMateriaActionMigrationDelegate(RaptureHotbarModule* thisPtr);

    /// <inheritdoc/>
    public event IPlayerState.ClassJobChangeDelegate? ClassJobChange;

    /// <inheritdoc/>
    public event IPlayerState.LevelChangeDelegate? LevelChange;

    /// <inheritdoc/>
    public event IPlayerState.UnlockDelegate Unlock;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.clientState.Login -= this.OnLogin;
        this.clientState.Logout -= this.OnLogout;
        this.uiModuleHandlePacketHook.Dispose();
        this.performMateriaActionMigrationDelegateHook.Dispose();
    }

    private void OnLogin()
    {
        try
        {
            this.UpdateUnlocks(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during initial unlock check");
        }
    }

    private void OnLogout(int type, int code)
    {
        this.cachedUnlockedRowIds.Clear();
    }

    private unsafe void UIModuleHandlePacketDetour(
        UIModule* thisPtr, UIModulePacketType type, uint uintParam, void* packet)
    {
        this.uiModuleHandlePacketHook.Original(thisPtr, type, uintParam, packet);

        switch (type)
        {
            case UIModulePacketType.ClassJobChange:
                {
                    var classJobId = uintParam;

                    foreach (var action in Delegate.EnumerateInvocationList(this.ClassJobChange))
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

                    foreach (var action in Delegate.EnumerateInvocationList(this.LevelChange))
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

    private void PerformMateriaActionMigrationDetour(RaptureHotbarModule* thisPtr)
    {
        try
        {
            this.UpdateUnlocks(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during unlock check");
        }

        this.performMateriaActionMigrationDelegateHook.Original(thisPtr);
    }
}

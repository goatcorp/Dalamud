using System;
using System.Runtime.InteropServices;

using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.AddonLifecycle;

/// <summary>
/// This class provides events for in-game addon lifecycles.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IAddonLifecycle>]
#pragma warning restore SA1015
internal unsafe class AddonLifecycle : IDisposable, IServiceType, IAddonLifecycle
{
    private readonly AddonLifecycleAddressResolver address;
    private readonly Hook<AddonSetupDelegate> onAddonSetupHook;
    private readonly Hook<AddonFinalizeDelegate> onAddonFinalizeHook;
    
    [ServiceManager.ServiceConstructor]
    private AddonLifecycle(SigScanner sigScanner)
    {
        this.address = new AddonLifecycleAddressResolver();
        this.address.Setup(sigScanner);

        this.onAddonSetupHook = Hook<AddonSetupDelegate>.FromAddress(this.address.AddonSetup, this.OnAddonSetup);
        this.onAddonFinalizeHook = Hook<AddonFinalizeDelegate>.FromAddress(this.address.AddonSetup, this.OnAddonFinalize);
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate nint AddonSetupDelegate(AtkUnitBase* addon);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void AddonFinalizeDelegate(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase);
    
    /// <inheritdoc/>
    public event Action<IAddonLifecycle.AddonArgs>? AddonPreSetup;
    
    /// <inheritdoc/>
    public event Action<IAddonLifecycle.AddonArgs>? AddonPostSetup;
    
    /// <inheritdoc/>
    public event Action<IAddonLifecycle.AddonArgs>? AddonPreFinalize;
    
    /// <inheritdoc/>
    public event Action<IAddonLifecycle.AddonArgs>? AddonPostFinalize;
    
    /// <inheritdoc/>
    public void Dispose()
    {
        this.onAddonSetupHook.Dispose();
        this.onAddonFinalizeHook.Dispose();
    }
    
    [ServiceManager.CallWhenServicesReady]
    private void ContinueConstruction()
    {
        this.onAddonSetupHook.Enable();
        this.onAddonFinalizeHook.Enable();
    }

    private nint OnAddonSetup(AtkUnitBase* addon)
    {
        try
        {
            this.AddonPreSetup?.Invoke(new IAddonLifecycle.AddonArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "[AddonLifecycle] Exception in OnAddonSetup pre-setup invoke.");
        }

        var result = this.onAddonSetupHook.Original(addon);

        try
        {
            this.AddonPostSetup?.Invoke(new IAddonLifecycle.AddonArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "[AddonLifecycle] Exception in OnAddonSetup post-setup invoke.");
        }

        return result;
    }

    private void OnAddonFinalize(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase)
    {
        try
        {
            this.AddonPreFinalize?.Invoke(new IAddonLifecycle.AddonArgs { Addon = (nint)atkUnitBase[0] });
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "[AddonLifecycle] Exception in OnAddonFinalize pre-finalize invoke.");
        }

        this.onAddonFinalizeHook.Original(unitManager, atkUnitBase);

        try
        {
            this.AddonPostFinalize?.Invoke(new IAddonLifecycle.AddonArgs { Addon = (nint)atkUnitBase[0] });
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "[AddonLifecycle] Exception in OnAddonFinalize post-finalize invoke.");
        }
    }
}

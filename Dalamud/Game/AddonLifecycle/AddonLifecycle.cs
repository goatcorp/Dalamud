using System;

using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.AddonLifecycle;

/// <summary>
/// This class provides events for in-game addon lifecycles.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal unsafe class AddonLifecycle : IDisposable, IServiceType, IAddonLifecycle
{
    private static readonly ModuleLog Log = new("AddonLifecycle");
    private readonly AddonLifecycleAddressResolver address;
    private readonly Hook<AddonSetupDelegate> onAddonSetupHook;
    private readonly Hook<AddonFinalizeDelegate> onAddonFinalizeHook;
    
    [ServiceManager.ServiceConstructor]
    private AddonLifecycle(SigScanner sigScanner)
    {
        this.address = new AddonLifecycleAddressResolver();
        this.address.Setup(sigScanner);

        this.onAddonSetupHook = Hook<AddonSetupDelegate>.FromAddress(this.address.AddonSetup, this.OnAddonSetup);
        this.onAddonFinalizeHook = Hook<AddonFinalizeDelegate>.FromAddress(this.address.AddonFinalize, this.OnAddonFinalize);
    }

    private delegate nint AddonSetupDelegate(AtkUnitBase* addon);

    private delegate void AddonFinalizeDelegate(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase);
    
    /// <inheritdoc/>
    public event Action<IAddonLifecycle.AddonArgs>? AddonPreSetup;
    
    /// <inheritdoc/>
    public event Action<IAddonLifecycle.AddonArgs>? AddonPostSetup;
    
    /// <inheritdoc/>
    public event Action<IAddonLifecycle.AddonArgs>? AddonPreFinalize;
    
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
        if (addon is null)
            return this.onAddonSetupHook.Original(addon);

        try
        {
            this.AddonPreSetup?.Invoke(new IAddonLifecycle.AddonArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonSetup pre-setup invoke.");
        }

        var result = this.onAddonSetupHook.Original(addon);

        try
        {
            this.AddonPostSetup?.Invoke(new IAddonLifecycle.AddonArgs { Addon = (nint)addon });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonSetup post-setup invoke.");
        }

        return result;
    }

    private void OnAddonFinalize(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase)
    {
        if (atkUnitBase is null)
        {
            this.onAddonFinalizeHook.Original(unitManager, atkUnitBase);
            return;
        }
        
        try
        {
            this.AddonPreFinalize?.Invoke(new IAddonLifecycle.AddonArgs { Addon = (nint)atkUnitBase[0] });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in OnAddonFinalize pre-finalize invoke.");
        }

        this.onAddonFinalizeHook.Original(unitManager, atkUnitBase);
    }
}

/// <summary>
/// Plugin-scoped version of a AddonLifecycle service.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IAddonLifecycle>]
#pragma warning restore SA1015
internal class AddonLifecyclePluginScoped : IDisposable, IServiceType, IAddonLifecycle
{
    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycleService = Service<AddonLifecycle>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonLifecyclePluginScoped"/> class.
    /// </summary>
    public AddonLifecyclePluginScoped()
    {
        this.addonLifecycleService.AddonPreSetup += this.AddonPreSetupForward;
        this.addonLifecycleService.AddonPostSetup += this.AddonPostSetupForward;
        this.addonLifecycleService.AddonPreFinalize += this.AddonPreFinalizeForward;
    }
    
    /// <inheritdoc/>
    public event Action<IAddonLifecycle.AddonArgs>? AddonPreSetup;
    
    /// <inheritdoc/>
    public event Action<IAddonLifecycle.AddonArgs>? AddonPostSetup;
    
    /// <inheritdoc/>
    public event Action<IAddonLifecycle.AddonArgs>? AddonPreFinalize;
    
    /// <inheritdoc/>
    public void Dispose()
    {
        this.addonLifecycleService.AddonPreSetup -= this.AddonPreSetupForward;
        this.addonLifecycleService.AddonPostSetup -= this.AddonPostSetupForward;
        this.addonLifecycleService.AddonPreFinalize -= this.AddonPreFinalizeForward;
    }

    private void AddonPreSetupForward(IAddonLifecycle.AddonArgs args) => this.AddonPreSetup?.Invoke(args);
    
    private void AddonPostSetupForward(IAddonLifecycle.AddonArgs args) => this.AddonPostSetup?.Invoke(args);
    
    private void AddonPreFinalizeForward(IAddonLifecycle.AddonArgs args) => this.AddonPreFinalize?.Invoke(args);
}

using System.Collections.Generic;

using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Logging.Internal;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// This class is a helper for tracking and invoking listener delegates for Addon_Show.
/// Multiple addons may use the same Show function, this helper makes sure that those addon events are handled properly.
/// </summary>
internal unsafe class AddonLifecycleShowListener : IDisposable
{
    private static readonly ModuleLog Log = new("AddonLifecycle");

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecyclePooledArgs argsPool = Service<AddonLifecyclePooledArgs>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonLifecycleShowListener"/> class.
    /// </summary>
    /// <param name="service">AddonLifecycle service instance.</param>
    /// <param name="addonName">Initial Addon Requesting this listener.</param>
    /// <param name="showAddress">Address of Addon's Show function.</param>
    internal AddonLifecycleShowListener(AddonLifecycle service, string addonName, nint showAddress)
    {
        this.AddonLifecycle = service;
        this.AddonNames = [addonName];
        this.Hook = Hook<AtkUnitBase.Delegates.Show>.FromAddress(showAddress, this.OnShow);
    }

    /// <summary>
    /// Gets the list of addons that use this hook.
    /// </summary>
    public List<string> AddonNames { get; init; }
    
    /// <summary>
    /// Gets the address of the registered hook.
    /// </summary>
    public nint HookAddress => this.Hook?.Address ?? nint.Zero;
    
    /// <summary>
    /// Gets the contained hook for these addons.
    /// </summary>
    public Hook<AtkUnitBase.Delegates.Show>? Hook { get; init; }
    
    /// <summary>
    /// Gets or sets the Reference to AddonLifecycle service instance.
    /// </summary>
    private AddonLifecycle AddonLifecycle { get; set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Hook?.Dispose();
    }

    private void OnShow(AtkUnitBase* addon, bool openSilently, uint unsetShowHideFlags)
    {
        // Check that we didn't get here through a call to another addons handler.
        var addonName = addon->NameString;
        if (!this.AddonNames.Contains(addonName))
        {
            this.Hook!.Original(addon, openSilently, unsetShowHideFlags);
            return;
        }

        using var returner = this.argsPool.Rent(out AddonShowArgs arg);
        arg.AddonInternal = (nint)addon;
        arg.OpenSilently = openSilently;
        arg.UnsetShowHideFlags = unsetShowHideFlags;
        this.AddonLifecycle.InvokeListenersSafely(AddonEvent.PreShow, arg);
        openSilently = arg.OpenSilently;
        unsetShowHideFlags = arg.UnsetShowHideFlags;
        
        try
        {
            this.Hook!.Original(addon, openSilently, unsetShowHideFlags);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonReceiveEvent. This may be a bug in the game or another plugin hooking this method.");
        }

        this.AddonLifecycle.InvokeListenersSafely(AddonEvent.PostShow, arg);
    }
}

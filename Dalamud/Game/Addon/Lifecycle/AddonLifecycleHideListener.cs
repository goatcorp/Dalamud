using System.Collections.Generic;

using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Logging.Internal;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// This class is a helper for tracking and invoking listener delegates for Addon_Hide.
/// Multiple addons may use the same Hide function, this helper makes sure that those addon events are handled properly.
/// </summary>
internal unsafe class AddonLifecycleHideListener : IDisposable
{
    private static readonly ModuleLog Log = new("AddonLifecycle");

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecyclePooledArgs argsPool = Service<AddonLifecyclePooledArgs>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonLifecycleHideListener"/> class.
    /// </summary>
    /// <param name="service">AddonLifecycle service instance.</param>
    /// <param name="addonName">Initial Addon Requesting this listener.</param>
    /// <param name="hideAddress">Address of Addon's Hide function.</param>
    internal AddonLifecycleHideListener(AddonLifecycle service, string addonName, nint hideAddress)
    {
        this.AddonLifecycle = service;
        this.AddonNames = [addonName];
        this.Hook = Hook<AtkUnitBase.Delegates.Hide>.FromAddress(hideAddress, this.OnHide);
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
    public Hook<AtkUnitBase.Delegates.Hide>? Hook { get; init; }
    
    /// <summary>
    /// Gets or sets the Reference to AddonLifecycle service instance.
    /// </summary>
    private AddonLifecycle AddonLifecycle { get; set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Hook?.Dispose();
    }

    private void OnHide(AtkUnitBase* addon, bool unkBool, bool callHideCallback, uint setShowHideFlags)
    {
        // Check that we didn't get here through a call to another addons handler.
        var addonName = addon->NameString;
        if (!this.AddonNames.Contains(addonName))
        {
            this.Hook!.Original(addon, unkBool, callHideCallback, setShowHideFlags);
            return;
        }

        using var returner = this.argsPool.Rent(out AddonHideArgs arg);
        arg.AddonInternal = (nint)addon;
        arg.Unknown = unkBool;
        arg.CallHideCallback = callHideCallback;
        arg.SetShowHideFlags = setShowHideFlags;
        this.AddonLifecycle.InvokeListenersSafely(AddonEvent.PreHide, arg);
        unkBool = arg.Unknown;
        callHideCallback = arg.CallHideCallback;
        setShowHideFlags = arg.SetShowHideFlags;
        
        try
        {
            this.Hook!.Original(addon, unkBool, callHideCallback, setShowHideFlags);
        }
        catch (Exception e)
        {
            Log.Error(e, "Caught exception when calling original AddonReceiveEvent. This may be a bug in the game or another plugin hooking this method.");
        }

        this.AddonLifecycle.InvokeListenersSafely(AddonEvent.PostHide, arg);
    }
}

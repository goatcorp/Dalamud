using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.NativeUi.BaseTypes.Addon;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.NativeUi;

/// <summary>
/// Service api implementation providing devs with access to managing native ui elements in overlay addons.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class NativeOverlay : IInternalDisposableService, INativeOverlayProvider
{
    private static readonly ModuleLog Log = ModuleLog.Create<NativeOverlay>();

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycle = Service<AddonLifecycle>.Get();

    private readonly Dictionary<int, OverlayAddon> overlayAddons = [];

    private readonly AddonLifecycleEventListener? addonNameplateSetupListener;
    private readonly AddonLifecycleEventListener? addonNameplateFinalizeListener;

    private readonly Hook<AtkUnitBase.Delegates.FireCallback>? fireCallbackHook;

    [ServiceManager.ServiceConstructor]
    private NativeOverlay()
    {
        this.fireCallbackHook = Hook<AtkUnitBase.Delegates.FireCallback>.FromAddress(AtkUnitBase.Addresses.FireCallback.Value, this.OnFireCallback);
        // this.fireCallbackHook.Enable(); // Disabled for now, this will need to be enabled if dalamud creates any normal Native UI windows (non-overlay).

        this.addonNameplateSetupListener = new AddonLifecycleEventListener(AddonEvent.PostSetup, "NamePlate", this.OnNameplateSetup);
        this.addonNameplateFinalizeListener = new AddonLifecycleEventListener(AddonEvent.PreFinalize, "NamePlate", this.OnNameplateFinalize);

        this.addonLifecycle.RegisterListener(this.addonNameplateSetupListener);
        this.addonLifecycle.RegisterListener(this.addonNameplateFinalizeListener);

        // If dalamud is injected after login, build overlays asap.
        var unitManager = RaptureAtkUnitManager.Instance();
        if (unitManager is not null)
        {
            if (unitManager->GetAddonByName("NamePlate") is not null)
            {
                Service<Framework>.Get().RunOnFrameworkThread(this.BuildAllOverlays);
            }
        }
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.fireCallbackHook?.Dispose();

        this.addonLifecycle.UnregisterListener(this.addonNameplateSetupListener);
        this.addonLifecycle.UnregisterListener(this.addonNameplateFinalizeListener);

        Service<Framework>.Get().RunOnFrameworkThread(() =>
        {
            foreach (var (_, addon) in this.overlayAddons)
            {
                addon.Dispose();
            }
        });
    }

    /// <inheritdoc/>
    public void AddNode(IOverlayNode node, int depthLayer)
    {
        if (depthLayer < 0 || depthLayer > RaptureAtkUnitManager.Instance()->DepthLayers.Length)
        {
            return;
        }

        if (node.GetAsAtkResNode() is null) return;

        ThreadSafety.AssertMainThread();

        if (this.overlayAddons.TryGetValue(depthLayer, out var addon))
        {
            addon.AttachNode(node);
        }
    }

    /// <inheritdoc/>
    public void RemoveNode(IOverlayNode node, int depthLayer)
    {
        if (depthLayer < 0 || depthLayer > RaptureAtkUnitManager.Instance()->DepthLayers.Length)
        {
            return;
        }

        if (node.GetAsAtkResNode() is null) return;

        ThreadSafety.AssertMainThread();

        if (this.overlayAddons.TryGetValue(depthLayer, out var addon))
        {
            addon.DetachNode(node);
        }
    }

    private void OnNameplateSetup(AddonEvent type, AddonArgs args)
    {
        this.BuildAllOverlays();
    }

    private void OnNameplateFinalize(AddonEvent type, AddonArgs args)
    {
        foreach (var (_, addon) in this.overlayAddons)
        {
            addon.Close();
        }
    }

    private void BuildAllOverlays()
    {
        var layerCount = RaptureAtkUnitManager.Instance()->DepthLayers.Length;

        foreach (var index in Enumerable.Range(0, layerCount))
        {
            if (this.overlayAddons.TryGetValue(index, out var addon))
            {
                addon.Open();
            }
            else
            {
                var newAddon = new OverlayAddon
                {
                    InternalName = $"_DalamudOverlay_Layer{index}",
                    Title = "Dalamud Overlay Addon",
                    Subtitle = $"Layer {index}",
                    Size = AtkStage.Instance()->ScreenSize,
                    DepthLayer = index + 1,
                };

                newAddon.Open();

                this.overlayAddons.Add(index, newAddon);
            }
        }
    }

    // This hook is invoked when the user presses ESC with no windows focused, normally this would cause any open AtkUnitBase's to be closed
    // But this doesn't get forwarded to custom addons, so we have to do it here. Devs can use RespectCloseAll to disable this behavior,
    // However this should generally be discouraged, unless the dev has a good reason to ignore the standard close behavior.
    private bool OnFireCallback(AtkUnitBase* thisPtr, uint valueCount, AtkValue* values, bool close)
    {
        try
        {
            foreach (var addon in NativeAddon.CreatedAddons)
            {
                if (addon == thisPtr && close && addon is { RespectCloseAll: true, IsOverlayAddon: false })
                {
                    addon.Close();
                    return true;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception handling OnFireCallback.");
        }

        return this.fireCallbackHook!.Original(thisPtr, valueCount, values, close);
    }
}

/// <summary>
/// Plugin scoped version of NativeOverlay.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<INativeOverlayProvider>]
#pragma warning restore SA1015
internal class NativeOverlayPluginScoped : IInternalDisposableService, INativeOverlayProvider
{
    private static readonly ModuleLog Log = ModuleLog.Create<NativeOverlayPluginScoped>();

    [ServiceManager.ServiceDependency]
    private readonly NativeOverlay nativeOverlayService = Service<NativeOverlay>.Get();

    private readonly Dictionary<int, List<IOverlayNode>> attachedNodes = [];

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        foreach (var (layer, nodeList) in this.attachedNodes)
        {
            foreach (var node in nodeList)
            {
                this.nativeOverlayService.RemoveNode(node, layer);
            }
        }
    }

    /// <inheritdoc/>
    public unsafe void AddNode(IOverlayNode node, int depthLayer)
    {
        if (depthLayer < 0 || depthLayer > RaptureAtkUnitManager.Instance()->DepthLayers.Length)
        {
            Log.Warning("Attempted to attach a overlay node to an invalid depth layer.");
            return;
        }

        ThreadSafety.AssertMainThread();

        this.nativeOverlayService.AddNode(node, depthLayer);

        // Probably unnecessary, but IDE complains that attachedNodes is unused if it's not here.
        this.attachedNodes.TryAdd(depthLayer, []);

        this.attachedNodes[depthLayer].Add(node);
    }

    /// <inheritdoc/>
    public unsafe void RemoveNode(IOverlayNode node, int depthLayer)
    {
        if (depthLayer < 0 || depthLayer > RaptureAtkUnitManager.Instance()->DepthLayers.Length)
        {
            Log.Warning("Attempted to attach a overlay node to an invalid depth layer.");
            return;
        }

        ThreadSafety.AssertMainThread();

        if (this.attachedNodes.TryGetValue(depthLayer, out var nodes) && nodes.Contains(node))
        {
            this.nativeOverlayService.RemoveNode(node, depthLayer);
            nodes.Remove(node);
        }
    }
}

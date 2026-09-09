using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.NativeUi;

/// <summary>
/// Service api implementation providing devs with access to managing native ui elements in overlay addons.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class NativeOverlay : IInternalDisposableService, INativeOverlay
{
    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycle = Service<AddonLifecycle>.Get();

    private readonly Dictionary<int, OverlayAddon> overlayAddons = [];

    private readonly AddonLifecycleEventListener? addonNameplateSetupListener;
    private readonly AddonLifecycleEventListener? addonNameplateFinalizeListener;

    [ServiceManager.ServiceConstructor]
    private NativeOverlay()
    {
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
}

/// <summary>
/// Plugin scoped version of NativeOverlay.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<INativeOverlay>]
#pragma warning restore SA1015
internal class NativeOverlayPluginScoped : IInternalDisposableService, INativeOverlay
{
    [ServiceManager.ServiceDependency]
    private readonly NativeOverlay nativeOverlayService = Service<NativeOverlay>.Get();

    private readonly Dictionary<int, List<IOverlayNode>> attachedNodes = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeOverlayPluginScoped"/> class.
    /// </summary>
    internal NativeOverlayPluginScoped()
    {
        foreach (var (layer, nodeList) in this.attachedNodes)
        {
            foreach (var node in nodeList)
            {
                this.nativeOverlayService.RemoveNode(node, layer);
            }
        }

        this.attachedNodes.Clear();
    }

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
    public void AddNode(IOverlayNode node, int depthLayer)
    {
        ThreadSafety.AssertMainThread();

        this.nativeOverlayService.AddNode(node, depthLayer);

        // Probably unnecessary, but IDE complains that attachedNodes is unused if it's not here.
        this.attachedNodes.TryAdd(depthLayer, []);

        this.attachedNodes[depthLayer].Add(node);
    }

    /// <inheritdoc/>
    public void RemoveNode(IOverlayNode node, int depthLayer)
    {
        ThreadSafety.AssertMainThread();

        if (this.attachedNodes.TryGetValue(depthLayer, out var nodes) && nodes.Contains(node))
        {
            this.nativeOverlayService.RemoveNode(node, depthLayer);
            nodes.Remove(node);
        }
    }
}

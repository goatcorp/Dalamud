using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Tooltip.Classes;
using Dalamud.Game.Tooltip.TooltipArgTypes;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.NativeUi.BaseTypes.Node;
using Dalamud.NativeUi.Enums;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Tooltip;

/// <summary>
/// This class provides events for in-game addon lifecycles.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class Tooltip : IInternalDisposableService
{
    private static readonly ModuleLog Log = ModuleLog.Create<Tooltip>();

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycle = Service<AddonLifecycle>.Get();

    private readonly AddonLifecycleEventListener preRequestedUpdateListener;
    private readonly AddonLifecycleEventListener postRequestedUpdateListener;

    /// <summary>
    /// Represents a dictionary tooltip args that have completed their delegate callbacks,
    /// and are pending node construction and lay-outing.
    /// This should be empty except after Pre-RequestedUpdate and before Post-RequestedUpdate.
    /// </summary>
    private readonly Dictionary<TooltipType, List<PluginTooltipInfo>> newTooltipNodes = [];
    private readonly List<NodeBase> attachedNodes = [];

    // Temporary value used to make the game resize the tooltip addon for us, and then we reset it after.
    private ushort heightChange;

    [ServiceManager.ServiceConstructor]
    private Tooltip()
    {
        this.preRequestedUpdateListener = new AddonLifecycleEventListener(AddonEvent.PreRequestedUpdate, "ItemDetail", this.OnItemDetailPreRequestedUpdate);
        this.postRequestedUpdateListener = new AddonLifecycleEventListener(AddonEvent.PostRequestedUpdate, "ItemDetail", this.OnItemDetailPostRequestedUpdate);

        this.addonLifecycle.RegisterListener(this.preRequestedUpdateListener);
        this.addonLifecycle.RegisterListener(this.postRequestedUpdateListener);
    }

    /// <summary>
    /// Gets a dictionary of all active tooltip entries.
    /// </summary>
    internal Dictionary<TooltipType, List<TooltipEventListener>> EventListeners { get; } = [];

    /// <inheritdoc/>
    public void DisposeService()
    {
        this.addonLifecycle.UnregisterListener(this.preRequestedUpdateListener);
        this.addonLifecycle.UnregisterListener(this.postRequestedUpdateListener);

        _ = Service<Framework>.Get().Run(() =>
        {
            foreach (var node in this.attachedNodes)
            {
                node.Dispose();
            }
        });
    }

    /// <summary>
    /// Checks the number array data provided by RequestedUpdateArgs to determine if this event is part of a rebuild.
    /// </summary>
    /// <param name="requestedUpdateArgs">Arg data.</param>
    /// <returns>True if the addon is requesting a layout rebuild.</returns>
    private static unsafe bool IsItemDetailRebuildingLayout(AddonRequestedUpdateArgs requestedUpdateArgs)
    {
        var numberArrays = (NumberArrayData**)requestedUpdateArgs.NumberArrayData;
        if (numberArrays == null)
        {
            return false;
        }

        var itemDetailNumberArray = numberArrays[(int)NumberArrayType.ItemDetail];
        if (itemDetailNumberArray == null)
        {
            return false;
        }

        if (itemDetailNumberArray->IntArray[0] is 0)
        {
            return false; // An item has not been populated, so the addon is setting up for the first time.
        }

        // IntArray index 3 represents a dirty or is rebuilding flag that the game checks
        // before calling 'GenerateTooltip', this prevents processing tooltip twice in a row for the same tooltip.
        var isRebuilding = itemDetailNumberArray->IntArray[3];

        return isRebuilding is 0;
    }

    private unsafe void OnItemDetailPreRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonRequestedUpdateArgs requestedUpdateArgs)
        {
            return; // Shouldn't be possible.
        }

        if (IsItemDetailRebuildingLayout(requestedUpdateArgs))
        {
            return; // This update isn't part of a tooltip rebuild, we should ignore it.
        }

        // Remove any left-over nodes, we are about to rebuild the tooltip as-well.
        foreach (var node in this.attachedNodes)
        {
            node.Dispose();
        }

        this.heightChange = 0;

        // If there are no listeners defined, we still need to populate the dictionary key.
        this.EventListeners.TryAdd(TooltipType.Item, []);

        // For each listener, build that listeners args object, call
        foreach (var listener in this.EventListeners[TooltipType.Item])
        {
            var newTooltipData = new ItemTooltipArgs
            {
                AddonPointer = args.Addon,
                NumberArrayDataPointer = (NumberArrayData**)requestedUpdateArgs.NumberArrayData,
                StringArrayDataRoot = (StringArrayData**)requestedUpdateArgs.StringArrayData,
                SourcePluginName = listener.SourcePluginName,
            };

            try
            {
                listener.ListenerDelegate(TooltipType.Item, newTooltipData);
            }
            catch (Exception e)
            {
                Log.Error(e, "Exception during ITooltip Listener Invoke.");
            }

            var builtNode = newTooltipData.BuildTooltipNode();

            // If it's not null, then this args object want's a custom tooltip node.
            if (builtNode is not null)
            {
                // Due to game limitation, the backing panel for the tooltip is limited to a min height of 96px.
                // If plugins want smaller tooltips, they should edit the native tooltip itself instead.
                if (builtNode.Height < 96.0f)
                {
                    builtNode.Height = 96.0f;
                }

                var tooltipNodeInfo = new PluginTooltipInfo
                {
                    SourcePluginName = listener.SourcePluginName,
                    TooltipNode = builtNode,
                };

                this.heightChange += (ushort)tooltipNodeInfo.TooltipNode.Height;

                this.newTooltipNodes.TryAdd(TooltipType.Item, []);
                this.newTooltipNodes[TooltipType.Item].Add(tooltipNodeInfo);
            }
        }

        // Offset 0x390 seems to be a value used to calculate a fixed additional offset used for the text NineGrid that shows below the tooltip.
        // We can manipulate this to make the game think the tooltip is bigger than it actually is to resize and fit out custom nodes.
        // Game uses a default value of -14, we add another -4 to squish our nodes together a little.
        Marshal.WriteInt16(args.Addon.Address, 0x390, (short)(this.heightChange - 14 - 4));
    }

    private unsafe void OnItemDetailPostRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonRequestedUpdateArgs requestedUpdateArgs)
        {
            return;
        }

        if (IsItemDetailRebuildingLayout(requestedUpdateArgs))
        {
            return; // This update isn't part of a tooltip rebuild, we should ignore it.
        }

        var addonPointer = (AtkUnitBase*)args.Addon.Address;

        var yPosition = 0.0f;

        // If there are no listeners defined, we still need to populate the dictionary key.
        this.newTooltipNodes.TryAdd(TooltipType.Item, []);

        // todo: Also do the dalamud config to enable/disable ? And Ordering.
        foreach (var tooltipEntry in this.newTooltipNodes[TooltipType.Item])
        {
            var tooltipNode = tooltipEntry.TooltipNode;

            tooltipNode.Y = (addonPointer->WindowNode->Height + yPosition) - 4.0f;
            yPosition += tooltipNode.Height - 4.0f;

            tooltipNode.AttachNode(addonPointer->WindowNode, NodePosition.AfterTarget);
            this.attachedNodes.Add(tooltipNode);
        }

        this.newTooltipNodes[TooltipType.Item].Clear();
    }
}

/// <summary>
/// Plugin-scoped version of a Tooltip service.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<ITooltip>]
#pragma warning restore SA1015
internal class TooltipPluginScoped : IInternalDisposableService, ITooltip
{
    [ServiceManager.ServiceDependency]
    private readonly Tooltip tooltipService = Service<Tooltip>.Get();
    private readonly LocalPlugin localPlugin;
    private readonly List<TooltipEventListener> listeners = [];
    private readonly Lock listenerLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipPluginScoped"/> class.
    /// </summary>
    /// <param name="localPlugin">Plugin instance requesting this service.</param>
    internal TooltipPluginScoped(LocalPlugin localPlugin)
    {
        this.localPlugin = localPlugin;
    }

    /// <inheritdoc/>
    public void DisposeService()
    {
        foreach (var typeGroup in this.listeners.GroupBy(listener => listener.TooltipType))
        {
            if (this.tooltipService.EventListeners.TryGetValue(typeGroup.Key, out var registeredListeners))
            {
                foreach (var listener in typeGroup)
                {
                    registeredListeners.Remove(listener);
                }
            }
        }
    }

    /// <inheritdoc/>
    public void RegisterListener(TooltipType type, ITooltip.TooltipChanged tooltipChangedDelegate)
    {
        using var scope = this.listenerLock.EnterScope();

        var newListener = new TooltipEventListener
        {
            SourcePluginName = this.localPlugin.Name,
            ListenerDelegate = tooltipChangedDelegate,
            TooltipType = type,
        };

        this.listeners.Add(newListener);
        this.tooltipService.EventListeners.TryAdd(type, []);
        this.tooltipService.EventListeners[type].Add(newListener);
    }

    /// <inheritdoc/>
    public void UnregisterListener(TooltipType type, ITooltip.TooltipChanged tooltipChangedDelegate)
    {
        using var scope = this.listenerLock.EnterScope();

        var toRemove =
            this.listeners
                .Where(listener => listener.ListenerDelegate == tooltipChangedDelegate && listener.TooltipType == type)
                .ToList();

        foreach (var listener in toRemove)
        {
            this.listeners.Remove(listener);
        }

        if (this.tooltipService.EventListeners.TryGetValue(type, out var registeredListeners))
        {
            foreach (var listener in toRemove)
            {
                registeredListeners.Remove(listener);
            }
        }
    }

    /// <inheritdoc/>
    public void UnregisterListener(TooltipType type)
    {
        using var scope = this.listenerLock.EnterScope();

        var toRemove =
            this.listeners
                .Where(listener => listener.TooltipType == type)
                .ToList();

        foreach (var listener in toRemove)
        {
            this.listeners.Remove(listener);
        }

        if (this.tooltipService.EventListeners.TryGetValue(type, out var registeredListeners))
        {
            foreach (var listener in toRemove)
            {
                registeredListeners.Remove(listener);
            }
        }
    }

    /// <inheritdoc/>
    public void UnregisterListener(ITooltip.TooltipChanged tooltipChangedDelegate)
    {
        using var scope = this.listenerLock.EnterScope();

        var toRemove =
            this.listeners
                .Where(listener => listener.ListenerDelegate == tooltipChangedDelegate)
                .ToList();

        foreach (var listener in toRemove)
        {
            this.listeners.Remove(listener);
        }

        foreach (var typeGroup in toRemove.GroupBy(listener => listener.TooltipType))
        {
            if (this.tooltipService.EventListeners.TryGetValue(typeGroup.Key, out var registeredListeners))
            {
                foreach (var listener in typeGroup)
                {
                    registeredListeners.Remove(listener);
                }
            }
        }
    }
}

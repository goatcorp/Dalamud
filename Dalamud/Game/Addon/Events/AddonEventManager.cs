using System.Collections.Concurrent;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Events;

/// <summary>
/// Service provider for addon event management.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal unsafe class AddonEventManager : IInternalDisposableService
{
    /// <summary>
    /// PluginName for Dalamud Internal use.
    /// </summary>
    public static readonly Guid DalamudInternalKey = Guid.NewGuid();

    private static readonly ModuleLog Log = new("AddonEventManager");

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycle = Service<AddonLifecycle>.Get();

    private readonly AddonLifecycleEventListener finalizeEventListener;

    private readonly Hook<AtkUnitManager.Delegates.UpdateCursor> onUpdateCursor;

    private readonly ConcurrentDictionary<Guid, PluginEventController> pluginEventControllers;

    private AtkCursor.CursorType? cursorOverride;

    [ServiceManager.ServiceConstructor]
    private AddonEventManager()
    {
        this.pluginEventControllers = new ConcurrentDictionary<Guid, PluginEventController>();
        this.pluginEventControllers.TryAdd(DalamudInternalKey, new PluginEventController());

        this.cursorOverride = null;

        this.onUpdateCursor = Hook<AtkUnitManager.Delegates.UpdateCursor>.FromAddress(AtkUnitManager.Addresses.UpdateCursor.Value, this.UpdateCursorDetour);

        this.finalizeEventListener = new AddonLifecycleEventListener(AddonEvent.PreFinalize, string.Empty, this.OnAddonFinalize);
        this.addonLifecycle.RegisterListener(this.finalizeEventListener);

        this.onUpdateCursor.Enable();
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.onUpdateCursor.Dispose();

        foreach (var (_, pluginEventController) in this.pluginEventControllers)
        {
            pluginEventController.Dispose();
        }

        this.addonLifecycle.UnregisterListener(this.finalizeEventListener);
    }

    /// <summary>
    /// Registers an event handler for the specified addon, node, and type.
    /// </summary>
    /// <param name="pluginId">Unique ID for this plugin.</param>
    /// <param name="atkUnitBase">The parent addon for this event.</param>
    /// <param name="atkResNode">The node that will trigger this event.</param>
    /// <param name="eventType">The event type for this event.</param>
    /// <param name="eventDelegate">The delegate to call when event is triggered.</param>
    /// <returns>IAddonEventHandle used to remove the event.</returns>
    internal IAddonEventHandle? AddEvent(Guid pluginId, nint atkUnitBase, nint atkResNode, AddonEventType eventType, IAddonEventManager.AddonEventDelegate eventDelegate)
    {
        if (this.pluginEventControllers.TryGetValue(pluginId, out var controller))
        {
            return controller.AddEvent(atkUnitBase, atkResNode, eventType, eventDelegate);
        }
        else
        {
            Log.Verbose($"Unable to locate controller for {pluginId}. No event was added.");
        }

        return null;
    }

    /// <summary>
    /// Unregisters an event handler with the specified event id and event type.
    /// </summary>
    /// <param name="pluginId">Unique ID for this plugin.</param>
    /// <param name="eventHandle">The Unique Id for this event.</param>
    internal void RemoveEvent(Guid pluginId, IAddonEventHandle eventHandle)
    {
        if (this.pluginEventControllers.TryGetValue(pluginId, out var controller))
        {
            controller.RemoveEvent(eventHandle);
        }
        else
        {
            Log.Verbose($"Unable to locate controller for {pluginId}. No event was removed.");
        }
    }

    /// <summary>
    /// Force the game cursor to be the specified cursor.
    /// </summary>
    /// <param name="cursor">Which cursor to use.</param>
    internal void SetCursor(AddonCursorType cursor) => this.cursorOverride = (AtkCursor.CursorType)cursor;

    /// <summary>
    /// Un-forces the game cursor.
    /// </summary>
    internal void ResetCursor() => this.cursorOverride = null;

    /// <summary>
    /// Adds a new managed event controller if one doesn't already exist for this pluginId.
    /// </summary>
    /// <param name="pluginId">Unique ID for this plugin.</param>
    internal void AddPluginEventController(Guid pluginId)
    {
        this.pluginEventControllers.GetOrAdd(
            pluginId,
            key =>
            {
                Log.Verbose($"Creating new PluginEventController for: {key}");
                return new PluginEventController();
            });
    }

    /// <summary>
    /// Removes an existing managed event controller for the specified plugin.
    /// </summary>
    /// <param name="pluginId">Unique ID for this plugin.</param>
    internal void RemovePluginEventController(Guid pluginId)
    {
        if (this.pluginEventControllers.TryRemove(pluginId, out var controller))
        {
            Log.Verbose($"Removing PluginEventController for: {pluginId}");
            controller.Dispose();
        }
    }

    /// <summary>
    /// When an addon finalizes, check it for any registered events, and unregister them.
    /// </summary>
    /// <param name="eventType">Event type that triggered this call.</param>
    /// <param name="addonInfo">Addon that triggered this call.</param>
    private void OnAddonFinalize(AddonEvent eventType, AddonArgs addonInfo)
    {
        // It shouldn't be possible for this event to be anything other than PreFinalize.
        if (eventType != AddonEvent.PreFinalize) return;

        foreach (var pluginList in this.pluginEventControllers)
        {
            pluginList.Value.RemoveForAddon(addonInfo.AddonName);
        }
    }

    private void UpdateCursorDetour(AtkUnitManager* thisPtr)
    {
        try
        {
            var atkStage = AtkStage.Instance();

            if (this.cursorOverride is not null && atkStage is not null)
            {
                ref var atkCursor = ref atkStage->AtkCursor;

                if (atkCursor.Type != this.cursorOverride)
                {
                    atkCursor.SetCursorType((AtkCursor.CursorType)this.cursorOverride, 1);
                }

                return;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in UpdateCursorDetour.");
        }

        this.onUpdateCursor!.Original(thisPtr);
    }
}

/// <summary>
/// Plugin-scoped version of a AddonEventManager service.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IAddonEventManager>]
#pragma warning restore SA1015
internal class AddonEventManagerPluginScoped : IInternalDisposableService, IAddonEventManager
{
    [ServiceManager.ServiceDependency]
    private readonly AddonEventManager eventManagerService = Service<AddonEventManager>.Get();

    private readonly LocalPlugin plugin;

    private bool isForcingCursor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonEventManagerPluginScoped"/> class.
    /// </summary>
    /// <param name="plugin">Plugin info for the plugin that requested this service.</param>
    public AddonEventManagerPluginScoped(LocalPlugin plugin)
    {
        this.plugin = plugin;

        this.eventManagerService.AddPluginEventController(plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        // if multiple plugins force cursors and dispose without un-forcing them then all forces will be cleared.
        if (this.isForcingCursor)
        {
            this.eventManagerService.ResetCursor();
        }

        Service<Framework>.Get().RunOnFrameworkThread(() =>
        {
            this.eventManagerService.RemovePluginEventController(this.plugin.EffectiveWorkingPluginId);
        }).Wait();
    }

    /// <inheritdoc/>
    public IAddonEventHandle? AddEvent(nint atkUnitBase, nint atkResNode, AddonEventType eventType, IAddonEventManager.AddonEventDelegate eventDelegate)
        => this.eventManagerService.AddEvent(this.plugin.EffectiveWorkingPluginId, atkUnitBase, atkResNode, eventType, eventDelegate);

    /// <inheritdoc/>
    public void RemoveEvent(IAddonEventHandle eventHandle)
        => this.eventManagerService.RemoveEvent(this.plugin.EffectiveWorkingPluginId, eventHandle);

    /// <inheritdoc/>
    public void SetCursor(AddonCursorType cursor)
    {
        this.isForcingCursor = true;

        this.eventManagerService.SetCursor(cursor);
    }

    /// <inheritdoc/>
    public void ResetCursor()
    {
        this.isForcingCursor = false;

        this.eventManagerService.ResetCursor();
    }
}

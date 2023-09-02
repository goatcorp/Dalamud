using System;
using System.Collections.Generic;

using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.AddonEventManager;

/// <summary>
/// Service provider for addon event management.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal unsafe class AddonEventManager : IDisposable, IServiceType
{
    // The starting value for param key ranges.
    // ascii `DD` 0x4444 chosen for the key start, this just has to be larger than anything vanilla makes.
    private const uint ParamKeyStart = 0x44440000;

    // The range each plugin is allowed to use.
    // 65,536 per plugin should be reasonable.
    private const uint ParamKeyPluginRange = 0x10000;
    
    // The maximum range allowed to be given to a plugin.
    // 20,560 maximum plugins should be reasonable.
    // 202,113,024 maximum event handlers should be reasonable.
    private const uint ParamKeyMax = 0x50500000;
    
    private static readonly ModuleLog Log = new("AddonEventManager");
    private readonly AddonEventManagerAddressResolver address;
    private readonly Hook<GlobalEventHandlerDetour> onGlobalEventHook;
    private readonly Dictionary<uint, IAddonEventManager.AddonEventHandler> eventHandlers;

    private uint currentPluginParamStart = ParamKeyStart;
    
    [ServiceManager.ServiceConstructor]
    private AddonEventManager(SigScanner sigScanner)
    {
        this.address = new AddonEventManagerAddressResolver();
        this.address.Setup(sigScanner);

        this.eventHandlers = new Dictionary<uint, IAddonEventManager.AddonEventHandler>();

        this.onGlobalEventHook = Hook<GlobalEventHandlerDetour>.FromAddress(this.address.GlobalEventHandler, this.GlobalEventHandler);
    }

    private delegate nint GlobalEventHandlerDetour(AtkUnitBase* atkUnitBase, AtkEventType eventType, uint eventParam, AtkResNode** eventData, nint unknown);
    
    /// <inheritdoc/>
    public void Dispose()
    {
        this.onGlobalEventHook.Dispose();
    }
    
    /// <summary>
    /// Get the start value for a new plugin register.
    /// </summary>
    /// <returns>A unique starting range for event handlers.</returns>
    /// <exception cref="Exception">Throws when attempting to register too many event handlers.</exception>
    public uint GetPluginParamStart()
    {
        if (this.currentPluginParamStart >= ParamKeyMax)
        {
            throw new Exception("Maximum number of event handlers reached.");
        }
        
        var result = this.currentPluginParamStart;

        this.currentPluginParamStart += ParamKeyPluginRange;
        return result;
    }

    /// <summary>
    /// Adds a event handler to be triggered when the specified id is received.
    /// </summary>
    /// <param name="eventId">Unique id for this event handler.</param>
    /// <param name="handler">The event handler to be called.</param>
    public void AddEvent(uint eventId, IAddonEventManager.AddonEventHandler handler) => this.eventHandlers.Add(eventId, handler);

    /// <summary>
    /// Removes the event handler with the specified id.
    /// </summary>
    /// <param name="eventId">Event id to unregister.</param>
    public void RemoveEvent(uint eventId) => this.eventHandlers.Remove(eventId);
    
    [ServiceManager.CallWhenServicesReady]
    private void ContinueConstruction()
    {
        this.onGlobalEventHook.Enable();
    }

    private nint GlobalEventHandler(AtkUnitBase* atkUnitBase, AtkEventType eventType, uint eventParam, AtkResNode** eventData, nint unknown)
    {
        try
        {
            if (this.eventHandlers.TryGetValue(eventParam, out var handler) && eventData is not null)
            {
                try
                {
                    handler?.Invoke((AddonEventType)eventType, (nint)atkUnitBase, (nint)eventData[0]);
                    return nint.Zero;
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Exception in GlobalEventHandler custom event invoke.");
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in GlobalEventHandler attempting to retrieve event handler.");
        }

        return this.onGlobalEventHook!.Original(atkUnitBase, eventType, eventParam, eventData, unknown);
    }
}

/// <summary>
/// Plugin-scoped version of a AddonEventManager service.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IAddonEventManager>]
#pragma warning restore SA1015
internal unsafe class AddonEventManagerPluginScoped : IDisposable, IServiceType, IAddonEventManager
{
    private static readonly ModuleLog Log = new("AddonEventManager");
    
    [ServiceManager.ServiceDependency]
    private readonly AddonEventManager baseEventManager = Service<AddonEventManager>.Get();
    
    private readonly uint paramKeyStartRange;
    private readonly List<uint> activeParamKeys;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonEventManagerPluginScoped"/> class.
    /// </summary>
    public AddonEventManagerPluginScoped()
    {
        this.paramKeyStartRange = this.baseEventManager.GetPluginParamStart();
        this.activeParamKeys = new List<uint>();
    }
    
    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var activeKey in this.activeParamKeys)
        {
            this.baseEventManager.RemoveEvent(activeKey);
        }
    }
    
    /// <inheritdoc/>
    public void AddEvent(uint eventId, IntPtr atkUnitBase, IntPtr atkResNode, AddonEventType eventType, IAddonEventManager.AddonEventHandler eventHandler)
    {
        if (eventId < 0x10000)
        {
            var type = (AtkEventType)eventType;
            var node = (AtkResNode*)atkResNode;
            var eventListener = (AtkEventListener*)atkUnitBase;
            var uniqueId = eventId + this.paramKeyStartRange;

            if (!this.activeParamKeys.Contains(uniqueId))
            {
                node->AddEvent(type, uniqueId, eventListener, node, true);
                this.baseEventManager.AddEvent(uniqueId, eventHandler);
        
                this.activeParamKeys.Add(uniqueId);
            }
            else
            {
                Log.Warning($"Attempted to register already registered eventId: {eventId}");
            }
        }
        else
        {
            Log.Warning($"Attempted to register eventId out of range: {eventId}\nMaximum value: {0x10000}");
        }
    }
    
    /// <inheritdoc/>
    public void RemoveEvent(uint eventId, IntPtr atkUnitBase, IntPtr atkResNode, AddonEventType eventType)
    {
        var type = (AtkEventType)eventType;
        var node = (AtkResNode*)atkResNode;
        var eventListener = (AtkEventListener*)atkUnitBase;
        var uniqueId = eventId + this.paramKeyStartRange;

        if (this.activeParamKeys.Contains(uniqueId))
        {
            node->RemoveEvent(type, uniqueId, eventListener, true);
            this.baseEventManager.RemoveEvent(uniqueId);

            this.activeParamKeys.Remove(uniqueId);
        }
        else
        {
            Log.Warning($"Attempted to unregister already unregistered eventId: {eventId}");
        }
    }
}

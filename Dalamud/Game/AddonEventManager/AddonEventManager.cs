using System;
using System.Collections.Generic;

using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
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
    private readonly Hook<GlobalEventHandlerDelegate> onGlobalEventHook;
    private readonly Hook<UpdateCursorDelegate> onUpdateCursor;
    private readonly Dictionary<uint, IAddonEventManager.AddonEventHandler> eventHandlers;

    private AddonCursorType currentCursor;
    private bool cursorSet;
    
    private uint currentPluginParamStart = ParamKeyStart;
    
    [ServiceManager.ServiceConstructor]
    private AddonEventManager(SigScanner sigScanner)
    {
        this.address = new AddonEventManagerAddressResolver();
        this.address.Setup(sigScanner);

        this.eventHandlers = new Dictionary<uint, IAddonEventManager.AddonEventHandler>();
        this.currentCursor = AddonCursorType.Arrow;

        this.onGlobalEventHook = Hook<GlobalEventHandlerDelegate>.FromAddress(this.address.GlobalEventHandler, this.GlobalEventHandler);
        this.onUpdateCursor = Hook<UpdateCursorDelegate>.FromAddress(this.address.UpdateCursor, this.UpdateCursorDetour);
    }

    private delegate nint GlobalEventHandlerDelegate(AtkUnitBase* atkUnitBase, AtkEventType eventType, uint eventParam, AtkResNode** eventData, nint unknown);

    private delegate nint UpdateCursorDelegate(RaptureAtkModule* module);
    
    /// <inheritdoc/>
    public void Dispose()
    {
        this.onGlobalEventHook.Dispose();
        this.onUpdateCursor.Dispose();
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

    /// <summary>
    /// Sets the game cursor.
    /// </summary>
    /// <param name="cursor">Cursor type to set.</param>
    public void SetCursor(AddonCursorType cursor)
    {
        this.currentCursor = cursor;
        this.cursorSet = true;
    }

    /// <summary>
    /// Resets and un-forces custom cursor.
    /// </summary>
    public void ResetCursor()
    {
        this.currentCursor = AddonCursorType.Arrow;
        this.cursorSet = false;
    }
    
    [ServiceManager.CallWhenServicesReady]
    private void ContinueConstruction()
    {
        this.onGlobalEventHook.Enable();
        this.onUpdateCursor.Enable();
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

    private nint UpdateCursorDetour(RaptureAtkModule* module)
    {
        try
        {
            var atkStage = AtkStage.GetSingleton();
            
            if (this.cursorSet && atkStage is not null)
            {
                var cursor = (AddonCursorType)atkStage->AtkCursor.Type;
                if (cursor != this.currentCursor) 
                {
                    AtkStage.GetSingleton()->AtkCursor.SetCursorType((AtkCursor.CursorType)this.currentCursor, 1);
                }
                
                return nint.Zero;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception in UpdateCursorDetour.");
        }

        return this.onUpdateCursor!.Original(module);
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
    private bool isForcingCursor;
    
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

        // if multiple plugins force cursors and dispose without un-forcing them then all forces will be cleared.
        if (this.isForcingCursor)
        {
            this.baseEventManager.ResetCursor();
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
    
    /// <inheritdoc/>
    public void SetCursor(AddonCursorType cursor)
    {
        this.isForcingCursor = true;
        
        this.baseEventManager.SetCursor(cursor);
    }
    
    /// <inheritdoc/>
    public void ResetCursor()
    {
        this.isForcingCursor = false;
        
        this.baseEventManager.ResetCursor();
    }
}

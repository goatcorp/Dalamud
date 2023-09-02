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
    // Reserve the first 0x10_000 for dalamud internal use.
    private const uint ParamKeyStart = 0x0010_0000;

    // The range each plugin is allowed to use.
    // 1,048,576 per plugin should be reasonable.
    private const uint ParamKeyPluginRange = 0x10_0000;
    
    // The maximum range allowed to be given to a plugin.
    // 1,048,576 maximum plugins should be reasonable.
    private const uint ParamKeyMax = 0xFFF0_0000;
    
    private static readonly ModuleLog Log = new("AddonEventManager");
    
    private readonly AddonEventManagerAddressResolver address;
    private readonly Hook<UpdateCursorDelegate> onUpdateCursor;
    private readonly Dictionary<uint, IAddonEventManager.AddonEventHandler> eventHandlers;
    private readonly AddonEventListener eventListener;

    private AddonCursorType currentCursor;
    private bool cursorSet;
    
    private uint currentPluginParamStart = ParamKeyStart;
    
    [ServiceManager.ServiceConstructor]
    private AddonEventManager(SigScanner sigScanner)
    {
        this.address = new AddonEventManagerAddressResolver();
        this.address.Setup(sigScanner);

        this.eventListener = new AddonEventListener(this.OnCustomEvent);
        
        this.eventHandlers = new Dictionary<uint, IAddonEventManager.AddonEventHandler>();
        this.currentCursor = AddonCursorType.Arrow;

        this.onUpdateCursor = Hook<UpdateCursorDelegate>.FromAddress(this.address.UpdateCursor, this.UpdateCursorDetour);
    }

    private delegate nint UpdateCursorDelegate(RaptureAtkModule* module);
    
    /// <inheritdoc/>
    public void Dispose()
    {
        this.eventListener.Dispose();
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
    /// Attaches an event to a node.
    /// </summary>
    /// <param name="addon">Addon that contains the node.</param>
    /// <param name="node">The node that will trigger the event.</param>
    /// <param name="eventType">The event type to trigger on.</param>
    /// <param name="param">The unique id for this event.</param>
    /// <param name="handler">The event handler to be called.</param>
    public void AddEvent(AtkUnitBase* addon, AtkResNode* node, AtkEventType eventType, uint param, IAddonEventManager.AddonEventHandler handler)
    {
        this.eventListener.RegisterEvent(addon, node, eventType, param);
        this.eventHandlers.TryAdd(param, handler);
    }

    /// <summary>
    /// Detaches an event from a node.
    /// </summary>
    /// <param name="node">The node to remove the event from.</param>
    /// <param name="eventType">The event type to remove.</param>
    /// <param name="param">The unique id of the event to remove.</param>
    public void RemoveEvent(AtkResNode* node, AtkEventType eventType, uint param)
    {
        this.eventListener.UnregisterEvent(node, eventType, param);
        this.eventHandlers.Remove(param);
    }

    /// <summary>
    /// Removes a delegate from the managed event handlers.
    /// </summary>
    /// <param name="param">Unique id of the delegate to remove.</param>
    public void RemoveHandler(uint param)
    {
        this.eventHandlers.Remove(param);
    }

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
        this.onUpdateCursor.Enable();
    }

    private void OnCustomEvent(AtkEventListener* self, AtkEventType eventType, uint eventParam, AtkEvent* eventData, nint unknown)
    {
        if (this.eventHandlers.TryGetValue(eventParam, out var handler) && eventData is not null)
        {
            try
            {
                // We passed the AtkUnitBase into the EventData.Node field from our AddonEventHandler
                handler?.Invoke((AddonEventType)eventType, (nint)eventData->Node, (nint)eventData->Target);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Exception in OnCustomEvent custom event invoke.");
            }
        }
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
            this.baseEventManager.RemoveHandler(activeKey);
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
        if (eventId < 0x10_000)
        {
            var type = (AtkEventType)eventType;
            var node = (AtkResNode*)atkResNode;
            var addon = (AtkUnitBase*)atkUnitBase;
            var uniqueId = eventId + this.paramKeyStartRange;

            if (!this.activeParamKeys.Contains(uniqueId))
            {
                this.baseEventManager.AddEvent(addon, node, type, uniqueId, eventHandler);
                this.activeParamKeys.Add(uniqueId);
            }
            else
            {
                Log.Warning($"Attempted to register already registered eventId: {eventId}");
            }
        }
        else
        {
            Log.Warning($"Attempted to register eventId out of range: {eventId}\nMaximum value: {0x10_000}");
        }
    }
    
    /// <inheritdoc/>
    public void RemoveEvent(uint eventId, IntPtr atkUnitBase, IntPtr atkResNode, AddonEventType eventType)
    {
        var type = (AtkEventType)eventType;
        var node = (AtkResNode*)atkResNode;
        var uniqueId = eventId + this.paramKeyStartRange;

        if (this.activeParamKeys.Contains(uniqueId))
        {
            this.baseEventManager.RemoveEvent(node, type, uniqueId);
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

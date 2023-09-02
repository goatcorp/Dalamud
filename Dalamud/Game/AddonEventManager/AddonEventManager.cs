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
internal unsafe class AddonEventManager : IDisposable, IServiceType, IAddonEventManager
{
    private static readonly ModuleLog Log = new("AddonEventManager");
    
    private readonly AddonEventManagerAddressResolver address;
    private readonly Hook<UpdateCursorDelegate> onUpdateCursor;

    private readonly AddonEventListener eventListener;
    private readonly Dictionary<uint, IAddonEventManager.AddonEventHandler> eventHandlers;
    
    private AddonCursorType? cursorOverride;
    
    [ServiceManager.ServiceConstructor]
    private AddonEventManager(SigScanner sigScanner)
    {
        this.address = new AddonEventManagerAddressResolver();
        this.address.Setup(sigScanner);

        this.eventHandlers = new Dictionary<uint, IAddonEventManager.AddonEventHandler>();
        this.eventListener = new AddonEventListener(this.DalamudAddonEventHandler);
        
        this.cursorOverride = null;

        this.onUpdateCursor = Hook<UpdateCursorDelegate>.FromAddress(this.address.UpdateCursor, this.UpdateCursorDetour);
    }

    private delegate nint UpdateCursorDelegate(RaptureAtkModule* module);

    /// <inheritdoc/>
    public void AddEvent(uint eventId, IntPtr atkUnitBase, IntPtr atkResNode, AddonEventType eventType, IAddonEventManager.AddonEventHandler eventHandler)
    {
        if (!this.eventHandlers.ContainsKey(eventId))
        {
            var type = (AtkEventType)eventType;
            var node = (AtkResNode*)atkResNode;
            var addon = (AtkUnitBase*)atkUnitBase;

            this.eventHandlers.Add(eventId, eventHandler);
            this.eventListener.RegisterEvent(addon, node, type, eventId);
        }
        else
        {
            Log.Warning($"Attempted to register already registered eventId: {eventId}");
        }
    }
    
    /// <inheritdoc/>
    public void RemoveEvent(uint eventId, IntPtr atkResNode, AddonEventType eventType)
    {
        if (this.eventHandlers.ContainsKey(eventId))
        {
            var type = (AtkEventType)eventType;
            var node = (AtkResNode*)atkResNode;
            
            this.eventListener.UnregisterEvent(node, type, eventId);
            this.eventHandlers.Remove(eventId);
        }
        else
        {
            Log.Warning($"Attempted to unregister already unregistered eventId: {eventId}");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.onUpdateCursor.Dispose();
        this.eventListener.Dispose();
        this.eventHandlers.Clear();
    }
    
    /// <inheritdoc/>
    public void SetCursor(AddonCursorType cursor) => this.cursorOverride = cursor;

    /// <inheritdoc/>
    public void ResetCursor() => this.cursorOverride = null;

    [ServiceManager.CallWhenServicesReady]
    private void ContinueConstruction()
    {
        this.onUpdateCursor.Enable();
    }

    private nint UpdateCursorDetour(RaptureAtkModule* module)
    {
        try
        {
            var atkStage = AtkStage.GetSingleton();
            
            if (this.cursorOverride is not null && atkStage is not null)
            {
                var cursor = (AddonCursorType)atkStage->AtkCursor.Type;
                if (cursor != this.cursorOverride) 
                {
                    AtkStage.GetSingleton()->AtkCursor.SetCursorType((AtkCursor.CursorType)this.cursorOverride, 1);
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
    
    private void DalamudAddonEventHandler(AtkEventListener* self, AtkEventType eventType, uint eventParam, AtkEvent* eventData, IntPtr unknown)
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
                Log.Error(exception, "Exception in DalamudAddonEventHandler custom event invoke.");
            }
        } 
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

    private readonly AddonEventListener eventListener;
    private readonly Dictionary<uint, IAddonEventManager.AddonEventHandler> eventHandlers;

    private bool isForcingCursor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonEventManagerPluginScoped"/> class.
    /// </summary>
    public AddonEventManagerPluginScoped()
    {
        this.eventHandlers = new Dictionary<uint, IAddonEventManager.AddonEventHandler>();
        this.eventListener = new AddonEventListener(this.PluginAddonEventHandler);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // if multiple plugins force cursors and dispose without un-forcing them then all forces will be cleared.
        if (this.isForcingCursor)
        {
            this.baseEventManager.ResetCursor();
        }
        
        this.eventListener.Dispose();
        this.eventHandlers.Clear();
    }
    
    /// <inheritdoc/>
    public void AddEvent(uint eventId, IntPtr atkUnitBase, IntPtr atkResNode, AddonEventType eventType, IAddonEventManager.AddonEventHandler eventHandler)
    {
        if (!this.eventHandlers.ContainsKey(eventId))
        {
            var type = (AtkEventType)eventType;
            var node = (AtkResNode*)atkResNode;
            var addon = (AtkUnitBase*)atkUnitBase;

            this.eventHandlers.Add(eventId, eventHandler);
            this.eventListener.RegisterEvent(addon, node, type, eventId);
        }
        else
        {
            Log.Warning($"Attempted to register already registered eventId: {eventId}");
        }
    }
    
    /// <inheritdoc/>
    public void RemoveEvent(uint eventId, IntPtr atkResNode, AddonEventType eventType)
    {
        if (this.eventHandlers.ContainsKey(eventId))
        {
            var type = (AtkEventType)eventType;
            var node = (AtkResNode*)atkResNode;
            
            this.eventListener.UnregisterEvent(node, type, eventId);
            this.eventHandlers.Remove(eventId);
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
    
    private void PluginAddonEventHandler(AtkEventListener* self, AtkEventType eventType, uint eventParam, AtkEvent* eventData, IntPtr unknown)
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
                Log.Error(exception, "Exception in PluginAddonEventHandler custom event invoke.");
            }
        }
    }
}

using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Events;

/// <summary>
/// This class represents a registered event that a plugin registers with a native ui node.
/// Contains all necessary information to track and clean up events automatically.
/// </summary>
internal unsafe class AddonEventEntry
{
    /// <summary>
    /// Name of an invalid addon.
    /// </summary>
    public const string InvalidAddonName = "NullAddon";
    
    private string? addonName;
    
    /// <summary>
    /// Gets the pointer to the addons AtkUnitBase.
    /// </summary>
    required public nint Addon { get; init; }
    
    /// <summary>
    /// Gets the name of the addon this args referrers to.
    /// </summary>
    public string AddonName => this.Addon == nint.Zero ? InvalidAddonName : this.addonName ??= MemoryHelper.ReadString((nint)((AtkUnitBase*)this.Addon)->Name, 0x20);

    /// <summary>
    /// Gets the pointer to the event source.
    /// </summary>
    required public nint Node { get; init; }

    /// <summary>
    /// Gets the handler that gets called when this event is triggered.
    /// </summary>
    required public IAddonEventManager.AddonEventHandler Handler { get; init; }
    
    /// <summary>
    /// Gets the unique id for this event.
    /// </summary>
    required public uint ParamKey { get; init; }
    
    /// <summary>
    /// Gets the event type for this event.
    /// </summary>
    required public AddonEventType EventType { get; init; }
    
    /// <summary>
    /// Gets the event handle for this event.
    /// </summary>
    required internal IAddonEventHandle Handle { get; init; }

    /// <summary>
    /// Gets the formatted log string for this AddonEventEntry.
    /// </summary>
    internal string LogString => $"ParamKey: {this.ParamKey}, Addon: {this.AddonName}, Event: {this.EventType}, GUID: {this.Handle.EventGuid}";
}

using System;

using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class provides events for in-game addon lifecycles.
/// </summary>
public interface IAddonLifecycle
{
    /// <summary>
    /// Event that fires before an addon is being setup.
    /// </summary>
    public event Action<AddonArgs> AddonPreSetup;
    
    /// <summary>
    /// Event that fires after an addon is done being setup.
    /// </summary>
    public event Action<AddonArgs> AddonPostSetup;
    
    /// <summary>
    /// Event that fires before an addon is being finalized.
    /// </summary>
    public event Action<AddonArgs> AddonPreFinalize;
    
    /// <summary>
    /// Addon argument data for use in event subscribers.
    /// </summary>
    public unsafe class AddonArgs
    {
        private string? addonName;
        
        /// <summary>
        /// Gets the name of the addon this args referrers to.
        /// </summary>
        public string AddonName => this.Addon == nint.Zero ? "NullAddon" : this.addonName ??= MemoryHelper.ReadString((nint)((AtkUnitBase*)this.Addon)->Name, 0x20);

        /// <summary>
        /// Gets the pointer to the addons AtkUnitBase.
        /// </summary>
        required public nint Addon { get; init; }
    }
}

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
    /// Event that fires after an addon is done being finalized.
    /// </summary>
    public event Action<AddonArgs> AddonPostFinalize;
    
    /// <summary>
    /// Addon argument data for use in event subscribers.
    /// </summary>
    public unsafe class AddonArgs
    {
        private string? addonName;
        
        /// <summary>
        /// Gets the name of the addon this args referrers to.
        /// </summary>
        public string AddonName => this.addonName ??= MemoryHelper.ReadString(new nint(Addon->Name), 0x20).Split('\0')[0];

        /// <summary>
        /// Gets the pointer to the addons AtkUnitBase.
        /// </summary>
        required public AtkUnitBase* Addon { get; init; }
    }
}

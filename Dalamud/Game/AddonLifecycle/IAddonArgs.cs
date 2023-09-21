using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon;

/// <summary>
/// Interface representing the argument data for AddonLifecycle events.
/// </summary>
public unsafe interface IAddonArgs
{
    /// <summary>
    /// Gets the name of the addon this args referrers to.
    /// </summary>
    string AddonName => this.Addon == nint.Zero ? "NullAddon" : MemoryHelper.ReadString((nint)((AtkUnitBase*)this.Addon)->Name, 0x20);
    
    /// <summary>
    /// Gets the pointer to the addons AtkUnitBase.
    /// </summary>
    nint Addon { get; init; }
    
    /// <summary>
    /// Gets the type of these args.
    /// </summary>
    AddonArgsType Type { get; }
}

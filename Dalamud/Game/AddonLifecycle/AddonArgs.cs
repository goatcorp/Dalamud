using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.AddonLifecycle;

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

using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Base class for AddonLifecycle AddonArgTypes.
/// </summary>
public abstract unsafe class AddonArgs
{
    /// <summary>
    /// Constant string representing the name of an addon that is invalid.
    /// </summary>
    public const string InvalidAddon = "NullAddon";
    
    private string? addonName;

    /// <summary>
    /// Gets the name of the addon this args referrers to.
    /// </summary>
    public string AddonName => this.GetAddonName();
    
    /// <summary>
    /// Gets the pointer to the addons AtkUnitBase.
    /// </summary>
    public nint Addon { get; init; }
    
    /// <summary>
    /// Gets the type of these args.
    /// </summary>
    public abstract AddonArgsType Type { get; }

    /// <summary>
    /// Helper method for ensuring the name of the addon is valid.
    /// </summary>
    /// <returns>The name of the addon for this object. <see cref="InvalidAddon"/> when invalid.</returns>
    private string GetAddonName()
    {
        if (this.Addon == nint.Zero) return InvalidAddon;

        var addonPointer = (AtkUnitBase*)this.Addon;
        if (addonPointer->Name is null) return InvalidAddon;

        return this.addonName ??= MemoryHelper.ReadString((nint)addonPointer->Name, 0x20);
    }
}

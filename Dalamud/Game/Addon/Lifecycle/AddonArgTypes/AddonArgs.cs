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
    private IntPtr addon;

    /// <summary>
    /// Gets the name of the addon this args referrers to.
    /// </summary>
    public string AddonName => this.GetAddonName();

    /// <summary>
    /// Gets the pointer to the addons AtkUnitBase.
    /// </summary>
    public nint Addon
    {
        get => this.AddonInternal;
        init => this.AddonInternal = value;
    }

    /// <summary>
    /// Gets the type of these args.
    /// </summary>
    public abstract AddonArgsType Type { get; }

    /// <summary>
    /// Gets or sets the pointer to the addons AtkUnitBase.
    /// </summary>
    internal nint AddonInternal
    {
        get => this.addon;
        set
        {
            this.addon = value;

            // Note: always clear addonName on updating the addon being pointed.
            // Same address may point to a different addon.
            this.addonName = null;
        }
    }

    /// <summary>
    /// Checks if addon name matches the given span of char.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>Whether it is the case.</returns>
    internal bool IsAddon(ReadOnlySpan<char> name)
    {
        if (this.Addon == nint.Zero) return false;
        if (name.Length is 0 or > 0x20)
            return false;

        var addonPointer = (AtkUnitBase*)this.Addon;
        if (addonPointer->Name is null) return false;

        return MemoryHelper.EqualsZeroTerminatedString(name, (nint)addonPointer->Name, null, 0x20);
    }

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

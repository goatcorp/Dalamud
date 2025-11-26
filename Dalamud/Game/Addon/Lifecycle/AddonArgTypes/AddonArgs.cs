using Dalamud.Game.NativeWrapper;

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Base class for AddonLifecycle AddonArgTypes.
/// </summary>
public abstract class AddonArgs
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
    public AtkUnitBasePtr Addon
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets the type of these args.
    /// </summary>
    public abstract AddonArgsType Type { get; }

    /// <summary>
    /// Checks if addon name matches the given span of char.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>Whether it is the case.</returns>
    internal bool IsAddon(string name)
    {
        if (this.Addon.IsNull)
            return false;

        if (name.Length is 0 or > 32)
            return false;

        if (string.IsNullOrEmpty(this.Addon.Name))
            return false;

        return name == this.Addon.Name;
    }

    /// <summary>
    /// Clears this AddonArgs values.
    /// </summary>
    internal virtual void Clear()
    {
        this.addonName = null;
        this.Addon = 0;
    }

    /// <summary>
    /// Helper method for ensuring the name of the addon is valid.
    /// </summary>
    /// <returns>The name of the addon for this object. <see cref="InvalidAddon"/> when invalid.</returns>
    private string GetAddonName()
    {
        if (this.Addon.IsNull) return InvalidAddon;

        var name = this.Addon.Name;

        if (string.IsNullOrEmpty(name))
            return InvalidAddon;

        return this.addonName ??= name;
    }
}

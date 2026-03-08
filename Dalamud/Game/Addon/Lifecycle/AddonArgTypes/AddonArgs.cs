using Dalamud.Game.NativeWrapper;

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Base class for AddonLifecycle AddonArgTypes.
/// </summary>
public class AddonArgs
{
    /// <summary>
    /// Constant string representing the name of an addon that is invalid.
    /// </summary>
    public const string InvalidAddon = "NullAddon";

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonArgs"/> class.
    /// </summary>
    internal AddonArgs()
    {
    }

    /// <summary>
    /// Gets the name of the addon this args referrers to.
    /// </summary>
    public string AddonName { get; private set; } = InvalidAddon;

    /// <summary>
    /// Gets the pointer to the addons AtkUnitBase.
    /// </summary>
    public AtkUnitBasePtr Addon
    {
        get;
        internal set
        {
            field = value;

            if (!this.Addon.IsNull && !string.IsNullOrEmpty(value.Name))
                this.AddonName = value.Name;
        }
    }

    /// <summary>
    /// Gets the type of these args.
    /// </summary>
    public virtual AddonArgsType Type => AddonArgsType.Generic;

    /// <summary>
    /// Gets a value indicating whether original is being requested to be skipped.
    /// </summary>
    public bool PreventOriginalRequested { get; internal set; }

    /// <summary>
    /// Request that the call to original is skipped.
    /// Only valid to be called from a Pre event listener not a Post event listener.
    /// </summary>
    public void PreventOriginal() => this.PreventOriginalRequested = true;
}

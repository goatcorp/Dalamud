using Dalamud.Game.NativeWrapper;
using Dalamud.Utility;

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Base class for AddonLifecycle AddonArgTypes.
/// </summary>
public class AddonArgs
{
    /// <summary>
    /// Constant string representing the name of an addon that is invalid.
    /// </summary>
    [Obsolete("AddonName can never be null or invalid. This check is unnecessary.")]
    public const string InvalidAddonName = "NullAddon";

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonArgs"/> class.
    /// </summary>
    internal AddonArgs()
    {
    }

    /// <summary>
    /// Gets the string representing the name of an addon that is invalid.
    /// </summary>
    [Obsolete("AddonName can never be null or invalid. This check is unnecessary.")]
    public static ReadOnlySpan<byte> InvalidAddonNameSpan => "NullAddon"u8;

    /// <summary>
    /// Gets the name of the addon this args referrers to.
    /// </summary>
    public string AddonName { get; internal init; } = string.Empty;

    /// <summary>
    /// Gets the name of the addon this args referrers to.
    /// </summary>
    public ReadOnlySpan<byte> AddonNameSpan => this.Addon.NameSpan.BeforeNull();

    /// <summary>
    /// Gets the pointer to the addons AtkUnitBase.
    /// </summary>
    public AtkUnitBasePtr Addon { get; internal init; }

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

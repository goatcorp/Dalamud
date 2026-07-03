using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Addon;

/// <summary>
/// Implementation of a custom native addon (AtkUnitBase).
/// </summary>
internal partial class NativeAddon
{
    /// <summary>
    /// Gets the pointer to the addons native memory.
    /// </summary>
    /// <remarks>
    /// This class implements <see cref="op_Implicit"/> to implicitly convert to AtkUnitBase*.
    /// </remarks>
    internal unsafe AtkUnitBase* InternalAddon { get; } = null;

    /// <summary>
    /// Converts this instance to a AtkUnitBase for seamless game interop.
    /// </summary>
    /// <param name="addon">The <see cref="NativeAddon"/> instance to convert to a AtkUnitBase.</param>
    public static unsafe implicit operator AtkUnitBase*(NativeAddon addon) => addon.InternalAddon;


}

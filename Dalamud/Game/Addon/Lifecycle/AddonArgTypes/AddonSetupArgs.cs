using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Setup events.
/// </summary>
public class AddonSetupArgs : AddonArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonSetupArgs"/> class.
    /// </summary>
    internal AddonSetupArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Setup;

    /// <summary>
    /// Gets or sets the number of AtkValues.
    /// </summary>
    public uint AtkValueCount { get; set; }

    /// <summary>
    /// Gets or sets the address of the AtkValue array.
    /// </summary>
    public nint AtkValues { get; set; }

    /// <summary>
    /// Gets the AtkValues in the form of a span.
    /// </summary>
    [Obsolete("Pending removal, unsafe to use when using custom ClientStructs")]
    [Api15ToDo("Remove this")]
    public unsafe Span<AtkValue> AtkValueSpan => new(this.AtkValues.ToPointer(), (int)this.AtkValueCount);
}

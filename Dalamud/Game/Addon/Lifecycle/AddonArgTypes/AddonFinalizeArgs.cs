using Dalamud.Utility;

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for ReceiveEvent events.
/// </summary>
[Obsolete("Use AddonGenericArgs instead.")]
[Api15ToDo("Remove this")]
public class AddonFinalizeArgs : AddonArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonFinalizeArgs"/> class.
    /// </summary>
    internal AddonFinalizeArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Finalize;
}

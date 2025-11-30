using Dalamud.Utility;

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Draw events.
/// </summary>
[Obsolete("Use AddonGenericArgs instead.")]
[Api15ToDo("Remove this")]
public class AddonDrawArgs : AddonArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonDrawArgs"/> class.
    /// </summary>
    internal AddonDrawArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Draw;
}

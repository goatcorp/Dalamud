using Dalamud.Utility;

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Update events.
/// </summary>
[Obsolete("Use AddonGenericArgs instead.")]
[Api15ToDo("Remove this")]
public class AddonUpdateArgs : AddonArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonUpdateArgs"/> class.
    /// </summary>
    internal AddonUpdateArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Update;

    /// <summary>
    /// Gets or sets the time since the last update.
    /// </summary>
    internal float TimeDeltaInternal { get; set; }

    /// <summary>
    /// Gets the time since the last update.
    /// </summary>
    private float TimeDelta
    {
        get => this.TimeDeltaInternal;
        init => this.TimeDeltaInternal = value;
    }
}

namespace Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

/// <summary>
/// Addon argument data for Update events.
/// </summary>
public class AddonUpdateArgs : AddonArgs, ICloneable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddonUpdateArgs"/> class.
    /// </summary>
    [Obsolete("Not intended for public construction.", false)]
    public AddonUpdateArgs()
    {
    }

    /// <inheritdoc/>
    public override AddonArgsType Type => AddonArgsType.Update;

    /// <summary>
    /// Gets the time since the last update.
    /// </summary>
    public float TimeDelta
    {
        get => this.TimeDeltaInternal;
        init => this.TimeDeltaInternal = value;
    }

    /// <summary>
    /// Gets or sets the time since the last update.
    /// </summary>
    internal float TimeDeltaInternal { get; set; }

    /// <inheritdoc cref="ICloneable.Clone"/>
    public AddonUpdateArgs Clone() => (AddonUpdateArgs)this.MemberwiseClone();

    /// <inheritdoc cref="Clone"/>
    object ICloneable.Clone() => this.Clone();
}

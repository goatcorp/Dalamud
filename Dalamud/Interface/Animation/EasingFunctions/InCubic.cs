namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "InCubic" easing animation.
/// </summary>
public class InCubic : Easing
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InCubic"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public InCubic(TimeSpan duration)
        : base(duration)
    {
        // ignored
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var p = this.Progress;
        this.Value = p * p * p;
    }
}

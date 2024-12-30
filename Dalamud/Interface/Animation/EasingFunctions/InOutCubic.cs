namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "InOutCubic" easing animation.
/// </summary>
public class InOutCubic : Easing
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InOutCubic"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public InOutCubic(TimeSpan duration)
        : base(duration)
    {
        // ignored
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var p = this.Progress;
        this.Value = p < 0.5 ? 4 * p * p * p : 1 - (Math.Pow((-2 * p) + 2, 3) / 2);
    }
}

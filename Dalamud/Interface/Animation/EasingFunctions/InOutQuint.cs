namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "InOutQuint" easing animation.
/// </summary>
public class InOutQuint : Easing
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InOutQuint"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public InOutQuint(TimeSpan duration)
        : base(duration)
    {
        // ignored
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var p = this.Progress;
        this.Value = p < 0.5 ? 16 * p * p * p * p * p : 1 - (Math.Pow((-2 * p) + 2, 5) / 2);
    }
}

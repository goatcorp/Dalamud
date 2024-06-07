namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "InOutCirc" easing animation.
/// </summary>
public class InOutCirc : Easing
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InOutCirc"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public InOutCirc(TimeSpan duration)
        : base(duration)
    {
        // ignored
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var p = this.Progress;
        this.Value = p < 0.5
                         ? (1 - Math.Sqrt(1 - Math.Pow(2 * p, 2))) / 2
                         : (Math.Sqrt(1 - Math.Pow((-2 * p) + 2, 2)) + 1) / 2;
    }
}

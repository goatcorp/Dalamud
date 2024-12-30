namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "InOutCirc" easing animation.
/// </summary>
public class InOutElastic : Easing
{
    private const double Constant = (2 * Math.PI) / 4.5;

    /// <summary>
    /// Initializes a new instance of the <see cref="InOutElastic"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public InOutElastic(TimeSpan duration)
        : base(duration)
    {
        // ignored
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var p = this.Progress;
        this.Value = p == 0
                         ? 0
                         : p == 1
                             ? 1
                             : p < 0.5
                                 ? -(Math.Pow(2, (20 * p) - 10) * Math.Sin(((20 * p) - 11.125) * Constant)) / 2
                                 : (Math.Pow(2, (-20 * p) + 10) * Math.Sin(((20 * p) - 11.125) * Constant) / 2) + 1;
    }
}

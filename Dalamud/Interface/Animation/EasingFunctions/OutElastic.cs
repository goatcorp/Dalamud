namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "OutElastic" easing animation.
/// </summary>
public class OutElastic : Easing
{
    private const double Constant = (2 * Math.PI) / 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutElastic"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public OutElastic(TimeSpan duration)
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
                             : (Math.Pow(2, -10 * p) * Math.Sin(((p * 10) - 0.75) * Constant)) + 1;
    }
}

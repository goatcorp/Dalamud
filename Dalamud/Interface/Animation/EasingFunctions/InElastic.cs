namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "InElastic" easing animation.
/// </summary>
public class InElastic : Easing
{
    private const double Constant = (2 * Math.PI) / 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="InElastic"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public InElastic(TimeSpan duration)
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
                             : -Math.Pow(2, (10 * p) - 10) * Math.Sin(((p * 10) - 10.75) * Constant);
    }
}

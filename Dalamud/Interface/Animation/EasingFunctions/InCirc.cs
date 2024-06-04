namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "InCirc" easing animation.
/// </summary>
public class InCirc : Easing
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InCirc"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public InCirc(TimeSpan duration)
        : base(duration)
    {
        // ignored
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var p = this.Progress;
        this.Value = 1 - Math.Sqrt(1 - Math.Pow(p, 2));
    }
}

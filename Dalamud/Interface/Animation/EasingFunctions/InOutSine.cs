namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "InOutSine" easing animation.
/// </summary>
public class InOutSine : Easing
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InOutSine"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public InOutSine(TimeSpan duration)
        : base(duration)
    {
        // ignored
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var p = this.Progress;
        this.Value = -(Math.Cos(Math.PI * p) - 1) / 2;
    }
}

namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "InSine" easing animation.
/// </summary>
public class InSine : Easing
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InSine"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public InSine(TimeSpan duration)
        : base(duration)
    {
        // ignored
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var p = this.Progress;
        this.Value = 1 - Math.Cos((p * Math.PI) / 2);
    }
}

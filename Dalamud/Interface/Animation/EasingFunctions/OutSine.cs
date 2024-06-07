namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "OutSine" easing animation.
/// </summary>
public class OutSine : Easing
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutSine"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public OutSine(TimeSpan duration)
        : base(duration)
    {
        // ignored
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var p = this.Progress;
        this.Value = Math.Sin((p * Math.PI) / 2);
    }
}

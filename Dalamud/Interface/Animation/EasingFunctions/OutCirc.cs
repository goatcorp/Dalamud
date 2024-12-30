namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "OutCirc" easing animation.
/// </summary>
public class OutCirc : Easing
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutCirc"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public OutCirc(TimeSpan duration)
        : base(duration)
    {
        // ignored
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var p = this.Progress;
        this.Value = Math.Sqrt(1 - Math.Pow(p - 1, 2));
    }
}

namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "OutQuint" easing animation.
/// </summary>
public class OutQuint : Easing
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutQuint"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public OutQuint(TimeSpan duration)
        : base(duration)
    {
        // ignored
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var p = this.Progress;
        this.Value = 1 - Math.Pow(1 - p, 5);
    }
}

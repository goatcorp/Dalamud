namespace Dalamud.Interface.Animation.EasingFunctions;

/// <summary>
/// Class providing an "InQuint" easing animation.
/// </summary>
public class InQuint : Easing
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InQuint"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    public InQuint(TimeSpan duration)
        : base(duration)
    {
        // ignored
    }

    /// <inheritdoc/>
    public override void Update()
    {
        var p = this.Progress;
        this.Value = p * p * p * p * p;
    }
}

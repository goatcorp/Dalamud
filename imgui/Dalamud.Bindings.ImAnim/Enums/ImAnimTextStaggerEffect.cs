namespace Dalamud.Bindings.ImAnim;

public enum ImAnimTextStaggerEffect
{
    /// <summary>
    /// No effect (instant appear)
    /// </summary>
    None,

    /// <summary>
    /// Fade in alpha
    /// </summary>
    Fade,

    /// <summary>
    /// Scale from center
    /// </summary>
    Scale,

    /// <summary>
    /// Slide up from below
    /// </summary>
    SlideUp,

    /// <summary>
    /// Slide down from above
    /// </summary>
    SlideDown,

    /// <summary>
    /// Slide in from right
    /// </summary>
    SlideLeft,

    /// <summary>
    /// Slide in from left
    /// </summary>
    SlideRight,

    /// <summary>
    /// Rotate in
    /// </summary>
    Rotate,

    /// <summary>
    /// Bounce in with overshoot
    /// </summary>
    Bounce,

    /// <summary>
    /// Wave motion (continuous)
    /// </summary>
    Wave,

    /// <summary>
    /// Typewriter style (instant per char)
    /// </summary>
    Typewriter,
}

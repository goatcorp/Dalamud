namespace Dalamud.Bindings.ImAnim;

public enum ImAnimEaseType
{
    Linear,
    InQuad,
    OutQuad,
    InOutQuad,
    InCubic,
    OutCubic,
    InOutCubic,
    InQuart,
    OutQuart,
    InOutQuart,
    InQuint,
    OutQuint,
    InOutQuint,
    InSine,
    OutSine,
    InOutSine,
    InExpo,
    OutExpo,
    InOutExpo,
    InCirc,
    OutCirc,
    InOutCirc,
    /// <summary>
    /// p0 = overshoot
    /// </summary>
    InBack,
    /// <summary>
    /// p0 = overshoot
    /// </summary>
    OutBack,
    /// <summary>
    /// p0 = overshoot
    /// </summary>
    InOutBack,
    /// <summary>
    /// p0 = amplitude, p1 = period
    /// </summary>
    InElastic,
    /// <summary>
    /// p0 = amplitude, p1 = period
    /// </summary>
    OutElastic,
    /// <summary>
    /// p0 = amplitude, p1 = period
    /// </summary>
    InOutElastic,
    InBounce,
    OutBounce,
    InOutBounce,
    /// <summary>
    /// p0 = steps (>=1), p1 = 0:end 1:start 2:both
    /// </summary>
    Steps,
    /// <summary>
    /// p0=x1 p1=y1 p2=x2 p3=y2
    /// </summary>
    CubicBezier,
    /// <summary>
    /// p0=mass p1=stiffness p2=damping p3=v0
    /// </summary>
    Spring,
    /// <summary>
    /// User-defined easing function (use iam_ease_custom_fn)
    /// </summary>
    Custom,
}

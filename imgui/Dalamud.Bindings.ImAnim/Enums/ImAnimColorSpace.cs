namespace Dalamud.Bindings.ImAnim;

public enum ImAnimColorSpace
{
    /// <summary>
    /// blend in sRGB (not physically linear)
    /// </summary>
    Srgb,

    /// <summary>
    /// sRGB<->linear, blend in linear, back to sRGB
    /// </summary>
    SrgbLinear,

    /// <summary>
    /// blend H/S/V (hue shortest arc), keep A linear
    /// </summary>
    Hsv,

    /// <summary>
    /// sRGB<->OKLAB, blend in OKLAB, back to sRGB
    /// </summary>
    Oklab,

    /// <summary>
    /// sRGB<->OKLCH (cylindrical OKLAB), blend in OKLCH, back to sRGB
    /// </summary>
    Oklch,
}

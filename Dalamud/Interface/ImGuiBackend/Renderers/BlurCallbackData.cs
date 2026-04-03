using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.ImGuiBackend.Renderers;

/// <summary>
/// Parameter block for the Blur draw callback.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct BlurCallbackData
{
    /// <summary>
    /// Dual Kawase blur spread factor.
    /// </summary>
    public float BlurStrength;

    /// <summary>
    /// Corner radius of the rounded-rectangle mask in pixels.
    /// </summary>
    public float Rounding;

    /// <summary>
    /// Tint color (RGB) and tint blend strength (A in [0, 1]).
    /// A = 0 means no tint is applied.
    /// </summary>
    public Vector4 TintColor;

    /// <summary>
    /// Luminosity target color (RGB) and luminosity blend strength (A in [0, 1]).
    /// The luminosity step reduces contrast by replacing the blurred image's lightness
    /// with the target's lightness (keeping the blurred image's hue and saturation).
    /// A = 0 means the luminosity step is skipped.
    /// </summary>
    public Vector4 LuminosityColor;

    /// <summary>
    /// Noise texture layer opacity in [0, 1].
    /// 0 = no grain.
    /// </summary>
    public float NoiseOpacity;
}

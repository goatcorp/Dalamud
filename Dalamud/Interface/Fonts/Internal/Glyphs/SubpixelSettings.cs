using System.Numerics;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>Manages subpixel text rendering state and ClearType gamma-correction tables.</summary>
/// <remarks><ul>
/// <li>Glyphs are rasterized in RGB subpixel order when enabled and supported by the game's monitor. Each viewport
/// draws using its own monitor's subpixel geometry and ClearType settings.</li>
/// <li>Monitor settings come from <see cref="MonitorCache"/>, falling back to grayscale if unavailable.</li>
/// <li>Main thread only.</li>
/// </ul></remarks>
internal static class SubpixelSettings
{
    // DWrite_GetGammaRatios' table for gammas 1.0 to 2.2 in steps of 0.1, and its normalization.
    private static readonly Vector4 GammaRatioScale = new(65536f / 65025, 256f / 255, 65536f / 65025, 256f / 255);

    private static readonly Vector4[] GammaRatioTable =
    [
        new(0.0000f, 0.0000f, 0.0000f, 0.0000f),
        new(0.0166f, -0.0807f, 0.2227f, -0.0751f),
        new(0.0350f, -0.1760f, 0.4325f, -0.1370f),
        new(0.0543f, -0.2821f, 0.6302f, -0.1876f),
        new(0.0739f, -0.3963f, 0.8167f, -0.2287f),
        new(0.0933f, -0.5161f, 0.9926f, -0.2616f),
        new(0.1121f, -0.6395f, 1.1588f, -0.2877f),
        new(0.1300f, -0.7649f, 1.3159f, -0.3080f),
        new(0.1469f, -0.8911f, 1.4644f, -0.3234f),
        new(0.1627f, -1.0170f, 1.6051f, -0.3347f),
        new(0.1773f, -1.1420f, 1.7385f, -0.3426f),
        new(0.1908f, -1.2652f, 1.8650f, -0.3476f),
        new(0.2031f, -1.3864f, 1.9851f, -0.3501f),
    ];

    private static bool requested;

    /// <summary>Physical subpixel stripe order after accounting for monitor rotation.</summary>
    public enum SubpixelGeometry
    {
        /// <summary>The monitor cannot show subpixel text.</summary>
        None,

        /// <summary>Red, green, blue, left to right.</summary>
        Rgb,

        /// <summary>Blue, green, red, left to right.</summary>
        Bgr,
    }

    /// <summary>Gets or sets a temporary override for the configured subpixel rendering setting, or
    /// <see langword="null"/>.</summary>
    /// <remarks>Takes effect immediately via <see cref="Enabled"/>, while <see cref="Available"/> reflects the last
    /// <see cref="Update"/>.</remarks>
    public static bool? Override { get; set; }

    /// <summary>Gets a value indicating whether text is rasterized per subpixel.</summary>
    public static bool Enabled => Available && (Override ?? requested);

    /// <summary>Gets a value indicating whether the monitor the game is on shows subpixel text.</summary>
    /// <value>As of the last <see cref="Update"/>.</value>
    public static bool Available { get; private set; }

    /// <summary>Gets the settings of a monitor that shows grayscale text.</summary>
    public static MonitorSettings Grayscale { get; } = new(GammaRatiosOf(1.8f), 0.5f, 1f, 1f, SubpixelGeometry.None);

    /// <summary>Updates subpixel availability and user preference for the upcoming frame.</summary>
    /// <param name="useSubpixelRendering">Whether subpixel rendering is enabled in configuration.</param>
    /// <param name="gameMonitor">The monitor the game is on.</param>
    /// <remarks>Called before each frame.</remarks>
    public static void Update(bool useSubpixelRendering, in MonitorSettings gameMonitor)
    {
        requested = useSubpixelRendering;
        Available = gameMonitor.Geometry != SubpixelGeometry.None;
    }

    /// <summary>Gets the alpha correction coefficients of the text shader for a display gamma.</summary>
    /// <param name="gamma">The gamma, clamped to 1 to 2.2.</param>
    /// <returns>The coefficients.</returns>
    /// <remarks>DirectWrite takes the nearest row of its table. This interpolates, so that gamma changes smoothly.
    /// </remarks>
    public static Vector4 GammaRatiosOf(float gamma)
    {
        var position = (Math.Clamp(gamma, 1f, 2.2f) * 10) - 10;
        var row = Math.Min((int)position, GammaRatioTable.Length - 2);
        return Vector4.Lerp(GammaRatioTable[row], GammaRatioTable[row + 1], position - row) * GammaRatioScale;
    }

    /// <summary>Per-monitor ClearType and gamma-correction parameters.</summary>
    /// <param name="GammaRatios">The alpha correction coefficients for the display gamma.</param>
    /// <param name="EnhancedContrast">The contrast added to subpixel coverage.</param>
    /// <param name="GrayscaleEnhancedContrast">The contrast added to grayscale coverage.</param>
    /// <param name="ClearTypeLevel">How much of the color fringes to keep, from 0 (grayscale) to 1.</param>
    /// <param name="Geometry">How the subpixels are laid out, or <see cref="SubpixelGeometry.None"/> if subpixel text
    /// cannot be shown.</param>
    public readonly record struct MonitorSettings(
        Vector4 GammaRatios,
        float EnhancedContrast,
        float GrayscaleEnhancedContrast,
        float ClearTypeLevel,
        SubpixelGeometry Geometry);
}

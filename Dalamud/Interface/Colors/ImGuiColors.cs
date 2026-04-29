using System.Numerics;

namespace Dalamud.Interface.Colors;

/// <summary>
/// Class containing frequently used colors for easier reference.
/// </summary>
public static class ImGuiColors
{
    /// <summary>
    /// Gets red used in dalamud.
    /// </summary>
    public static Vector4 DalamudRed { get; internal set; } = new(1f, 0f, 0f, 1f);

    /// <summary>
    /// Gets grey used in dalamud.
    /// </summary>
    public static Vector4 DalamudGrey { get; internal set; } = new(0.7f, 0.7f, 0.7f, 1f);

    /// <summary>
    /// Gets grey used in dalamud.
    /// </summary>
    public static Vector4 DalamudGrey2 { get; internal set; } = new(0.7f, 0.7f, 0.7f, 1f);

    /// <summary>
    /// Gets grey used in dalamud.
    /// </summary>
    public static Vector4 DalamudGrey3 { get; internal set; } = new(0.5f, 0.5f, 0.5f, 1f);

    /// <summary>
    /// Gets white used in dalamud.
    /// </summary>
    public static Vector4 DalamudWhite { get; internal set; } = new(1f, 1f, 1f, 1f);

    /// <summary>
    /// Gets white used in dalamud.
    /// </summary>
    public static Vector4 DalamudWhite2 { get; internal set; } = new(0.878f, 0.878f, 0.878f, 1f);

    /// <summary>
    /// Gets orange used in dalamud.
    /// </summary>
    public static Vector4 DalamudOrange { get; internal set; } = new(1f, 0.709f, 0f, 1f);

    /// <summary>
    /// Gets yellow used in dalamud.
    /// </summary>
    public static Vector4 DalamudYellow { get; internal set; } = new(1f, 1f, .4f, 1f);

    /// <summary>
    /// Gets violet used in dalamud.
    /// </summary>
    public static Vector4 DalamudViolet { get; internal set; } = new(0.770f, 0.700f, 0.965f, 1.000f);

    /// <summary>
    /// Gets tank blue (UIColor37).
    /// </summary>
    public static Vector4 TankBlue { get; internal set; } = new(0f, 0.6f, 1f, 1f);

    /// <summary>
    /// Gets healer green (UIColor504).
    /// </summary>
    public static Vector4 HealerGreen { get; internal set; } = new(0f, 0.8f, 0.1333333f, 1f);

    /// <summary>
    /// Gets dps red (UIColor545).
    /// </summary>
    public static Vector4 DPSRed { get; internal set; } = new(0.7058824f, 0f, 0f, 1f);

    /// <summary>
    /// Gets parsed grey.
    /// </summary>
    public static Vector4 ParsedGrey { get; internal set; } = new(0.4f, 0.4f, 0.4f, 1f);

    /// <summary>
    /// Gets parsed green.
    /// </summary>
    public static Vector4 ParsedGreen { get; internal set; } = new(0.117f, 1f, 0f, 1f);

    /// <summary>
    /// Gets parsed blue.
    /// </summary>
    public static Vector4 ParsedBlue { get; internal set; } = new(0f, 0.439f, 1f, 1f);

    /// <summary>
    /// Gets parsed purple.
    /// </summary>
    public static Vector4 ParsedPurple { get; internal set; } = new(0.639f, 0.207f, 0.933f, 1f);

    /// <summary>
    /// Gets parsed orange.
    /// </summary>
    public static Vector4 ParsedOrange { get; internal set; } = new(1f, 0.501f, 0f, 1f);

    /// <summary>
    /// Gets parsed pink.
    /// </summary>
    public static Vector4 ParsedPink { get; internal set; } = new(0.886f, 0.407f, 0.658f, 1f);

    /// <summary>
    /// Gets parsed gold.
    /// </summary>
    public static Vector4 ParsedGold { get; internal set; } = new(0.898f, 0.8f, 0.501f, 1f);

    /// <summary>
    /// Gets a foreground color used globally to denote general information.
    /// </summary>
    /// <remarks> Default is the same as <see cref="TankBlue"/>. </remarks>
    public static Vector4 InfoForeground { get; internal set; } = new(0f, 0.6f, 1f, 1f);

    /// <summary>
    /// Gets a background color used globally to denote general information.
    /// </summary>
    /// <remarks> This color is not fully opaque by default, and is intended to be a muted version of <see cref="InfoForeground"/>. </remarks>
    public static Vector4 InfoBackground { get; internal set; } = new(0.2f, 0.45f, 0.6f, 0.4f);

    /// <summary>
    /// Gets a foreground color used globally to denote successful actions.
    /// </summary>
    /// <remarks> Default is the same as <see cref="HealerGreen"/>. </remarks>
    public static Vector4 SuccessForeground { get; internal set; } = new(0f, 0.8f, 0.1333333f, 1f);

    /// <summary>
    /// Gets a background color used globally to denote successful actions.
    /// </summary>
    /// <remarks> This color is not fully opaque by default, and is intended to be a muted version of <see cref="SuccessForeground"/>. </remarks>
    public static Vector4 SuccessBackground { get; internal set; } = new(0.3f, 0.6f, 0.35f, 0.4f);

    /// <summary>
    /// Gets a foreground color used globally to denote warnings.
    /// </summary>
    /// <remarks> Default is the same as <see cref="DalamudOrange"/>. </remarks>
    public static Vector4 WarningForeground { get; internal set; } = new(1f, 0.709f, 0f, 1f);

    /// <summary>
    /// Gets a background color used globally to denote warnings.
    /// </summary>
    /// <remarks> This color is not fully opaque by default, and is intended to be a muted version of <see cref="WarningForeground"/>. </remarks>
    public static Vector4 WarningBackground { get; internal set; } = new(0.75f, 0.65f, 0.3f, 0.4f);

    /// <summary>
    /// Gets a foreground color used globally to denote errors and unsuccessful actions.
    /// </summary>
    /// <remarks> Default is the same as <see cref="DalamudRed"/>. </remarks>
    public static Vector4 ErrorForeground { get; internal set; } = new(1f, 0f, 0f, 1f);

    /// <summary>
    /// Gets a background color used globally to denote errors and unsuccessful actions.
    /// </summary>
    /// <remarks> This color is not fully opaque by default, and is intended to be a muted version of <see cref="ErrorForeground"/>. </remarks>
    public static Vector4 ErrorBackground { get; internal set; } = new(0.7f, 0.3f, 0.3f, 0.4f);

    /// <summary>
    /// Gets a foreground color used globally to highlight things that should catch a user's attention.
    /// </summary>
    /// <remarks> Default is the same as <see cref="WarningForeground"/> and <see cref="DalamudOrange"/>. </remarks>
    public static Vector4 AttentionForeground { get; internal set; } = new(1f, 0.709f, 0f, 1f);

    /// <summary>
    /// Gets a background color used globally to highlight things that should catch a user's attention.
    /// </summary>
    /// <remarks> This color is not fully opaque by default, and is intended to be a muted version of <see cref="AttentionForeground"/>. </remarks>
    public static Vector4 AttentionBackground { get; internal set; } = new(0.75f, 0.65f, 0.3f, 0.4f);
}

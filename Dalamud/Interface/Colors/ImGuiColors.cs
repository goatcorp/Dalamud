using System.Numerics;

namespace Dalamud.Interface.Colors
{
    /// <summary>
    /// Class containing frequently used colors for easier reference.
    /// </summary>
    public static class ImGuiColors
    {
        /// <summary>
        /// Gets red used in dalamud.
        /// </summary>
        public static Vector4 DalamudRed { get; } = new Vector4(1f, 0f, 0f, 1f);

        /// <summary>
        /// Gets grey used in dalamud.
        /// </summary>
        public static Vector4 DalamudGrey { get; } = new Vector4(0.7f, 0.7f, 0.7f, 1f);

        /// <summary>
        /// Gets grey used in dalamud.
        /// </summary>
        public static Vector4 DalamudGrey2 { get; } = new Vector4(0.7f, 0.7f, 0.7f, 1f);

        /// <summary>
        /// Gets grey used in dalamud.
        /// </summary>
        public static Vector4 DalamudGrey3 { get; } = new Vector4(0.5f, 0.5f, 0.5f, 1f);

        /// <summary>
        /// Gets white used in dalamud.
        /// </summary>
        public static Vector4 DalamudWhite { get; } = new Vector4(1f, 1f, 1f, 1f);

        /// <summary>
        /// Gets white used in dalamud.
        /// </summary>
        public static Vector4 DalamudWhite2 { get; } = new Vector4(0.878f, 0.878f, 0.878f, 1f);

        /// <summary>
        /// Gets orange used in dalamud.
        /// </summary>
        public static Vector4 DalamudOrange { get; } = new Vector4(1f, 0.709f, 0f, 1f);

        /// <summary>
        /// Gets tank blue (UIColor37).
        /// </summary>
        public static Vector4 TankBlue { get; } = new Vector4(0f, 0.6f, 1f, 1f);

        /// <summary>
        /// Gets healer green (UIColor504).
        /// </summary>
        public static Vector4 HealerGreen { get; } = new Vector4(0f, 0.8f, 0.1333333f, 1f);

        /// <summary>
        /// Gets dps red (UIColor545).
        /// </summary>
        public static Vector4 DPSRed { get; } = new Vector4(0.7058824f, 0f, 0f, 1f);
    }
}

using System.Numerics;
using ImGuiNET;

namespace Dalamud.Fools.Helper.YesHealMe;

public static class Colors
{
    public static Vector4 Purple = new(176 / 255.0f, 38 / 255.0f, 236 / 255.0f, 1.0f);
    public static Vector4 Blue = new(37 / 255.0f, 168 / 255.0f, 1.0f, 1.0f);
    public static Vector4 ForestGreen = new(0.133f, 0.545f, 0.1333f, 1.0f);
    public static Vector4 White = new(1.0f, 1.0f, 1.0f,1.0f);
    public static Vector4 Red = new(1.0f, 0.0f, 0.0f, 1.0f);
    public static Vector4 Green = new(0.0f, 1.0f, 0.0f, 1.0f);
    public static Vector4 Black = new(0.0f, 0.0f, 0.0f, 1.0f);
    public static Vector4 HealerGreen = new(33 / 255f, 193 / 255f, 0, 1.0f);
    public static Vector4 DPSRed = new(210/255f, 42/255f, 43/255f, 1.0f);
    public static Vector4 SoftRed = new(0.8f, 0.2f, 0.2f, 1.0f);
    public static Vector4 Grey = new(0.6f, 0.6f, 0.6f, 1.0f);
    public static Vector4 LightGrey = new(220/250.0f, 220/250.0f, 220/250f, 1.0f);
    public static Vector4 Orange = new(1.0f, 165.0f / 255.0f, 0.0f, 1.0f);
    public static Vector4 SoftGreen = new(0.2f, 0.8f, 0.2f, 1.0f);
    public static Vector4 FatePink = new(0.58f, 0.388f, 0.827f, 0.33f);
    public static Vector4 FateDarkPink = new(0.58f, 0.388f, 0.827f, 1.0f);
    public static Vector4 MapTextBrown = new(0.655f, 0.396f, 0.149f, 1.0f);
    public static Vector4 BabyBlue = new(0.537f, 0.812f, 0.941f, 1.0f);
}

public static class ColorExtensions
{
    public static uint ToU32(this Vector4 color) => ImGui.GetColorU32(color);
}

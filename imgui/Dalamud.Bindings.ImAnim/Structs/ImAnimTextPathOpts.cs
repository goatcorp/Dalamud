using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public struct ImAnimTextPathOpts
{
    public Vector2 Origin;
    public float Offset;
    public float LetterSpacing;
    public ImAnimTextPathAlign Align;
    public bool FlipY;
    public uint Color;
    public ImFontPtr Font;
    public float FontScale;

    public static ImAnimTextPathOpts Default() => new() { Color = 0xFFFFFFFF, FontScale = 1.0f };
}

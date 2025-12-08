using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImAnimTextStaggerOpts
{
    public Vector2 Pos;
    public ImAnimTextStaggerEffect Effect;
    public float CharDelay;
    public float CharDuration;
    public float EffectIntensity;
    public ImAnimEaseDesc Ease;
    public uint Color;
    public ImFontPtr Font;
    public float FontScale;
    public float LetterSpacing;

    public static ImAnimTextStaggerOpts Default() => new()
    {
        Effect = ImAnimTextStaggerEffect.Fade,
        CharDelay = 0.05f,
        CharDuration = 0.3f,
        EffectIntensity = 20.0f,
        Ease = new ImAnimEaseDesc { Type = ImAnimEaseType.OutCubic },
        Color = 0xFFFFFFFF,
        FontScale = 1.0f
    };
}

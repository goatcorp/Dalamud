using System.Numerics;
using System.Runtime.CompilerServices;

namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImFont
{
    public readonly int CalcWordWrapPositionA(
        float scale, ReadOnlySpan<byte> text, float wrapWidth)
    {
        fixed (ImFont* thisPtr = &this)
            return ImGui.CalcWordWrapPositionA(thisPtr, scale, text, wrapWidth);
    }

    public readonly void RenderText(
        ImDrawListPtr drawList, float size, Vector2 pos, uint col, Vector4 clipRect,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer text, float wrapWidth = 0.0f, bool cpuFineClip = false)
    {
        fixed (ImFont* thisPtr = &this)
            ImGui.RenderText(thisPtr, drawList, size, pos, col, clipRect, text, wrapWidth, cpuFineClip);
    }
}

public partial struct ImFontPtr
{
    public readonly int CalcWordWrapPositionA(float scale, [InterpolatedStringHandlerArgument] AutoUtf8Buffer text, float wrapWidth) =>
        ImGui.CalcWordWrapPositionA(this, scale, text, wrapWidth);

    public readonly void RenderText(
        ImDrawListPtr drawList, float size, Vector2 pos, uint col, Vector4 clipRect, [InterpolatedStringHandlerArgument] AutoUtf8Buffer text,
        float wrapWidth = 0.0f, bool cpuFineClip = false) =>
        ImGui.RenderText(this, drawList, size, pos, col, clipRect, text, wrapWidth, cpuFineClip);
}

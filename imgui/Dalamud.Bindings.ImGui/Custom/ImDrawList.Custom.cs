using System.Numerics;

namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImDrawList
{
    public void AddText(Vector2 pos, uint col, AutoUtf8Buffer text)
    {
        fixed (ImDrawList* thisPtr = &this)
            ImGui.AddText(thisPtr, pos, col, text);
    }

    public void AddText(
        ImFontPtr font, float fontSize, Vector2 pos, uint col, AutoUtf8Buffer text, float wrapWidth,
        scoped in Vector4 cpuFineClipRect)
    {
        fixed (ImDrawList* thisPtr = &this)
            ImGui.AddText(thisPtr, font, fontSize, pos, col, text, wrapWidth, cpuFineClipRect);
    }

    public void AddText(
        ImFontPtr font, float fontSize, Vector2 pos, uint col, AutoUtf8Buffer text, float wrapWidth = 0f)
    {
        fixed (ImDrawList* thisPtr = &this)
            ImGui.AddText(thisPtr, font, fontSize, pos, col, text, wrapWidth);
    }
}

public partial struct ImDrawListPtr
{
    public void AddText(Vector2 pos, uint col, AutoUtf8Buffer text) =>
        ImGui.AddText(this, pos, col, text);

    public void AddText(
        ImFontPtr font, float fontSize, Vector2 pos, uint col, AutoUtf8Buffer text, float wrapWidth,
        scoped in Vector4 cpuFineClipRect) =>
        ImGui.AddText(this, font, fontSize, pos, col, text, wrapWidth, cpuFineClipRect);

    public void AddText(
        ImFontPtr font, float fontSize, Vector2 pos, uint col, AutoUtf8Buffer text, float wrapWidth = 0f) =>
        ImGui.AddText(this, font, fontSize, pos, col, text, wrapWidth);
}

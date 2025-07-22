namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiTextFilter
{
    public void Draw(Utf8Buffer label = default, float width = 0.0f)
    {
        fixed (ImGuiTextFilter* thisPtr = &this)
            ImGui.Draw(thisPtr, label, width);
    }

    public void PassFilter(Utf8Buffer text)
    {
        fixed (ImGuiTextFilter* thisPtr = &this)
            ImGui.PassFilter(thisPtr, text);
    }
}

public partial struct ImGuiTextFilterPtr
{
    public void Draw(Utf8Buffer label = default, float width = 0.0f) => ImGui.Draw(this, label, width);
    public void PassFilter(Utf8Buffer text) => ImGui.PassFilter(this, text);
}

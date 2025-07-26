namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiTextFilter
{
    public void Draw(ImU8String label = default, float width = 0.0f)
    {
        fixed (ImGuiTextFilter* thisPtr = &this)
            ImGui.Draw(thisPtr, label, width);
    }

    public void PassFilter(ImU8String text)
    {
        fixed (ImGuiTextFilter* thisPtr = &this)
            ImGui.PassFilter(thisPtr, text);
    }
}

public partial struct ImGuiTextFilterPtr
{
    public void Draw(ImU8String label = default, float width = 0.0f) => ImGui.Draw(this, label, width);
    public void PassFilter(ImU8String text) => ImGui.PassFilter(this, text);
}

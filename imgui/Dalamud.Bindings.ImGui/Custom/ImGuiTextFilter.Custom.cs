namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiTextFilter
{
    public bool Draw(ImU8String label = default, float width = 0.0f)
    {
        fixed (ImGuiTextFilter* thisPtr = &this)
            return ImGui.Draw(thisPtr, label, width);
    }

    public bool PassFilter(ImU8String text)
    {
        fixed (ImGuiTextFilter* thisPtr = &this)
            return ImGui.PassFilter(thisPtr, text);
    }
}

public partial struct ImGuiTextFilterPtr
{
    public bool Draw(ImU8String label = default, float width = 0.0f) => ImGui.Draw(this, label, width);
    public bool PassFilter(ImU8String text) => ImGui.PassFilter(this, text);
}

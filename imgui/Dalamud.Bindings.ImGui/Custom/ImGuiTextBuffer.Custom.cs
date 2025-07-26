namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiTextBuffer
{
    public void append(ImU8String str)
    {
        fixed (ImGuiTextBuffer* thisPtr = &this)
            ImGui.append(thisPtr, str);
    }
}

public partial struct ImGuiTextBufferPtr
{
    public void append(ImU8String str) => ImGui.append(this, str);
}

namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiTextBuffer
{
    public void append(Utf8Buffer str)
    {
        fixed (ImGuiTextBuffer* thisPtr = &this)
            ImGui.append(thisPtr, str);
    }
}

public partial struct ImGuiTextBufferPtr
{
    public void append(Utf8Buffer str) => ImGui.append(this, str);
}

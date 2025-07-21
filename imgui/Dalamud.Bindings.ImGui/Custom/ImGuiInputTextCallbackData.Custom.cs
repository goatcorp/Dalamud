using System.Runtime.CompilerServices;

namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiInputTextCallbackData
{
    public void InsertChars(int pos, [InterpolatedStringHandlerArgument] AutoUtf8Buffer text)
    {
        fixed (ImGuiInputTextCallbackData* thisPtr = &this)
            ImGui.InsertChars(thisPtr, pos, text);
    }
}

public partial struct ImGuiInputTextCallbackDataPtr
{
    public void InsertChars(int pos, [InterpolatedStringHandlerArgument] AutoUtf8Buffer text) => ImGui.InsertChars(this, pos, text);
}

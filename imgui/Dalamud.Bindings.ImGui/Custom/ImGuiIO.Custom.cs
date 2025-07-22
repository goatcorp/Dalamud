using System.Text;

namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiIO
{
    public void AddInputCharacter(char c)
    {
        fixed (ImGuiIO* thisPtr = &this)
            ImGui.AddInputCharacter(thisPtr, c);
    }

    public void AddInputCharacter(Rune c)
    {
        fixed (ImGuiIO* thisPtr = &this)
            ImGui.AddInputCharacter(thisPtr, c);
    }

    public void AddInputCharacters(Utf8Buffer str)
    {
        fixed (ImGuiIO* thisPtr = &this)
            ImGui.AddInputCharacters(thisPtr, str);
    }
}

public partial struct ImGuiIOPtr
{
    public void AddInputCharacter(char c) => ImGui.AddInputCharacter(this, c);

    public void AddInputCharacter(Rune c) => ImGui.AddInputCharacter(this, c);

    public void AddInputCharacters(Utf8Buffer str) => ImGui.AddInputCharacters(this, str);
}

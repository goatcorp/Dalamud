using System.Runtime.CompilerServices;

namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiWindow
{
    public readonly uint GetID([InterpolatedStringHandlerArgument] AutoUtf8Buffer str)
    {
        fixed (ImGuiWindow* thisPtr = &this)
            return ImGuiP.GetID(thisPtr, str);
    }

    public readonly uint GetID(void* ptr)
    {
        fixed (ImGuiWindow* thisPtr = &this)
            return ImGuiP.GetID(thisPtr, ptr);
    }

    public readonly uint GetID(int n)
    {
        fixed (ImGuiWindow* thisPtr = &this)
            return ImGuiP.GetID(thisPtr, n);
    }
}

public unsafe partial struct ImGuiWindowPtr
{
    public readonly uint GetID([InterpolatedStringHandlerArgument] AutoUtf8Buffer str) => ImGuiP.GetID(this.Handle, str);

    public readonly uint GetID(void* ptr) => ImGuiP.GetID(this.Handle, ptr);

    public readonly uint GetID(int n) => ImGuiP.GetID(this.Handle, n);
}

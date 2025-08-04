using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

public static unsafe partial class ImGuiNative
{
    private const string LibraryName = "cimgui";

    static ImGuiNative()
    {
        if (LibraryName != ImGui.GetLibraryName())
        {
            throw new(
                $"{nameof(LibraryName)}(={LibraryName})" +
                $" does not match " +
                $"{nameof(ImGui)}.{nameof(ImGui.GetLibraryName)}(={ImGui.GetLibraryName()})");
        }
    }

    [LibraryImport(LibraryName, EntryPoint = "ImDrawList_AddCallback")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void AddCallback(
        ImDrawList* self,
        delegate*<ImDrawList*, ImDrawCmd*, void> callback,
        void* callbackData = null);

    [LibraryImport(LibraryName, EntryPoint = "igInputTextEx")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int InputTextEx(
        byte* label,
        byte* hint,
        byte* buf,
        int bufSize,
        Vector2 sizeArg,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        delegate* unmanaged[Cdecl]<ImGuiInputTextCallbackData*, int> callback = null,
        void* userData = null);
}

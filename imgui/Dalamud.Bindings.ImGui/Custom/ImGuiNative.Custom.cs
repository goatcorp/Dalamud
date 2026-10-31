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
        void* callbackData,
        nuint callbackDataSize);

    /// <summary>Sets the host text callbacks as function pointers.</summary>
    /// <param name="layout">The callback that creates layouts.</param>
    /// <param name="metrics">The callback that gives font metrics.</param>
    /// <remarks>Only the host calls this. The callbacks lay out the text of every window.</remarks>
    [LibraryImport(LibraryName, EntryPoint = "igSetTextCallbacks")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SetTextCallbacks(
        delegate* unmanaged[Cdecl]<ImFont*, ImTextDesc*, byte*, byte*, ImTextLayout**, int> layout,
        delegate* unmanaged[Cdecl]<ImFont*, float, float, ImTextMetrics*, void> metrics);

    [LibraryImport(LibraryName, EntryPoint = "igInputTextEx")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte InputTextEx(
        byte* label,
        byte* hint,
        byte* buf,
        int bufSize,
        Vector2 sizeArg,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
        delegate* unmanaged[Cdecl]<ImGuiInputTextCallbackData*, int> callback = null,
        void* userData = null);
}

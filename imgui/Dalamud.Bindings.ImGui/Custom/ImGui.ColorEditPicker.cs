using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static bool ColorEdit3(
        AutoUtf8Buffer label, scoped ref Vector3 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        fixed (byte* labelPtr = label.NullTerminatedSpan)
        fixed (Vector3* colPtr = &col)
        {
            var res = ImGuiNative.ColorEdit3(labelPtr, &colPtr->X, flags) != 0;
            label.Dispose();
            return res;
        }
    }

    public static bool ColorEdit4(
        AutoUtf8Buffer label, scoped ref Vector4 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        fixed (byte* labelPtr = label.NullTerminatedSpan)
        fixed (Vector4* colPtr = &col)
        {
            var res = ImGuiNative.ColorEdit4(labelPtr, &colPtr->X, flags) != 0;
            label.Dispose();
            return res;
        }
    }

    public static bool ColorPicker3(
        AutoUtf8Buffer label, scoped ref Vector3 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        fixed (byte* labelPtr = label.NullTerminatedSpan)
        fixed (Vector3* colPtr = &col)
        {
            var res = ImGuiNative.ColorPicker3(labelPtr, &colPtr->X, flags) != 0;
            label.Dispose();
            return res;
        }
    }

    public static bool ColorPicker4(
        AutoUtf8Buffer label, scoped ref Vector4 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        fixed (byte* labelPtr = label.NullTerminatedSpan)
        fixed (Vector4* colPtr = &col)
        {
            var res = ImGuiNative.ColorPicker4(labelPtr, &colPtr->X, flags, null) != 0;
            label.Dispose();
            return res;
        }
    }

    public static bool ColorPicker4(
        AutoUtf8Buffer label, scoped ref Vector4 col, ImGuiColorEditFlags flags, scoped in Vector4 refCol)
    {
        fixed (byte* labelPtr = label.NullTerminatedSpan)
        fixed (Vector4* colPtr = &col)
        fixed (Vector4* refColPtr = &refCol)
        {
            var res = ImGuiNative.ColorPicker4(labelPtr, &colPtr->X, flags, &refColPtr->X) != 0;
            label.Dispose();
            return res;
        }
    }

    public static bool ColorPicker4(AutoUtf8Buffer label, scoped ref Vector4 col, scoped in Vector4 refCol)
    {
        fixed (byte* labelPtr = label.NullTerminatedSpan)
        fixed (Vector4* colPtr = &col)
        fixed (Vector4* refColPtr = &refCol)
        {
            var res = ImGuiNative.ColorPicker4(labelPtr, &colPtr->X, ImGuiColorEditFlags.None, &refColPtr->X) != 0;
            label.Dispose();
            return res;
        }
    }
}

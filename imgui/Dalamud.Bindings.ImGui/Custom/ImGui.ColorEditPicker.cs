using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static bool ColorEdit3(
        ImU8String label, scoped ref Vector3 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (Vector3* colPtr = &col)
        {
            var res = ImGuiNative.ColorEdit3(labelPtr, &colPtr->X, flags) != 0;
            label.Recycle();
            return res;
        }
    }

    public static bool ColorEdit4(
        ImU8String label, scoped ref Vector4 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (Vector4* colPtr = &col)
        {
            var res = ImGuiNative.ColorEdit4(labelPtr, &colPtr->X, flags) != 0;
            label.Recycle();
            return res;
        }
    }

    /// <summary>Edits a color packed as ImGui packs colors, as <see cref="ColorConvertFloat4ToU32"/> does.</summary>
    /// <param name="label">The label.</param>
    /// <param name="col">The color, written only when it was changed.</param>
    /// <param name="flags">The flags.</param>
    /// <returns><see langword="true"/> if the color was changed.</returns>
    public static bool ColorEdit4(
        ImU8String label, scoped ref uint col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        var v = ColorConvertU32ToFloat4(col);
        if (!ColorEdit4(label, ref v, flags))
            return false;
        col = ColorConvertFloat4ToU32(v);
        return true;
    }

    /// <summary>Picks a color packed as ImGui packs colors, as <see cref="ColorConvertFloat4ToU32"/> does.</summary>
    /// <param name="label">The label.</param>
    /// <param name="col">The color, written only when it was changed.</param>
    /// <param name="flags">The flags.</param>
    /// <returns><see langword="true"/> if the color was changed.</returns>
    public static bool ColorPicker4(
        ImU8String label, scoped ref uint col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        var v = ColorConvertU32ToFloat4(col);
        if (!ColorPicker4(label, ref v, flags))
            return false;
        col = ColorConvertFloat4ToU32(v);
        return true;
    }

    public static bool ColorPicker3(
        ImU8String label, scoped ref Vector3 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (Vector3* colPtr = &col)
        {
            var res = ImGuiNative.ColorPicker3(labelPtr, &colPtr->X, flags) != 0;
            label.Recycle();
            return res;
        }
    }

    public static bool ColorPicker4(
        ImU8String label, scoped ref Vector4 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (Vector4* colPtr = &col)
        {
            var res = ImGuiNative.ColorPicker4(labelPtr, &colPtr->X, flags, null) != 0;
            label.Recycle();
            return res;
        }
    }

    public static bool ColorPicker4(
        ImU8String label, scoped ref Vector4 col, ImGuiColorEditFlags flags, scoped in Vector4 refCol)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (Vector4* colPtr = &col)
        fixed (Vector4* refColPtr = &refCol)
        {
            var res = ImGuiNative.ColorPicker4(labelPtr, &colPtr->X, flags, &refColPtr->X) != 0;
            label.Recycle();
            return res;
        }
    }

    public static bool ColorPicker4(ImU8String label, scoped ref Vector4 col, scoped in Vector4 refCol)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (Vector4* colPtr = &col)
        fixed (Vector4* refColPtr = &refCol)
        {
            var res = ImGuiNative.ColorPicker4(labelPtr, &colPtr->X, ImGuiColorEditFlags.None, &refColPtr->X) != 0;
            label.Recycle();
            return res;
        }
    }
}

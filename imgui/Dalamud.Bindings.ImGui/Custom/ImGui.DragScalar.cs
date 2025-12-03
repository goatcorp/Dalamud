using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static bool DragSByte(
        ImU8String label, scoped ref sbyte v, float vSpeed = 1.0f, sbyte vMin = 0, sbyte vMax = 0,
        ImU8String format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.S8,
        ref v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%hhd"u8),
        flags);

    public static bool DragSByte(
        ImU8String label, Span<sbyte> v, float vSpeed = 1.0f, sbyte vMin = 0, sbyte vMax = 0,
        ImU8String format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.S8,
        v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%hhd"u8),
        flags);

    public static bool DragByte(
        ImU8String label, scoped ref byte v, float vSpeed = 1.0f, byte vMin = 0, byte vMax = 0,
        ImU8String format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.U8,
        ref v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%hhu"u8),
        flags);

    public static bool DragByte(
        ImU8String label, Span<byte> v, float vSpeed = 1.0f, byte vMin = 0, byte vMax = 0,
        ImU8String format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.U8,
        v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%hhu"u8),
        flags);

    public static bool DragShort(
        ImU8String label, scoped ref short v, float vSpeed = 1.0f, short vMin = 0, short vMax = 0,
        ImU8String format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.S16,
        ref v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%hd"u8),
        flags);

    public static bool DragShort(
        ImU8String label, Span<short> v, float vSpeed = 1.0f, short vMin = 0, short vMax = 0,
        ImU8String format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.S16,
        v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%hd"u8),
        flags);

    public static bool DragUShort(
        ImU8String label, scoped ref ushort v, float vSpeed = 1.0f, ushort vMin = 0, ushort vMax = 0,
        ImU8String format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.U16,
        ref v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%hu"u8),
        flags);

    public static bool DragUShort(
        ImU8String label, Span<ushort> v, float vSpeed = 1.0f, ushort vMin = 0, ushort vMax = 0,
        ImU8String format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.U16,
        v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%hu"u8),
        flags);

    public static bool DragInt(
        ImU8String label, scoped ref int v, float vSpeed = 1.0f, int vMin = 0, int vMax = 0,
        ImU8String format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.S32,
        ref v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%d"u8),
        flags);

    public static bool DragInt(
        ImU8String label, Span<int> v, float vSpeed = 1.0f, int vMin = 0,
        int vMax = 0, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.S32,
        v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%d"u8),
        flags);

    public static bool DragUInt(
        ImU8String label, scoped ref uint v, float vSpeed = 1.0f, uint vMin = 0,
        uint vMax = 0, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.U32,
        ref v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%u"u8),
        flags);

    public static bool DragUInt(
        ImU8String label, Span<uint> v, float vSpeed = 1.0f, uint vMin = 0,
        uint vMax = 0, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.U32,
        v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%u"u8),
        flags);

    public static bool DragLong(
        ImU8String label, scoped ref long v, float vSpeed = 1.0f, long vMin = 0,
        long vMax = 0, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.S64,
        ref v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%I64d"u8),
        flags);

    public static bool DragLong(
        ImU8String label, Span<long> v, float vSpeed = 1.0f, long vMin = 0,
        long vMax = 0, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.S64,
        v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%I64d"u8),
        flags);

    public static bool DragULong(
        ImU8String label, scoped ref ulong v, float vSpeed = 1.0f,
        ulong vMin = 0, ulong vMax = 0, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.U64,
        ref v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%I64u"u8),
        flags);

    public static bool DragULong(
        ImU8String label, Span<ulong> v, float vSpeed = 1.0f, ulong vMin = 0,
        ulong vMax = 0, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.U64,
        v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%I64u"u8),
        flags);

    public static bool DragFloat(
        ImU8String label, scoped ref float v, float vSpeed = 1.0f,
        float vMin = 0.0f, float vMax = 0.0f, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.Float,
        ref v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%.3f"u8),
        flags);

    public static bool DragFloat(
        ImU8String label, Span<float> v, float vSpeed = 1.0f, float vMin = 0.0f,
        float vMax = 0.0f, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.Float,
        v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%.3f"u8),
        flags);

    public static bool DragFloat2(
        ImU8String label, scoped ref Vector2 v, float vSpeed = 1.0f,
        float vMin = 0.0f, float vMax = 0.0f, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.Float,
        MemoryMarshal.Cast<Vector2, float>(new(ref v)),
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%.3f"u8),
        flags);

    public static bool DragFloat3(
        ImU8String label, scoped ref Vector3 v, float vSpeed = 1.0f,
        float vMin = 0.0f, float vMax = 0.0f, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.Float,
        MemoryMarshal.Cast<Vector3, float>(new(ref v)),
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%.3f"u8),
        flags);

    public static bool DragFloat4(
        ImU8String label, scoped ref Vector4 v, float vSpeed = 1.0f,
        float vMin = 0.0f, float vMax = 0.0f, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.Float,
        MemoryMarshal.Cast<Vector4, float>(new(ref v)),
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%.3f"u8),
        flags);

    public static bool DragDouble(
        ImU8String label, scoped ref double v, float vSpeed = 1.0f,
        double vMin = 0.0f, double vMax = 0.0f, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.Double,
        ref v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%.3f"u8),
        flags);

    public static bool DragDouble(
        ImU8String label, Span<double> v, float vSpeed = 1.0f,
        double vMin = 0.0f, double vMax = 0.0f, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => DragScalar(
        label,
        ImGuiDataType.Double,
        v,
        vSpeed,
        vMin,
        vMax,
        format.MoveOrDefault("%.3f"u8),
        flags);

    public static bool DragScalar<T>(
        ImU8String label, ImGuiDataType dataType, scoped ref T v, float vSpeed,
        scoped in T vMin, scoped in T vMax, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* vPtr = &v)
        fixed (T* vMinPtr = &vMin)
        fixed (T* vMaxPtr = &vMax)
        {
            var res = ImGuiNative.DragScalar(labelPtr, dataType, vPtr, vSpeed, vMinPtr, vMaxPtr, formatPtr, flags) != 0;
            label.Recycle();
            format.Recycle();
            return res;
        }
    }

    public static bool DragScalar<T>(
        ImU8String label, ImGuiDataType dataType, Span<T> v, float vSpeed,
        scoped in T vMin, scoped in T vMax, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* vPtr = v)
        fixed (T* vMinPtr = &vMin)
        fixed (T* vMaxPtr = &vMax)
        {
            var res = ImGuiNative.DragScalarN(
                          labelPtr,
                          dataType,
                          vPtr,
                          v.Length,
                          vSpeed,
                          vMinPtr,
                          vMaxPtr,
                          formatPtr,
                          flags) != 0;
            label.Recycle();
            format.Recycle();
            return res;
        }
    }

    public static bool DragScalar<T>(
        ImU8String label, scoped ref T v, float vSpeed,
        scoped in T vMin, scoped in T vMax, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* vPtr = &v)
        fixed (T* vMinPtr = &vMin)
        fixed (T* vMaxPtr = &vMax)
        {
            var res = ImGuiNative.DragScalar(
                          labelPtr,
                          GetImGuiDataType<T>(),
                          vPtr,
                          vSpeed,
                          vMinPtr,
                          vMaxPtr,
                          formatPtr,
                          flags) != 0;
            label.Recycle();
            format.Recycle();
            return res;
        }
    }

    public static bool DragScalar<T>(
        ImU8String label, Span<T> v, float vSpeed,
        scoped in T vMin, scoped in T vMax, ImU8String format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* vPtr = v)
        fixed (T* vMinPtr = &vMin)
        fixed (T* vMaxPtr = &vMax)
        {
            var res = ImGuiNative.DragScalarN(
                          labelPtr,
                          GetImGuiDataType<T>(),
                          vPtr,
                          v.Length,
                          vSpeed,
                          vMinPtr,
                          vMaxPtr,
                          formatPtr,
                          flags) != 0;
            label.Recycle();
            format.Recycle();
            return res;
        }
    }

    public static bool DragFloatRange2(
        ImU8String label, scoped ref float vCurrentMin,
        scoped ref float vCurrentMax, float vSpeed = 1.0f, float vMin = 0.0f, float vMax = 0.0f,
        ImU8String format = default,
        ImU8String formatMax = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (float* vCurrentMinPtr = &vCurrentMin)
        fixed (float* vCurrentMaxPtr = &vCurrentMax)
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference("%.3f"u8))
        fixed (byte* formatMaxPtr = &formatMax.GetPinnableNullTerminatedReference())
        {
            var res = ImGuiNative.DragFloatRange2(
                labelPtr,
                vCurrentMinPtr,
                vCurrentMaxPtr,
                vSpeed,
                vMin,
                vMax,
                formatPtr,
                formatMaxPtr,
                flags);
            label.Recycle();
            format.Recycle();
            formatMax.Recycle();
            return res != 0;
        }
    }

    public static bool DragIntRange2(
        ImU8String label, scoped ref int vCurrentMin,
        scoped ref int vCurrentMax, float vSpeed = 1.0f, int vMin = 0, int vMax = 0,
        ImU8String format = default,
        ImU8String formatMax = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* vCurrentMinPtr = &vCurrentMin)
        fixed (int* vCurrentMaxPtr = &vCurrentMax)
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference("%d"u8))
        fixed (byte* formatMaxPtr = &formatMax.GetPinnableNullTerminatedReference())
        {
            var res = ImGuiNative.DragIntRange2(
                labelPtr,
                vCurrentMinPtr,
                vCurrentMaxPtr,
                vSpeed,
                vMin,
                vMax,
                formatPtr,
                formatMaxPtr,
                flags);
            label.Recycle();
            format.Recycle();
            formatMax.Recycle();
            return res != 0;
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static bool DragSByte(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref sbyte v, float vSpeed = 1.0f, sbyte vMin = 0, sbyte vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.S8, ref v, vSpeed, vMin, vMax, format.MoveOrDefault("%hhd"u8), flags);

    public static bool DragSByte(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<sbyte> v, float vSpeed = 1.0f, sbyte vMin = 0, sbyte vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.S8, v, vSpeed, vMin, vMax, format.MoveOrDefault("%hhd"u8), flags);

    public static bool DragByte(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref byte v, float vSpeed = 1.0f, byte vMin = 0, byte vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.U8, ref v, vSpeed, vMin, vMax, format.MoveOrDefault("%hhu"u8), flags);

    public static bool DragByte(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<byte> v, float vSpeed = 1.0f, byte vMin = 0, byte vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.U8, v, vSpeed, vMin, vMax, format.MoveOrDefault("%hhu"u8), flags);

    public static bool DragShort(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref short v, float vSpeed = 1.0f, short vMin = 0, short vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.S16, ref v, vSpeed, vMin, vMax, format.MoveOrDefault("%hd"u8), flags);

    public static bool DragShort(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<short> v, float vSpeed = 1.0f, short vMin = 0, short vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.S16, v, vSpeed, vMin, vMax, format.MoveOrDefault("%hd"u8), flags);

    public static bool DragUShort(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref ushort v, float vSpeed = 1.0f, ushort vMin = 0, ushort vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.U16, ref v, vSpeed, vMin, vMax, format.MoveOrDefault("%hu"u8), flags);

    public static bool DragUShort(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<ushort> v, float vSpeed = 1.0f, ushort vMin = 0, ushort vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.U16, v, vSpeed, vMin, vMax, format.MoveOrDefault("%hu"u8), flags);

    public static bool DragInt(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref int v, float vSpeed = 1.0f, int vMin = 0, int vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.S32, ref v, vSpeed, vMin, vMax, format.MoveOrDefault("%d"u8), flags);

    public static bool DragInt(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<int> v, float vSpeed = 1.0f, int vMin = 0, int vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.S32, v, vSpeed, vMin, vMax, format.MoveOrDefault("%d"u8), flags);

    public static bool DragUInt(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref uint v, float vSpeed = 1.0f, uint vMin = 0, uint vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.U32, ref v, vSpeed, vMin, vMax, format.MoveOrDefault("%u"u8), flags);

    public static bool DragUInt(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<uint> v, float vSpeed = 1.0f, uint vMin = 0, uint vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.U32, v, vSpeed, vMin, vMax, format.MoveOrDefault("%u"u8), flags);

    public static bool DragLong(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref long v, float vSpeed = 1.0f, long vMin = 0, long vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.S64, ref v, vSpeed, vMin, vMax, format.MoveOrDefault("%I64d"u8), flags);

    public static bool DragLong(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<long> v, float vSpeed = 1.0f, long vMin = 0, long vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.S64, v, vSpeed, vMin, vMax, format.MoveOrDefault("%I64d"u8), flags);

    public static bool DragULong(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref ulong v, float vSpeed = 1.0f, ulong vMin = 0, ulong vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.U64, ref v, vSpeed, vMin, vMax, format.MoveOrDefault("%I64u"u8), flags);

    public static bool DragULong(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<ulong> v, float vSpeed = 1.0f, ulong vMin = 0, ulong vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.U64, v, vSpeed, vMin, vMax, format.MoveOrDefault("%I64u"u8), flags);

    public static bool DragFloat(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref float v, float vSpeed = 1.0f, float vMin = 0.0f, float vMax = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.Float, ref v, vSpeed, vMin, vMax, format.MoveOrDefault("%.3f"u8), flags);

    public static bool DragFloat(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<float> v, float vSpeed = 1.0f, float vMin = 0.0f, float vMax = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.Float, v, vSpeed, vMin, vMax, format.MoveOrDefault("%.3f"u8), flags);

    public static bool DragFloat2(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref Vector2 v, float vSpeed = 1.0f, float vMin = 0.0f, float vMax = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(
            label,
            ImGuiDataType.Float,
            MemoryMarshal.Cast<Vector2, float>(new(ref v)),
            vSpeed,
            vMin,
            vMax,
            format.MoveOrDefault("%.3f"u8),
            flags);

    public static bool DragFloat3(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref Vector3 v, float vSpeed = 1.0f, float vMin = 0.0f, float vMax = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(
            label,
            ImGuiDataType.Float,
            MemoryMarshal.Cast<Vector3, float>(new(ref v)),
            vSpeed,
            vMin,
            vMax,
            format.MoveOrDefault("%.3f"u8),
            flags);

    public static bool DragFloat4(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref Vector4 v, float vSpeed = 1.0f, float vMin = 0.0f, float vMax = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(
            label,
            ImGuiDataType.Float,
            MemoryMarshal.Cast<Vector4, float>(new(ref v)),
            vSpeed,
            vMin,
            vMax,
            format.MoveOrDefault("%.3f"u8),
            flags);

    public static bool DragDouble(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref double v, float vSpeed = 1.0f, double vMin = 0.0f, double vMax = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.Double, ref v, vSpeed, vMin, vMax, format.MoveOrDefault("%.3f"u8), flags);

    public static bool DragDouble(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<double> v, float vSpeed = 1.0f, double vMin = 0.0f, double vMax = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        DragScalar(label, ImGuiDataType.Double, v, vSpeed, vMin, vMax, format.MoveOrDefault("%.3f"u8), flags);

    public static bool DragScalar<T>(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, ImGuiDataType dataType, scoped ref T v, float vSpeed, scoped in T vMin, scoped in T vMax,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        where T : unmanaged, INumber<T>, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = label.NullTerminatedSpan)
        fixed (byte* formatPtr = format.IsInitialized ? format.NullTerminatedSpan : null)
        fixed (T* vPtr = &v)
        fixed (T* vMinPtr = &vMin)
        fixed (T* vMaxPtr = &vMax)
        {
            var res = ImGuiNative.DragScalar(
                          labelPtr,
                          dataType,
                          vPtr,
                          vSpeed,
                          vMinPtr,
                          vMaxPtr,
                          formatPtr,
                          flags) != 0;
            label.Dispose();
            format.Dispose();
            return res;
        }
    }

    public static bool DragScalar<T>(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, ImGuiDataType dataType, Span<T> v, float vSpeed, scoped in T vMin, scoped in T vMax,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        where T : unmanaged, INumber<T>, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = label.NullTerminatedSpan)
        fixed (byte* formatPtr = format.IsInitialized ? format.NullTerminatedSpan : null)
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
            label.Dispose();
            format.Dispose();
            return res;
        }
    }

    public static bool DragFloatRange2(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, ref float vCurrentMin, ref float vCurrentMax, float vSpeed = 1.0f, float vMin = 0.0f,
        float vMax = 0.0f, [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, [InterpolatedStringHandlerArgument] AutoUtf8Buffer formatMax = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        fixed (byte* labelPtr = label.NullTerminatedSpan)
        fixed (float* vCurrentMinPtr = &vCurrentMin)
        fixed (float* vCurrentMaxPtr = &vCurrentMax)
        fixed (byte* formatPtr = format.IsInitialized ? format.NullTerminatedSpan : "%.3f"u8)
        fixed (byte* formatMaxPtr = formatMax.IsInitialized ? formatMax.NullTerminatedSpan : null)
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
            label.Dispose();
            format.Dispose();
            formatMax.Dispose();
            return res != 0;
        }
    }

    public static bool DragIntRange2(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, ref int vCurrentMin, ref int vCurrentMax, float vSpeed = 1.0f, int vMin = 0, int vMax = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, [InterpolatedStringHandlerArgument] AutoUtf8Buffer formatMax = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        fixed (byte* labelPtr = label.NullTerminatedSpan)
        fixed (int* vCurrentMinPtr = &vCurrentMin)
        fixed (int* vCurrentMaxPtr = &vCurrentMax)
        fixed (byte* formatPtr = format.IsInitialized ? format.NullTerminatedSpan : "%d"u8)
        fixed (byte* formatMaxPtr = formatMax.IsInitialized ? formatMax.NullTerminatedSpan : null)
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
            label.Dispose();
            format.Dispose();
            formatMax.Dispose();
            return res != 0;
        }
    }
}

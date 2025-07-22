using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static bool SliderSByte(
        Utf8Buffer label, scoped ref sbyte v, sbyte vMin = 0, sbyte vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.S8,
        ref v,
        vMin,
        vMax,
        format.MoveOrDefault("%hhd"u8),
        flags);

    public static bool SliderSByte(
        Utf8Buffer label, Span<sbyte> v, sbyte vMin = 0, sbyte vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.S8,
        v,
        vMin,
        vMax,
        format.MoveOrDefault("%hhd"u8),
        flags);

    public static bool SliderByte(
        Utf8Buffer label, scoped ref byte v, byte vMin = 0, byte vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.U8,
        ref v,
        vMin,
        vMax,
        format.MoveOrDefault("%hhu"u8),
        flags);

    public static bool SliderByte(
        Utf8Buffer label, Span<byte> v, byte vMin = 0, byte vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.U8,
        v,
        vMin,
        vMax,
        format.MoveOrDefault("%hhu"u8),
        flags);

    public static bool SliderShort(
        Utf8Buffer label, scoped ref short v, short vMin = 0, short vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.S16,
        ref v,
        vMin,
        vMax,
        format.MoveOrDefault("%hd"u8),
        flags);

    public static bool SliderShort(
        Utf8Buffer label, Span<short> v, short vMin = 0, short vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.S16,
        v,
        vMin,
        vMax,
        format.MoveOrDefault("%hd"u8),
        flags);

    public static bool SliderUShort(
        Utf8Buffer label, scoped ref ushort v, ushort vMin = 0, ushort vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.U16,
        ref v,
        vMin,
        vMax,
        format.MoveOrDefault("%hu"u8),
        flags);

    public static bool SliderUShort(
        Utf8Buffer label, Span<ushort> v, ushort vMin = 0, ushort vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.U16,
        v,
        vMin,
        vMax,
        format.MoveOrDefault("%hu"u8),
        flags);

    public static bool SliderInt(
        Utf8Buffer label, scoped ref int v, int vMin = 0, int vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.S32,
        ref v,
        vMin,
        vMax,
        format.MoveOrDefault("%d"u8),
        flags);

    public static bool SliderInt(
        Utf8Buffer label, Span<int> v, int vMin = 0, int vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.S32,
        v,
        vMin,
        vMax,
        format.MoveOrDefault("%d"u8),
        flags);

    public static bool SliderUInt(
        Utf8Buffer label, scoped ref uint v, uint vMin = 0, uint vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.U32,
        ref v,
        vMin,
        vMax,
        format.MoveOrDefault("%u"u8),
        flags);

    public static bool SliderUInt(
        Utf8Buffer label, Span<uint> v, uint vMin = 0, uint vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.U32,
        v,
        vMin,
        vMax,
        format.MoveOrDefault("%u"u8),
        flags);

    public static bool SliderLong(
        Utf8Buffer label, scoped ref long v, long vMin = 0, long vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.S64,
        ref v,
        vMin,
        vMax,
        format.MoveOrDefault("%I64d"u8),
        flags);

    public static bool SliderLong(
        Utf8Buffer label, Span<long> v, long vMin = 0, long vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.S64,
        v,
        vMin,
        vMax,
        format.MoveOrDefault("%I64d"u8),
        flags);

    public static bool SliderULong(
        Utf8Buffer label, scoped ref ulong v, ulong vMin = 0, ulong vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.U64,
        ref v,
        vMin,
        vMax,
        format.MoveOrDefault("%I64u"u8),
        flags);

    public static bool SliderULong(
        Utf8Buffer label, Span<ulong> v, ulong vMin = 0, ulong vMax = 0, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.U64,
        v,
        vMin,
        vMax,
        format.MoveOrDefault("%I64u"u8),
        flags);

    public static bool SliderFloat(
        Utf8Buffer label, scoped ref float v, float vMin = 0.0f, float vMax = 0.0f, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.Float,
        ref v,
        vMin,
        vMax,
        format.MoveOrDefault("%.3f"u8),
        flags);

    public static bool SliderFloat(
        Utf8Buffer label, Span<float> v, float vMin = 0.0f, float vMax = 0.0f, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.Float,
        v,
        vMin,
        vMax,
        format.MoveOrDefault("%.3f"u8),
        flags);

    public static bool SliderFloat2(
        Utf8Buffer label, scoped ref Vector2 v, float vMin = 0.0f, float vMax = 0.0f,
        Utf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) => SliderScalar(
        label,
        ImGuiDataType.Float,
        MemoryMarshal.Cast<Vector2, float>(new(ref v)),
        vMin,
        vMax,
        format.MoveOrDefault("%.3f"u8),
        flags);

    public static bool SliderFloat3(
        Utf8Buffer label, scoped ref Vector3 v, float vMin = 0.0f, float vMax = 0.0f,
        Utf8Buffer format = default, ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        SliderScalar(
            label,
            ImGuiDataType.Float,
            MemoryMarshal.Cast<Vector3, float>(new(ref v)),
            vMin,
            vMax,
            format.MoveOrDefault("%.3f"u8),
            flags);

    public static bool SliderFloat4(
        Utf8Buffer label, scoped ref Vector4 v, float vMin = 0.0f,
        float vMax = 0.0f,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        SliderScalar(
            label,
            ImGuiDataType.Float,
            MemoryMarshal.Cast<Vector4, float>(new(ref v)),
            vMin,
            vMax,
            format.MoveOrDefault("%.3f"u8),
            flags);

    public static bool SliderDouble(
        Utf8Buffer label, scoped ref double v, double vMin = 0.0f,
        double vMax = 0.0f,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        SliderScalar(label, ImGuiDataType.Double, ref v, vMin, vMax, format.MoveOrDefault("%.3f"u8), flags);

    public static bool SliderDouble(
        Utf8Buffer label, Span<double> v, double vMin = 0.0f,
        double vMax = 0.0f,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        SliderScalar(label, ImGuiDataType.Double, v, vMin, vMax, format.MoveOrDefault("%.3f"u8), flags);

    public static bool SliderScalar<T>(
        Utf8Buffer label, ImGuiDataType dataType, scoped ref T v,
        scoped in T vMin, scoped in T vMax,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* vPtr = &v)
        fixed (T* vMinPtr = &vMin)
        fixed (T* vMaxPtr = &vMax)
        {
            var res = ImGuiNative.SliderScalar(
                          labelPtr,
                          dataType,
                          vPtr,
                          vMinPtr,
                          vMaxPtr,
                          formatPtr,
                          flags) != 0;
            label.Dispose();
            format.Dispose();
            return res;
        }
    }

    public static bool SliderScalar<T>(
        Utf8Buffer label, ImGuiDataType dataType, Span<T> v, scoped in T vMin,
        scoped in T vMax,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        where T : unmanaged, INumber<T>, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* vPtr = v)
        fixed (T* vMinPtr = &vMin)
        fixed (T* vMaxPtr = &vMax)
        {
            var res = ImGuiNative.SliderScalarN(
                          labelPtr,
                          dataType,
                          vPtr,
                          v.Length,
                          vMinPtr,
                          vMaxPtr,
                          formatPtr,
                          flags) != 0;
            label.Dispose();
            format.Dispose();
            return res;
        }
    }

    public static bool SliderAngle(
        Utf8Buffer label, ref float vRad, float vDegreesMin = -360.0f,
        float vDegreesMax = +360.0f,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference("%.0f deg"u8))
        fixed (float* vRadPtr = &vRad)
        {
            var res = ImGuiNative.SliderAngle(
                          labelPtr,
                          vRadPtr,
                          vDegreesMin,
                          vDegreesMax,
                          formatPtr,
                          flags) != 0;
            label.Dispose();
            format.Dispose();
            return res;
        }
    }

    public static bool VSliderSByte(
        Utf8Buffer label, Vector2 size, scoped ref sbyte v, sbyte vMin,
        sbyte vMax, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        VSliderScalar(label, size, ImGuiDataType.S8, ref v, vMin, vMax, format.MoveOrDefault("%hhd"), flags);

    public static bool VSliderByte(
        Utf8Buffer label, Vector2 size, scoped ref byte v, byte vMin, byte vMax,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        VSliderScalar(label, size, ImGuiDataType.U8, ref v, vMin, vMax, format.MoveOrDefault("%hhu"), flags);

    public static bool VSliderShort(
        Utf8Buffer label, Vector2 size, scoped ref short v, short vMin,
        short vMax, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        VSliderScalar(label, size, ImGuiDataType.S16, ref v, vMin, vMax, format.MoveOrDefault("%hd"), flags);

    public static bool VSliderUShort(
        Utf8Buffer label, Vector2 size, scoped ref ushort v, ushort vMin,
        ushort vMax,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        VSliderScalar(label, size, ImGuiDataType.U16, ref v, vMin, vMax, format.MoveOrDefault("%hu"), flags);

    public static bool VSliderInt(
        Utf8Buffer label, Vector2 size, scoped ref int v, int vMin, int vMax,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        VSliderScalar(label, size, ImGuiDataType.S32, ref v, vMin, vMax, format.MoveOrDefault("%d"), flags);

    public static bool VSliderUInt(
        Utf8Buffer label, Vector2 size, scoped ref uint v, uint vMin, uint vMax,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        VSliderScalar(label, size, ImGuiDataType.U32, ref v, vMin, vMax, format.MoveOrDefault("%u"), flags);

    public static bool VSliderLong(
        Utf8Buffer label, Vector2 size, scoped ref long v, long vMin, long vMax,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        VSliderScalar(label, size, ImGuiDataType.S32, ref v, vMin, vMax, format.MoveOrDefault("%I64d"), flags);

    public static bool VSliderULong(
        Utf8Buffer label, Vector2 size, scoped ref ulong v, ulong vMin,
        ulong vMax, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        VSliderScalar(label, size, ImGuiDataType.U32, ref v, vMin, vMax, format.MoveOrDefault("%I64u"), flags);

    public static bool VSliderFloat(
        Utf8Buffer label, Vector2 size, scoped ref float v, float vMin,
        float vMax, Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        VSliderScalar(label, size, ImGuiDataType.Float, ref v, vMin, vMax, format.MoveOrDefault("%.03f"), flags);

    public static bool VSliderDouble(
        Utf8Buffer label, Vector2 size, scoped ref double v, double vMin,
        double vMax,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) =>
        VSliderScalar(label, size, ImGuiDataType.Double, ref v, vMin, vMax, format.MoveOrDefault("%.03f"), flags);

    public static bool VSliderScalar<T>(
        Utf8Buffer label, Vector2 size, ImGuiDataType dataType,
        scoped ref T data, scoped in T min, scoped in T max,
        Utf8Buffer format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* dataPtr = &data)
        fixed (T* minPtr = &min)
        fixed (T* maxPtr = &max)
        {
            var res = ImGuiNative.VSliderScalar(labelPtr, size, dataType, dataPtr, minPtr, maxPtr, formatPtr, flags) !=
                      0;
            label.Dispose();
            format.Dispose();
            return res;
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static bool InputSByte(
        ImU8String label, scoped ref sbyte data, sbyte step = 0, sbyte stepFast = 0,
        ImU8String format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.S8,
        ref data,
        step,
        stepFast,
        format.MoveOrDefault("%hhd"u8),
        flags);

    public static bool InputSByte(
        ImU8String label, Span<sbyte> data, sbyte step = 0, sbyte stepFast = 0, ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.S8,
        data,
        step,
        stepFast,
        format.MoveOrDefault("%hhd"u8),
        flags);

    public static bool InputByte(
        ImU8String label, scoped ref byte data, byte step = 0, byte stepFast = 0, ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.U8,
        ref data,
        step,
        stepFast,
        format.MoveOrDefault("%hhu"u8),
        flags);

    public static bool InputByte(
        ImU8String label, Span<byte> data, byte step = 0, byte stepFast = 0, ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.U8,
        data,
        step,
        stepFast,
        format.MoveOrDefault("%hhu"u8),
        flags);

    public static bool InputShort(
        ImU8String label, scoped ref short data, short step = 0, short stepFast = 0,
        ImU8String format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.S16,
        ref data,
        step,
        stepFast,
        format.MoveOrDefault("%hd"u8),
        flags);

    public static bool InputShort(
        ImU8String label, Span<short> data, short step = 0, short stepFast = 0, ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.S16,
        data,
        step,
        stepFast,
        format.MoveOrDefault("%hd"u8),
        flags);

    public static bool InputUShort(
        ImU8String label, scoped ref ushort data, ushort step = 0, ushort stepFast = 0,
        ImU8String format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.U16,
        ref data,
        step,
        stepFast,
        format.MoveOrDefault("%hu"u8),
        flags);

    public static bool InputUShort(
        ImU8String label, Span<ushort> data, ushort step = 0, ushort stepFast = 0, ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.U16,
        data,
        step,
        stepFast,
        format.MoveOrDefault("%hu"u8),
        flags);

    public static bool InputInt(
        ImU8String label, scoped ref int data, int step = 0, int stepFast = 0, ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.S32,
        ref data,
        step,
        stepFast,
        format.MoveOrDefault("%d"u8),
        flags);

    public static bool InputInt(
        ImU8String label, Span<int> data, int step = 0, int stepFast = 0, ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.S32,
        data,
        step,
        stepFast,
        format.MoveOrDefault("%d"u8),
        flags);

    public static bool InputUInt(
        ImU8String label, scoped ref uint data, uint step = 0, uint stepFast = 0, ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.U32,
        ref data,
        step,
        stepFast,
        format.MoveOrDefault("%u"u8),
        flags);

    public static bool InputUInt(
        ImU8String label, Span<uint> data, uint step = 0, uint stepFast = 0, ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.U32,
        data,
        step,
        stepFast,
        format.MoveOrDefault("%u"u8),
        flags);

    public static bool InputLong(
        ImU8String label, scoped ref long data, long step = 0, long stepFast = 0, ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.S64,
        ref data,
        step,
        stepFast,
        format.MoveOrDefault("%I64d"u8),
        flags);

    public static bool InputLong(
        ImU8String label, Span<long> data, long step = 0, long stepFast = 0, ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.S64,
        data,
        step,
        stepFast,
        format.MoveOrDefault("%I64d"u8),
        flags);

    public static bool InputULong(
        ImU8String label, scoped ref ulong data, ulong step = 0, ulong stepFast = 0,
        ImU8String format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.U64,
        ref data,
        step,
        stepFast,
        format.MoveOrDefault("%I64u"u8),
        flags);

    public static bool InputULong(
        ImU8String label, Span<ulong> data, ulong step = 0, ulong stepFast = 0, ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) => InputScalar(
        label,
        ImGuiDataType.U64,
        data,
        step,
        stepFast,
        format.MoveOrDefault("%I64u"u8),
        flags);

    public static bool InputFloat(
        ImU8String label, scoped ref float data, float step = 0.0f,
        float stepFast = 0.0f,
        ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.Float, ref data, step, stepFast, format.MoveOrDefault("%.3f"u8), flags);

    public static bool InputFloat(
        ImU8String label, Span<float> data, float step = 0.0f,
        float stepFast = 0.0f,
        ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.Float, data, step, stepFast, format.MoveOrDefault("%.3f"u8), flags);

    public static bool InputFloat2(
        ImU8String label, scoped ref Vector2 data, float step = 0.0f,
        float stepFast = 0.0f,
        ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(
            label,
            ImGuiDataType.Float,
            MemoryMarshal.Cast<Vector2, float>(new(ref data)),
            step,
            stepFast,
            format.MoveOrDefault("%.3f"u8),
            flags);

    public static bool InputFloat3(
        ImU8String label, scoped ref Vector3 data, float step = 0.0f,
        float stepFast = 0.0f,
        ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(
            label,
            ImGuiDataType.Float,
            MemoryMarshal.Cast<Vector3, float>(new(ref data)),
            step,
            stepFast,
            format.MoveOrDefault("%.3f"u8),
            flags);

    public static bool InputFloat4(
        ImU8String label, scoped ref Vector4 data, float step = 0.0f,
        float stepFast = 0.0f,
        ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(
            label,
            ImGuiDataType.Float,
            MemoryMarshal.Cast<Vector4, float>(new(ref data)),
            step,
            stepFast,
            format.MoveOrDefault("%.3f"u8),
            flags);

    public static bool InputDouble(
        ImU8String label, scoped ref double data, double step = 0.0f,
        double stepFast = 0.0f,
        ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.Double, ref data, step, stepFast, format.MoveOrDefault("%.3f"u8), flags);

    public static bool InputDouble(
        ImU8String label, Span<double> data, double step = 0.0f,
        double stepFast = 0.0f,
        ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.Double, data, step, stepFast, format.MoveOrDefault("%.3f"u8), flags);

    public static bool InputScalar<T>(
        ImU8String label, ImGuiDataType dataType, scoped ref T data,
        scoped in T step, scoped in T stepFast,
        ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* dataPtr = &data)
        fixed (T* stepPtr = &step)
        fixed (T* stepFastPtr = &stepFast)
        {
            var res = ImGuiNative.InputScalar(
                          labelPtr,
                          dataType,
                          dataPtr,
                          step > T.Zero ? stepPtr : null,
                          stepFast > T.Zero ? stepFastPtr : null,
                          formatPtr,
                          flags) != 0;
            label.Recycle();
            format.Recycle();
            return res;
        }
    }

    public static bool InputScalar<T>(
        ImU8String label, ImGuiDataType dataType, Span<T> data,
        scoped in T step, scoped in T stepFast,
        ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* dataPtr = data)
        fixed (T* stepPtr = &step)
        fixed (T* stepFastPtr = &stepFast)
        {
            var res = ImGuiNative.InputScalarN(
                          labelPtr,
                          dataType,
                          dataPtr,
                          data.Length,
                          step > T.Zero ? stepPtr : null,
                          stepFast > T.Zero ? stepFastPtr : null,
                          formatPtr,
                          flags) != 0;
            label.Recycle();
            format.Recycle();
            return res;
        }
    }

    public static bool InputScalar<T>(
        ImU8String label, scoped ref T data,
        scoped in T step, scoped in T stepFast,
        ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* dataPtr = &data)
        fixed (T* stepPtr = &step)
        fixed (T* stepFastPtr = &stepFast)
        {
            var res = ImGuiNative.InputScalar(
                          labelPtr,
                          GetImGuiDataType<T>(),
                          dataPtr,
                          step > T.Zero ? stepPtr : null,
                          stepFast > T.Zero ? stepFastPtr : null,
                          formatPtr,
                          flags) != 0;
            label.Recycle();
            format.Recycle();
            return res;
        }
    }

    public static bool InputScalar<T>(
        ImU8String label, Span<T> data,
        scoped in T step, scoped in T stepFast,
        ImU8String format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* formatPtr = &format.GetPinnableNullTerminatedReference())
        fixed (T* dataPtr = data)
        fixed (T* stepPtr = &step)
        fixed (T* stepFastPtr = &stepFast)
        {
            var res = ImGuiNative.InputScalarN(
                          labelPtr,
                          GetImGuiDataType<T>(),
                          dataPtr,
                          data.Length,
                          step > T.Zero ? stepPtr : null,
                          stepFast > T.Zero ? stepFastPtr : null,
                          formatPtr,
                          flags) != 0;
            label.Recycle();
            format.Recycle();
            return res;
        }
    }
}

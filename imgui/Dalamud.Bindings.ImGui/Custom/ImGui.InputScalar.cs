using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static bool InputSByte(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref sbyte data, sbyte step = 0, sbyte stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.S8, ref data, step, stepFast, format.MoveOrDefault("%hhd"u8), flags);

    public static bool InputSByte(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<sbyte> data, sbyte step = 0, sbyte stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.S8, data, step, stepFast, format.MoveOrDefault("%hhd"u8), flags);

    public static bool InputByte(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref byte data, byte step = 0, byte stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.U8, ref data, step, stepFast, format.MoveOrDefault("%hhu"u8), flags);

    public static bool InputByte(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<byte> data, byte step = 0, byte stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.U8, data, step, stepFast, format.MoveOrDefault("%hhu"u8), flags);

    public static bool InputShort(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref short data, short step = 0, short stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.S16, ref data, step, stepFast, format.MoveOrDefault("%hd"u8), flags);

    public static bool InputShort(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<short> data, short step = 0, short stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.S16, data, step, stepFast, format.MoveOrDefault("%hd"u8), flags);

    public static bool InputUShort(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref ushort data, ushort step = 0, ushort stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.U16, ref data, step, stepFast, format.MoveOrDefault("%hu"u8), flags);

    public static bool InputUShort(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<ushort> data, ushort step = 0, ushort stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.U16, data, step, stepFast, format.MoveOrDefault("%hu"u8), flags);

    public static bool InputInt(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref int data, int step = 0, int stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.S32, ref data, step, stepFast, format.MoveOrDefault("%d"u8), flags);

    public static bool InputInt(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<int> data, int step = 0, int stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.S32, data, step, stepFast, format.MoveOrDefault("%d"u8), flags);

    public static bool InputUInt(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref uint data, uint step = 0, uint stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.U32, ref data, step, stepFast, format.MoveOrDefault("%u"u8), flags);

    public static bool InputUInt(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<uint> data, uint step = 0, uint stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.U32, data, step, stepFast, format.MoveOrDefault("%u"u8), flags);

    public static bool InputLong(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref long data, long step = 0, long stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.S64, ref data, step, stepFast, format.MoveOrDefault("%I64d"u8), flags);

    public static bool InputLong(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<long> data, long step = 0, long stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.S64, data, step, stepFast, format.MoveOrDefault("%I64d"u8), flags);

    public static bool InputULong(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref ulong data, ulong step = 0, ulong stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.U64, ref data, step, stepFast, format.MoveOrDefault("%I64u"u8), flags);

    public static bool InputULong(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<ulong> data, ulong step = 0, ulong stepFast = 0,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.U64, data, step, stepFast, format.MoveOrDefault("%I64u"u8), flags);

    public static bool InputFloat(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref float data, float step = 0.0f, float stepFast = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.Float, ref data, step, stepFast, format.MoveOrDefault("%.3f"u8), flags);

    public static bool InputFloat(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<float> data, float step = 0.0f, float stepFast = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.Float, data, step, stepFast, format.MoveOrDefault("%.3f"u8), flags);

    public static bool InputFloat2(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref Vector2 data, float step = 0.0f, float stepFast = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(
            label,
            ImGuiDataType.Float,
            MemoryMarshal.Cast<Vector2, float>(new(ref data)),
            step,
            stepFast,
            format.MoveOrDefault("%.3f"u8),
            flags);

    public static bool InputFloat3(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref Vector3 data, float step = 0.0f, float stepFast = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(
            label,
            ImGuiDataType.Float,
            MemoryMarshal.Cast<Vector3, float>(new(ref data)),
            step,
            stepFast,
            format.MoveOrDefault("%.3f"u8),
            flags);

    public static bool InputFloat4(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref Vector4 data, float step = 0.0f, float stepFast = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(
            label,
            ImGuiDataType.Float,
            MemoryMarshal.Cast<Vector4, float>(new(ref data)),
            step,
            stepFast,
            format.MoveOrDefault("%.3f"u8),
            flags);

    public static bool InputDouble(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, scoped ref double data, double step = 0.0f, double stepFast = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.Double, ref data, step, stepFast, format.MoveOrDefault("%.3f"u8), flags);

    public static bool InputDouble(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, Span<double> data, double step = 0.0f, double stepFast = 0.0f,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
        InputScalar(label, ImGuiDataType.Double, data, step, stepFast, format.MoveOrDefault("%.3f"u8), flags);

    public static bool InputScalar<T>(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, ImGuiDataType dataType, scoped ref T data, scoped in T step, scoped in T stepFast,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        where T : unmanaged, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = label.NullTerminatedSpan)
        fixed (byte* formatPtr = format.IsInitialized ? format.NullTerminatedSpan : null)
        fixed (T* dataPtr = &data)
        fixed (T* stepPtr = &step)
        fixed (T* stepFastPtr = &stepFast)
        {
            var res = ImGuiNative.InputScalar(
                          labelPtr,
                          dataType,
                          dataPtr,
                          stepPtr,
                          stepFastPtr,
                          formatPtr,
                          flags) != 0;
            label.Dispose();
            format.Dispose();
            return res;
        }
    }

    public static bool InputScalar<T>(
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer label, ImGuiDataType dataType, Span<T> data, scoped in T step, scoped in T stepFast,
        [InterpolatedStringHandlerArgument] AutoUtf8Buffer format = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        where T : unmanaged, INumber<T>, IBinaryNumber<T>
    {
        fixed (byte* labelPtr = label.NullTerminatedSpan)
        fixed (byte* formatPtr = format.IsInitialized ? format.NullTerminatedSpan : null)
        fixed (T* dataPtr = data)
        fixed (T* stepPtr = &step)
        fixed (T* stepFastPtr = &stepFast)
        {
            var res = ImGuiNative.InputScalarN(
                          labelPtr,
                          dataType,
                          dataPtr,
                          data.Length,
                          stepPtr,
                          stepFastPtr,
                          formatPtr,
                          flags) != 0;
            label.Dispose();
            format.Dispose();
            return res;
        }
    }
}

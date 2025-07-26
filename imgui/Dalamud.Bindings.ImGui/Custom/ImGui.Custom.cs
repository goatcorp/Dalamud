using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

public unsafe partial class ImGui
{
    public static int GetImGuiDataTypeSize(ImGuiDataType dataType) => dataType switch
    {
        ImGuiDataType.S8 => sizeof(sbyte),
        ImGuiDataType.U8 => sizeof(byte),
        ImGuiDataType.S16 => sizeof(short),
        ImGuiDataType.U16 => sizeof(ushort),
        ImGuiDataType.S32 => sizeof(int),
        ImGuiDataType.U32 => sizeof(uint),
        ImGuiDataType.S64 => sizeof(long),
        ImGuiDataType.U64 => sizeof(ulong),
        ImGuiDataType.Float => sizeof(float),
        ImGuiDataType.Double => sizeof(double),
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
    };

    public static ImGuiDataType GetImGuiDataType(Type type)
    {
        if (type == typeof(sbyte)) return ImGuiDataType.S8;
        if (type == typeof(byte)) return ImGuiDataType.U8;
        if (type == typeof(short)) return ImGuiDataType.S16;
        if (type == typeof(ushort)) return ImGuiDataType.U16;
        if (type == typeof(int)) return ImGuiDataType.S32;
        if (type == typeof(uint)) return ImGuiDataType.U32;
        if (type == typeof(long)) return ImGuiDataType.S64;
        if (type == typeof(ulong)) return ImGuiDataType.U64;
        if (type == typeof(float)) return ImGuiDataType.Float;
        if (type == typeof(double)) return ImGuiDataType.Double;
        throw new ArgumentOutOfRangeException(nameof(type), type, null);
    }

    public static ImGuiDataType GetImGuiDataType<T>() => GetImGuiDataType(typeof(T));

    public static string GetFormatSpecifier(ImGuiDataType dataType) => dataType switch
    {
        ImGuiDataType.S8 => "%hhd",
        ImGuiDataType.U8 => "%hhu",
        ImGuiDataType.S16 => "%hd",
        ImGuiDataType.U16 => "%hu",
        ImGuiDataType.S32 => "%d",
        ImGuiDataType.U32 => "%u",
        ImGuiDataType.S64 => "%I64d",
        ImGuiDataType.U64 => "%I64u",
        ImGuiDataType.Float => "%f",
        ImGuiDataType.Double => "%lf",
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
    };

    public static ReadOnlySpan<byte> GetFormatSpecifierU8(ImGuiDataType dataType) => dataType switch
    {
        ImGuiDataType.S8 => "%hhd"u8,
        ImGuiDataType.U8 => "%hhu"u8,
        ImGuiDataType.S16 => "%hd"u8,
        ImGuiDataType.U16 => "%hu"u8,
        ImGuiDataType.S32 => "%d"u8,
        ImGuiDataType.U32 => "%u"u8,
        ImGuiDataType.S64 => "%I64d"u8,
        ImGuiDataType.U64 => "%I64u"u8,
        ImGuiDataType.Float => "%f"u8,
        ImGuiDataType.Double => "%lf"u8,
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
    };
}

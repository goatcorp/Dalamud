using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using InteropGenerator.Runtime;
using InteropGenerator.Runtime.Attributes;
// ReSharper disable LoopCanBeConvertedToQuery
// Linq can be a performance hit in most cases
namespace Dalamud.Utility;

public static partial class TypeExtensions {
    private static readonly BindingFlags BindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    [GeneratedRegex(@"^`[1-9]\+Node", RegexOptions.Compiled)]
    public static partial Regex StdNodeRegex();

    public static bool IsFixedBuffer(this Type type) {
        return type.Name.EndsWith("e__FixedBuffer");
    }

    public static bool IsStruct(this Type type) {
        return type != typeof(decimal) && type is { IsValueType: true, IsPrimitive: false, IsEnum: false };
    }

    public static bool IsBaseType(this Type type) {
        while (true) {
            if (type.IsPointer) {
                type = type.GetElementType()!;
                continue;
            }
            return type == typeof(void) || type == typeof(bool) || type == typeof(char) ||
                   type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) ||
                   type == typeof(ushort) || type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) || type == typeof(float) ||
                   type == typeof(double) || type == typeof(decimal) || type == typeof(nint) ||
                   type == typeof(nuint) || type == typeof(Half) || type == typeof(CStringPointer);
        }
    }

    public static Type[] GetInheritsTypes(this Type type) {
        const string inheritsAttribute = "InheritsAttribute`1";
        Type[] inheritances = [];
        foreach (var attr in type.GetCustomAttributes()) {
            if (attr.GetType().Name.Contains(inheritsAttribute)) {
                inheritances = [.. inheritances, attr.GetType().GetGenericArguments()[0]];
            }
        }
        return inheritances;
    }

    public static bool IsInheritance(this Type type, FieldInfo field) {
        var inheritances = type.GetInheritsTypes();
        if (inheritances.Length == 0) return false;
        foreach (var inheritance in inheritances) {
            if (inheritance.IsFieldInType(field)) return true;
            if (IsInheritance(inheritance, field)) return true;
        }
        return false;
    }

    public static bool IsFieldInType(this Type type, FieldInfo field) {
        var nameStrings = field.Name.Split('_');
        var index = Array.IndexOf(nameStrings, type.Name);
        if (index <= 0) return type.GetFields(BindingFlags).Any(f => f.Name == field.Name && f.FieldType == field.FieldType);
        var name = string.Join("_", nameStrings[(index + 1)..]);
        return type.GetFields(BindingFlags).Any(f => f.Name == name && f.FieldType == field.FieldType) || type.GetFields(BindingFlags).Any(f => f.Name == field.Name && f.FieldType == field.FieldType);
    }

    public static bool IsDirectBase(this FieldInfo field) {
        var bases = field.DeclaringType?.GetInheritsTypes() ?? [];
        return bases.Any(b => field.FieldType == b && field.Name == (b.Name == field.DeclaringType?.Name ? b.Name + "Base" : b.Name));
    }

    public static int SizeOf(this Type type) {
        return type switch {
            _ when type == typeof(sbyte) || type == typeof(byte) || type == typeof(bool) => 1,
            _ when type == typeof(char) || type == typeof(short) || type == typeof(ushort) || type == typeof(Half) => 2,
            _ when type == typeof(int) || type == typeof(uint) || type == typeof(float) => 4,
            _ when type == typeof(long) || type == typeof(ulong) || type == typeof(double) || type.IsPointer || type.IsFunctionPointer || type.IsUnmanagedFunctionPointer || type == typeof(CStringPointer) => 8,
            _ when type.Name.StartsWith("FixedSizeArray") => type.GetGenericArguments()[0].SizeOf() * int.Parse(type.Name[14..type.Name.IndexOf('`')]),
            _ when type.GetCustomAttribute<InlineArrayAttribute>() is { Length: var length } => type.GetGenericArguments()[0].SizeOf() * length,
            _ when type.IsStruct() && !type.IsGenericType && (type.StructLayoutAttribute?.Value ?? LayoutKind.Sequential) != LayoutKind.Sequential => type.StructLayoutAttribute?.Size ?? (int?)typeof(Unsafe).GetMethod("SizeOf")?.MakeGenericMethod(type).Invoke(null, null) ?? 0,
            _ when type.IsEnum => Enum.GetUnderlyingType(type).SizeOf(),
            _ when type.IsGenericType => Marshal.SizeOf(Activator.CreateInstance(type)!),
            _ => GetSizeOf(type)
        };
    }

    private static int GetSizeOf(this Type type) {
        try {
            return Marshal.SizeOf(Activator.CreateInstance(type)!);
        } catch {
            return 0;
        }
    }

    public static string GetNamespace(this Type type) {
        var ns = type.Namespace!;
        var offset = ns.IndexOf('.', ns.IndexOf('.') + 1) + 1;
        return offset == 0 ? "" : ns[offset..];
    }

    public static string GetFullname(this Type type) {
        return type.Namespace + "." + type.Name;
    }

    public static Type GetPointerType(this Type type) {
        while (type.IsPointer()) {
            if (type.IsPointer) type = type.GetElementType()!;
            else if (type.IsFunctionPointer) type = type.GetFunctionPointerReturnType();
            else if (type.IsUnmanagedFunctionPointer) type = type.GetFunctionPointerReturnType();
        }
        return type;
    }

    public static bool IsPointer(this Type type) {
        return type.IsPointer || type.IsFunctionPointer || type.IsUnmanagedFunctionPointer;
    }

    public static int PackSize(this Type type) {
        if (type.GetCustomAttribute<FixedSizeArrayAttribute>() != null) return 1; // FixedSizeArrayAttribute is always packed to 1 as the generated struct gets generated with Pack = 1
        if (!type.IsStruct()) return type.SizeOf();
        var pack = type.StructLayoutAttribute?.Pack ?? 8;
        if (pack == 0) pack = 8;
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return fields.Max(t => Math.Min(pack, t.FieldType.PackSize()));
    }
}

public static class FieldInfoExtensions {
    public static int GetFieldOffset(this FieldInfo info) {
        var attrs = info.GetCustomAttributes(typeof(FieldOffsetAttribute), false);
        return attrs.Length != 0 ? attrs.Cast<FieldOffsetAttribute>().Single().Value : GetFieldOffsetSequential(info);
    }

    public static int GetFieldOffsetSequential(this FieldInfo info) {
        if (info.DeclaringType is not { } declaring)
            throw new Exception($"Unable to access declaring type of field {info.Name}");
        var pack = declaring.StructLayoutAttribute?.Pack ?? 0; // Default to 0 if no pack is specified
        var fields = declaring.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var offset = 0;
        foreach (var field in fields) {
            if (pack != 0) {
                var actualPack = Math.Min(pack, field.FieldType.PackSize());
                offset = (offset + actualPack - 1) / actualPack * actualPack;
            }
            if (field == info) {
                return offset;
            }
            offset += field.FieldType.SizeOf();
        }
        throw new Exception("Field not found");
    }
}
public static class Extensions {
    public static void WriteFile(this FileInfo file, string content) {
        using var stream = file.CreateText();
        stream.Write(content);
    }
}

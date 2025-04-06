using System.Runtime.CompilerServices;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility;

/// <summary>Represents any type of ImGui ID.</summary>
public readonly ref struct ImGuiId
{
    /// <summary>Type of the ID.</summary>
    public readonly Type IdType;

    /// <summary>Numeric ID. Valid if <see cref="IdType"/> is <see cref="Type.Numeric"/>.</summary>
    public readonly nint Numeric;

    /// <summary>UTF-16 string ID. Valid if <see cref="IdType"/> is <see cref="Type.U16"/>.</summary>
    public readonly ReadOnlySpan<char> U16;

    /// <summary>UTF-8 string ID. Valid if <see cref="IdType"/> is <see cref="Type.U8"/>.</summary>
    public readonly ReadOnlySpan<byte> U8;

    /// <summary>Initializes a new instance of the <see cref="ImGuiId"/> struct.</summary>
    /// <param name="id">A numeric ID, or 0 to not provide an ID.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImGuiId(nint id)
    {
        if (id != 0)
            (this.IdType, this.Numeric) = (Type.Numeric, id);
    }

    /// <summary>Initializes a new instance of the <see cref="ImGuiId"/> struct.</summary>
    /// <param name="id">A UTF-16 string ID, or <see cref="ReadOnlySpan{T}.Empty"/> to not provide an ID.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImGuiId(ReadOnlySpan<char> id)
    {
        if (!id.IsEmpty)
        {
            this.IdType = Type.U16;
            this.U16 = id;
        }
    }

    /// <summary>Initializes a new instance of the <see cref="ImGuiId"/> struct.</summary>
    /// <param name="id">A UTF-8 string ID, or <see cref="ReadOnlySpan{T}.Empty"/> to not provide an ID.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImGuiId(ReadOnlySpan<byte> id)
    {
        if (!id.IsEmpty)
        {
            this.IdType = Type.U8;
            this.U8 = id;
        }
    }

    /// <summary>Possible types for an ImGui ID.</summary>
    public enum Type
    {
        /// <summary>No ID is specified.</summary>
        None,

        /// <summary><see cref="ImGuiId.Numeric"/> field is used.</summary>
        Numeric,

        /// <summary><see cref="ImGuiId.U16"/> field is used.</summary>
        U16,

        /// <summary><see cref="ImGuiId.U8"/> field is used.</summary>
        U8,
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator ImGuiId(void* id) => new((nint)id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator ImGuiId(float id) => new(*(int*)&id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator ImGuiId(double id) => new(*(nint*)&id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(sbyte id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(byte id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(char id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(short id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(ushort id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(int id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(uint id) => new((nint)id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(nint id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(nuint id) => new((nint)id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(Span<char> id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(ReadOnlySpan<char> id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(Memory<char> id) => new(id.Span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(ReadOnlyMemory<char> id) => new(id.Span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(char[] id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(string id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(Span<byte> id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(ReadOnlySpan<byte> id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(Memory<byte> id) => new(id.Span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(ReadOnlyMemory<byte> id) => new(id.Span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImGuiId(byte[] id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(ImGuiId id) => !id.IsEmpty();

    /// <summary>Determines if no ID is stored.</summary>
    /// <returns><c>true</c> if no ID is stored.</returns>
    public bool IsEmpty() => this.IdType switch
    {
        Type.None => true,
        Type.Numeric => this.Numeric == 0,
        Type.U16 => this.U16.IsEmpty,
        Type.U8 => this.U8.IsEmpty,
        _ => true,
    };

    /// <summary>Pushes ID if any is stored.</summary>
    /// <returns><c>true</c> if any ID is pushed.</returns>
    public unsafe bool PushId()
    {
        switch (this.IdType)
        {
            case Type.Numeric:
                ImGui.PushID((void*)this.Numeric);
                return true;
            case Type.U16:
                fixed (void* p = this.U16)
                    ImGui.PushID((byte*)p, (byte*)p + (this.U16.Length * 2));
                return true;
            case Type.U8:
                fixed (void* p = this.U8)
                    ImGui.PushID((byte*)p, (byte*)p + this.U8.Length);
                return true;
            case Type.None:
            default:
                return false;
        }
    }
}

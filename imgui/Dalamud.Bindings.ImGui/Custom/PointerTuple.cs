using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
internal static unsafe class PointerTuple
{
    public static ref PointerTuple<T1> From<T1>(void* ptr)
        where T1 : allows ref struct
        => ref *(PointerTuple<T1>*)ptr;

    public static PointerTuple<T1> Create<T1>(T1* item1Ptr)
        where T1 : allows ref struct
        => new()
        {
            Item1Ptr = item1Ptr,
        };

    public static PointerTuple<T1> CreateFixed<T1>(ref T1 item1)
        where T1 : allows ref struct
        => new()
        {
            Item1Ptr = (T1*)Unsafe.AsPointer(ref item1),
        };

    public static ref PointerTuple<T1, T2> From<T1, T2>(void* ptr)
        where T1 : allows ref struct
        where T2 : allows ref struct
        => ref *(PointerTuple<T1, T2>*)ptr;

    public static PointerTuple<T1, T2> Create<T1, T2>(T1* item1Ptr, T2* item2Ptr)
        where T1 : allows ref struct
        where T2 : allows ref struct
        => new()
        {
            Item1Ptr = item1Ptr,
            Item2Ptr = item2Ptr,
        };

    public static PointerTuple<T1, T2> CreateFixed<T1, T2>(ref T1 item1, ref T2 item2)
        where T1 : allows ref struct
        where T2 : allows ref struct
        => new()
        {
            Item1Ptr = (T1*)Unsafe.AsPointer(ref item1),
            Item2Ptr = (T2*)Unsafe.AsPointer(ref item2),
        };

    public static ref PointerTuple<T1, T2, T3> From<T1, T2, T3>(void* ptr)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        => ref *(PointerTuple<T1, T2, T3>*)ptr;

    public static PointerTuple<T1, T2, T3> Create<T1, T2, T3>(T1* item1Ptr, T2* item2Ptr, T3* item3Ptr)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        => new()
        {
            Item1Ptr = item1Ptr,
            Item2Ptr = item2Ptr,
            Item3Ptr = item3Ptr,
        };

    public static PointerTuple<T1, T2, T3> CreateFixed<T1, T2, T3>(ref T1 item1, ref T2 item2, ref T3 item3)
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
        => new()
        {
            Item1Ptr = (T1*)Unsafe.AsPointer(ref item1),
            Item2Ptr = (T2*)Unsafe.AsPointer(ref item2),
            Item3Ptr = (T3*)Unsafe.AsPointer(ref item3),
        };
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct PointerTuple<T1>
    where T1 : allows ref struct
{
    public T1* Item1Ptr;

    public readonly ref T1 Item1 => ref *this.Item1Ptr;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct PointerTuple<T1, T2>
    where T1 : allows ref struct
    where T2 : allows ref struct
{
    public T1* Item1Ptr;
    public T2* Item2Ptr;

    public readonly ref T1 Item1 => ref *this.Item1Ptr;

    public readonly ref T2 Item2 => ref *this.Item2Ptr;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct PointerTuple<T1, T2, T3>
    where T1 : allows ref struct
    where T2 : allows ref struct
    where T3 : allows ref struct
{
    public T1* Item1Ptr;
    public T2* Item2Ptr;
    public T3* Item3Ptr;

    public readonly ref T1 Item1 => ref *this.Item1Ptr;

    public readonly ref T2 Item2 => ref *this.Item2Ptr;

    public readonly ref T3 Item3 => ref *this.Item3Ptr;
}

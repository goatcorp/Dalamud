using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public delegate ImU8String PopulateAutoUtf8BufferDelegate(int index);

    public delegate ImU8String PopulateAutoUtf8BufferInContextDelegate<T>(scoped in T context, int index)
        where T : allows ref struct;

    public delegate ImU8String PopulateAutoUtf8BufferRefContextDelegate<T>(scoped ref T context, int index)
        where T : allows ref struct;

    [OverloadResolutionPriority(8)]
    public static bool Combo(
        ImU8String label, ref int currentItem, ImU8String itemsSeparatedByZeros, int popupMaxHeightInItems = -1)
    {
        if (!itemsSeparatedByZeros.Span.EndsWith("\0\0"u8))
            itemsSeparatedByZeros.AppendFormatted("\0\0"u8);

        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* currentItemPtr = &currentItem)
        fixed (byte* itemsSeparatedByZerosPtr = itemsSeparatedByZeros)
        {
            var r = ImGuiNative.Combo(labelPtr, currentItemPtr, itemsSeparatedByZerosPtr, popupMaxHeightInItems) != 0;
            label.Recycle();
            itemsSeparatedByZeros.Recycle();
            return r;
        }
    }

    [OverloadResolutionPriority(7)]
    public static bool Combo(
        ImU8String label, ref int currentItem, ReadOnlySpan<string> items, int popupMaxHeightInItems = -1) =>
        Combo(
            label,
            ref currentItem,
            static (scoped in ReadOnlySpan<string> items, int index) => items[index],
            items,
            items.Length,
            popupMaxHeightInItems);

    [OverloadResolutionPriority(6)]
    public static bool Combo<T>(
        ImU8String label, ref int currentItem, scoped in T items, int popupMaxHeightInItems = -1)
        where T : IList<string> =>
        Combo(
            label,
            ref currentItem,
            static (scoped in T items, int index) => items[index],
            items,
            items.Count,
            popupMaxHeightInItems);

    [OverloadResolutionPriority(5)]
    public static bool Combo(
        ImU8String label, ref int currentItem, IReadOnlyList<string> items, int popupMaxHeightInItems = -1) =>
        Combo(
            label,
            ref currentItem,
            static (scoped in IReadOnlyList<string> items, int index) => items[index],
            items,
            items.Count,
            popupMaxHeightInItems);

    [OverloadResolutionPriority(4)]
    public static bool Combo<T>(
        ImU8String label, ref int currentItem, ReadOnlySpan<T> items, Func<T, string> toString,
        int popupMaxHeightInItems = -1)
    {
        var tmp = PointerTuple.CreateFixed(ref items, ref toString);
        return Combo(
            label,
            ref currentItem,
            static (scoped in PointerTuple<ReadOnlySpan<T>, Func<T, string>> items, int index) =>
                items.Item2(items.Item1[index]),
            tmp,
            items.Length,
            popupMaxHeightInItems);
    }

    [OverloadResolutionPriority(3)]
    public static bool Combo<T, TList>(
        ImU8String label, ref int currentItem, scoped in TList items, Func<T, string> toString,
        int popupMaxHeightInItems = -1)
        where TList : IList<T> =>
        Combo(
            label,
            ref currentItem,
            static (scoped in (TList, Func<T, string>) items, int index) => items.Item2(items.Item1[index]),
            (items, toString),
            items.Count,
            popupMaxHeightInItems);

    [OverloadResolutionPriority(2)]
    public static bool Combo<T>(
        ImU8String label, ref int currentItem, IReadOnlyList<T> items, Func<T, string> toString,
        int popupMaxHeightInItems = -1) =>
        Combo(
            label,
            ref currentItem,
            static (scoped in (IReadOnlyList<T>, Func<T, string>) items, int index) => items.Item2(items.Item1[index]),
            (items, toString),
            items.Count,
            popupMaxHeightInItems);

    public static bool Combo<TContext>(
        ImU8String label, ref int currentItem, PopulateAutoUtf8BufferInContextDelegate<TContext> itemsGetter,
        scoped in TContext context, int itemsCount, int popupMaxHeightInItems = -1)
        where TContext : allows ref struct
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* currentItemPtr = &currentItem)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            ImU8String textBuffer = default;
            var dataBuffer = PointerTuple.Create(&itemsGetter, &textBuffer, contextPtr);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.Combo(
                        labelPtr,
                        currentItemPtr,
                        (delegate*<byte*, int*, delegate*<void*, int, byte**, bool>, void*, int, int, bool>)
                        (nint)(delegate* unmanaged<void*, int, byte**, bool>)&PopulateUtf8BufferInContextStatic,
                        &dataBuffer,
                        itemsCount,
                        popupMaxHeightInItems) != 0;
            label.Recycle();
            textBuffer.Recycle();
            return r;
        }
    }

    public static bool Combo<TContext>(
        ImU8String label, ref int currentItem, PopulateAutoUtf8BufferRefContextDelegate<TContext> itemsGetter,
        scoped ref TContext context, int itemsCount, int popupMaxHeightInItems = -1)
        where TContext : allows ref struct
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* currentItemPtr = &currentItem)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            ImU8String textBuffer = default;
            var dataBuffer = PointerTuple.Create(&itemsGetter, &textBuffer, contextPtr);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.Combo(
                        labelPtr,
                        currentItemPtr,
                        (delegate*<byte*, int*, delegate*<void*, int, byte**, bool>, void*, int, int, bool>)
                        (nint)(delegate* unmanaged<void*, int, byte**, bool>)&PopulateUtf8BufferRefContextStatic,
                        &dataBuffer,
                        itemsCount,
                        popupMaxHeightInItems) != 0;
            label.Recycle();
            textBuffer.Recycle();
            return r;
        }
    }

    public static bool Combo(
        ImU8String label, ref int currentItem, PopulateAutoUtf8BufferDelegate itemsGetter, int itemsCount,
        int popupMaxHeightInItems = -1)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* currentItemPtr = &currentItem)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        {
            ImU8String textBuffer = default;
            var dataBuffer = PointerTuple.Create(&itemsGetter, &textBuffer);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.Combo(
                        labelPtr,
                        currentItemPtr,
                        (delegate*<byte*, int*, delegate*<void*, int, byte**, bool>, void*, int, int, bool>)
                        (nint)(delegate* unmanaged<void*, int, byte**, bool>)&PopulateUtf8BufferStatic,
                        &dataBuffer,
                        itemsCount,
                        popupMaxHeightInItems) != 0;
            label.Recycle();
            textBuffer.Recycle();
            return r;
        }
    }

    [OverloadResolutionPriority(2)]
    public static bool ListBox(
        ImU8String label, ref int currentItem, ReadOnlySpan<string> items, int heightInItems = -1) =>
        ListBox(
            label,
            ref currentItem,
            static (scoped in ReadOnlySpan<string> items, int index) => items[index],
            items,
            items.Length,
            heightInItems);

    [OverloadResolutionPriority(3)]
    public static bool ListBox<T>(ImU8String label, ref int currentItem, scoped in T items, int heightInItems = -1)
        where T : IList<string> =>
        ListBox(
            label,
            ref currentItem,
            static (scoped in T items, int index) => items[index],
            items,
            items.Count,
            heightInItems);

    [OverloadResolutionPriority(4)]
    public static bool ListBox(
        ImU8String label, ref int currentItem, IReadOnlyList<string> items, int heightInItems = -1) =>
        ListBox(
            label,
            ref currentItem,
            static (scoped in IReadOnlyList<string> items, int index) => items[index],
            items,
            items.Count,
            heightInItems);

    [OverloadResolutionPriority(5)]
    public static bool ListBox<T>(
        ImU8String label, ref int currentItem, ReadOnlySpan<T> items, Func<T, string> toString,
        int heightInItems = -1)
    {
        var tmp = PointerTuple.CreateFixed(ref items, ref toString);
        return ListBox(
            label,
            ref currentItem,
            static (scoped in PointerTuple<ReadOnlySpan<T>, Func<T, string>> items, int index) =>
                items.Item2(items.Item1[index]),
            tmp,
            items.Length,
            heightInItems);
    }

    [OverloadResolutionPriority(6)]
    public static bool ListBox<T, TList>(
        ImU8String label, ref int currentItem, scoped in TList items, Func<T, string> toString,
        int heightInItems = -1)
        where TList : IList<T> =>
        ListBox(
            label,
            ref currentItem,
            static (scoped in (TList, Func<T, string>) items, int index) => items.Item2(items.Item1[index]),
            (items, toString),
            items.Count,
            heightInItems);

    [OverloadResolutionPriority(7)]
    public static bool ListBox<T>(
        ImU8String label, ref int currentItem, IReadOnlyList<T> items, Func<T, string> toString,
        int heightInItems = -1) =>
        ListBox(
            label,
            ref currentItem,
            static (scoped in (IReadOnlyList<T>, Func<T, string>) items, int index) => items.Item2(items.Item1[index]),
            (items, toString),
            items.Count,
            heightInItems);

    public static bool ListBox<TContext>(
        ImU8String label, ref int currentItem, PopulateAutoUtf8BufferRefContextDelegate<TContext> itemsGetter,
        scoped ref TContext context, int itemsCount, int heightInItems = -1)
        where TContext : allows ref struct
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* currentItemPtr = &currentItem)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            ImU8String textBuffer = default;
            var dataBuffer = PointerTuple.Create(&itemsGetter, &textBuffer, contextPtr);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.ListBox(
                        labelPtr,
                        currentItemPtr,
                        (delegate*<byte*, int*, delegate*<void*, int, byte**, bool>, void*, int, int, bool>)
                        (nint)(delegate* unmanaged<void*, int, byte**, bool>)&PopulateUtf8BufferRefContextStatic,
                        &dataBuffer,
                        itemsCount,
                        heightInItems) != 0;
            label.Recycle();
            textBuffer.Recycle();
            return r;
        }
    }

    public static bool ListBox<TContext>(
        ImU8String label, ref int currentItem, PopulateAutoUtf8BufferInContextDelegate<TContext> itemsGetter,
        scoped in TContext context, int itemsCount, int heightInItems = -1)
        where TContext : allows ref struct
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* currentItemPtr = &currentItem)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            ImU8String textBuffer = default;
            var dataBuffer = PointerTuple.Create(&itemsGetter, &textBuffer, contextPtr);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.ListBox(
                        labelPtr,
                        currentItemPtr,
                        (delegate*<byte*, int*, delegate*<void*, int, byte**, bool>, void*, int, int, bool>)
                        (nint)(delegate* unmanaged<void*, int, byte**, bool>)&PopulateUtf8BufferInContextStatic,
                        &dataBuffer,
                        itemsCount,
                        heightInItems) != 0;
            label.Recycle();
            textBuffer.Recycle();
            return r;
        }
    }

    public static bool ListBox(
        ImU8String label, ref int currentItem, PopulateAutoUtf8BufferDelegate itemsGetter, int itemsCount,
        int heightInItems = -1)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* currentItemPtr = &currentItem)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        {
            ImU8String textBuffer = default;
            var dataBuffer = PointerTuple.Create(&itemsGetter, &textBuffer);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.ListBox(
                        labelPtr,
                        currentItemPtr,
                        (delegate*<byte*, int*, delegate*<void*, int, byte**, bool>, void*, int, int, bool>)
                        (nint)(delegate* unmanaged<void*, int, byte**, bool>)&PopulateUtf8BufferStatic,
                        &dataBuffer,
                        itemsCount,
                        heightInItems) != 0;
            label.Recycle();
            textBuffer.Recycle();
            return r;
        }
    }

    [UnmanagedCallersOnly]
    private static bool PopulateUtf8BufferRefContextStatic(void* data, int index, byte** text)
    {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        ref var s = ref PointerTuple.From<PopulateAutoUtf8BufferRefContextDelegate<object>, ImU8String, object>(data);
        s.Item2.Recycle();
        s.Item2 = s.Item1.Invoke(ref s.Item3, index);
        if (s.Item2.IsNull)
            return false;
        *text = (byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in s.Item2.GetPinnableNullTerminatedReference()));
        return true;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    }

    [UnmanagedCallersOnly]
    private static bool PopulateUtf8BufferInContextStatic(void* data, int index, byte** text)
    {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        ref var s = ref PointerTuple.From<PopulateAutoUtf8BufferInContextDelegate<object>, ImU8String, object>(data);
        s.Item2.Recycle();
        s.Item2 = s.Item1.Invoke(s.Item3, index);
        if (s.Item2.IsNull)
            return false;
        *text = (byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in s.Item2.GetPinnableNullTerminatedReference()));
        return true;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    }

    [UnmanagedCallersOnly]
    private static bool PopulateUtf8BufferStatic(void* data, int index, byte** text)
    {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        ref var s = ref PointerTuple.From<PopulateAutoUtf8BufferDelegate, ImU8String>(data);
        s.Item2.Recycle();
        s.Item2 = s.Item1.Invoke(index);
        if (s.Item2.IsNull)
            return false;
        *text = (byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in s.Item2.GetPinnableNullTerminatedReference()));
        return true;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    }
}

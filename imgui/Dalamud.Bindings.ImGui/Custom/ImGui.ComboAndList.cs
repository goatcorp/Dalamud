using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public delegate bool PopulateAutoUtf8BufferDelegate(int index, out Utf8Buffer outText);

    public delegate bool PopulateAutoUtf8BufferDelegate<T>(scoped in T context, int index, out Utf8Buffer outText);

    public static bool Combo<T>(
        Utf8Buffer label, ref int currentItem, scoped in T items, int popupMaxHeightInItems = -1)
        where T : IList<string> =>
        Combo(
            label,
            ref currentItem,
            static (scoped in T items, int index, out Utf8Buffer outText) =>
            {
                outText = items[index];
                return true;
            },
            items,
            items.Count,
            popupMaxHeightInItems);

    public static bool Combo(
        Utf8Buffer label, ref int currentItem, IReadOnlyList<string> items, int popupMaxHeightInItems = -1) =>
        Combo(
            label,
            ref currentItem,
            static (scoped in IReadOnlyList<string> items, int index, out Utf8Buffer outText) =>
            {
                outText = items[index];
                return true;
            },
            items,
            items.Count,
            popupMaxHeightInItems);

    public static bool Combo(
        Utf8Buffer label, ref int currentItem, Utf8Buffer itemsSeparatedByZeros, int popupMaxHeightInItems = -1)
    {
        if (!itemsSeparatedByZeros.Span.EndsWith("\0\0"u8))
            itemsSeparatedByZeros.AppendFormatted("\0\0"u8);

        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* currentItemPtr = &currentItem)
        fixed (byte* itemsSeparatedByZerosPtr = itemsSeparatedByZeros.Span)
        {
            var r = ImGuiNative.Combo(labelPtr, currentItemPtr, itemsSeparatedByZerosPtr, popupMaxHeightInItems) != 0;
            label.Dispose();
            itemsSeparatedByZeros.Dispose();
            return r;
        }
    }

    public static bool Combo<TContext>(
        Utf8Buffer label, ref int currentItem, PopulateAutoUtf8BufferDelegate<TContext> itemsGetter,
        scoped in TContext context, int itemsCount, int popupMaxHeightInItems = -1)
    {
        Utf8Buffer textBuffer = default;
        var dataBuffer = stackalloc void*[3];
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* currentItemPtr = &currentItem)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            dataBuffer[0] = &textBuffer;
            dataBuffer[1] = &itemsGetter;
            dataBuffer[2] = contextPtr;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.Combo(
                        labelPtr,
                        currentItemPtr,
                        (delegate*<byte*, int*, delegate*<void*, int, byte**, bool>, void*, int, int, bool>)
                        (nint)(delegate* unmanaged<void*, int, byte**, bool>)&PopulateUtf8BufferDelegateWithContext,
                        dataBuffer,
                        itemsCount,
                        popupMaxHeightInItems) != 0;
            label.Dispose();
            textBuffer.Dispose();
            return r;
        }
    }

    public static bool Combo(
        Utf8Buffer label, ref int currentItem, PopulateAutoUtf8BufferDelegate itemsGetter, int itemsCount,
        int popupMaxHeightInItems = -1)
    {
        Utf8Buffer textBuffer = default;
        var dataBuffer = stackalloc void*[2];
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* currentItemPtr = &currentItem)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        {
            dataBuffer[0] = &textBuffer;
            dataBuffer[1] = &itemsGetter;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.Combo(
                        labelPtr,
                        currentItemPtr,
                        (delegate*<byte*, int*, delegate*<void*, int, byte**, bool>, void*, int, int, bool>)
                        (nint)(delegate* unmanaged<void*, int, byte**, bool>)&PopulateUtf8BufferDelegateWithoutContext,
                        dataBuffer,
                        itemsCount,
                        popupMaxHeightInItems) != 0;
            label.Dispose();
            textBuffer.Dispose();
            return r;
        }
    }

    public static bool ListBox<T>(
        Utf8Buffer label, ref int currentItem, scoped in T items, int popupMaxHeightInItems = -1)
        where T : IList<string> =>
        ListBox(
            label,
            ref currentItem,
            static (scoped in T items, int index, out Utf8Buffer outText) =>
            {
                outText = items[index];
                return true;
            },
            items,
            items.Count,
            popupMaxHeightInItems);

    public static bool ListBox(
        Utf8Buffer label, ref int currentItem, IReadOnlyList<string> items, int popupMaxHeightInItems = -1) =>
        ListBox(
            label,
            ref currentItem,
            static (scoped in IReadOnlyList<string> items, int index, out Utf8Buffer outText) =>
            {
                outText = items[index];
                return true;
            },
            items,
            items.Count,
            popupMaxHeightInItems);

    public static bool ListBox<TContext>(
        Utf8Buffer label, ref int currentItem, PopulateAutoUtf8BufferDelegate<TContext> itemsGetter,
        scoped in TContext context, int itemsCount, int popupMaxHeightInItems = -1)
    {
        Utf8Buffer textBuffer = default;
        var dataBuffer = stackalloc void*[3];
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* currentItemPtr = &currentItem)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            dataBuffer[0] = &textBuffer;
            dataBuffer[1] = &itemsGetter;
            dataBuffer[2] = contextPtr;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.ListBox(
                        labelPtr,
                        currentItemPtr,
                        (delegate*<byte*, int*, delegate*<void*, int, byte**, bool>, void*, int, int, bool>)
                        (nint)(delegate* unmanaged<void*, int, byte**, bool>)&PopulateUtf8BufferDelegateWithContext,
                        dataBuffer,
                        itemsCount,
                        popupMaxHeightInItems) != 0;
            label.Dispose();
            textBuffer.Dispose();
            return r;
        }
    }

    public static bool ListBox(
        Utf8Buffer label, ref int currentItem, PopulateAutoUtf8BufferDelegate itemsGetter, int itemsCount,
        int popupMaxHeightInItems = -1)
    {
        Utf8Buffer textBuffer = default;
        var dataBuffer = stackalloc void*[2];
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (int* currentItemPtr = &currentItem)
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        {
            dataBuffer[0] = &textBuffer;
            dataBuffer[1] = &itemsGetter;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiNative.ListBox(
                        labelPtr,
                        currentItemPtr,
                        (delegate*<byte*, int*, delegate*<void*, int, byte**, bool>, void*, int, int, bool>)
                        (nint)(delegate* unmanaged<void*, int, byte**, bool>)&PopulateUtf8BufferDelegateWithoutContext,
                        dataBuffer,
                        itemsCount,
                        popupMaxHeightInItems) != 0;
            label.Dispose();
            textBuffer.Dispose();
            return r;
        }
    }

    [UnmanagedCallersOnly]
    private static bool PopulateUtf8BufferDelegateWithContext(void* data, int index, byte** text)
    {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        ref var textBuffer = ref *(Utf8Buffer*)((void**)data)[0];
        return ((PopulateAutoUtf8BufferDelegate<object>*)((void**)data)[1])->Invoke(
            *(object*)((void**)data)[2],
            index,
            out textBuffer);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    }

    [UnmanagedCallersOnly]
    private static bool PopulateUtf8BufferDelegateWithoutContext(void* data, int index, byte** text)
    {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        ref var textBuffer = ref *(Utf8Buffer*)((void**)data)[0];
        return ((PopulateAutoUtf8BufferDelegate*)((void**)data)[1])->Invoke(
            index,
            out textBuffer);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    }
}

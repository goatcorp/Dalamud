using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public delegate float GetFloatDelegate(int index);

    public delegate float GetFloatInContextDelegate<T>(scoped in T context, int index) where T : allows ref struct;

    public delegate float GetFloatRefContextDelegate<T>(scoped ref T context, int index) where T : allows ref struct;

    public static void PlotHistogram(
        ImU8String label, ReadOnlySpan<float> values, int valuesOffset = 0, ImU8String overlayText = default,
        float scaleMin = float.MaxValue, float scaleMax = float.MaxValue, Vector2 graphSize = default,
        int stride = sizeof(float))
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (float* valuesPtr = values)
        fixed (byte* overlayTextPtr = &overlayText.GetPinnableNullTerminatedReference())
        {
            ImGuiNative.PlotHistogram(
                labelPtr,
                valuesPtr,
                values.Length,
                valuesOffset,
                overlayTextPtr,
                scaleMin,
                scaleMax,
                graphSize,
                stride);
        }

        label.Dispose();
        overlayText.Dispose();
    }

    public static void PlotHistogram<TContext>(
        ImU8String label, GetFloatRefContextDelegate<TContext> valuesGetter, scoped ref TContext context,
        int valuesCount,
        int valuesOffset = 0, ImU8String overlayText = default, float scaleMin = float.MaxValue,
        float scaleMax = float.MaxValue, Vector2 graphSize = default)
    {
        var dataBuffer = stackalloc void*[2];
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* overlayTextPtr = &overlayText.GetPinnableNullTerminatedReference())
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            dataBuffer[0] = &valuesGetter;
            dataBuffer[1] = contextPtr;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            ImGuiNative.PlotHistogram(
                labelPtr,
                (delegate*<byte*, delegate*<void*, int, float>, void*, int, int, byte*, float, float, Vector2, float>)
                (nint)(delegate* unmanaged<void*, int, float>)&GetFloatRefContextStatic,
                dataBuffer,
                valuesCount,
                valuesOffset,
                overlayTextPtr,
                scaleMin,
                scaleMax,
                graphSize);
        }

        label.Dispose();
        overlayText.Dispose();
    }

    public static void PlotHistogram<TContext>(
        ImU8String label, GetFloatInContextDelegate<TContext> valuesGetter, scoped in TContext context, int valuesCount,
        int valuesOffset = 0, ImU8String overlayText = default, float scaleMin = float.MaxValue,
        float scaleMax = float.MaxValue, Vector2 graphSize = default)
    {
        var dataBuffer = stackalloc void*[2];
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* overlayTextPtr = &overlayText.GetPinnableNullTerminatedReference())
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            dataBuffer[0] = &valuesGetter;
            dataBuffer[1] = contextPtr;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            ImGuiNative.PlotHistogram(
                labelPtr,
                (delegate*<byte*, delegate*<void*, int, float>, void*, int, int, byte*, float, float, Vector2, float>)
                (nint)(delegate* unmanaged<void*, int, float>)&GetFloatInContextStatic,
                dataBuffer,
                valuesCount,
                valuesOffset,
                overlayTextPtr,
                scaleMin,
                scaleMax,
                graphSize);
        }

        label.Dispose();
        overlayText.Dispose();
    }

    public static void PlotHistogram(
        ImU8String label, GetFloatDelegate valuesGetter, int valuesCount,
        int valuesOffset = 0, ImU8String overlayText = default, float scaleMin = float.MaxValue,
        float scaleMax = float.MaxValue, Vector2 graphSize = default)
    {
        var dataBuffer = stackalloc void*[1];
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* overlayTextPtr = &overlayText.GetPinnableNullTerminatedReference())
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        {
            dataBuffer[0] = &valuesGetter;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            ImGuiNative.PlotHistogram(
                labelPtr,
                (delegate*<byte*, delegate*<void*, int, float>, void*, int, int, byte*, float, float, Vector2, float>)
                (nint)(delegate* unmanaged<void*, int, float>)&GetFloatStatic,
                dataBuffer,
                valuesCount,
                valuesOffset,
                overlayTextPtr,
                scaleMin,
                scaleMax,
                graphSize);
        }

        label.Dispose();
        overlayText.Dispose();
    }

    public static void PlotLines(
        ImU8String label, ReadOnlySpan<float> values, int valuesOffset = 0, ImU8String overlayText = default,
        float scaleMin = float.MaxValue, float scaleMax = float.MaxValue, Vector2 graphSize = default,
        int stride = sizeof(float))
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (float* valuesPtr = values)
        fixed (byte* overlayTextPtr = &overlayText.GetPinnableNullTerminatedReference())
        {
            ImGuiNative.PlotLines(
                labelPtr,
                valuesPtr,
                values.Length,
                valuesOffset,
                overlayTextPtr,
                scaleMin,
                scaleMax,
                graphSize,
                stride);
        }

        label.Dispose();
        overlayText.Dispose();
    }

    public static void PlotLines<TContext>(
        ImU8String label, GetFloatInContextDelegate<TContext> valuesGetter, scoped in TContext context, int valuesCount,
        int valuesOffset = 0, ImU8String overlayText = default, float scaleMin = float.MaxValue,
        float scaleMax = float.MaxValue, Vector2 graphSize = default)
    {
        var dataBuffer = stackalloc void*[2];
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* overlayTextPtr = &overlayText.GetPinnableNullTerminatedReference())
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            dataBuffer[0] = &valuesGetter;
            dataBuffer[1] = contextPtr;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            ImGuiNative.PlotLines(
                labelPtr,
                (delegate*<byte*, delegate*<void*, int, float>, void*, int, int, byte*, float, float, Vector2, float>)
                (nint)(delegate* unmanaged<void*, int, float>)&GetFloatInContextStatic,
                dataBuffer,
                valuesCount,
                valuesOffset,
                overlayTextPtr,
                scaleMin,
                scaleMax,
                graphSize);
        }

        label.Dispose();
        overlayText.Dispose();
    }

    public static void PlotLines<TContext>(
        ImU8String label, GetFloatRefContextDelegate<TContext> valuesGetter, scoped in TContext context,
        int valuesCount,
        int valuesOffset = 0, ImU8String overlayText = default, float scaleMin = float.MaxValue,
        float scaleMax = float.MaxValue, Vector2 graphSize = default)
    {
        var dataBuffer = stackalloc void*[2];
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* overlayTextPtr = &overlayText.GetPinnableNullTerminatedReference())
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            dataBuffer[0] = &valuesGetter;
            dataBuffer[1] = contextPtr;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            ImGuiNative.PlotLines(
                labelPtr,
                (delegate*<byte*, delegate*<void*, int, float>, void*, int, int, byte*, float, float, Vector2, float>)
                (nint)(delegate* unmanaged<void*, int, float>)&GetFloatRefContextStatic,
                dataBuffer,
                valuesCount,
                valuesOffset,
                overlayTextPtr,
                scaleMin,
                scaleMax,
                graphSize);
        }

        label.Dispose();
        overlayText.Dispose();
    }

    public static void PlotLines(
        ImU8String label, GetFloatDelegate valuesGetter, int valuesCount,
        int valuesOffset = 0, ImU8String overlayText = default, float scaleMin = float.MaxValue,
        float scaleMax = float.MaxValue, Vector2 graphSize = default)
    {
        var dataBuffer = stackalloc void*[1];
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* overlayTextPtr = &overlayText.GetPinnableNullTerminatedReference())
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        {
            dataBuffer[0] = &valuesGetter;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            ImGuiNative.PlotLines(
                labelPtr,
                (delegate*<byte*, delegate*<void*, int, float>, void*, int, int, byte*, float, float, Vector2, float>)
                (nint)(delegate* unmanaged<void*, int, float>)&GetFloatStatic,
                dataBuffer,
                valuesCount,
                valuesOffset,
                overlayTextPtr,
                scaleMin,
                scaleMax,
                graphSize);
        }

        label.Dispose();
        overlayText.Dispose();
    }

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    [UnmanagedCallersOnly]
    internal static float GetFloatRefContextStatic(void* data, int index)
    {
        ref var pt = ref PointerTuple.From<GetFloatRefContextDelegate<object>, object>(data);
        return pt.Item1.Invoke(ref pt.Item2, index);
    }

    [UnmanagedCallersOnly]
    internal static float GetFloatInContextStatic(void* data, int index)
    {
        ref var pt = ref PointerTuple.From<GetFloatInContextDelegate<object>, object>(data);
        return pt.Item1.Invoke(pt.Item2, index);
    }

    [UnmanagedCallersOnly]
    internal static float GetFloatStatic(void* data, int index) =>
        PointerTuple.From<GetFloatDelegate>(data).Item1.Invoke(index);
}

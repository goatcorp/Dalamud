using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public delegate float GetFloatDelegate(int index);

    public delegate float GetFloatDelegate<T>(scoped in T context, int index);

    public static void PlotHistogram(
        Utf8Buffer label, ReadOnlySpan<float> values, int valuesOffset = 0, Utf8Buffer overlayText = default,
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
        Utf8Buffer label, GetFloatDelegate<TContext> valuesGetter, scoped in TContext context, int valuesCount,
        int valuesOffset = 0, Utf8Buffer overlayText = default, float scaleMin = float.MaxValue,
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
                (nint)(delegate* unmanaged<void*, int, float>)&GetFloatWithContext,
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
        Utf8Buffer label, GetFloatDelegate valuesGetter, int valuesCount,
        int valuesOffset = 0, Utf8Buffer overlayText = default, float scaleMin = float.MaxValue,
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
                (nint)(delegate* unmanaged<void*, int, float>)&GetFloatWithContext,
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
        Utf8Buffer label, ReadOnlySpan<float> values, int valuesOffset = 0, Utf8Buffer overlayText = default,
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
        Utf8Buffer label, GetFloatDelegate<TContext> valuesGetter, scoped in TContext context, int valuesCount,
        int valuesOffset = 0, Utf8Buffer overlayText = default, float scaleMin = float.MaxValue,
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
                (nint)(delegate* unmanaged<void*, int, float>)&GetFloatWithContext,
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
        Utf8Buffer label, GetFloatDelegate valuesGetter, int valuesCount,
        int valuesOffset = 0, Utf8Buffer overlayText = default, float scaleMin = float.MaxValue,
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
                (nint)(delegate* unmanaged<void*, int, float>)&GetFloatWithContext,
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
    private static float GetFloatWithContext(void* data, int index) =>
        ((GetFloatDelegate<object>*)((void**)data)[0])->Invoke(*(object*)((void**)data)[1], index);

    [UnmanagedCallersOnly]
    private static float GetFloatWithoutContext(void* data, int index) =>
        ((GetFloatDelegate*)((void**)data)[0])->Invoke(index);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
}

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGuiP
{
    public static int PlotEx(
        ImGuiPlotType plotType, ImU8String label, ImGui.GetFloatDelegate valuesGetter,
        int valuesCount, int valuesOffset, ImU8String overlayText, float scaleMin, float scaleMax, Vector2 frameSize)
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* overlayTextPtr = &overlayText.GetPinnableNullTerminatedReference())
        {
            var dataBuffer = PointerTuple.CreateFixed(ref valuesGetter);
            var r = ImGuiPNative.PlotEx(
                plotType,
                labelPtr,
                (delegate*<ImGuiPlotType, byte*, delegate*<void*, int, float>, void*, int, int, byte*, float, float,
                    Vector2, float>)
                (nint)(delegate* unmanaged<void*, int, float>)&ImGui.GetFloatStatic,
                &dataBuffer,
                valuesCount,
                valuesOffset,
                overlayTextPtr,
                scaleMin,
                scaleMax,
                frameSize);
            label.Dispose();
            overlayText.Dispose();
            return r;
        }
    }

    public static int PlotEx<TContext>(
        ImGuiPlotType plotType, ImU8String label, ImGui.GetFloatInContextDelegate<TContext> valuesGetter,
        scoped in TContext context,
        int valuesCount, int valuesOffset, ImU8String overlayText, float scaleMin, float scaleMax, Vector2 frameSize)
        where TContext : allows ref struct
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* overlayTextPtr = &overlayText.GetPinnableNullTerminatedReference())
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            var dataBuffer = PointerTuple.Create(&valuesGetter, contextPtr);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiPNative.PlotEx(
                plotType,
                labelPtr,
                (delegate*<ImGuiPlotType, byte*, delegate*<void*, int, float>, void*, int, int, byte*, float, float,
                    Vector2, float>)
                (nint)(delegate* unmanaged<void*, int, float>)&ImGui.GetFloatInContextStatic,
                &dataBuffer,
                valuesCount,
                valuesOffset,
                overlayTextPtr,
                scaleMin,
                scaleMax,
                frameSize);
            label.Dispose();
            overlayText.Dispose();
            return r;
        }
    }

    public static int PlotEx<TContext>(
        ImGuiPlotType plotType, ImU8String label, ImGui.GetFloatRefContextDelegate<TContext> valuesGetter,
        scoped in TContext context,
        int valuesCount, int valuesOffset, ImU8String overlayText, float scaleMin, float scaleMax, Vector2 frameSize)
        where TContext: allows ref struct
    {
        fixed (byte* labelPtr = &label.GetPinnableNullTerminatedReference())
        fixed (byte* overlayTextPtr = &overlayText.GetPinnableNullTerminatedReference())
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        fixed (TContext* contextPtr = &context)
        {
            var dataBuffer = PointerTuple.Create(&valuesGetter, contextPtr);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var r = ImGuiPNative.PlotEx(
                plotType,
                labelPtr,
                (delegate*<ImGuiPlotType, byte*, delegate*<void*, int, float>, void*, int, int, byte*, float, float,
                    Vector2, float>)
                (nint)(delegate* unmanaged<void*, int, float>)&ImGui.GetFloatRefContextStatic,
                &dataBuffer,
                valuesCount,
                valuesOffset,
                overlayTextPtr,
                scaleMin,
                scaleMax,
                frameSize);
            label.Dispose();
            overlayText.Dispose();
            return r;
        }
    }
}

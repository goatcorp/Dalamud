using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;

namespace Dalamud.Interface.Utility.Raii;

// All previous files, but only for ImPlot specific functions.
public static partial class ImRaii
{
    #region EndObjects

    public static PlotDisposable Plot(string titleId, Vector2 size, ImPlotFlags flags)
        => new(titleId, size, flags);

    public static PlotDisposable Plot(ReadOnlySpan<byte> titleId, Vector2 size, ImPlotFlags flags)
        => new(titleId, size, flags);

    public static PlotAlignedDisposable AlignedPlots(string groupId, bool vertical = true)
        => new(groupId, vertical);

    public static PlotAlignedDisposable AlignedPlots(ReadOnlySpan<byte> groupId, bool vertical = true)
        => new(groupId, vertical);

    public static PlotLegendDisposable LegendPopup(string labelId, ImGuiMouseButton mouseButton = ImGuiMouseButton.Right)
        => new(labelId, mouseButton);

    public static PlotLegendDisposable LegendPopup(ReadOnlySpan<byte> labelId, ImGuiMouseButton mouseButton = ImGuiMouseButton.Right)
        => new(labelId, mouseButton);

    public static PlotSubDisposable Subplots(string titleId, int rows, int cols, Vector2 size, ImPlotSubplotFlags flags = ImPlotSubplotFlags.None)
        => new(titleId, rows, cols, size, flags);

    public static PlotSubDisposable Subplots(ReadOnlySpan<byte> titleId, int rows, int cols, Vector2 size, ImPlotSubplotFlags flags = ImPlotSubplotFlags.None)
        => new(titleId, rows, cols, size, flags);

    public static PlotSubDisposable Subplots(string titleId, int rows, int cols, Vector2 size, ImPlotSubplotFlags flags, ref float rowRatios, ref float colRatios)
        => new(titleId, rows, cols, size, flags, ref rowRatios, ref colRatios);

    public static PlotSubDisposable Subplots(ReadOnlySpan<byte> titleId, int rows, int cols, Vector2 size, ImPlotSubplotFlags flags, ref float rowRatios, ref float colRatios)
        => new(titleId, rows, cols, size, flags, ref rowRatios, ref colRatios);

    public static PlotDragDropSourceDisposable DragDropSourceItem(string labelId, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => new(labelId, flags);

    public static PlotDragDropSourceDisposable DragDropSourceItem(ReadOnlySpan<byte> labelId, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => new(labelId, flags);

    public static PlotDragDropSourceDisposable DragDropSourceAxis(ImAxis axis, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => PlotDragDropSourceDisposable.AxisPlot(axis, flags);

    public static PlotDragDropSourceDisposable DragDropSourcePlot(ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => PlotDragDropSourceDisposable.SourcePlot(flags);

    public static PlotDragDropTargetDisposable DragDropTargetAxis(ImAxis axis)
        => PlotDragDropTargetDisposable.AxisPlot(axis);

    public static PlotDragDropTargetDisposable DragDropTargetLegend()
        => PlotDragDropTargetDisposable.LegendPlot();

    public static PlotDragDropTargetDisposable DragDropTargetPlot()
        => PlotDragDropTargetDisposable.SourcePlot();

    #endregion EndObjects

    #region Style

    public static PlotStyleDisposable PushStyle(ImPlotStyleVar idx, int value, bool condition = true)
        => new PlotStyleDisposable().Push(idx, value, condition);

    public static PlotStyleDisposable PushStyle(ImPlotStyleVar idx, float value, bool condition = true)
        => new PlotStyleDisposable().Push(idx, value, condition);

    public static PlotStyleDisposable PushStyle(ImPlotStyleVar idx, Vector2 value, bool condition = true)
        => new PlotStyleDisposable().Push(idx, value, condition);

    // Push styles that revert all current plot style changes made temporarily.
    public static PlotStyleDisposable DefaultPlotStyle()
        => PlotStyleDisposable.PlotDefaultStyle();

    #endregion Style

    #region Color

    public static PlotColorDisposable PushColor(ImPlotCol idx, uint color, bool condition = true)
        => new PlotColorDisposable().Push(idx, color, condition);

    public static PlotColorDisposable PushColor(ImPlotCol idx, Vector4 color, bool condition = true)
        => new PlotColorDisposable().Push(idx, color, condition);

    // Push colors that revert all current color changes made temporarily.
    public static PlotColorDisposable DefaultPlotColors()
        => PlotColorDisposable.PlotDefaultColors();

    #endregion Color
}

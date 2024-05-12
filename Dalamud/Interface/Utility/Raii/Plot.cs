using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiNET;

using ImPlotNET;

namespace Dalamud.Interface.Utility.Raii;

// All previous files, but only for ImPlot specific functions.
public static partial class ImRaii
{
    #region EndObjects

    public static IEndObject Plot(string title_id, Vector2 size, ImPlotFlags flags)
        => new EndConditionally(ImPlot.EndPlot, ImPlot.BeginPlot(title_id, size, flags));

    public static IEndObject AlignedPlots(string group_id, bool vertical = true)
        => new EndConditionally(ImPlot.EndAlignedPlots, ImPlot.BeginAlignedPlots(group_id, vertical));

    public static IEndObject LegendPopup(string label_id, ImGuiMouseButton mouse_button = ImGuiMouseButton.Right)
        => new EndConditionally(ImPlot.EndLegendPopup, ImPlot.BeginLegendPopup(label_id, mouse_button));

    public static IEndObject Subplots(string title_id, int rows, int cols, Vector2 size, ImPlotSubplotFlags flags = ImPlotSubplotFlags.None)
        => new EndConditionally(ImPlot.EndSubplots, ImPlot.BeginSubplots(title_id, rows, cols, size, flags));

    public static IEndObject Subplots(string title_id, int rows, int cols, Vector2 size, ImPlotSubplotFlags flags, ref float row_ratios, ref float col_ratios)
        => new EndConditionally(ImPlot.EndSubplots, ImPlot.BeginSubplots(title_id, rows, cols, size, flags, ref row_ratios, ref col_ratios));

    public static IEndObject DragDropSourceAxis(ImAxis axis, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => new EndConditionally(ImPlot.EndDragDropSource, ImPlot.BeginDragDropSourceAxis(axis, flags));
    
    public static IEndObject DragDropSourceItem(string label_id, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => new EndConditionally(ImPlot.EndDragDropSource, ImPlot.BeginDragDropSourceItem(label_id, flags));

    public static IEndObject DragDropSourcePlot(ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => new EndConditionally(ImPlot.EndDragDropSource, ImPlot.BeginDragDropSourcePlot(flags));

    public static IEndObject DragDropTargetAxis(ImAxis axis)
        => new EndConditionally(ImPlot.EndDragDropTarget, ImPlot.BeginDragDropTargetAxis(axis));

    public static IEndObject DragDropTargetLegend()
        => new EndConditionally(ImPlot.EndDragDropTarget, ImPlot.BeginDragDropTargetLegend());

    public static IEndObject DragDropTargetPlot()
        => new EndConditionally(ImPlot.EndDragDropTarget, ImPlot.BeginDragDropTargetPlot());

    #endregion EndObjects

    #region Style

    public static PlotStyle PushStyle(ImPlotStyleVar idx, int value, bool condition = true)
        => new PlotStyle().Push(idx, value, condition);

    public static PlotStyle PushStyle(ImPlotStyleVar idx, float value, bool condition = true)
        => new PlotStyle().Push(idx, value, condition);

    public static PlotStyle PushStyle(ImPlotStyleVar idx, Vector2 value, bool condition = true)
        => new PlotStyle().Push(idx, value, condition);

    // Push styles that revert all current plot style changes made temporarily.
    public static PlotStyle DefaultPlotStyle()
    {
        var ret = new PlotStyle();
        var reverseStack = PlotStyle.Stack.GroupBy(p => p.Item1).Select(p => (p.Key, p.First().Item2)).ToArray();
        foreach (var (idx, val) in reverseStack)
        {
            if (idx == ImPlotStyleVar.Marker)
                ret.Push(idx, (int)val.X);
            else if (float.IsNaN(val.Y))
                ret.Push(idx, val.X);
            else
                ret.Push(idx, val);
        }

        return ret;
    }

    public sealed class PlotStyle : IDisposable
    {
        internal static readonly List<(ImPlotStyleVar, Vector2)> Stack = new();

        private int count;

        [System.Diagnostics.Conditional("DEBUG")]
        private static void CheckStyleIdx(ImPlotStyleVar idx, Type type)
        {
            var shouldThrow = idx switch
            {
                ImPlotStyleVar.LineWeight => type != typeof(float),
                ImPlotStyleVar.Marker => type != typeof(int),
                ImPlotStyleVar.MarkerSize => type != typeof(float),
                ImPlotStyleVar.MarkerWeight => type != typeof(float),
                ImPlotStyleVar.FillAlpha => type != typeof(float),
                ImPlotStyleVar.ErrorBarSize => type != typeof(float),
                ImPlotStyleVar.ErrorBarWeight => type != typeof(float),
                ImPlotStyleVar.DigitalBitHeight => type != typeof(float),
                ImPlotStyleVar.DigitalBitGap => type != typeof(float),
                ImPlotStyleVar.PlotBorderSize => type != typeof(float),
                ImPlotStyleVar.MinorAlpha => type != typeof(float),
                ImPlotStyleVar.MajorTickLen => type != typeof(Vector2),
                ImPlotStyleVar.MinorTickLen => type != typeof(Vector2),
                ImPlotStyleVar.MajorTickSize => type != typeof(Vector2),
                ImPlotStyleVar.MinorTickSize => type != typeof(Vector2),
                ImPlotStyleVar.MajorGridSize => type != typeof(Vector2),
                ImPlotStyleVar.MinorGridSize => type != typeof(Vector2),
                ImPlotStyleVar.PlotPadding => type != typeof(Vector2),
                ImPlotStyleVar.LabelPadding => type != typeof(Vector2),
                ImPlotStyleVar.LegendPadding => type != typeof(Vector2),
                ImPlotStyleVar.LegendInnerPadding => type != typeof(Vector2),
                ImPlotStyleVar.LegendSpacing => type != typeof(Vector2),
                ImPlotStyleVar.MousePosPadding => type != typeof(Vector2),
                ImPlotStyleVar.AnnotationPadding => type != typeof(Vector2),
                ImPlotStyleVar.FitPadding => type != typeof(Vector2),
                ImPlotStyleVar.PlotDefaultSize => type != typeof(Vector2),
                ImPlotStyleVar.PlotMinSize => type != typeof(Vector2),
                _ => throw new ArgumentOutOfRangeException(nameof(idx), idx, null),
            };

            if (shouldThrow)
                throw new ArgumentException($"Unable to push {type} to {idx}.");
        }

        public static Vector2 GetStyle(ImPlotStyleVar idx)
        {
            var style = ImPlot.GetStyle();
            return idx switch
            {
                ImPlotStyleVar.LineWeight => new Vector2(style.LineWeight, float.NaN),
                ImPlotStyleVar.Marker => new Vector2(style.Marker, float.NaN),
                ImPlotStyleVar.MarkerSize => new Vector2(style.MarkerSize, float.NaN),
                ImPlotStyleVar.MarkerWeight => new Vector2(style.MarkerWeight, float.NaN),
                ImPlotStyleVar.FillAlpha => new Vector2(style.FillAlpha, float.NaN),
                ImPlotStyleVar.ErrorBarSize => new Vector2(style.ErrorBarSize, float.NaN),
                ImPlotStyleVar.ErrorBarWeight => new Vector2(style.ErrorBarWeight, float.NaN),
                ImPlotStyleVar.DigitalBitHeight => new Vector2(style.DigitalBitHeight, float.NaN),
                ImPlotStyleVar.DigitalBitGap => new Vector2(style.DigitalBitGap, float.NaN),
                ImPlotStyleVar.PlotBorderSize => new Vector2(style.PlotBorderSize, float.NaN),
                ImPlotStyleVar.MinorAlpha => new Vector2(style.MinorAlpha, float.NaN),
                ImPlotStyleVar.MajorTickLen => style.MajorTickLen,
                ImPlotStyleVar.MinorTickLen => style.MinorTickLen,
                ImPlotStyleVar.MajorTickSize => style.MajorTickSize,
                ImPlotStyleVar.MinorTickSize => style.MinorTickSize,
                ImPlotStyleVar.MajorGridSize => style.MajorGridSize,
                ImPlotStyleVar.MinorGridSize => style.MinorGridSize,
                ImPlotStyleVar.PlotPadding => style.PlotPadding,
                ImPlotStyleVar.LabelPadding => style.LabelPadding,
                ImPlotStyleVar.LegendPadding => style.LegendPadding,
                ImPlotStyleVar.LegendInnerPadding => style.LegendInnerPadding,
                ImPlotStyleVar.LegendSpacing => style.LegendSpacing,
                ImPlotStyleVar.MousePosPadding => style.MousePosPadding,
                ImPlotStyleVar.AnnotationPadding => style.AnnotationPadding,
                ImPlotStyleVar.FitPadding => style.FitPadding,
                ImPlotStyleVar.PlotDefaultSize => style.PlotDefaultSize,
                ImPlotStyleVar.PlotMinSize => style.PlotMinSize,
                _ => throw new ArgumentOutOfRangeException(nameof(idx), idx, null),
            };
        }

        public PlotStyle Push(ImPlotStyleVar idx, int value, bool condition = true)
        {
            if (!condition)
                return this;

            // Should be accurate for +/- 2^24 markers, which is fine, because the only valid range
            // for markers is [-1, 9].
            CheckStyleIdx(idx, typeof(int));
            Stack.Add((idx, GetStyle(idx)));
            ImPlot.PushStyleVar(idx, value);
            ++this.count;

            return this;
        }

        public PlotStyle Push(ImPlotStyleVar idx, float value, bool condition = true)
        {
            if (!condition)
                return this;

            CheckStyleIdx(idx, typeof(float));
            Stack.Add((idx, GetStyle(idx)));
            ImPlot.PushStyleVar(idx, value);
            ++this.count;

            return this;
        }

        public PlotStyle Push(ImPlotStyleVar idx, Vector2 value, bool condition = true)
        {
            if (!condition)
                return this;

            CheckStyleIdx(idx, typeof(Vector2));
            Stack.Add((idx, GetStyle(idx)));
            ImPlot.PushStyleVar(idx, value);
            ++this.count;

            return this;
        }

        public void Pop(int num = 1)
        {
            num = Math.Min(num, this.count);
            this.count -= num;
            ImPlot.PopStyleVar(num);
            Stack.RemoveRange(Stack.Count - num, num);
        }

        public void Dispose()
            => this.Pop(this.count);
    }

    #endregion Style

    #region Color

    public static PlotColor PushColor(ImPlotCol idx, uint color, bool condition = true)
        => new PlotColor().Push(idx, color, condition);

    public static PlotColor PushColor(ImPlotCol idx, Vector4 color, bool condition = true)
        => new PlotColor().Push(idx, color, condition);

    // Push colors that revert all current color changes made temporarily.
    public static PlotColor DefaultPlotColors()
    {
        var ret = new PlotColor();
        var reverseStack = PlotColor.Stack.GroupBy(p => p.Item1).Select(p => (p.Key, p.First().Item2)).ToArray();
        foreach (var (idx, val) in reverseStack)
            ret.Push(idx, val);
        return ret;
    }

    public sealed class PlotColor : IDisposable
    {
        internal static readonly List<(ImPlotCol, uint)> Stack = new();
        private int count;

        // Reimplementation of https://github.com/ocornut/imgui/blob/868facff9ded2d61425c67deeba354eb24275bd1/imgui.cpp#L3035
        // for ImPlot
        private static uint GetColorU32(ImPlotCol idx)
            => ImGui.GetColorU32(ImPlot.GetStyle().Colors[(int)idx]);

        public PlotColor Push(ImPlotCol idx, uint color, bool condition = true)
        {
            if (condition)
            {
                Stack.Add((idx, GetColorU32(idx)));
                ImPlot.PushStyleColor(idx, color);
                ++this.count;
            }

            return this;
        }

        public PlotColor Push(ImPlotCol idx, Vector4 color, bool condition = true)
        {
            if (condition)
            {
                Stack.Add((idx, GetColorU32(idx)));
                ImPlot.PushStyleColor(idx, color);
                ++this.count;
            }

            return this;
        }

        public void Pop(int num = 1)
        {
            num = Math.Min(num, this.count);
            this.count -= num;
            ImPlot.PopStyleColor(num);
            Stack.RemoveRange(Stack.Count - num, num);
        }

        public void Dispose()
            => this.Pop(this.count);
    }

    #endregion Color
}

// ReSharper disable once CheckNamespace

using System.Numerics;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around style pushing. </summary>
    public sealed class PlotStyleDisposable : IDisposable
    {
        internal static readonly List<(ImPlotStyleVar Type, Vector2 Value)> Stack = [];

        /// <summary> The number of styles currently pushed using this disposable. </summary>
        public int Count { get; private set; }

        /// <summary> Push a style variable to the style stack. </summary>
        /// <param name="type"> The type of style variable to change. </param>
        /// <param name="value"> The value to change it to. </param>
        /// <param name="condition"> If this is false, the style is not pushed. </param>
        /// <returns> A disposable object that can be used to push further style variables and pops those style variables after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep styles pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        public PlotStyleDisposable Push(ImPlotStyleVar type, float value, bool condition)
            => condition ? this.Push(type, value) : this;

        /// <inheritdoc cref="Push(ImGuiStyleVar,float,bool)"/>
        public PlotStyleDisposable Push(ImPlotStyleVar type, Vector2 value, bool condition)
            => condition ? this.Push(type, value) : this;

        public PlotStyleDisposable Push(ImPlotStyleVar type, float value)
        {
            CheckStyleIdx(type, typeof(float));
            Stack.Add((type, GetStyle(type)));

            ImPlot.PushStyleVar(type, value);
            ++this.Count;
            return this;
        }

        /// <inheritdoc cref="Push(ImGuiStyleVar,float,bool)"/>
        public PlotStyleDisposable Push(ImPlotStyleVar type, Vector2 value)
        {
            CheckStyleIdx(type, typeof(Vector2));
            Stack.Add((type, GetStyle(type)));

            ImPlot.PushStyleVar(type, value);
            ++this.Count;
            return this;
        }

        /// <summary> Push styles that revert all current style changes made temporarily. </summary>
        public static PlotStyleDisposable PlotDefaultStyle()
        {
            var ret = new PlotStyleDisposable();
            var reverseStack = Stack.GroupBy(p => p.Type).Select(p => (p.Key, p.First().Value)).ToArray();
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

        /// <summary> Push the default value, i.e. the value as if nothing was ever pushed to this, of a style variable to the style stack. </summary>
        /// <param name="type"> The type of style variable to return to its default value. </param>
        /// <returns> A disposable object that can be used to push further style variables and pops those style variables after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep styles pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        public PlotStyleDisposable PlotPushDefault(ImPlotStyleVar type)
        {
            foreach (var styleMod in Stack.Where(m => m.Type == type))
                return Push(type, styleMod.Value);

            return this;
        }

        /// <summary> Pop a number of style variables. </summary>
        /// <param name="num"> The number of style variables to pop. This is clamped to the number of style variables pushed by this object. </param>
        public PlotStyleDisposable Pop(int num = 1)
        {
            num   =  Math.Min(num, this.Count);
            if (num > 0)
            {
                this.Count -= num;
                ImPlot.PopStyleVar(num);
                Stack.RemoveRange(Stack.Count - num, num);
            }

            return this;
        }

        /// <summary> Pop all pushed styles. </summary>
        public void Dispose()
        {
            ImPlot.PopStyleVar(this.Count);
            this.Count = 0;
        }

        /// <summary> Pop a number of style variables. </summary>
        /// <param name="num"> The number of style variables to pop. The number is not checked against the style stack. </param>
        /// <remarks> Avoid using this function, and styles across scopes, as much as possible. </remarks>
        public static void PopUnsafe(int num = 1)
            => ImPlot.PopStyleVar(num);

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
                // ImPlotStyleVar.DigitalBitHeight => type != typeof(float),
                // ImPlotStyleVar.DigitalBitGap => type != typeof(float),
                // ImPlotStyleVar.PlotBorderSize => type != typeof(float),
                ImPlotStyleVar.MinorAlpha => type != typeof(float),
                ImPlotStyleVar.MajorTickLen => type != typeof(Vector2),
                ImPlotStyleVar.MinorTickLen => type != typeof(Vector2),
                ImPlotStyleVar.MajorTickSize => type != typeof(Vector2),
                ImPlotStyleVar.MinorTickSize => type != typeof(Vector2),
                ImPlotStyleVar.MajorGridSize => type != typeof(Vector2),
                ImPlotStyleVar.MinorGridSize => type != typeof(Vector2),
                // ImPlotStyleVar.PlotPadding => type != typeof(Vector2),
                ImPlotStyleVar.LabelPadding => type != typeof(Vector2),
                ImPlotStyleVar.LegendPadding => type != typeof(Vector2),
                ImPlotStyleVar.LegendInnerPadding => type != typeof(Vector2),
                ImPlotStyleVar.LegendSpacing => type != typeof(Vector2),
                ImPlotStyleVar.MousePosPadding => type != typeof(Vector2),
                ImPlotStyleVar.AnnotationPadding => type != typeof(Vector2),
                ImPlotStyleVar.FitPadding => type != typeof(Vector2),
                // ImPlotStyleVar.PlotDefaultSize => type != typeof(Vector2),
                // ImPlotStyleVar.PlotMinSize => type != typeof(Vector2),
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
                // ImPlotStyleVar.DigitalBitHeight => new Vector2(style.DigitalBitHeight, float.NaN),
                // ImPlotStyleVar.DigitalBitGap => new Vector2(style.DigitalBitGap, float.NaN),
                // ImPlotStyleVar.PlotBorderSize => new Vector2(style.PlotBorderSize, float.NaN),
                ImPlotStyleVar.MinorAlpha => new Vector2(style.MinorAlpha, float.NaN),
                ImPlotStyleVar.MajorTickLen => style.MajorTickLen,
                ImPlotStyleVar.MinorTickLen => style.MinorTickLen,
                ImPlotStyleVar.MajorTickSize => style.MajorTickSize,
                ImPlotStyleVar.MinorTickSize => style.MinorTickSize,
                ImPlotStyleVar.MajorGridSize => style.MajorGridSize,
                ImPlotStyleVar.MinorGridSize => style.MinorGridSize,
                // ImPlotStyleVar.PlotPadding => style.PlotPadding,
                ImPlotStyleVar.LabelPadding => style.LabelPadding,
                ImPlotStyleVar.LegendPadding => style.LegendPadding,
                ImPlotStyleVar.LegendInnerPadding => style.LegendInnerPadding,
                ImPlotStyleVar.LegendSpacing => style.LegendSpacing,
                ImPlotStyleVar.MousePosPadding => style.MousePosPadding,
                ImPlotStyleVar.AnnotationPadding => style.AnnotationPadding,
                ImPlotStyleVar.FitPadding => style.FitPadding,
                // ImPlotStyleVar.PlotDefaultSize => style.PlotDefaultSize,
                // ImPlotStyleVar.PlotMinSize => style.PlotMinSize,
                _ => throw new ArgumentOutOfRangeException(nameof(idx), idx, null),
            };
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImPlot;

// ReSharper disable once CheckNamespace
namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    public sealed class PlotColorDisposable : IDisposable
    {
        internal static readonly List<(ImPlotCol Type, uint BackupColor)> Stack = [];

        /// <summary> Gets the number of colors currently pushed using this disposable. </summary>
        public int Count { get; private set; }

        /// <summary> Push a color to the color stack. </summary>
        /// <param name="type"> The type of color to change. </param>
        /// <param name="color"> The color to change it to. </param>
        /// <param name="condition"> If this is false, the color is not pushed. </param>
        /// <returns> A disposable object that can be used to push further colors and pops those colors after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep colors pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        public PlotColorDisposable Push(ImPlotCol type, uint color, bool condition = true)
            => condition ? this.InternalPush(type, color) : this;

        /// <summary> Push a color to the color stack. </summary>
        /// <param name="type"> The type of color to change. </param>
        /// <param name="color"> The color to change it to. </param>
        /// <param name="condition"> If this is false, the color is not pushed. </param>
        /// <returns> A disposable object that can be used to push further colors and pops those colors after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep colors pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        public PlotColorDisposable Push(ImPlotCol type, Vector4 color, bool condition = true)
            => condition ? this.InternalPush(type, color) : this;

        /// <summary> Push a color to the color stack. </summary>
        /// <param name="type"> The type of color to change. </param>
        /// <param name="color"> The color to change it to. If this is null, no color will be set. </param>
        /// <param name="condition"> If this is false, the color is not pushed. </param>
        /// <returns> A disposable object that can be used to push further colors and pops those colors after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep colors pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        public PlotColorDisposable Push(ImPlotCol type, Vector4? color, bool condition = true)
        {
            if (!color.HasValue)
                return this;

            return condition ? this.InternalPush(type, color.Value) : this;
        }

        private PlotColorDisposable InternalPush(ImPlotCol type, uint color)
        {
            Stack.Add((type, GetColorU32(type)));
            ImPlot.PushStyleColor(type, color);
            ++this.Count;
            return this;
        }

        private PlotColorDisposable InternalPush(ImPlotCol type, Vector4 color)
        {
            Stack.Add((type, GetColorU32(type)));
            ImPlot.PushStyleColor(type, color);
            ++this.Count;
            return this;
        }

        /// <summary> Reverts all pushed colors to their previous values temporarily. </summary>
        /// <returns> A disposable object that can be used to push further colors and pops those colors after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep colors pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        public static PlotColorDisposable PlotDefaultColors()
        {
            var ret = new PlotColorDisposable();
            var reverseStack = Stack.GroupBy(p => p.Type).Select(p => (p.Key, p.First().BackupColor)).ToArray();
            foreach (var (idx, val) in reverseStack)
                ret.Push(idx, val);
            return ret;
        }

        /// <summary> Push the default value, i.e. the value as if nothing was ever pushed to this, of a color to the color stack. </summary>
        /// <param name="type"> The type of color to return to its default value. </param>
        /// <returns> A disposable object that can be used to push further colors and pops those colors after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep colors pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        public PlotColorDisposable PlotPushDefault(ImPlotCol type)
        {
            foreach (var styleMod in Stack.Where(m => m.Type == type))
                return this.Push(type, styleMod.BackupColor);

            return this;
        }

        /// <summary> Pop a number of colors. </summary>
        /// <param name="num"> The number of colors to pop. This is clamped to the number of colors pushed by this object. </param>
        public PlotColorDisposable Pop(int num = 1)
        {
            num = Math.Min(num, this.Count);
            if (num > 0)
            {
                this.Count -= num;
                ImPlot.PopStyleColor(num);
                Stack.RemoveRange(Stack.Count - num, num);
            }

            return this;
        }

        /// <summary> Pop all pushed colors. </summary>
        public void Dispose()
        {
            this.Pop(this.Count);
            this.Count = 0;
        }

        /// <summary> Pop a number of colors. </summary>
        /// <param name="num"> The number of colors to pop. The number is not checked against the color stack. </param>
        /// <remarks> Avoid using this function, and colors across scopes, as much as possible. </remarks>
        public static void PopUnsafe(int num = 1)
            => ImPlot.PopStyleColor(num);

        // Reimplementation of https://github.com/ocornut/imgui/blob/868facff9ded2d61425c67deeba354eb24275bd1/imgui.cpp#L3035
        // for ImPlot
        private static uint GetColorU32(ImPlotCol idx)
            => ImGui.GetColorU32(ImPlot.GetStyle().Colors[(int)idx]);
    }
}

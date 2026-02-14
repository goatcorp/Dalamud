// ReSharper disable once CheckNamespace

using System.Numerics;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Utility.Raii;

public static partial class ImRaii
{
    /// <summary> A wrapper around style pushing. </summary>
    public sealed class StyleDisposable : IDisposable
    {
        internal static readonly List<(ImGuiStyleVar Type, Vector2 Value)> Stack = [];

        /// <summary> The number of styles currently pushed using this disposable. </summary>
        public int Count { get; private set; }

        /// <summary> Push a style variable to the style stack. </summary>
        /// <param name="type"> The type of style variable to change. </param>
        /// <param name="value"> The value to change it to. </param>
        /// <param name="condition"> If this is false, the style is not pushed. </param>
        /// <returns> A disposable object that can be used to push further style variables and pops those style variables after leaving scope. Use with using. </returns>
        /// <remarks> If you need to keep styles pushed longer than the current scope, use without using and use <seealso cref="PopUnsafe"/>. </remarks>
        public StyleDisposable Push(ImGuiStyleVar type, float value, bool condition)
            => condition ? this.Push(type, value) : this;

        /// <inheritdoc cref="Push(ImGuiStyleVar,float,bool)"/>
        public StyleDisposable Push(ImGuiStyleVar type, Vector2 value, bool condition)
            => condition ? this.Push(type, value) : this;

        public StyleDisposable Push(ImGuiStyleVar type, float value)
        {
            CheckStyleIdx(type, typeof(float));
            Stack.Add((type, GetStyle(type)));

            ImGui.PushStyleVar(type, value);
            ++this.Count;
            return this;
        }

        /// <inheritdoc cref="Push(ImGuiStyleVar,float,bool)"/>
        public StyleDisposable Push(ImGuiStyleVar type, Vector2 value)
        {
            CheckStyleIdx(type, typeof(Vector2));
            Stack.Add((type, GetStyle(type)));

            ImGui.PushStyleVar(type, value);
            ++this.Count;
            return this;
        }

        /// <summary> Push styles that revert all current style changes made temporarily. </summary>
        public static StyleDisposable DefaultStyle()
        {
            var ret = new StyleDisposable();
            var reverseStack = Stack.GroupBy(p => p.Type).Select(p => (p.Key, p.First().Value)).ToArray();
            foreach (var (idx, val) in reverseStack)
            {
                if (float.IsNaN(val.Y))
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
        public StyleDisposable PushDefault(ImGuiStyleVar type)
        {
            foreach (var styleMod in Stack.Where(m => m.Type == type))
                return Push(type, styleMod.Value);

            return this;
        }

        /// <summary> Pop a number of style variables. </summary>
        /// <param name="num"> The number of style variables to pop. This is clamped to the number of style variables pushed by this object. </param>
        public StyleDisposable Pop(int num = 1)
        {
            num   =  Math.Min(num, this.Count);
            if (num > 0)
            {
                this.Count -= num;
                ImGui.PopStyleVar(num);
                Stack.RemoveRange(Stack.Count - num, num);
            }

            return this;
        }

        /// <summary> Pop all pushed styles. </summary>
        public void Dispose()
        {
            ImGui.PopStyleVar(this.Count);
            this.Count = 0;
        }

        /// <summary> Pop a number of style variables. </summary>
        /// <param name="num"> The number of style variables to pop. The number is not checked against the style stack. </param>
        /// <remarks> Avoid using this function, and styles across scopes, as much as possible. </remarks>
        public static void PopUnsafe(int num = 1)
            => ImGui.PopStyleVar(num);

        private static void CheckStyleIdx(ImGuiStyleVar idx, Type type)
        {
            var shouldThrow = idx switch
            {
                ImGuiStyleVar.Alpha               => type != typeof(float),
                ImGuiStyleVar.WindowPadding       => type != typeof(Vector2),
                ImGuiStyleVar.WindowRounding      => type != typeof(float),
                ImGuiStyleVar.WindowBorderSize    => type != typeof(float),
                ImGuiStyleVar.WindowMinSize       => type != typeof(Vector2),
                ImGuiStyleVar.WindowTitleAlign    => type != typeof(Vector2),
                ImGuiStyleVar.ChildRounding       => type != typeof(float),
                ImGuiStyleVar.ChildBorderSize     => type != typeof(float),
                ImGuiStyleVar.PopupRounding       => type != typeof(float),
                ImGuiStyleVar.PopupBorderSize     => type != typeof(float),
                ImGuiStyleVar.FramePadding        => type != typeof(Vector2),
                ImGuiStyleVar.FrameRounding       => type != typeof(float),
                ImGuiStyleVar.FrameBorderSize     => type != typeof(float),
                ImGuiStyleVar.ItemSpacing         => type != typeof(Vector2),
                ImGuiStyleVar.ItemInnerSpacing    => type != typeof(Vector2),
                ImGuiStyleVar.IndentSpacing       => type != typeof(float),
                ImGuiStyleVar.CellPadding         => type != typeof(Vector2),
                ImGuiStyleVar.ScrollbarSize       => type != typeof(float),
                ImGuiStyleVar.ScrollbarRounding   => type != typeof(float),
                ImGuiStyleVar.GrabMinSize         => type != typeof(float),
                ImGuiStyleVar.GrabRounding        => type != typeof(float),
                ImGuiStyleVar.TabRounding         => type != typeof(float),
                ImGuiStyleVar.ButtonTextAlign     => type != typeof(Vector2),
                ImGuiStyleVar.SelectableTextAlign => type != typeof(Vector2),
                ImGuiStyleVar.DisabledAlpha       => type != typeof(float),
                _                                 => throw new ArgumentOutOfRangeException(nameof(idx), idx, null),
            };

            if (shouldThrow)
                throw new ArgumentException($"Unable to push {type} to {idx}.");
        }

        public static Vector2 GetStyle(ImGuiStyleVar idx)
        {
            var style = ImGui.GetStyle();
            return idx switch
            {
                ImGuiStyleVar.Alpha               => new Vector2(style.Alpha, float.NaN),
                ImGuiStyleVar.WindowPadding       => style.WindowPadding,
                ImGuiStyleVar.WindowRounding      => new Vector2(style.WindowRounding,   float.NaN),
                ImGuiStyleVar.WindowBorderSize    => new Vector2(style.WindowBorderSize, float.NaN),
                ImGuiStyleVar.WindowMinSize       => style.WindowMinSize,
                ImGuiStyleVar.WindowTitleAlign    => style.WindowTitleAlign,
                ImGuiStyleVar.ChildRounding       => new Vector2(style.ChildRounding,   float.NaN),
                ImGuiStyleVar.ChildBorderSize     => new Vector2(style.ChildBorderSize, float.NaN),
                ImGuiStyleVar.PopupRounding       => new Vector2(style.PopupRounding,   float.NaN),
                ImGuiStyleVar.PopupBorderSize     => new Vector2(style.PopupBorderSize, float.NaN),
                ImGuiStyleVar.FramePadding        => style.FramePadding,
                ImGuiStyleVar.FrameRounding       => new Vector2(style.FrameRounding,   float.NaN),
                ImGuiStyleVar.FrameBorderSize     => new Vector2(style.FrameBorderSize, float.NaN),
                ImGuiStyleVar.ItemSpacing         => style.ItemSpacing,
                ImGuiStyleVar.ItemInnerSpacing    => style.ItemInnerSpacing,
                ImGuiStyleVar.IndentSpacing       => new Vector2(style.IndentSpacing, float.NaN),
                ImGuiStyleVar.CellPadding         => style.CellPadding,
                ImGuiStyleVar.ScrollbarSize       => new Vector2(style.ScrollbarSize,     float.NaN),
                ImGuiStyleVar.ScrollbarRounding   => new Vector2(style.ScrollbarRounding, float.NaN),
                ImGuiStyleVar.GrabMinSize         => new Vector2(style.GrabMinSize,       float.NaN),
                ImGuiStyleVar.GrabRounding        => new Vector2(style.GrabRounding,      float.NaN),
                ImGuiStyleVar.TabRounding         => new Vector2(style.TabRounding,       float.NaN),
                ImGuiStyleVar.ButtonTextAlign     => style.ButtonTextAlign,
                ImGuiStyleVar.SelectableTextAlign => style.SelectableTextAlign,
                ImGuiStyleVar.DisabledAlpha       => new Vector2(style.DisabledAlpha, float.NaN),
                _                                 => throw new ArgumentOutOfRangeException(nameof(idx), idx, null),
            };
        }
    }
}

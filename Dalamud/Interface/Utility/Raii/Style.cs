using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Utility.Raii;

// Push an arbitrary amount of styles into an object that are all popped when it is disposed.
// If condition is false, no style is pushed.
// In debug mode, checks that the type of the value given for the style is valid.
public static partial class ImRaii
{
    public static Style PushStyle(ImGuiStyleVar idx, float value, bool condition = true)
        => new Style().Push(idx, value, condition);

    public static Style PushStyle(ImGuiStyleVar idx, Vector2 value, bool condition = true)
        => new Style().Push(idx, value, condition);

    // Push styles that revert all current style changes made temporarily.
    public static Style DefaultStyle()
    {
        var ret = new Style();
        var reverseStack = Style.Stack.GroupBy(p => p.Item1).Select(p => (p.Key, p.First().Item2)).ToArray();
        foreach (var (idx, val) in reverseStack)
        {
            if (float.IsNaN(val.Y))
                ret.Push(idx, val.X);
            else
                ret.Push(idx, val);
        }

        return ret;
    }

    public sealed class Style : IDisposable
    {
        internal static readonly List<(ImGuiStyleVar, Vector2)> Stack = new();

        private int count;

        [System.Diagnostics.Conditional("DEBUG")]
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

        public Style Push(ImGuiStyleVar idx, float value, bool condition = true)
        {
            if (!condition)
                return this;

            CheckStyleIdx(idx, typeof(float));
            Stack.Add((idx, GetStyle(idx)));
            ImGui.PushStyleVar(idx, value);
            ++this.count;

            return this;
        }

        public Style Push(ImGuiStyleVar idx, Vector2 value, bool condition = true)
        {
            if (!condition)
                return this;

            CheckStyleIdx(idx, typeof(Vector2));
            Stack.Add((idx, GetStyle(idx)));
            ImGui.PushStyleVar(idx, value);
            ++this.count;

            return this;
        }

        public void Pop(int num = 1)
        {
            num    =  Math.Min(num, this.count);
            this.count -= num;
            ImGui.PopStyleVar(num);
            Stack.RemoveRange(Stack.Count - num, num);
        }

        public void Dispose()
            => this.Pop(this.count);
    }
}

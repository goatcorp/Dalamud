using System.Collections.Generic;
using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface
{
    /// <summary>
    /// Class containing various helper methods for use with ImGui inside Dalamud.
    /// </summary>
    public static class ImGuiHelpers
    {
        /// <summary>
        /// Gets the main viewport.
        /// </summary>
        public static ImGuiViewportPtr MainViewport { get; internal set; }

        /// <summary>
        /// Gets the global Dalamud scale.
        /// </summary>
        public static float GlobalScale { get; private set; }

        /// <summary>
        /// Gets a <see cref="Vector2"/> that is pre-scaled with the <see cref="GlobalScale"/> multiplier.
        /// </summary>
        /// <param name="x">Vector2 X parameter.</param>
        /// <param name="y">Vector2 Y parameter.</param>
        /// <returns>A scaled Vector2.</returns>
        public static Vector2 ScaledVector2(float x, float y) => new Vector2(x, y) * GlobalScale;

        /// <summary>
        /// Gets a <see cref="Vector4"/> that is pre-scaled with the <see cref="GlobalScale"/> multiplier.
        /// </summary>
        /// <param name="x">Vector4 X parameter.</param>
        /// <param name="y">Vector4 Y parameter.</param>
        /// <param name="z">Vector4 Z parameter.</param>
        /// <param name="w">Vector4 W parameter.</param>
        /// <returns>A scaled Vector2.</returns>
        public static Vector4 ScaledVector4(float x, float y, float z, float w) => new Vector4(x, y, z, w) * GlobalScale;

        /// <summary>
        /// Force the next ImGui window to stay inside the main game window.
        /// </summary>
        public static void ForceNextWindowMainViewport() => ImGui.SetNextWindowViewport(MainViewport.ID);

        /// <summary>
        /// Create a dummy scaled by the global Dalamud scale.
        /// </summary>
        /// <param name="size">The size of the dummy.</param>
        public static void ScaledDummy(float size) => ScaledDummy(size, size);

        /// <summary>
        /// Create a dummy scaled by the global Dalamud scale.
        /// </summary>
        /// <param name="x">Vector2 X parameter.</param>
        /// <param name="y">Vector2 Y parameter.</param>
        public static void ScaledDummy(float x, float y) => ScaledDummy(new Vector2(x, y));

        /// <summary>
        /// Create a dummy scaled by the global Dalamud scale.
        /// </summary>
        /// <param name="size">The size of the dummy.</param>
        public static void ScaledDummy(Vector2 size) => ImGui.Dummy(size * GlobalScale);

        /// <summary>
        /// Use a relative ImGui.SameLine() from your current cursor position, scaled by the Dalamud global scale.
        /// </summary>
        /// <param name="offset">The offset from your current cursor position.</param>
        /// <param name="spacing">The spacing to use.</param>
        public static void ScaledRelativeSameLine(float offset, float spacing = -1.0f)
            => ImGui.SameLine(ImGui.GetCursorPosX() + (offset * GlobalScale), spacing);

        /// <summary>
        /// Set the position of the next window relative to the main viewport.
        /// </summary>
        /// <param name="position">The position of the next window.</param>
        /// <param name="condition">When to set the position.</param>
        /// <param name="pivot">The pivot to set the position around.</param>
        public static void SetNextWindowPosRelativeMainViewport(Vector2 position, ImGuiCond condition = ImGuiCond.None, Vector2 pivot = default)
            => ImGui.SetNextWindowPos(position + MainViewport.Pos, condition, pivot);

        /// <summary>
        /// Set the position of a window relative to the main viewport.
        /// </summary>
        /// <param name="name">The name/ID of the window.</param>
        /// <param name="position">The position of the window.</param>
        /// <param name="condition">When to set the position.</param>
        public static void SetWindowPosRelativeMainViewport(string name, Vector2 position, ImGuiCond condition = ImGuiCond.None)
            => ImGui.SetWindowPos(name, position + MainViewport.Pos, condition);

        /// <summary>
        /// Creates default color palette for use with color pickers.
        /// </summary>
        /// <param name="swatchCount">The total number of swatches to use.</param>
        /// <returns>Default color palette.</returns>
        public static List<Vector4> DefaultColorPalette(int swatchCount = 32)
        {
            var colorPalette = new List<Vector4>();
            for (var i = 0; i < swatchCount; i++)
            {
                ImGui.ColorConvertHSVtoRGB(i / 31.0f, 0.7f, 0.8f, out var r, out var g, out var b);
                colorPalette.Add(new Vector4(r, g, b, 1.0f));
            }

            return colorPalette;
        }

        /// <summary>
        /// Get the size of a button considering the default frame padding.
        /// </summary>
        /// <param name="text">Text in the button.</param>
        /// <returns><see cref="Vector2"/> with the size of the button.</returns>
        public static Vector2 GetButtonSize(string text) => ImGui.CalcTextSize(text) + (ImGui.GetStyle().FramePadding * 2);

        /// <summary>
        /// Print out text that can be copied when clicked.
        /// </summary>
        /// <param name="text">The text to show.</param>
        /// <param name="textCopy">The text to copy when clicked.</param>
        public static void ClickToCopyText(string text, string? textCopy = null)
        {
            textCopy ??= text;
            ImGui.Text($"{text}");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (textCopy != text) ImGui.SetTooltip(textCopy);
            }

            if (ImGui.IsItemClicked()) ImGui.SetClipboardText($"{textCopy}");
        }

        /// <summary>
        /// Get data needed for each new frame.
        /// </summary>
        internal static void NewFrame()
        {
            GlobalScale = ImGui.GetIO().FontGlobalScale;
        }
    }
}

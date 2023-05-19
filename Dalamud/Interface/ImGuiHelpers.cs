using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Raii;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Interface;

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
    /// <param name="x">Vector2 X/Y parameter.</param>
    /// <returns>A scaled Vector2.</returns>
    public static Vector2 ScaledVector2(float x) => new Vector2(x, x) * GlobalScale;

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
    /// Create an indent scaled by the global Dalamud scale.
    /// </summary>
    /// <param name="size">The size of the indent.</param>
    public static void ScaledIndent(float size) => ImGui.Indent(size * GlobalScale);
    
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
    /// Write unformatted text wrapped.
    /// </summary>
    /// <param name="text">The text to write.</param>
    public static void SafeTextWrapped(string text) => ImGui.TextWrapped(text.Replace("%", "%%"));

    /// <summary>
    /// Write unformatted text wrapped.
    /// </summary>
    /// <param name="color">The color of the text.</param>
    /// <param name="text">The text to write.</param>
    public static void SafeTextColoredWrapped(Vector4 color, string text)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, color))
        {
            ImGui.TextWrapped(text.Replace("%", "%%"));
        }
    }

    /// <summary>
    /// Fills missing glyphs in target font from source font, if both are not null.
    /// </summary>
    /// <param name="source">Source font.</param>
    /// <param name="target">Target font.</param>
    /// <param name="missingOnly">Whether to copy missing glyphs only.</param>
    /// <param name="rebuildLookupTable">Whether to call target.BuildLookupTable().</param>
    /// <param name="rangeLow">Low codepoint range to copy.</param>
    /// <param name="rangeHigh">High codepoing range to copy.</param>
    public static void CopyGlyphsAcrossFonts(ImFontPtr? source, ImFontPtr? target, bool missingOnly, bool rebuildLookupTable, int rangeLow = 32, int rangeHigh = 0xFFFE)
    {
        if (!source.HasValue || !target.HasValue)
            return;

        var scale = target.Value!.FontSize / source.Value!.FontSize;
        var addedCodepoints = new HashSet<int>();
        unsafe
        {
            var glyphs = (ImFontGlyphReal*)source.Value!.Glyphs.Data;
            for (int j = 0, k = source.Value!.Glyphs.Size; j < k; j++)
            {
                Debug.Assert(glyphs != null, nameof(glyphs) + " != null");

                var glyph = &glyphs[j];
                if (glyph->Codepoint < rangeLow || glyph->Codepoint > rangeHigh)
                    continue;

                var prevGlyphPtr = (ImFontGlyphReal*)target.Value!.FindGlyphNoFallback((ushort)glyph->Codepoint).NativePtr;
                if ((IntPtr)prevGlyphPtr == IntPtr.Zero)
                {
                    addedCodepoints.Add(glyph->Codepoint);
                    target.Value!.AddGlyph(
                        target.Value!.ConfigData,
                        (ushort)glyph->Codepoint,
                        glyph->TextureIndex,
                        glyph->X0 * scale,
                        ((glyph->Y0 - source.Value!.Ascent) * scale) + target.Value!.Ascent,
                        glyph->X1 * scale,
                        ((glyph->Y1 - source.Value!.Ascent) * scale) + target.Value!.Ascent,
                        glyph->U0,
                        glyph->V0,
                        glyph->U1,
                        glyph->V1,
                        glyph->AdvanceX * scale);
                }
                else if (!missingOnly)
                {
                    addedCodepoints.Add(glyph->Codepoint);
                    prevGlyphPtr->TextureIndex = glyph->TextureIndex;
                    prevGlyphPtr->X0 = glyph->X0 * scale;
                    prevGlyphPtr->Y0 = ((glyph->Y0 - source.Value!.Ascent) * scale) + target.Value!.Ascent;
                    prevGlyphPtr->X1 = glyph->X1 * scale;
                    prevGlyphPtr->Y1 = ((glyph->Y1 - source.Value!.Ascent) * scale) + target.Value!.Ascent;
                    prevGlyphPtr->U0 = glyph->U0;
                    prevGlyphPtr->V0 = glyph->V0;
                    prevGlyphPtr->U1 = glyph->U1;
                    prevGlyphPtr->V1 = glyph->V1;
                    prevGlyphPtr->AdvanceX = glyph->AdvanceX * scale;
                }
            }

            var kernPairs = source.Value!.KerningPairs;
            for (int j = 0, k = kernPairs.Size; j < k; j++)
            {
                if (!addedCodepoints.Contains(kernPairs[j].Left))
                    continue;
                if (!addedCodepoints.Contains(kernPairs[j].Right))
                    continue;
                target.Value.AddKerningPair(kernPairs[j].Left, kernPairs[j].Right, kernPairs[j].AdvanceXAdjustment);
            }
        }

        if (rebuildLookupTable && target.Value!.Glyphs.Size > 0)
            target.Value!.BuildLookupTable();
    }

    /// <summary>
    /// Map a VirtualKey keycode to an ImGuiKey enum value.
    /// </summary>
    /// <param name="key">The VirtualKey value to retrieve the ImGuiKey counterpart for.</param>
    /// <returns>The ImGuiKey that corresponds to this VirtualKey, or <c>ImGuiKey.None</c> otherwise.</returns>
    public static ImGuiKey VirtualKeyToImGuiKey(VirtualKey key)
    {
        return ImGui_Input_Impl_Direct.VirtualKeyToImGuiKey((int)key);
    }

    /// <summary>
    /// Map an ImGuiKey enum value to a VirtualKey code.
    /// </summary>
    /// <param name="key">The ImGuiKey value to retrieve the VirtualKey counterpart for.</param>
    /// <returns>The VirtualKey that corresponds to this ImGuiKey, or <c>VirtualKey.NO_KEY</c> otherwise.</returns>
    public static VirtualKey ImGuiKeyToVirtualKey(ImGuiKey key)
    {
        return (VirtualKey)ImGui_Input_Impl_Direct.ImGuiKeyToVirtualKey(key);
    }

    /// <summary>
    /// Show centered text.
    /// </summary>
    /// <param name="text">Text to show.</param>
    public static void CenteredText(string text)
    {
        CenterCursorForText(text);
        ImGui.TextUnformatted(text);
    }

    /// <summary>
    /// Center the ImGui cursor for a certain text.
    /// </summary>
    /// <param name="text">The text to center for.</param>
    public static void CenterCursorForText(string text)
    {
        var textWidth = ImGui.CalcTextSize(text).X;
        CenterCursorFor((int)textWidth);
    }

    /// <summary>
    /// Center the ImGui cursor for an item with a certain width.
    /// </summary>
    /// <param name="itemWidth">The width to center for.</param>
    public static void CenterCursorFor(int itemWidth)
    {
        var window = (int)ImGui.GetWindowWidth();
        ImGui.SetCursorPosX((window / 2) - (itemWidth / 2));
    }

    /// <summary>
    /// Get data needed for each new frame.
    /// </summary>
    internal static void NewFrame()
    {
        GlobalScale = ImGui.GetIO().FontGlobalScale;
        InterfaceHelpers.GlobalScale = GlobalScale;
    }

    /// <summary>
    /// ImFontGlyph the correct version.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "ImGui internals")]
    public struct ImFontGlyphReal
    {
        public uint ColoredVisibleTextureIndexCodepoint;
        public float AdvanceX;
        public float X0;
        public float Y0;
        public float X1;
        public float Y1;
        public float U0;
        public float V0;
        public float U1;
        public float V1;

        private const uint ColoredMask /*****/ = 0b_00000000_00000000_00000000_00000001u;
        private const uint VisibleMask /*****/ = 0b_00000000_00000000_00000000_00000010u;
        private const uint TextureMask /*****/ = 0b_00000000_00000000_00000111_11111100u;
        private const uint CodepointMask /***/ = 0b_11111111_11111111_11111000_00000000u;

        private const int ColoredShift = 0;
        private const int VisibleShift = 1;
        private const int TextureShift = 2;
        private const int CodepointShift = 11;

        public bool Colored
        {
            get => (int)((this.ColoredVisibleTextureIndexCodepoint & ColoredMask) >> ColoredShift) != 0;
            set => this.ColoredVisibleTextureIndexCodepoint = (this.ColoredVisibleTextureIndexCodepoint & ~ColoredMask) | (value ? 1u << ColoredShift : 0u);
        }

        public bool Visible
        {
            get => (int)((this.ColoredVisibleTextureIndexCodepoint & VisibleMask) >> VisibleShift) != 0;
            set => this.ColoredVisibleTextureIndexCodepoint = (this.ColoredVisibleTextureIndexCodepoint & ~VisibleMask) | (value ? 1u << VisibleShift : 0u);
        }

        public int TextureIndex
        {
            get => (int)(this.ColoredVisibleTextureIndexCodepoint & TextureMask) >> TextureShift;
            set => this.ColoredVisibleTextureIndexCodepoint = (this.ColoredVisibleTextureIndexCodepoint & ~TextureMask) | ((uint)value << TextureShift);
        }

        public int Codepoint
        {
            get => (int)(this.ColoredVisibleTextureIndexCodepoint & CodepointMask) >> CodepointShift;
            set => this.ColoredVisibleTextureIndexCodepoint = (this.ColoredVisibleTextureIndexCodepoint & ~CodepointMask) | ((uint)value << CodepointShift);
        }
    }

    /// <summary>
    /// ImFontGlyphHotData the correct version.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "ImGui internals")]
    public struct ImFontGlyphHotDataReal
    {
        public float AdvanceX;
        public float OccupiedWidth;
        public uint KerningPairInfo;

        private const uint UseBisectMask /***/ = 0b_00000000_00000000_00000000_00000001u;
        private const uint OffsetMask /******/ = 0b_00000000_00001111_11111111_11111110u;
        private const uint CountMask /*******/ = 0b_11111111_11110000_00000111_11111100u;

        private const int UseBisectShift = 0;
        private const int OffsetShift = 1;
        private const int CountShift = 20;

        public bool UseBisect
        {
            get => (int)((this.KerningPairInfo & UseBisectMask) >> UseBisectShift) != 0;
            set => this.KerningPairInfo = (this.KerningPairInfo & ~UseBisectMask) | (value ? 1u << UseBisectShift : 0u);
        }

        public bool Offset
        {
            get => (int)((this.KerningPairInfo & OffsetMask) >> OffsetShift) != 0;
            set => this.KerningPairInfo = (this.KerningPairInfo & ~OffsetMask) | (value ? 1u << OffsetShift : 0u);
        }

        public int Count
        {
            get => (int)(this.KerningPairInfo & CountMask) >> CountShift;
            set => this.KerningPairInfo = (this.KerningPairInfo & ~CountMask) | ((uint)value << CountShift);
        }
    }

    /// <summary>
    /// ImFontAtlasCustomRect the correct version.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "ImGui internals")]
    public unsafe struct ImFontAtlasCustomRectReal
    {
        public ushort Width;
        public ushort Height;
        public ushort X;
        public ushort Y;
        public uint TextureIndexAndGlyphId;
        public float GlyphAdvanceX;
        public Vector2 GlyphOffset;
        public ImFont* Font;

        private const uint TextureIndexMask /***/ = 0b_00000000_00000000_00000111_11111100u;
        private const uint GlyphIDMask /********/ = 0b_11111111_11111111_11111000_00000000u;

        private const int TextureIndexShift = 2;
        private const int GlyphIDShift = 11;

        public int TextureIndex
        {
            get => (int)(this.TextureIndexAndGlyphId & TextureIndexMask) >> TextureIndexShift;
            set => this.TextureIndexAndGlyphId = (this.TextureIndexAndGlyphId & ~TextureIndexMask) | ((uint)value << TextureIndexShift);
        }

        public int GlyphId
        {
            get => (int)(this.TextureIndexAndGlyphId & GlyphIDMask) >> GlyphIDShift;
            set => this.TextureIndexAndGlyphId = (this.TextureIndexAndGlyphId & ~GlyphIDMask) | ((uint)value << GlyphIDShift);
        }
    }
}

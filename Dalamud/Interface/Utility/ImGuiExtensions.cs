using System.Numerics;
using System.Text;

using ImGuiNET;

namespace Dalamud.Interface.Utility;

/// <summary>
/// Class containing various extensions to ImGui, aiding with building custom widgets.
/// </summary>
public static class ImGuiExtensions
{
    /// <summary>
    /// Draw clipped text.
    /// </summary>
    /// <param name="drawListPtr">Pointer to the draw list.</param>
    /// <param name="posMin">Minimum position.</param>
    /// <param name="posMax">Maximum position.</param>
    /// <param name="text">Text to draw.</param>
    /// <param name="textSizeIfKnown">Size of the text, if known.</param>
    /// <param name="align">Alignment.</param>
    /// <param name="clipRect">Clip rect to use.</param>
    public static void AddTextClippedEx(this ImDrawListPtr drawListPtr, Vector2 posMin, Vector2 posMax, string text, Vector2? textSizeIfKnown, Vector2 align, Vector4? clipRect)
    {
        var pos = posMin;
        var textSize = textSizeIfKnown ?? ImGui.CalcTextSize(text, false, 0);

        var clipMin = clipRect.HasValue ? new Vector2(clipRect.Value.X, clipRect.Value.Y) : posMin;
        var clipMax = clipRect.HasValue ? new Vector2(clipRect.Value.Z, clipRect.Value.W) : posMax;

        var needClipping = (pos.X + textSize.X >= clipMax.X) || (pos.Y + textSize.Y >= clipMax.Y);
        if (clipRect.HasValue)
            needClipping |= (pos.X < clipMin.X) || (pos.Y < clipMin.Y);

        if (align.X > 0)
        {
            pos.X = Math.Max(pos.X, pos.X + ((posMax.X - pos.X - textSize.X) * align.X));
        }

        if (align.Y > 0)
        {
            pos.Y = Math.Max(pos.Y, pos.Y + ((posMax.Y - pos.Y - textSize.Y) * align.Y));
        }

        if (needClipping)
        {
            var fineClipRect = new Vector4(clipMin.X, clipMin.Y, clipMax.X, clipMax.Y);
            drawListPtr.AddText(ImGui.GetFont(), ImGui.GetFontSize(), pos, ImGui.GetColorU32(ImGuiCol.Text), text, ref fineClipRect);
        }
        else
        {
            drawListPtr.AddText(ImGui.GetFont(), ImGui.GetFontSize(), pos, ImGui.GetColorU32(ImGuiCol.Text), text);
        }
    }

    /// <summary>
    /// Add text to a draw list.
    /// </summary>
    /// <param name="drawListPtr">Pointer to the draw list.</param>
    /// <param name="font">Font to use.</param>
    /// <param name="fontSize">Font size.</param>
    /// <param name="pos">Position to draw at.</param>
    /// <param name="col">Color to use.</param>
    /// <param name="textBegin">Text to draw.</param>
    /// <param name="cpuFineClipRect">Clip rect to use.</param>
    // TODO: This should go into ImDrawList.Manual.cs in ImGui.NET...
    public static unsafe void AddText(this ImDrawListPtr drawListPtr, ImFontPtr font, float fontSize, Vector2 pos, uint col, string textBegin, ref Vector4 cpuFineClipRect)
    {
        var nativeFont = font.NativePtr;
        var textBeginByteCount = Encoding.UTF8.GetByteCount(textBegin);
        var nativeTextBegin = stackalloc byte[textBeginByteCount + 1];

        fixed (char* textBeginPtr = textBegin)
        {
            var nativeTextBeginOffset = Encoding.UTF8.GetBytes(textBeginPtr, textBegin.Length, nativeTextBegin, textBeginByteCount);
            nativeTextBegin[nativeTextBeginOffset] = 0;
        }

        byte* nativeTextEnd = null;
        var wrapWidth = 0.0f;

        fixed (Vector4* nativeCpuFineClipRect = &cpuFineClipRect)
        {
            ImGuiNative.ImDrawList_AddText_FontPtr(drawListPtr.NativePtr, nativeFont, fontSize, pos, col, nativeTextBegin, nativeTextEnd, wrapWidth, nativeCpuFineClipRect);
        }
    }
}

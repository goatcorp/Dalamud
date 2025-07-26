using System.Numerics;

using Dalamud.Bindings.ImGui;

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
    public static unsafe void AddTextClippedEx(this ImDrawListPtr drawListPtr, Vector2 posMin, Vector2 posMax, string text, Vector2? textSizeIfKnown, Vector2 align, Vector4? clipRect)
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
            drawListPtr.AddText(
                ImGui.GetFont(),
                ImGui.GetFontSize(),
                pos,
                ImGui.GetColorU32(ImGuiCol.Text),
                text,
                0,
                fineClipRect);
        }
        else
        {
            drawListPtr.AddText(ImGui.GetFont(), ImGui.GetFontSize(), pos, ImGui.GetColorU32(ImGuiCol.Text), text);
        }
    }
}

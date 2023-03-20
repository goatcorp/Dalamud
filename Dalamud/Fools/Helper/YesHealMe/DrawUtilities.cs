using System;
using System.Numerics;
using ImGuiNET;
using KamiLib.Caching;

namespace Dalamud.Fools.Helper.YesHealMe;

internal static class DrawUtilities
{
    public static void TextOutlined(FontManager fontManager, Vector2 startingPosition, string text, float scale, Vector4 color)
    {
        startingPosition = startingPosition.Ceil();

        var outlineThickness = (int)MathF.Ceiling(1 * scale);

        for (var x = -outlineThickness; x <= outlineThickness; ++x)
        {
            for (var y = -outlineThickness; y <= outlineThickness; ++y)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                DrawText(fontManager, startingPosition + new Vector2(x, y), text, Colors.Black, scale);
            }
        }

        DrawText(fontManager, startingPosition, text, color, scale);
    }

    public static void DrawIconWithName(FontManager fontManager, Vector2 drawPosition, uint iconID, string name, float iconScale, float textScale, bool drawText = true)
    {
        if (!fontManager.GameFont.Available) return;
    
        var icon = IconCache.Instance.GetIcon(iconID);
        if (icon != null)
        {
            var drawList = ImGui.GetBackgroundDrawList();
    
            var imagePadding = new Vector2(20.0f, 10.0f) * iconScale;
            var imageSize = new Vector2(50.0f, 50.0f) * iconScale;
    
            drawPosition += imagePadding;
    
            drawList.AddImage(icon.ImGuiHandle, drawPosition, drawPosition + imageSize);
    
            if (drawText)
            {
                drawPosition.X += imageSize.X / 2.0f;
                drawPosition.Y += imageSize.Y + 2.0f * iconScale;
    
                var textSize = CalculateTextSize(fontManager, name, textScale / 2.75f);
                var textOffset = new Vector2(0.0f, 5.0f) * iconScale;
    
                drawPosition.X -= textSize.X / 2.0f;
    
                TextOutlined(fontManager, drawPosition + textOffset, name, textScale / 2.75f, Colors.White);
            }
        }
    }

    public static Vector2 CalculateTextSize(FontManager fontManager, string text, float scale)
    {
        if(!fontManager.GameFont.Available) return Vector2.Zero;

        var fontSize = fontManager.GameFont.ImFont.FontSize;
        var textSize = ImGui.CalcTextSize(text);
        var fontScalar = 62.0f / textSize.Y;

        var textWidth = textSize.X * fontScalar;

        return new Vector2(textWidth, fontSize) * scale;
    }

    private static void DrawText(FontManager fontManager, Vector2 drawPosition, string text, Vector4 color, float scale, bool debug = false)
    {
        if (!fontManager.GameFont.Available) return;
        var font = fontManager.GameFont.ImFont;

        var drawList = ImGui.GetBackgroundDrawList();
        var stringSize = CalculateTextSize(fontManager, text, scale);

        if (debug)
        {
            drawList.AddRect(drawPosition, drawPosition + stringSize, ImGui.GetColorU32(Colors.Green));
        }

        drawList.AddText(font, font.FontSize * scale, drawPosition, ImGui.GetColorU32(color), text);
    }
}

public static class VectorExtensions
{
    public static Vector2 Ceil(this Vector2 data)
    {
        return new Vector2(MathF.Ceiling(data.X), MathF.Ceiling(data.Y));
    }
}

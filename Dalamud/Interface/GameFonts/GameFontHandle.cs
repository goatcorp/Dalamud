using System;
using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.GameFonts;

/// <summary>
/// Prepare and keep game font loaded for use in OnDraw.
/// </summary>
public class GameFontHandle : IDisposable
{
    private readonly GameFontManager manager;
    private readonly GameFontStyle fontStyle;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameFontHandle"/> class.
    /// </summary>
    /// <param name="manager">GameFontManager instance.</param>
    /// <param name="font">Font to use.</param>
    internal GameFontHandle(GameFontManager manager, GameFontStyle font)
    {
        this.manager = manager;
        this.fontStyle = font;
    }

    /// <summary>
    /// Gets the font style.
    /// </summary>
    public GameFontStyle Style => this.fontStyle;

    /// <summary>
    /// Gets a value indicating whether this font is ready for use.
    /// </summary>
    public bool Available
    {
        get
        {
            unsafe
            {
                return this.manager.GetFont(this.fontStyle).GetValueOrDefault(null).NativePtr != null;
            }
        }
    }

    /// <summary>
    /// Gets the font.
    /// </summary>
    public ImFontPtr ImFont => this.manager.GetFont(this.fontStyle).Value;

    /// <summary>
    /// Gets the FdtReader.
    /// </summary>
    public FdtReader FdtReader => this.manager.GetFdtReader(this.fontStyle.FamilyAndSize);

    /// <summary>
    /// Creates a new GameFontLayoutPlan.Builder.
    /// </summary>
    /// <param name="text">Text.</param>
    /// <returns>A new builder for GameFontLayoutPlan.</returns>
    public GameFontLayoutPlan.Builder LayoutBuilder(string text)
    {
        return new GameFontLayoutPlan.Builder(this.ImFont, this.FdtReader, text);
    }

    /// <inheritdoc/>
    public void Dispose() => this.manager.DecreaseFontRef(this.fontStyle);

    /// <summary>
    /// Draws text.
    /// </summary>
    /// <param name="text">Text to draw.</param>
    public void Text(string text)
    {
        if (!this.Available)
        {
            ImGui.TextUnformatted(text);
        }
        else
        {
            var pos = ImGui.GetWindowPos() + ImGui.GetCursorPos();
            pos.X -= ImGui.GetScrollX();
            pos.Y -= ImGui.GetScrollY();

            var layout = this.LayoutBuilder(text).Build();
            layout.Draw(ImGui.GetWindowDrawList(), pos, ImGui.GetColorU32(ImGuiCol.Text));
            ImGui.Dummy(new Vector2(layout.Width, layout.Height));
        }
    }

    /// <summary>
    /// Draws text in given color.
    /// </summary>
    /// <param name="col">Color.</param>
    /// <param name="text">Text to draw.</param>
    public void TextColored(Vector4 col, string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, col);
        this.Text(text);
        ImGui.PopStyleColor();
    }

    /// <summary>
    /// Draws disabled text.
    /// </summary>
    /// <param name="text">Text to draw.</param>
    public void TextDisabled(string text)
    {
        unsafe
        {
            this.TextColored(*ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled), text);
        }
    }
}

using System.Numerics;

namespace Dalamud.Bindings.ImGui;

/// <summary>An opaque handle of a font. Native code never reads what it points to.</summary>
public unsafe partial struct ImFontPtr
{
    /// <summary>Draws one character, like the removed <c>ImFont::RenderChar</c>.</summary>
    /// <param name="drawList">The draw list.</param>
    /// <param name="size">Size in pixels (after global scaling).</param>
    /// <param name="pos">Top-left position of the line.</param>
    /// <param name="col">Color.</param>
    /// <param name="codepoint">The character.</param>
    public void RenderChar(ImDrawListPtr drawList, float size, Vector2 pos, uint col, uint codepoint)
    {
        if (!System.Text.Rune.TryCreate(codepoint, out var rune))
            return;
        Span<byte> buf = stackalloc byte[4];
        var len = rune.EncodeToUtf8(buf);
        ImGuiP.GetTextDesc(out var desc, drawList: drawList);
        desc.Size = size;
        fixed (byte* p = buf)
            ImGuiNative.AddTextEx(
                drawList, this.Handle, &desc, pos, col, p, p + len, null, ImTextRenderFlags.None, null);
    }
}

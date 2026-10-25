using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <summary>Draw data with text drawn onto it, for textures made from text.</summary>
internal static unsafe class TextDrawData
{
    /// <summary>Creates a draw data with text drawn onto it.</summary>
    /// <param name="font">The font, whose style the text starts with. Its markup decides how the text is read.</param>
    /// <param name="desc">Layout parameters. The density is always 1, that of the draw data.</param>
    /// <param name="text">The text.</param>
    /// <returns>A new self-contained draw data. It covers the layout, and what is drawn outside it.</returns>
    /// <exception cref="InvalidOperationException">If called outside the main thread.</exception>
    /// <remarks>It has every glyph, however long rasterizing them takes, as it is drawn once.</remarks>
    public static BufferBackedImDrawData Create(FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text)
    {
        ThreadSafety.AssertMainThread();
        var d = desc with { Density = 1 };
        var layout = TextLayouter.CreateStyled(font, d, text);

        // The scope covers what inline objects draw too.
        using var synchronous = GlyphRasterBudget.BeginSynchronous();
        var dd = BufferBackedImDrawData.Create();
        try
        {
            layout.Render(dd.ListPtr, Vector2.Zero, uint.MaxValue);

            // Shadows, edges and overhangs may be drawn past the layout box on any side.
            var (min, max) = (Vector2.Zero, layout.Size);
            foreach (ref readonly var vtx in new Span<ImDrawVert>(dd.ListPtr.VtxBuffer.Data, dd.ListPtr.VtxBuffer.Size))
                (min, max) = (Vector2.Min(min, vtx.Pos), Vector2.Max(max, vtx.Pos));
            dd.Data.DisplayPos = min;
            dd.Data.DisplaySize = max - min;
            dd.Data.Valid = 1;
            dd.UpdateDrawDataStatistics();
            return dd;
        }
        catch
        {
            dd.Dispose();
            throw;
        }
    }
}

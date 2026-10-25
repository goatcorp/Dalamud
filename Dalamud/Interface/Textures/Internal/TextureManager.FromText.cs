using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts;
using Dalamud.Interface.Fonts.Internal;
using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Layout;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

namespace Dalamud.Interface.Textures.Internal;

/// <content>Text-to-texture rendering for <see cref="TextureManager"/>.</content>
internal sealed partial class TextureManager
{
    /// <inheritdoc/>
    public unsafe IDalamudTextureWrap CreateTextureFromText(
        ImU8String text,
        TextMarkup markup,
        scoped in TextStyle style = default,
        float scale = 0,
        float wrapWidth = float.MaxValue,
        string? debugName = null)
    {
        if (scale < 0 || !float.IsFinite(scale))
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Must be zero or positive and finite.");

        ThreadSafety.AssertMainThread();
        var resolver = Service<FontResolver>.GetNullable()
                       ?? throw new InvalidOperationException("The fonts are not ready.");

        var effectiveScale = scale > 0 ? scale : FontSizes.GlobalScale;
        var impl = resolver.GetImpl(style with { Markup = markup });
        var target = impl.Entry.TryWaitTarget() ?? resolver.DefaultTarget;
        var sizes = FontSizes.Over(impl.Style, target, resolver.RootAt(effectiveScale), effectiveScale);

        var desc = new TextLayoutDesc(
            sizes.PushPx * effectiveScale,
            sizes.GlyphPx * effectiveScale,
            effectiveScale,
            WrapWidth: wrapWidth >= float.MaxValue ? 0 : wrapWidth);
        using var scaleScope = FontSizes.WithGlobalScale(effectiveScale);
        var layout = TextLayouter.CreateStyled(impl, desc, text.Span);

        // Rasterize all glyphs synchronously (including inline objects) since this is a single-pass offscreen render.
        using var dd = BufferBackedImDrawData.Create();
        using (GlyphRasterBudget.BeginSynchronous())
            layout.Render(dd.ListPtr, Vector2.Zero, uint.MaxValue);
        text.Recycle();

        // Shadows, edges and overhangs may be drawn past the layout box on any side.
        var (min, max) = (Vector2.Zero, layout.Size);
        foreach (ref readonly var vtx in new Span<ImDrawVert>(dd.ListPtr.VtxBuffer.Data, dd.ListPtr.VtxBuffer.Size))
            (min, max) = (Vector2.Min(min, vtx.Pos), Vector2.Max(max, vtx.Pos));
        dd.Data.DisplayPos = min;
        dd.Data.DisplaySize = max - min;
        dd.Data.Valid = 1;
        dd.UpdateDrawDataStatistics();

        var texture = this.CreateDrawListTexture(debugName ?? nameof(this.CreateTextureFromText));
        try
        {
            texture.Size = Vector2.Max(dd.Data.DisplaySize, Vector2.One);
            texture.Draw(dd.DataPtr);
            return texture;
        }
        catch
        {
            texture.Dispose();
            throw;
        }
    }
}

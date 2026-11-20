using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Markup;
using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <content>Rendering glyphs, decorations, and inline objects.</content>
internal sealed unsafe partial class TextLayout
{
    // Sentinel in cachedSteps indicating no cached GlyphImage.
    private const byte NoCachedImage = byte.MaxValue;

    // Reused by drawing, which is on the main thread only.
    private readonly List<DecorationSpan> decorationSpans = [];
    private readonly List<(int Begin, int End)> visibleRanges = [];

    // Slices referenced by cached images, with their generations.
    private readonly List<(GlyphKind Kind, int Slice, int Generation)> cachedSlices = [];

    // Per-glyph cached GlyphImages (followed by shadow entries at +glyphs.Count when cachedShadows is true) and their
    // subpixel steps, invalidated if atlas, snap, or subpixel state changes.
    private GlyphImage[] cachedImages = [];
    private byte[] cachedSteps = [];
    private GlyphAtlas? cachedAtlas;
    private bool cachedSnap;
    private bool cachedSubpixel;
    private bool cachedShadows;

    // Reusable resolved-style buffer for top-level renders. Nested renders rent a separate buffer.
    private FontImpl[] renderStyles = [];

    // Active render state, saved and restored across re-entrant renders such as custom decorations.
    private RenderPass pass;

    private delegate float KeyOf<T>(in T value);

    private enum Pass
    {
        Shadow,
        Edge,
        Fill,
    }

    /// <summary>Draws layout items starting within <paramref name="textBegin"/> to <paramref name="textEnd"/>. Inline
    /// objects are drawn by <see cref="RenderHook"/> when set and untransformed, or left to the caller otherwise.
    /// </summary>
    /// <param name="drawList">The draw list.</param>
    /// <param name="pos">Position of the layout origin.</param>
    /// <param name="col">Text color, whose alpha also multiplies that of colors given by styles.</param>
    /// <param name="clipRect">Clip rectangle in untransformed space, or null for the draw list's.</param>
    /// <param name="flags">Rendering-only flags.</param>
    /// <param name="transform">Transform around <paramref name="pos"/>, or null.</param>
    /// <param name="textBegin">Start offset, in code units.</param>
    /// <param name="textEnd">End offset in code units, or less than 0 for the end of the text.</param>
    public void Render(
        ImDrawListPtr drawList, Vector2 pos, uint col, Vector4? clipRect = null,
        ImTextRenderFlags flags = ImTextRenderFlags.None, ImTextTransform? transform = null, int textBegin = 0,
        int textEnd = -1)
    {
        // The atlas is main thread only. Other threads would race it.
        if (!ThreadSafety.IsMainThread)
            return;

        var start = FontProfiler.Begin(FontProfiler.Section.Render);
        var verticesBefore = drawList.VtxBuffer.Size;
        if (textEnd < 0)
            textEnd = int.MaxValue;
        var snap = transform is null && (flags & ImTextRenderFlags.NoPixelSnap) == 0;
        if (snap)
            pos = new(MathF.Floor(pos.X), MathF.Floor(pos.Y));
        var clip = clipRect ?? drawList.CmdHeader.ClipRect;
        // The draw list's clip rectangle is in screen space, not in the untransformed space of the glyphs.
        // The scissor rectangle applies it anyway.
        var fineClip = transform is null
                           ? (flags & ImTextRenderFlags.CpuFineClip) != 0
                           : clipRect is not null;
        var density = this.Density;
        var dl = drawList.Handle;

        var setupStart = FontProfiler.Begin(FontProfiler.Section.RenderSetup);

        // Re-entrant renders allocate separate scratch lists so they do not overwrite the outer pass.
        var outer = this.pass;
        var nested = outer.VisibleRanges is not null;
        var hook = transform is null && !nested ? this.RenderHook : null;
        hook?.BeginRender(this, drawList, pos, clip, textBegin, textEnd);
        var ranges = nested ? [] : this.visibleRanges;

        // Resolve styles only for runs intersecting the visible range.
        var reach = this.desc.Size * 2;
        this.FindVisibleItems(ranges, pos, clip, reach, transform is null);
        var (runBegin, runEnd) = this.VisibleRuns(ranges);
        var pooledStyles = nested
                               ? PooledSpan<FontImpl>.Rent(runEnd - runBegin + 1)
                               : PooledSpan<FontImpl>.FromBuffer(ref this.renderStyles, runEnd - runBegin + 1);
        var styles = pooledStyles.Span;
        this.ResolveStyles(styles, runBegin, runEnd);
        // Apply link styling even when transformed or nested, disabling hover highlight when hook is null.
        if (this.RenderHook is { HasLinks: true } linkHook)
            this.ResolveLinks(styles, linkHook, hook is not null, runBegin, runEnd);

        var layers = DecorationLayersOf(styles, out var shadows);
        this.pass = new(
            textBegin,
            textEnd,
            pos,
            col,
            transform,
            snap,
            fineClip ? clip : null,
            transform is null ? clip + new Vector4(-reach, -reach, reach, reach) : null,
            shadows,
            runBegin,
            ranges,
            nested ? [] : this.decorationSpans);

        // Only glyphs are clipped on the CPU. Decorations and objects get a scissor, pushed only for text that has any
        // so that plain text stays in the draw command it is in.
        var scissor = false;
        try
        {
            this.ValidateImageCache(snap, shadows);
            FontProfiler.End(FontProfiler.Section.RenderSetup, setupStart);

            if (fineClip && (layers != TextDecorationFlags.None || (hook is not null && this.objectCount > 0)))
            {
                PushScissor(drawList, clip, pos, transform);
                scissor = true;
            }

            this.DrawDecorationLayer(dl, styles, layers, TextDecorationLayer.BelowText);
            if ((flags & ImTextRenderFlags.CompleteGlyphs) != 0)
                this.DrawGlyphsSynchronously(dl, styles, layers);
            else
                this.DrawGlyphs(dl, styles, layers);

            // Inline objects, for layouts that draw them. Hooks draw them as they are, so not with transforms.
            if (hook is not null && this.objectCount > 0)
            {
                foreach (ref readonly var item in this.VisibleItems())
                {
                    if (item.Kind != ItemKind.Object || !this.InRange(item, textBegin, textEnd))
                        continue;
                    var b = this.objects[item.Object].Bounds;
                    var topLeft = pos + this.ItemPen(item) + new Vector2(b.X, b.Y);

                    // Snapped to device pixels as glyphs are, so that images are not blurred.
                    if (snap)
                    {
                        topLeft.X = MathF.Round(topLeft.X * density) / density;
                        topLeft.Y = MathF.Round(topLeft.Y * density) / density;
                    }

                    hook.DrawObject(drawList, item.Object, topLeft, col);
                }
            }

            this.DrawDecorationLayer(dl, styles, layers, TextDecorationLayer.OverText);
            pooledStyles.Recycle();
        }
        finally
        {
            if (scissor)
                drawList.PopClipRect();

            // The outer render goes on with images kept for its own snapping and shadows.
            this.pass = outer;
            if (nested)
                this.ValidateImageCache(outer.Snap, outer.Shadows);
        }

        FontProfiler.End(FontProfiler.Section.Render, start, (drawList.VtxBuffer.Size - verticesBefore) / 4);
    }

    /// <summary>Multiplies the alpha of a color by that of another.</summary>
    /// <param name="color">The color to change.</param>
    /// <param name="alphaSource">The color whose alpha to multiply by.</param>
    /// <returns>The color with the alpha of both.</returns>
    internal static uint MulAlpha(uint color, uint alphaSource) =>
        (color & 0x00FFFFFFu) | ((((color >> 24) * (alphaSource >> 24)) / 255) << 24);

    /// <summary>Draws the glyphs of a span of a decoration being drawn.</summary>
    /// <param name="span">The span.</param>
    /// <param name="color">The color to draw with.</param>
    /// <param name="width">How far to dilate the glyphs, in pixels.</param>
    /// <param name="softness">How soft the dilated edge is.</param>
    /// <param name="offset">How far to move the glyphs, in pixels.</param>
    /// <remarks>Zero-offset passes reuse the fill pass's cached glyph images. Shifted passes fetch uncached images so
    /// they do not overwrite the fill cache.</remarks>
    /// <seealso cref="TextDecorationSpan.DrawGlyphs"/>
    internal void DrawDecorationGlyphs(
        in TextDecorationSpan span, uint color, float width, float softness, Vector2 offset)
    {
        var itemSpan = CollectionsMarshal.AsSpan(this.items)[span.ItemBegin..span.ItemEnd];
        var maxQuads = 0;
        foreach (ref readonly var item in itemSpan)
        {
            if (item.Kind is ItemKind.Glyph or ItemKind.Ellipsis)
                maxQuads += item.GlyphEnd - item.GlyphBegin;
        }

        if (maxQuads == 0)
            return;

        var (textBegin, textEnd) = (this.pass.TextBegin, this.pass.TextEnd);
        var dilation = Math.Max(width, 0) * this.Density;
        var packedSoftness = PackSoftness(softness);
        var quads = new QuadWriter(span.DrawListHandle, maxQuads);
        foreach (ref readonly var item in itemSpan)
        {
            if (!this.IsDrawnGlyphItem(item, textBegin, textEnd))
                continue;

            this.DrawItemGlyphs(
                ref quads,
                item,
                offset,
                color,
                dilation,
                packedSoftness,
                offset == Vector2.Zero ? 0 : -1);
        }

        quads.Finish();
    }

    // Collects active decoration layer flags across visible styles so plain text skips decoration passes, and records
    // whether any visible style uses shadows.
    private static TextDecorationFlags DecorationLayersOf(ReadOnlySpan<FontImpl> styles, out bool shadows)
    {
        var layers = TextDecorationFlags.None;
        shadows = false;
        foreach (var font in styles)
        {
            ref readonly var st = ref font.Style;
            shadows |= st.Shadow.IsOn;
            if (st.Background.Alpha != 0)
                layers |= TextDecorationFlags.BelowText;
            if (st.Lines.IsOn && TextDecorationRegistry.TryGet(TextDecorationId.Line, out var lines))
                layers |= lines.LayersOf(st.Lines.ToDecoration());
            if (!st.ExtraDecoration.IsNone && TextDecorationRegistry.TryGet(st.ExtraDecoration.Id, out var extra))
                layers |= extra.LayersOf(st.ExtraDecoration);
        }

        return layers;
    }

    private static TextDecorationFlags LayerFlag(TextDecorationLayer layer) => (TextDecorationFlags)(1 << (int)layer);

    private static byte PackSoftness(float softness) =>
        (byte)Math.Clamp(MathF.Round(softness * GlyphAtlas.ExtSoftnessSteps), 0, byte.MaxValue);

    private static uint Tag(uint mode, in GlyphImage img) =>
        GlyphAtlas.ExtTag(mode, img.Kind, img.Slice) | (img.CorrectCoverage ? GlyphAtlas.ExtCorrect : 0);

    // Grows a glyph quad by the reach of a dilation, and makes its tag.
    private static uint Dilate(
        in GlyphImage img, float radius, byte packedSoftness, ref Vector4 rect, ref Vector2 uv0, ref Vector2 uv1)
    {
        // Subpixel glyphs take three texels across per pixel. The radius and the reach are in pixels of the bitmap.
        var subpixel = img.Kind == GlyphKind.Subpixel;
        var tag = Tag(GlyphAtlas.ExtDilate | (subpixel ? GlyphAtlas.ExtRgb : 0), img);
        var texels = ((int)(img.TexelMax & 0xFFFF) - (int)(img.TexelMin & 0xFFFF)) / (subpixel ? 3 : 1);
        var perTexel = (img.Rect.Z - img.Rect.X) / texels;
        const int steps = GlyphAtlas.ExtRadiusSteps;
        var maxReach = subpixel ? GlyphAtlas.ExtMaxSubpixelReach : GlyphAtlas.ExtMaxReach;
        var r = (uint)Math.Clamp(MathF.Round((radius / perTexel) * steps), 0, maxReach * steps);
        uint k = packedSoftness;
        var rimEnd = r == 0 ? 0 : RimEnd((float)r / steps, (float)k / GlyphAtlas.ExtSoftnessSteps, maxReach);
        var pad = Math.Min(MathF.Ceiling(rimEnd), maxReach);
        var texelSize = new Vector2(
            (img.Uv1.X - img.Uv0.X) / ((img.TexelMax & 0xFFFF) - (img.TexelMin & 0xFFFF)),
            (img.Uv1.Y - img.Uv0.Y) / ((img.TexelMax >> 16) - (img.TexelMin >> 16)));
        var uvPad = new Vector2(pad * (subpixel ? 3 : 1), pad) * texelSize;
        uv0 -= uvPad;
        uv1 += uvPad;
        rect += new Vector4(-pad, -pad, pad, pad) * perTexel;
        return tag | (r << GlyphAtlas.ExtRadiusShift) | (k << GlyphAtlas.ExtSoftnessShift);
    }

    // Where the weight falls to 0, in texels, as ExtRim(radius, k, maxReach).z of imgui-text.hlsli.
    private static float RimEnd(float radius, float k, int maxReach)
    {
        radius = Math.Min(radius, maxReach);
        k = k > 1 ? Math.Min(k, Math.Max(maxReach / radius, 1)) : k;
        var inner = Math.Max(1 - k, 0) * radius;
        var outer = Math.Max(k, 1) * radius;
        return ((inner + outer) * 0.5f) + 0.5f + (0.5f * Math.Max(outer - inner, 1));
    }

    private static uint TextColorOf(in TextStyle style, uint col) =>
        style.Color != 0 ? MulAlpha(style.Color, col) : col;

    private static Vector2 Apply(in ImTextTransform t, Vector2 pos, Vector2 p)
    {
        var d = p - pos;
        return pos + (t.AxisX * d.X) + (t.AxisY * d.Y);
    }

    // Pushes a clip rectangle in untransformed space as a scissor, within the draw list's. With a transform, it is
    // the box around the transformed rectangle, which is exact only for transforms that do not rotate.
    private static void PushScissor(ImDrawListPtr drawList, in Vector4 clip, Vector2 pos, ImTextTransform? transform)
    {
        var min = new Vector2(clip.X, clip.Y);
        var max = new Vector2(clip.Z, clip.W);
        if (transform is { } t)
        {
            var a = Apply(t, pos, min);
            var b = Apply(t, pos, new(max.X, min.Y));
            var c = Apply(t, pos, max);
            var d = Apply(t, pos, new(min.X, max.Y));
            min = Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d));
            max = Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d));
        }

        drawList.PushClipRect(min, max, true);
    }

    private static bool Clip(ref Vector2 p0, ref Vector2 p1, ref Vector2 uv0, ref Vector2 uv1, in Vector4 clip)
    {
        if (p0.X >= clip.Z || p1.X <= clip.X || p0.Y >= clip.W || p1.Y <= clip.Y)
            return false;
        var size = p1 - p0;
        var uvSize = uv1 - uv0;
        if (p0.X < clip.X)
        {
            uv0.X += (uvSize.X * (clip.X - p0.X)) / size.X;
            p0.X = clip.X;
        }

        if (p1.X > clip.Z)
        {
            uv1.X -= (uvSize.X * (p1.X - clip.Z)) / size.X;
            p1.X = clip.Z;
        }

        if (p0.Y < clip.Y)
        {
            uv0.Y += (uvSize.Y * (clip.Y - p0.Y)) / size.Y;
            p0.Y = clip.Y;
        }

        if (p1.Y > clip.W)
        {
            uv1.Y -= (uvSize.Y * (p1.Y - clip.W)) / size.Y;
            p1.Y = clip.W;
        }

        return true;
    }

    // A glyph quad, clipped on the CPU if asked, slanted around the baseline for synthetic italic, then transformed.
    private static void Emit(
        ref QuadWriter quads, Vector2 p0, Vector2 p1, Vector2 uv0, Vector2 uv1, uint color, uint tag, uint texelMin,
        uint texelMax, uint maskTexelMin, uint mask, float slant, float baselineY, Vector4? fineClip, Vector2 pos,
        ImTextTransform? transform)
    {
        // The fields, not ImDrawVert.Ext, which makes a span per access in unoptimized builds.
        var vertex = new ImDrawVert
        {
            Col = color, Ext_0 = tag, Ext_1 = texelMin, Ext_2 = texelMax, Ext_3 = maskTexelMin, Ext_4 = mask,
        };

        if (fineClip is { } clip && !Clip(ref p0, ref p1, ref uv0, ref uv1, clip))
            return;
        Span<Vector2> q = [new(p0.X, p0.Y), new(p1.X, p0.Y), new(p1.X, p1.Y), new(p0.X, p1.Y)];
        if (slant != 0)
        {
            for (var i = 0; i < 4; i++)
                q[i].X += (baselineY - q[i].Y) * slant;
        }

        if (transform is { } t)
        {
            for (var i = 0; i < 4; i++)
                q[i] = Apply(t, pos, q[i]);
        }

        quads.Add(q[0], q[1], q[2], q[3], uv0, uv1, vertex);
    }

    // The first index whose key is at least the value (or more than it, if strictly), in a span of ascending keys.
    private static int LowerBound<T>(ReadOnlySpan<T> span, float value, KeyOf<T> key, bool strictly = false)
    {
        var (lo, hi) = (0, span.Length);
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var k = key(span[mid]);
            if (strictly ? k <= value : k < value)
                lo = mid + 1;
            else
                hi = mid;
        }

        return lo;
    }

    private void DrawGlyphsSynchronously(
        ImDrawList* dl, ReadOnlySpan<FontImpl> styles, TextDecorationFlags decorationLayers)
    {
        using (GlyphRasterBudget.BeginSynchronous())
        {
            this.DrawGlyphs(dl, styles, decorationLayers);
        }
    }

    // Renders visible glyphs in the pass's code-unit range (Shadow, Edge, then Fill) with intermediate decoration layers.
    // Deferred glyphs render blank.
    private void DrawGlyphs(ImDrawList* dl, ReadOnlySpan<FontImpl> styles, TextDecorationFlags decorationLayers)
    {
        var (col, textBegin, textEnd) = (this.pass.Col, this.pass.TextBegin, this.pass.TextEnd);
        var density = this.Density;
        // At most a quad per glyph per pass its style draws, reserved at once rather than a native call per quad.
        var maxQuads = 0;
        var anyShadow = false;
        var anyEdge = false;
        foreach (ref readonly var item in this.VisibleItems())
        {
            if (!this.IsDrawnGlyphItem(item, textBegin, textEnd))
                continue;

            ref readonly var st = ref this.StyleOfRun(styles, item.Run).Style;
            anyShadow |= st.Shadow.IsOn;
            anyEdge |= st.Edge.IsOn;
            var passes = 1 + (st.Shadow.IsOn ? 1 : 0) + (st.Edge.IsOn ? 1 : 0);
            maxQuads += (item.GlyphEnd - item.GlyphBegin) * passes;
        }

        var globalScale = this.desc.GlobalScale;
        var quads = new QuadWriter(dl, maxQuads);
        for (var pass = Pass.Shadow; pass <= Pass.Fill; pass++)
        {
            // The decorations of the layer before the pass. Nothing else may draw while quads are reserved.
            var layer = pass == Pass.Edge ? TextDecorationLayer.BelowEdge : TextDecorationLayer.BelowFill;
            if (pass != Pass.Shadow && (decorationLayers & LayerFlag(layer)) != 0)
            {
                quads.Finish();
                this.DrawDecorationLayer(dl, styles, decorationLayers, layer);
                quads = new(dl, maxQuads);
            }

            if (maxQuads == 0 || (pass == Pass.Shadow && !anyShadow) || (pass == Pass.Edge && !anyEdge))
                continue;

            foreach (ref readonly var item in this.VisibleItems())
            {
                if (!this.IsDrawnGlyphItem(item, textBegin, textEnd))
                    continue;

                ref readonly var st = ref this.StyleOfRun(styles, item.Run).Style;
                uint color;
                var offset = Vector2.Zero;
                var edge = st.Edge;

                // In device pixels, or negative to draw the glyph as it is. Shadows are of the edged glyph.
                var dilation = -1f;
                switch (pass)
                {
                    case Pass.Shadow:
                        var shadow = st.Shadow;
                        if (!shadow.IsOn)
                            continue;
                        var em = this.EmOf(item.FontIndex);
                        color = MulAlpha(shadow.Color, col);
                        offset = shadow.OffsetAt(em, globalScale);
                        dilation = edge.WidthAt(em, globalScale) * density;
                        break;
                    case Pass.Edge:
                        if (!edge.IsOn)
                            continue;
                        color = MulAlpha(edge.Color, col);
                        dilation = edge.WidthAt(this.EmOf(item.FontIndex), globalScale) * density;
                        break;
                    case Pass.Fill:
                    default:
                        color = TextColorOf(st, col);
                        break;
                }

                // Shadows have shifted pens and different subpixel steps, so they use the second half of cachedImages.
                this.DrawItemGlyphs(
                    ref quads,
                    item,
                    offset,
                    color,
                    dilation,
                    PackSoftness(edge.Softness),
                    pass == Pass.Shadow ? this.glyphs.Count : 0);
            }
        }

        quads.Finish();
    }

    // Emits quads for an item's glyphs under the current pass, where dilation < 0 disables dilation and cacheBase < 0 bypasses cachedImages.
    private void DrawItemGlyphs(
        ref QuadWriter quads, in Item item, Vector2 offset, uint color, float dilation, byte packedSoftness,
        int cacheBase)
    {
        var (pos, transform) = (this.pass.Pos, this.pass.Transform);
        var itemPen = pos + this.ItemPen(item) + offset;
        var glyphSpan = CollectionsMarshal.AsSpan(this.glyphs);
        var lookup = GlyphLookup.Get(item.GlyphEnd - item.GlyphBegin);
        var count = 0;
        var (density, snap, fineClip, cull) = (this.Density, this.pass.Snap, this.pass.FineClip, this.pass.Cull);
        for (var gi = item.GlyphBegin; gi < item.GlyphEnd; gi++)
        {
            ref readonly var glyph = ref glyphSpan[gi];
            if (glyph.Id == 0)
                continue;

            // Items are culled as a whole, and a long one may be mostly out of sight.
            var pen = itemPen + glyph.Pos;
            if (cull is { } c && (pen.X < c.X || pen.X > c.Z || pen.Y < c.Y || pen.Y > c.W))
                continue;

            // Snap y to a whole device pixel, and x to 1/SubpixelStepCount pixels split into integer pen and step.
            var dx = pen.X * density;
            var dy = pen.Y * density;
            if (snap)
            {
                var steps = (int)MathF.Floor(dx * Font.SubpixelStepCount);
                lookup.Pens[count] = new(steps >> Font.SubpixelStepShift, MathF.Round(dy));
                lookup.Steps[count] = (byte)(steps & (Font.SubpixelStepCount - 1));
            }
            else
            {
                lookup.Pens[count] = new(dx, dy);
                lookup.Steps[count] = 0;
            }

            lookup.Slots[count] = cacheBase + gi;
            lookup.Ids[count++] = glyph.Id;
        }

        var cached = cacheBase >= 0;
        var allCached = cached;
        for (var k = 0; k < count && allCached; k++)
            allCached = this.cachedSteps[lookup.Slots[k]] == lookup.Steps[k];
        if (!allCached)
        {
            var subpixel = snap && !this.FontOfRun(item.Run).Style.NoSubpixel;
            ref readonly var slot = ref this.SlotOf(item.FontIndex);
            this.GetGlyphImages(slot.Font, slot.Size, density, subpixel, lookup, count, cached);
        }

        for (var k = 0; k < count; k++)
        {
            ref readonly var img = ref cached ? ref this.cachedImages[lookup.Slots[k]] : ref lookup.Images[k];
            if (img.IsEmpty)
                continue;
            var penDevice = lookup.Pens[k];
            if (snap && img.WholePixel && lookup.Steps[k] >= Font.SubpixelStepCount / 2)
                penDevice.X++;

            var rect = img.Rect;
            var uv0 = img.Uv0;
            var uv1 = img.Uv1;
            var tag = dilation >= 0
                          ? Dilate(img, dilation, packedSoftness, ref rect, ref uv0, ref uv1)
                          : Tag(
                              img.Kind switch
                              {
                                  GlyphKind.Color => GlyphAtlas.ExtColor,
                                  GlyphKind.Subpixel => GlyphAtlas.ExtSubpixel,
                                  _ => GlyphAtlas.ExtGlyph,
                              },
                              img);
            var mask = dilation < 0 && img.MaskSlice >= 0
                           ? 1u | ((uint)img.MaskSlice << GlyphAtlas.ExtMaskSliceShift)
                           : 0;
            // An image too large for the atlas is drawn from its own textures, between commands that bind them.
            if (img.LargeHandle != 0)
            {
                quads.Finish();
                GlyphAtlas.BindTextures(quads.DrawList, img.LargeHandle);
            }

            var a = new Vector2(penDevice.X + rect.X, penDevice.Y + rect.Y) / density;
            var c = new Vector2(penDevice.X + rect.Z, penDevice.Y + rect.W) / density;
            Emit(
                ref quads,
                a,
                c,
                uv0,
                uv1,
                color,
                tag,
                img.TexelMin,
                img.TexelMax,
                img.MaskTexelMin,
                mask,
                img.Slant,
                penDevice.Y / density,
                fineClip,
                pos,
                transform);
            if (img.LargeHandle != 0)
            {
                quads.Finish();
                GlyphAtlas.BindTextures(quads.DrawList, 0);
            }
        }
    }

    // Validates cached GlyphImages against current atlas, snap, and subpixel state and marks their slices used, clearing on mismatch.
    private void ValidateImageCache(bool snap, bool shadows)
    {
        var atlas = GlyphAtlas.Shared;
        var subpixel = SubpixelSettings.Enabled;
        shadows |= this.cachedShadows;
        var slots = this.glyphs.Count * (shadows ? 2 : 1);
        var valid = this.cachedAtlas == atlas && this.cachedSnap == snap && this.cachedSubpixel == subpixel &&
                    this.cachedShadows == shadows && this.cachedSteps.Length >= slots;
        if (valid)
        {
            foreach (ref readonly var s in CollectionsMarshal.AsSpan(this.cachedSlices))
            {
                if (!atlas.TryUse(s.Kind, s.Slice, s.Generation))
                {
                    valid = false;
                    break;
                }
            }
        }

        if (valid)
            return;

        if (this.cachedSteps.Length < slots)
        {
            this.cachedSteps = new byte[slots];
            this.cachedImages = new GlyphImage[slots];
        }

        this.cachedSteps.AsSpan(0, slots).Fill(NoCachedImage);
        this.cachedSlices.Clear();
        (this.cachedAtlas, this.cachedSnap, this.cachedSubpixel, this.cachedShadows) = (atlas, snap, subpixel, shadows);
    }

    // Fetches GlyphImages for lookup and optionally caches final non-large results.
    private void GetGlyphImages(
        Font font, float size, float density, bool subpixel, GlyphLookup lookup, int count, bool keep)
    {
        var start = FontProfiler.Begin(FontProfiler.Section.RenderImages);
        font.GetGlyphImages(
            lookup.Ids.AsSpan(0, count),
            lookup.Steps.AsSpan(0, count),
            size,
            density,
            0,
            subpixel,
            lookup.Images.AsSpan(0, count));
        FontProfiler.End(FontProfiler.Section.RenderImages, start, calls: count);
        if (!keep)
            return;

        for (var k = 0; k < count; k++)
        {
            ref readonly var img = ref lookup.Images[k];
            var slot = lookup.Slots[k];
            // Large images are not tracked in cachedSlices, so fetch them each frame instead of caching.
            var kept = img.IsFinal && img.LargeHandle == 0;
            this.cachedImages[slot] = img;
            this.cachedSteps[slot] = kept ? lookup.Steps[k] : NoCachedImage;
            if (!kept || img.IsEmpty)
                continue;
            var plane = img.Kind == GlyphKind.Color ? GlyphKind.Color : GlyphKind.Grayscale;
            this.KeepSlice(plane, img.Slice, img.Generation);
            if (img.MaskSlice >= 0)
                this.KeepSlice(GlyphKind.Grayscale, img.MaskSlice, img.MaskGeneration);
        }
    }

    private void KeepSlice(GlyphKind kind, int slice, int generation)
    {
        foreach (ref readonly var s in CollectionsMarshal.AsSpan(this.cachedSlices))
        {
            if (s.Kind == kind && s.Slice == slice && s.Generation == generation)
                return;
        }

        this.cachedSlices.Add((kind, slice, generation));
    }

    private (int Begin, int End) VisibleRuns(List<(int Begin, int End)> ranges)
    {
        var (begin, end) = (int.MaxValue, 0);
        foreach (ref readonly var item in new VisibleItemEnumerator(CollectionsMarshal.AsSpan(this.items), ranges))
        {
            if (item.Run < 0)
                continue;
            begin = Math.Min(begin, item.Run);
            end = Math.Max(end, item.Run + 1);
        }

        return begin < end ? (begin, end) : (0, 0);
    }

    // The style to draw each run in a range with, at its index in the range plus 1, and text outside runs at 0.
    private void ResolveStyles(Span<FontImpl> styles, int runBegin, int runEnd)
    {
        var frame = FontClock.Frame;
        styles[0] = this.defaultFont;
        styles[0].LastUsedFrame = frame;
        for (var r = runBegin; r < runEnd; r++)
        {
            var impl = this.runs[r].Impl ?? styles[0];
            impl.LastUsedFrame = frame;
            styles[r - runBegin + 1] = impl;
        }
    }

    private FontImpl StyleOfRun(ReadOnlySpan<FontImpl> styles, int run) =>
        styles[run < 0 ? 0 : run - this.pass.RunBase + 1];

    private bool IsDrawnGlyphItem(in Item item, int textBegin, int textEnd) =>
        item.Kind is ItemKind.Glyph or ItemKind.Ellipsis && item.GlyphBegin != item.GlyphEnd &&
        this.InRange(item, textBegin, textEnd);

    // Applies render-time link styling from hook so links track the current ImGui style, preserving layout fields via WithLayoutOf.
    private void ResolveLinks(
        Span<FontImpl> styles, StyledTextHook hook, bool highlight, int runBegin, int runEnd)
    {
        var (last, lastLink, lastResolved) = (default(FontImpl), 0ul, default(FontImpl));
        for (var r = runBegin; r < runEnd; r++)
        {
            var link = this.runs[r].UserData;
            if (link == 0)
                continue;
            ref var font = ref styles[r - runBegin + 1];
            if (font != last || link != lastLink)
            {
                (last, lastLink) = (font, link);
                ref readonly var style = ref font.Style;
                lastResolved = font.Resolver.GetImpl(hook.StyleOf(link, style, highlight).WithLayoutOf(style));
            }

            font = lastResolved!;
        }
    }

    private void DrawDecorationLayer(
        ImDrawList* dl, ReadOnlySpan<FontImpl> styles, TextDecorationFlags layers, TextDecorationLayer layer)
    {
        if ((layers & LayerFlag(layer)) == 0)
            return;
        var start = FontProfiler.Begin(FontProfiler.Section.RenderDecorations);
        this.DrawDecorations(dl, styles, layer);
        FontProfiler.End(FontProfiler.Section.RenderDecorations, start);
    }

    // Merges adjacent items on a line sharing a decoration and text color into continuous spans (Background, Lines, then Extra),
    // and draws band by band across all spans.
    private void DrawDecorations(ImDrawList* dl, ReadOnlySpan<FontImpl> styles, TextDecorationLayer layer)
    {
        var (pos, col, transform) = (this.pass.Pos, this.pass.Col, this.pass.Transform);
        var (textBegin, textEnd) = (this.pass.TextBegin, this.pass.TextEnd);
        var layerFlag = LayerFlag(layer);
        var spans = this.pass.DecorationSpans!;
        spans.Clear();
        Span<TextDecoration> decorations = stackalloc TextDecoration[3];
        var bands = 1;
        for (var e = this.VisibleItems(); e.MoveNext();)
        {
            ref readonly var item = ref e.Current;
            if (item.Kind is ItemKind.Newline or ItemKind.Control or ItemKind.SoftHyphen ||
                !this.InRange(item, textBegin, textEnd))
            {
                continue;
            }

            var font = this.StyleOfRun(styles, item.Run);
            ref readonly var st = ref font.Style;
            var textColor = TextColorOf(st, col);
            decorations[0] = st.Background.Alpha != 0 ? new(TextDecorationId.Background, st.Background) : default;
            decorations[1] = st.Lines.IsOn ? st.Lines.ToDecoration() : default;
            decorations[2] = st.ExtraDecoration;
            foreach (var d in decorations)
            {
                if (d.IsNone || !TextDecorationRegistry.TryGet(d.Id, out var entry) ||
                    (entry.LayersOf(d) & layerFlag) == 0)
                {
                    continue;
                }

                var merged = false;
                for (var i = spans.Count - 1; i >= 0 && spans[i].LineIndex == item.LineIndex; i--)
                {
                    var s = spans[i];
                    if (s.Decoration == d && s.TextColor == textColor && Math.Abs(s.U1 - item.X) < 0.01f)
                    {
                        spans[i] = s with { U1 = item.X + item.Advance, ItemEnd = e.Index + 1 };
                        merged = true;
                        break;
                    }
                }

                if (!merged)
                {
                    spans.Add(
                        new(
                            d,
                            entry,
                            item.LineIndex,
                            item.X,
                            item.X + item.Advance,
                            item.FontIndex,
                            font,
                            textColor,
                            e.Index,
                            e.Index + 1));
                    bands = Math.Max(bands, entry.Bands);
                }
            }
        }

        for (var band = 0; band < bands; band++)
        {
            foreach (ref readonly var s in CollectionsMarshal.AsSpan(spans))
            {
                if (band >= s.Entry.Bands)
                    continue;
                var line = this.lines[s.LineIndex];
                var m = this.MetricsOf(s.FontIndex);
                var fontSize = this.SlotOf(s.FontIndex).Size;
                var r = new Vector4(s.U0, line.BlockPos, s.U1, line.BlockPos + line.BlockSize);
                s.Entry.Draw(
                    new(dl, pos, transform, col)
                    {
                        Layer = layer,
                        Band = band,
                        Layout = this,
                        Density = this.Density,
                        ItemBegin = s.ItemBegin,
                        ItemEnd = s.ItemEnd,
                        Context = s.Decoration.Context,
                        Data = s.Decoration.Data,
                        TextColor = s.TextColor,
                        X0 = r.X,
                        X1 = r.Z,
                        LineTop = r.Y,
                        LineBottom = r.W,
                        LineStart = line.Start,
                        Baseline = line.BlockPos + line.Baseline,
                        Style = s.Font.Style,
                        FontSize = fontSize,
                        EmSize = this.EmOf(s.FontIndex),
                        Ascent = m.Ascent,
                        Descent = m.Descent,
                        UnderlinePosition = m.UnderlinePosition,
                        UnderlineThickness = Math.Max(1f, m.UnderlineThickness),
                        StrikethroughPosition = m.StrikethroughPosition,
                        StrikethroughThickness = Math.Max(1f, m.StrikethroughThickness),
                    });
            }
        }
    }

    // Binary-searches lines and items intersecting clip expanded by reach (twice the layout font size, for overhangs,
    // slants, edges, and shadows). Disabled when cull is false (transformed text).
    private void FindVisibleItems(
        List<(int Begin, int End)> ranges, Vector2 pos, in Vector4 clip, float reach, bool cull)
    {
        ranges.Clear();
        if (!cull)
        {
            ranges.Add((0, this.items.Count));
            return;
        }

        var (x0, x1) = (clip.X - pos.X - reach, (clip.Z - pos.X) + reach);
        var itemSpan = CollectionsMarshal.AsSpan(this.items);
        var lineSpan = CollectionsMarshal.AsSpan(this.lines);
        var first = LowerBound(lineSpan, clip.Y - pos.Y - reach, static (in l) => l.BlockPos + l.BlockSize);
        for (var i = first; i < lineSpan.Length && (pos.Y + lineSpan[i].BlockPos) - reach <= clip.W; i++)
        {
            var lineItems = itemSpan[lineSpan[i].ItemBegin..lineSpan[i].ItemEnd];
            var begin = LowerBound(lineItems, x0, static (in it) => it.X + it.Advance);
            var end = begin + LowerBound(lineItems[begin..], x1, static (in it) => it.X, true);
            if (begin < end)
                ranges.Add((lineSpan[i].ItemBegin + begin, lineSpan[i].ItemBegin + end));
        }
    }

    private VisibleItemEnumerator VisibleItems() =>
        new(CollectionsMarshal.AsSpan(this.items), this.pass.VisibleRanges!);

    // Active render state passed to decorations and hooks (lists are null outside Render). Cull is clip expanded by
    // twice the layout font size, or null when transformed. RunBase is the first visible run (index 1 in resolved styles).
    private readonly record struct RenderPass(
        int TextBegin,
        int TextEnd,
        Vector2 Pos,
        uint Col,
        ImTextTransform? Transform,
        bool Snap,
        Vector4? FineClip,
        Vector4? Cull,
        bool Shadows,
        int RunBase,
        List<(int Begin, int End)>? VisibleRanges,
        List<DecorationSpan>? DecorationSpans);

    private readonly record struct DecorationSpan(
        TextDecoration Decoration,
        TextDecorationRegistry.Entry Entry,
        int LineIndex,
        float U0,
        float U1,
        int FontIndex,
        FontImpl Font,
        uint TextColor,
        int ItemBegin,
        int ItemEnd);

    // Batches quad writes into chunked PrimReserve calls (up to 4096 quads, within 16-bit vertex indices) to avoid
    // per-quad native calls. No other draws may touch dl before Finish. Pointers are advanced and indices zeroed on
    // Reserve so an exception before Finish leaves valid degenerate triangles without try/finally on hot paths.
    private ref struct QuadWriter(ImDrawList* dl, int maxQuads)
    {
        private const int ChunkQuads = 4096;

        private int unreserved = maxQuads;
        private int reserved;
        private ImDrawVert* vtx;
        private ushort* idx;
        private uint vtxIndex;

        public readonly ImDrawList* DrawList => dl;

        // Quad corners in order, the UVs of the first and the third, and the color and extra data of all four.
        public void Add(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 uv0, Vector2 uv1, ImDrawVert vertex)
        {
            // More than counted is a bug, but drawing it is better than writing past the reservation.
            if (this.reserved == 0)
                this.Reserve();

            var vertices = this.vtx;
            var indices = this.idx;
            var i = (ushort)this.vtxIndex;
            indices[0] = i;
            indices[1] = (ushort)(i + 1);
            indices[2] = (ushort)(i + 2);
            indices[3] = i;
            indices[4] = (ushort)(i + 2);
            indices[5] = (ushort)(i + 3);
            vertex.Pos = a;
            vertex.Uv = uv0;
            vertices[0] = vertex;
            vertex.Pos = b;
            vertex.Uv = new(uv1.X, uv0.Y);
            vertices[1] = vertex;
            vertex.Pos = c;
            vertex.Uv = uv1;
            vertices[2] = vertex;
            vertex.Pos = d;
            vertex.Uv = new(uv0.X, uv1.Y);
            vertices[3] = vertex;
            this.vtx = vertices + 4;
            this.idx = indices + 6;
            this.vtxIndex += 4;
            this.reserved--;
        }

        // Returns unused reserved vertices and indices to dl. Subsequent Add calls reserve again.
        public void Finish()
        {
            if (this.reserved > 0)
            {
                dl->VtxWritePtr = this.vtx;
                dl->IdxWritePtr = this.idx;
                dl->VtxCurrentIdx = this.vtxIndex;
                ImGuiNative.PrimUnreserve(dl, this.reserved * 6, this.reserved * 4);
            }

            this.unreserved += this.reserved;
            this.reserved = 0;
        }

        private void Reserve()
        {
            var n = Math.Max(Math.Min(this.unreserved, ChunkQuads), 1);
            ImGuiNative.PrimReserve(dl, n * 6, n * 4);
            this.vtx = dl->VtxWritePtr;
            this.idx = dl->IdxWritePtr;
            this.vtxIndex = dl->VtxCurrentIdx;
            new Span<ushort>(this.idx, n * 6).Clear();
            dl->VtxWritePtr += n * 4;
            dl->IdxWritePtr += n * 6;
            dl->VtxCurrentIdx += (uint)(n * 4);
            this.unreserved -= n;
            this.reserved = n;
        }
    }

    private ref struct VisibleItemEnumerator(Span<Item> items, List<(int Begin, int End)> ranges)
    {
        private readonly Span<Item> items = items;
        private int range = -1;
        private int end;

        public readonly ref readonly Item Current => ref this.items[this.Index];

        public int Index { get; private set; }

        public readonly VisibleItemEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            this.Index++;
            while (this.Index >= this.end)
            {
                if (++this.range >= ranges.Count)
                    return false;
                (this.Index, this.end) = ranges[this.range];
            }

            return true;
        }
    }

    // The glyphs of an item being drawn, shared by all layouts, as drawing is on the main thread only.
    private sealed class GlyphLookup
    {
        private static GlyphLookup shared = new(64);

        private GlyphLookup(int capacity)
        {
            this.Ids = new uint[capacity];
            this.Slots = new int[capacity];
            this.Steps = new byte[capacity];
            this.Pens = new Vector2[capacity];
            this.Images = new GlyphImage[capacity];
        }

        public uint[] Ids { get; }

        // Where the glyphs' images are kept in the layout.
        public int[] Slots { get; }

        // The subpixel steps, as Font.GetGlyphImages takes them.
        public byte[] Steps { get; }

        // The pens in device pixels.
        public Vector2[] Pens { get; }

        public GlyphImage[] Images { get; }

        public static GlyphLookup Get(int count)
        {
            if (shared.Ids.Length < count)
                shared = new(Math.Max(count, shared.Ids.Length * 2));
            return shared;
        }
    }
}

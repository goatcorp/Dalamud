using System.Numerics;
using System.Runtime.CompilerServices;

namespace Dalamud.Bindings.ImGui;

/// <summary>A pointer to an <see cref="ImTextLayout"/>, calling through its interface. It does not own a reference.
/// </summary>
/// <remarks><ul>
/// <li>Main thread only. Dalamud's layouts ignore <see cref="AddRef"/> and <see cref="Release"/> on other threads.</li>
/// <li>A layout stays valid while referenced. Call <see cref="Release"/> once per owned reference, and never release
/// borrowed pointers.</li>
/// <li>Spans returned by <see cref="GetLines"/> and <see cref="GetRunRects"/> are owned by the layout and remain valid
/// until its last reference is released.</li>
/// <li>Calling methods on a null layout is a no-op. Queries return default values, or <see cref="ImTextCaret.None"/>
/// for carets.</li>
/// <li>Rectangles are <see cref="Vector4"/> (x0, y0, x1, y1), matching ImGui clip rectangles.</li>
/// </ul></remarks>
public unsafe partial struct ImTextLayoutPtr
{
    private ImTextLayoutVtbl* Vtbl => this.Handle->LpVtbl;

    /// <summary>Adds a reference.</summary>
    /// <returns>The new reference count, or 0 for a null layout.</returns>
    public uint AddRef()
    {
        if (this.Handle is null)
            return 0;
        if (ManagedTextLayouts.Of(this.Handle) is { } managed)
            return managed.AddRef(this.Handle);
        return ((delegate* unmanaged<ImTextLayout*, uint>)this.Vtbl->AddRef)(this.Handle);
    }

    /// <summary>Releases a reference.</summary>
    /// <returns>The new reference count, or 0 for a null layout.</returns>
    public uint Release()
    {
        if (this.Handle is null)
            return 0;
        if (ManagedTextLayouts.Of(this.Handle) is { } managed)
            return managed.Release(this.Handle);
        return ((delegate* unmanaged<ImTextLayout*, uint>)this.Vtbl->Release)(this.Handle);
    }

    /// <summary>Gets the layout box (from advances and line heights) and the ink box (enclosing the layout box and
    /// what is drawn, including overhangs, edges, and shadows).</summary>
    /// <param name="ink">The ink box (x0, y0, x1, y1).</param>
    /// <returns>The layout box (x0, y0, x1, y1).</returns>
    public Vector4 GetBoundingBox(out Vector4 ink)
    {
        Vector4 layout = default;
        Vector4 inkBox = default;
        if (this.Handle is not null)
        {
            if (ManagedTextLayouts.Of(this.Handle) is { } managed)
                return managed.GetBoundingBox(this.Handle, out ink);
            ((delegate* unmanaged<ImTextLayout*, Vector4*, Vector4*, void>)this.Vtbl->GetBoundingBox)(
                this.Handle,
                &layout,
                &inkBox);
        }

        ink = inkBox;
        return layout;
    }

    /// <summary>Gets the number of code units laid out.</summary>
    /// <returns>The length.</returns>
    public int GetTextLength()
    {
        if (this.Handle is null)
            return 0;
        if (ManagedTextLayouts.Of(this.Handle) is { } managed)
            return managed.GetTextLength(this.Handle);
        return ((delegate* unmanaged<ImTextLayout*, int>)this.Vtbl->GetTextLength)(this.Handle);
    }

    /// <summary>Gets the first code unit hidden by <see cref="ImTextLayoutFlags.Ellipsis"/>.</summary>
    /// <returns>The offset, or -1.</returns>
    public int GetEllipsisOffset()
    {
        if (this.Handle is null)
            return -1;
        if (ManagedTextLayouts.Of(this.Handle) is { } managed)
            return managed.GetEllipsisOffset(this.Handle);
        return ((delegate* unmanaged<ImTextLayout*, int>)this.Vtbl->GetEllipsisOffset)(this.Handle);
    }

    /// <summary>Gets the lines, owned by the layout.</summary>
    /// <returns>The lines.</returns>
    public ReadOnlySpan<ImTextLine> GetLines()
    {
        if (this.Handle is null)
            return default;
        if (ManagedTextLayouts.Of(this.Handle) is { } managed)
            return managed.GetLines(this.Handle);
        ImTextLine* lines = null;
        var count = ((delegate* unmanaged<ImTextLayout*, ImTextLine**, int>)this.Vtbl->GetLines)(this.Handle, &lines);
        return count <= 0 ? default : new(lines, count);
    }

    /// <summary>Gets the caret at an offset.</summary>
    /// <param name="textOffset">The offset, in code units.</param>
    /// <param name="preferPreviousLine"><see langword="true"/> to place the caret at the end of the previous line on a soft-wrap boundary.</param>
    /// <returns>The caret.</returns>
    public ImTextCaret GetCaret(int textOffset, bool preferPreviousLine = false)
    {
        if (this.Handle is null)
            return ImTextCaret.None;
        if (ManagedTextLayouts.Of(this.Handle) is { } managed)
            return managed.GetCaret(this.Handle, textOffset, preferPreviousLine);
        ImTextCaret caret;
        ((delegate* unmanaged<ImTextLayout*, int, byte, ImTextCaret*, void>)this.Vtbl->GetCaret)(
            this.Handle,
            textOffset,
            preferPreviousLine ? (byte)1 : (byte)0,
            &caret);
        return caret;
    }

    /// <summary>Gets the caret nearest to a point, with the run or object under it.</summary>
    /// <param name="point">The point, relative to the layout origin.</param>
    /// <returns>The caret.</returns>
    public ImTextCaret HitTest(Vector2 point)
    {
        if (this.Handle is null)
            return ImTextCaret.None;
        if (ManagedTextLayouts.Of(this.Handle) is { } managed)
            return managed.HitTest(this.Handle, point);
        ImTextCaret caret;
        ((delegate* unmanaged<ImTextLayout*, Vector2, ImTextCaret*, void>)this.Vtbl->HitTest)(
            this.Handle, point, &caret);
        return caret;
    }

    /// <summary>Moves a caret. It moves along the block axis first, then the inline axis, then dx units along the
    /// text.</summary>
    /// <param name="source">The caret to move from.</param>
    /// <param name="flags">Where to move.</param>
    /// <param name="dx">Signed step count in the unit selected by <paramref name="flags"/>, applied after block and inline movement.</param>
    /// <param name="dy">Lines, for <see cref="ImTextMoveFlags.BlockRelative"/>.</param>
    /// <param name="preferredInlinePos">Position along the line to keep. If &lt; 0, it is taken from the caret.</param>
    /// <returns>The moved caret, or <paramref name="source"/> if the layout is null or the move fails.</returns>
    public ImTextCaret Move(
        in ImTextCaret source, ImTextMoveFlags flags, int dx, int dy, float preferredInlinePos = -1)
    {
        if (this.Handle is null)
            return source;
        if (ManagedTextLayouts.Of(this.Handle) is { } managed)
            return managed.Move(this.Handle, source, flags, dx, dy, preferredInlinePos);
        ImTextCaret caret;
        fixed (ImTextCaret* pFrom = &source)
        {
            ((delegate* unmanaged<ImTextLayout*, ImTextCaret*, ImTextMoveFlags, int, int, float, ImTextCaret*, void>)
                this.Vtbl->Move)(
                this.Handle,
                pFrom,
                flags,
                dx,
                dy,
                preferredInlinePos,
                &caret);
        }

        return caret;
    }

    /// <summary>Gets the rectangles of a range, one per line it spans.</summary>
    /// <param name="textBegin">Start offset, in code units.</param>
    /// <param name="textEnd">End offset, in code units.</param>
    /// <param name="rects">Receives the rectangles (x0, y0, x1, y1).</param>
    /// <returns>The number of rectangles, which may exceed the length of <paramref name="rects"/>.</returns>
    public int GetRangeRects(int textBegin, int textEnd, Span<Vector4> rects)
    {
        if (this.Handle is null)
            return 0;
        if (ManagedTextLayouts.Of(this.Handle) is { } managed)
            return managed.GetRangeRects(this.Handle, textBegin, textEnd, rects);
        fixed (Vector4* p = rects)
        {
            return ((delegate* unmanaged<ImTextLayout*, int, int, Vector4*, int, int>)this.Vtbl->GetRangeRects)(
                this.Handle,
                textBegin,
                textEnd,
                p,
                rects.Length);
        }
    }

    /// <summary>Draws the graphemes of the layout that start within a range of code units.</summary>
    /// <param name="drawList">The draw list.</param>
    /// <param name="pos">Position of the layout origin.</param>
    /// <param name="col">Text color.</param>
    /// <param name="textBegin">Start offset in code units. Negative values resolve to
    /// <c>GetTextLength() + 1 + textBegin</c> clamped to <c>0</c>, so <c>-1</c> is <see cref="GetTextLength"/>.</param>
    /// <param name="textEnd">End offset in code units. <c>-1</c> renders through the end including any ellipsis, and
    /// values below <c>-1</c> resolve to <c>GetTextLength() + 1 + textEnd</c> clamped to <c>0</c>.</param>
    /// <param name="clipRect">Clip rectangle (x0, y0, x1, y1), untransformed. Default for the draw list's.</param>
    /// <param name="flags">Rendering-only flags.</param>
    /// <param name="transform">Transform around <paramref name="pos"/>. Default for none.</param>
    /// <remarks>Without a transform, <paramref name="clipRect"/> performs coarse culling widened for overhangs unless
    /// <see cref="ImTextRenderFlags.CpuFineClip"/> is set, while the draw list's clip rectangle still clips. With a
    /// transform, glyphs are clipped exactly, and decorations and inline objects are clipped to the axis-aligned
    /// bounding box of the transformed rectangle.</remarks>
    public void Render(
        ImDrawListPtr drawList, Vector2 pos, ImTextColor col, int textBegin = 0, int textEnd = -1,
        in Vector4 clipRect = default, ImTextRenderFlags flags = default, in ImTextTransform transform = default)
    {
        if (this.Handle is null)
            return;
        var hasClip = !Unsafe.IsNullRef(in clipRect) && clipRect != default;
        var hasTransform = !Unsafe.IsNullRef(in transform) &&
                           (transform.AxisX != default || transform.AxisY != default);
        if (textBegin < 0 || textEnd < -1)
            (textBegin, textEnd) = this.FromEnd(textBegin, textEnd);

        fixed (Vector4* pClip = &clipRect)
        fixed (ImTextTransform* pTransform = &transform)
        {
            var clip = hasClip ? pClip : null;
            var xf = hasTransform ? pTransform : null;
            if (ManagedTextLayouts.Of(this.Handle) is { } managed)
            {
                managed.Render(this.Handle, drawList, pos, col, textBegin, textEnd, clip, flags, xf);
            }
            else
            {
                ((delegate* unmanaged<
                    ImTextLayout*, ImDrawList*, Vector2, ImTextTransform*, uint, Vector4*, ImTextRenderFlags, int, int,
                    void>)
                    this.Vtbl->Render)(this.Handle, drawList, pos, xf, col, clip, flags, textBegin, textEnd);
            }
        }
    }

    /// <summary>Gets the number of runs.</summary>
    /// <returns>The number of runs.</returns>
    public int GetRunCount()
    {
        if (this.Handle is null)
            return 0;
        if (ManagedTextLayouts.Of(this.Handle) is { } managed)
            return managed.GetRunCount(this.Handle);
        return ((delegate* unmanaged<ImTextLayout*, int>)this.Vtbl->GetRunCount)(this.Handle);
    }

    /// <summary>Gets the hover boxes of a run, one per line it spans. The layout owns them.</summary>
    /// <param name="runIndex">The run.</param>
    /// <returns>The rectangles (x0, y0, x1, y1).</returns>
    public ReadOnlySpan<Vector4> GetRunRects(int runIndex)
    {
        if (this.Handle is null)
            return default;
        if (ManagedTextLayouts.Of(this.Handle) is { } managed)
            return managed.GetRunRects(this.Handle, runIndex);
        Vector4* rects = null;
        var count = ((delegate* unmanaged<ImTextLayout*, int, Vector4**, int>)this.Vtbl->GetRunRects)(
            this.Handle, runIndex, &rects);
        return count <= 0 ? default : new(rects, count);
    }

    private (int Begin, int End) FromEnd(int textBegin, int textEnd)
    {
        var end = this.GetTextLength() + 1;
        if (textBegin < 0)
            textBegin = Math.Max(textBegin + end, 0);

        // Keep -1 unchanged because layouts treat -1 as rendering through any trailing ellipsis.
        if (textEnd < -1)
            textEnd = Math.Max(textEnd + end, 0);
        return (textBegin, textEnd);
    }
}

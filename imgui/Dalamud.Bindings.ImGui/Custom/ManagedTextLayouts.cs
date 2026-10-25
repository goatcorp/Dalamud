using System.Numerics;

namespace Dalamud.Bindings.ImGui;

/// <summary>Text layouts implemented in managed code, which <see cref="ImTextLayoutPtr"/> calls without going through
/// native code.</summary>
internal abstract unsafe class ManagedTextLayouts
{
    private static ManagedTextLayouts? current;

    private readonly ImTextLayoutVtbl* vtbl;

    /// <summary>Initializes a new instance of the <see cref="ManagedTextLayouts"/> class.</summary>
    /// <param name="vtbl">The vtable of the layouts it implements.</param>
    protected ManagedTextLayouts(ImTextLayoutVtbl* vtbl) => this.vtbl = vtbl;

    /// <summary>Sets the implementation that <see cref="ImTextLayoutPtr"/> calls for layouts with its vtable.</summary>
    /// <param name="impl">The implementation.</param>
    public static void Register(ManagedTextLayouts impl) => current = impl;

    /// <summary>Gets the implementation of a layout.</summary>
    /// <param name="layout">The layout, not null.</param>
    /// <returns>The implementation, or null if the layout has another vtable.</returns>
    public static ManagedTextLayouts? Of(ImTextLayout* layout)
    {
        var impl = current;
        return impl is not null && layout->LpVtbl == impl.vtbl ? impl : null;
    }

    /// <inheritdoc cref="ImTextLayoutPtr.AddRef"/>
    public abstract uint AddRef(ImTextLayout* self);

    /// <inheritdoc cref="ImTextLayoutPtr.Release"/>
    public abstract uint Release(ImTextLayout* self);

    /// <inheritdoc cref="ImTextLayoutPtr.GetBoundingBox"/>
    public abstract Vector4 GetBoundingBox(ImTextLayout* self, out Vector4 ink);

    /// <inheritdoc cref="ImTextLayoutPtr.GetTextLength"/>
    public abstract int GetTextLength(ImTextLayout* self);

    /// <inheritdoc cref="ImTextLayoutPtr.GetEllipsisOffset"/>
    public abstract int GetEllipsisOffset(ImTextLayout* self);

    /// <inheritdoc cref="ImTextLayoutPtr.GetLines"/>
    public abstract ReadOnlySpan<ImTextLine> GetLines(ImTextLayout* self);

    /// <inheritdoc cref="ImTextLayoutPtr.GetCaret"/>
    public abstract ImTextCaret GetCaret(ImTextLayout* self, int textOffset, bool preferPreviousLine);

    /// <inheritdoc cref="ImTextLayoutPtr.HitTest"/>
    public abstract ImTextCaret HitTest(ImTextLayout* self, Vector2 point);

    /// <inheritdoc cref="ImTextLayoutPtr.Move"/>
    public abstract ImTextCaret Move(
        ImTextLayout* self, in ImTextCaret source, ImTextMoveFlags flags, int dx, int dy, float preferredInlinePos);

    /// <inheritdoc cref="ImTextLayoutPtr.GetRangeRects"/>
    public abstract int GetRangeRects(ImTextLayout* self, int textBegin, int textEnd, Span<Vector4> rects);

    /// <summary>Draws the glyphs of a range of the layout.</summary>
    /// <param name="self">The layout.</param>
    /// <param name="drawList">The draw list.</param>
    /// <param name="pos">Position of the layout origin.</param>
    /// <param name="col">Text color.</param>
    /// <param name="textBegin">Start byte offset.</param>
    /// <param name="textEnd">End byte offset, or less than 0 for the end of the text.</param>
    /// <param name="clipRect">Clip rectangle, or null for the draw list's.</param>
    /// <param name="flags">Rendering-only flags.</param>
    /// <param name="transform">Transform, or null.</param>
    public abstract void RenderRange(
        ImTextLayout* self, ImDrawList* drawList, Vector2 pos, uint col, int textBegin, int textEnd,
        Vector4* clipRect, ImTextRenderFlags flags, ImTextTransform* transform);

    /// <inheritdoc cref="ImTextLayoutPtr.GetRunCount"/>
    public abstract int GetRunCount(ImTextLayout* self);

    /// <inheritdoc cref="ImTextLayoutPtr.GetRunRects"/>
    public abstract ReadOnlySpan<Vector4> GetRunRects(ImTextLayout* self, int runIndex);
}

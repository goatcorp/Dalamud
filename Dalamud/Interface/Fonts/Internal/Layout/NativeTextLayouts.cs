using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Utility;

using Serilog;

using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.Layout;

/// <summary>Layouts as ImGui's <see cref="ImTextLayout"/>, with their vtable and reference counts.</summary>
/// <remarks>Main thread only.</remarks>
internal static unsafe class NativeTextLayouts
{
    private static readonly ImTextLayoutVtbl* Vtbl = CreateVtbl();

    // What ImTextLayoutPtr calls from C# for the layouts here, instead of the vtable.
    private static readonly ManagedLayouts Managed = new(Vtbl);

    /// <summary>Makes <see cref="ImTextLayoutPtr"/> call the layouts here without going through the vtable.</summary>
    public static void Register() => ManagedTextLayouts.Register(Managed);

    /// <summary>Gives ImGui a reference to a layout.</summary>
    /// <param name="layout">The layout.</param>
    /// <returns>The native layout, which ImGui releases.</returns>
    /// <remarks>A layout has one native layout at a time, made on the first reference. The cache holds one while it
    /// has the layout, so that drawing it only counts references.</remarks>
    public static ImTextLayout* ToNative(TextLayout layout)
    {
        var p = (NativeLayout*)layout.NativeHandle;
        if (p is null)
        {
            p = (NativeLayout*)NativeMemory.Alloc((nuint)sizeof(NativeLayout));
            p->Vtbl = Vtbl;
            p->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(layout));
            p->RefCount = 0;
            layout.NativeHandle = (nint)p;
        }

        p->RefCount++;
        return (ImTextLayout*)p;
    }

    /// <summary>Releases a reference. The last frees the native layout, and gives the layout back to the pool if the
    /// cache does not have it either.</summary>
    /// <param name="layout">The layout, which has a native layout.</param>
    /// <returns>The new reference count.</returns>
    public static uint Release(TextLayout layout) => Release((NativeLayout*)layout.NativeHandle);

    /// <summary>Gets the layout of a native layout made here.</summary>
    /// <param name="self">The native layout.</param>
    /// <returns>The layout.</returns>
    public static TextLayout Get(ImTextLayout* self) =>
        (TextLayout)GCHandle.FromIntPtr(((NativeLayout*)self)->Handle).Target!;

    private static uint Release(NativeLayout* p)
    {
        var refs = --p->RefCount;
        if (refs != 0)
            return refs;
        var handle = GCHandle.FromIntPtr(p->Handle);
        var layout = (TextLayout)handle.Target!;
        handle.Free();
        NativeMemory.Free(p);
        layout.NativeHandle = 0;
        if (!layout.Cached)
            layout.Recycle();
        return 0;
    }

    private static ImTextLayoutVtbl* CreateVtbl()
    {
        var v = (ImTextLayoutVtbl*)RuntimeHelpers.AllocateTypeAssociatedMemory(
            typeof(NativeTextLayouts),
            sizeof(ImTextLayoutVtbl));
        v->QueryInterface = (delegate* unmanaged<ImTextLayout*, ImGUID*, void**, int>)&QueryInterfaceImpl;
        v->AddRef = (delegate* unmanaged<ImTextLayout*, uint>)&AddRefImpl;
        v->Release = (delegate* unmanaged<ImTextLayout*, uint>)&ReleaseImpl;
        v->GetBoundingBox = (delegate* unmanaged<ImTextLayout*, Vector4*, Vector4*, void>)&GetBoundingBoxImpl;
        v->GetTextLength = (delegate* unmanaged<ImTextLayout*, int>)&GetTextLengthImpl;
        v->GetEllipsisOffset = (delegate* unmanaged<ImTextLayout*, int>)&GetEllipsisOffsetImpl;
        v->GetLines = (delegate* unmanaged<ImTextLayout*, ImTextLine**, int>)&GetLinesImpl;
        v->GetCaret = (delegate* unmanaged<ImTextLayout*, int, byte, ImTextCaret*, void>)&GetCaretImpl;
        v->HitTest = (delegate* unmanaged<ImTextLayout*, Vector2, ImTextCaret*, void>)&HitTestImpl;
        v->Move =
            (delegate* unmanaged<ImTextLayout*, ImTextCaret*, ImTextMoveFlags, int, int, float, ImTextCaret*,
                void>)&MoveImpl;
        v->GetRangeRects = (delegate* unmanaged<ImTextLayout*, int, int, Vector4*, int, int>)&GetRangeRectsImpl;
        v->Render =
            (delegate* unmanaged<ImTextLayout*, ImDrawList*, Vector2, ImTextTransform*, uint, Vector4*,
                ImTextRenderFlags, void>)&RenderImpl;
        v->RenderRange =
            (delegate* unmanaged<ImTextLayout*, ImDrawList*, Vector2, ImTextTransform*, uint, Vector4*,
                ImTextRenderFlags, int, int, void>)&RenderRangeImpl;
        v->GetRunCount = (delegate* unmanaged<ImTextLayout*, int>)&GetRunCountImpl;
        v->GetRunRects = (delegate* unmanaged<ImTextLayout*, int, Vector4**, int>)&GetRunRectsImpl;
        return v;
    }

    [UnmanagedCallersOnly]
    private static int QueryInterfaceImpl(ImTextLayout* self, ImGUID* iid, void** outObject)
    {
        if (outObject is not null)
            *outObject = null;
        return E.E_NOINTERFACE;
    }

    [UnmanagedCallersOnly]
    private static uint AddRefImpl(ImTextLayout* self) => Managed.AddRef(self);

    [UnmanagedCallersOnly]
    private static uint ReleaseImpl(ImTextLayout* self) => Managed.Release(self);

    // Logs a reference change off the main thread. Returns the count, as it stays.
    private static uint ReportWrongThread(NativeLayout* p)
    {
        Log.Logger.Error("A reference to a text layout was added or released off the main thread, and was ignored");
        return p->RefCount;
    }

    [UnmanagedCallersOnly]
    private static void GetBoundingBoxImpl(ImTextLayout* self, Vector4* outLayout, Vector4* outInk)
    {
        var layout = Managed.GetBoundingBox(self, out var ink);
        if (outLayout is not null)
            *outLayout = layout;
        if (outInk is not null)
            *outInk = ink;
    }

    [UnmanagedCallersOnly]
    private static int GetTextLengthImpl(ImTextLayout* self) => Managed.GetTextLength(self);

    [UnmanagedCallersOnly]
    private static int GetEllipsisOffsetImpl(ImTextLayout* self) => Managed.GetEllipsisOffset(self);

    [UnmanagedCallersOnly]
    private static int GetLinesImpl(ImTextLayout* self, ImTextLine** outLines)
    {
        var lines = Managed.GetLines(self);
        if (outLines is not null)
            *outLines = lines.IsEmpty ? null : (ImTextLine*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(lines));
        return lines.Length;
    }

    [UnmanagedCallersOnly]
    private static void GetCaretImpl(
        ImTextLayout* self, int textOffset, byte preferPreviousLine, ImTextCaret* outCaret) =>
        *outCaret = Managed.GetCaret(self, textOffset, preferPreviousLine != 0);

    [UnmanagedCallersOnly]
    private static void HitTestImpl(ImTextLayout* self, Vector2 point, ImTextCaret* outCaret) =>
        *outCaret = Managed.HitTest(self, point);

    [UnmanagedCallersOnly]
    private static void MoveImpl(
        ImTextLayout* self, ImTextCaret* from, ImTextMoveFlags flags, int dx, int dy, float preferredInlinePos,
        ImTextCaret* outCaret) =>
        *outCaret = Managed.Move(self, *from, flags, dx, dy, preferredInlinePos);

    [UnmanagedCallersOnly]
    private static int GetRangeRectsImpl(
        ImTextLayout* self, int textBegin, int textEnd, Vector4* outRects, int capacity) =>
        Managed.GetRangeRects(
            self,
            textBegin,
            textEnd,
            outRects is null ? default : new(outRects, Math.Max(capacity, 0)));

    [UnmanagedCallersOnly]
    private static void RenderImpl(
        ImTextLayout* self, ImDrawList* drawList, Vector2 pos, ImTextTransform* transform, uint col, Vector4* clipRect,
        ImTextRenderFlags flags) =>
        Managed.RenderRange(self, drawList, pos, col, 0, -1, clipRect, flags, transform);

    [UnmanagedCallersOnly]
    private static void RenderRangeImpl(
        ImTextLayout* self, ImDrawList* drawList, Vector2 pos, ImTextTransform* transform, uint col, Vector4* clipRect,
        ImTextRenderFlags flags, int textBegin, int textEnd) =>
        Managed.RenderRange(self, drawList, pos, col, textBegin, textEnd, clipRect, flags, transform);

    [UnmanagedCallersOnly]
    private static int GetRunCountImpl(ImTextLayout* self) => Managed.GetRunCount(self);

    [UnmanagedCallersOnly]
    private static int GetRunRectsImpl(ImTextLayout* self, int runIndex, Vector4** outRects)
    {
        var rects = Managed.GetRunRects(self, runIndex);
        if (outRects is not null)
            *outRects = rects.IsEmpty ? null : (Vector4*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(rects));
        return rects.Length;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeLayout
    {
        public ImTextLayoutVtbl* Vtbl;
        public nint Handle;
        public uint RefCount;
    }

    // The layouts here, for both the vtable and ImTextLayoutPtr. Calls that can throw report it and give a default, as
    // the vtable cannot throw to ImGui.
    private sealed class ManagedLayouts(ImTextLayoutVtbl* vtbl) : ManagedTextLayouts(vtbl)
    {
        // The count is not atomic, as layouts are main thread only. Other threads would race it, so they change
        // nothing.
        public override uint AddRef(ImTextLayout* self)
        {
            var p = (NativeLayout*)self;
            if (!ThreadSafety.IsMainThread)
                return ReportWrongThread(p);
            return ++p->RefCount;
        }

        public override uint Release(ImTextLayout* self)
        {
            var p = (NativeLayout*)self;
            if (!ThreadSafety.IsMainThread)
                return ReportWrongThread(p);
            return NativeTextLayouts.Release(p);
        }

        public override Vector4 GetBoundingBox(ImTextLayout* self, out Vector4 ink)
        {
            var layout = Get(self);
            ink = layout.Ink;
            return new(0, 0, layout.Size.X, layout.Size.Y);
        }

        public override int GetTextLength(ImTextLayout* self) => Get(self).TextLength;

        public override int GetEllipsisOffset(ImTextLayout* self) => Get(self).EllipsisOffset;

        public override ReadOnlySpan<ImTextLine> GetLines(ImTextLayout* self) => Get(self).Lines;

        public override ImTextCaret GetCaret(ImTextLayout* self, int textOffset, bool preferPreviousLine)
        {
            try
            {
                return Get(self).GetCaret(textOffset, preferPreviousLine);
            }
            catch (Exception e)
            {
                TextLayouter.Report(e);
                return ImTextCaret.None;
            }
        }

        public override ImTextCaret HitTest(ImTextLayout* self, Vector2 point)
        {
            try
            {
                return Get(self).HitTest(point);
            }
            catch (Exception e)
            {
                TextLayouter.Report(e);
                return ImTextCaret.None;
            }
        }

        public override ImTextCaret Move(
            ImTextLayout* self, in ImTextCaret source, ImTextMoveFlags flags, int dx, int dy, float preferredInlinePos)
        {
            try
            {
                return Get(self).Move(source, flags, dx, dy, preferredInlinePos);
            }
            catch (Exception e)
            {
                TextLayouter.Report(e);
                return source;
            }
        }

        public override int GetRangeRects(ImTextLayout* self, int textBegin, int textEnd, Span<Vector4> rects)
        {
            try
            {
                return Get(self).GetRangeRects(textBegin, textEnd, rects);
            }
            catch (Exception e)
            {
                TextLayouter.Report(e);
                return 0;
            }
        }

        public override void RenderRange(
            ImTextLayout* self, ImDrawList* drawList, Vector2 pos, uint col, int textBegin, int textEnd,
            Vector4* clipRect, ImTextRenderFlags flags, ImTextTransform* transform)
        {
            try
            {
                Get(self).RenderRange(
                    drawList,
                    pos,
                    col,
                    textBegin,
                    textEnd,
                    clipRect is null ? null : *clipRect,
                    flags,
                    transform is null ? null : *transform);
            }
            catch (Exception e)
            {
                TextLayouter.Report(e);
            }
        }

        public override int GetRunCount(ImTextLayout* self) => Get(self).RunCount;

        public override ReadOnlySpan<Vector4> GetRunRects(ImTextLayout* self, int runIndex) =>
            Get(self).GetRunRects(runIndex);
    }
}

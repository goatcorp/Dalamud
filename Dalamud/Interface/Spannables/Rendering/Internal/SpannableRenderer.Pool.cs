using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Strings;
using Dalamud.Utility;

using ImGuiNET;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables.Rendering.Internal;

/// <summary>A custom text renderer implementation.</summary>
internal sealed unsafe partial class SpannableRenderer
{
    private ObjectPool<SpannedStringBuilder>? builderPool =
        new DefaultObjectPool<SpannedStringBuilder>(new DefaultPooledObjectPolicy<SpannedStringBuilder>());

    /// <summary>Do not use directly. Use <see cref="RentSplitter"/>.</summary>
    [Obsolete($"Do not use directly. Use {nameof(RentSplitter)}.")]
    private nint[] splitters = new nint[16];

    /// <inheritdoc/>
    // Let it throw NRE of builderPool is null (disposed).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpannedStringBuilder RentBuilder() => this.builderPool!.Get();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnBuilder(SpannedStringBuilder? builder)
    {
        // Let it throw NRE of builderPool is null (disposed).
        if (builder != null)
            this.builderPool!.Return(builder);
    }

#pragma warning disable CS0618 // Type or member is obsolete

    /// <summary>Rents a splitter.</summary>
    /// <param name="drawListPtr">An instance of <see cref="ImDrawList"/>.</param>
    /// <returns>The rented instance.</returns>
    public DrawListSplitter RentSplitter(ImDrawListPtr drawListPtr)
    {
        ThreadSafety.DebugAssertMainThread();

        if (drawListPtr.NativePtr is null)
            return default;

        foreach (ref var slot in this.splitters.AsSpan())
        {
            if (slot == 0)
                continue;
            var rented = new DrawListSplitter(slot, drawListPtr, this);
            slot = nint.Zero;
            return rented;
        }

        return new(ImGuiNative.ImDrawListSplitter_ImDrawListSplitter(), drawListPtr, this);
    }

    private void DisposePooledObjects()
    {
        foreach (ref var slot in this.splitters.AsSpan())
        {
            if (slot != 0)
                ImGuiNative.ImDrawListSplitter_destroy((ImDrawListSplitter*)slot);
            slot = 0;
        }

        this.splitters = Array.Empty<nint>();
        this.builderPool = null;
    }

    /// <summary>Pooled draw list splitter.</summary>
    public struct DrawListSplitter : IDisposable
    {
        private ImDrawListSplitter* splitter;
        private ImDrawList* drawList;
        private SpannableRenderer? returnTo;

        /// <summary>Initializes a new instance of the <see cref="DrawListSplitter"/> struct.</summary>
        /// <param name="splitter">An instance of <see cref="ImDrawListSplitter"/>.</param>
        /// <param name="drawList">An instance of <see cref="ImDrawList"/>.</param>
        /// <param name="returnTo">The pool owner.</param>
        public DrawListSplitter(
            ImDrawListSplitterPtr splitter,
            ImDrawListPtr drawList,
            SpannableRenderer? returnTo)
        {
            ThreadSafety.DebugAssertMainThread();

            this.splitter = splitter;
            this.drawList = drawList;
            this.returnTo = returnTo;

            if (this.drawList is not null && this.splitter is not null)
                ImGuiNative.ImDrawListSplitter_Split(this.splitter, this.drawList, (int)RenderChannel.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ImDrawListSplitterPtr(DrawListSplitter s) => s.splitter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ImDrawListSplitter*(DrawListSplitter s) => s.splitter;

        /// <summary>Sets the current channel.</summary>
        /// <param name="channel">The channel.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void SetChannel(RenderChannel channel)
        {
            if (this.drawList is not null && this.splitter is not null)
                ImGuiNative.ImDrawListSplitter_SetCurrentChannel(this.splitter, this.drawList, (int)channel);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ThreadSafety.DebugAssertMainThread();

            if (this.drawList is not null && this.splitter is not null)
                ImGuiNative.ImDrawListSplitter_Merge(this.splitter, this.drawList);

            if (this.returnTo is not null)
            {
                foreach (ref var slot in this.returnTo.splitters.AsSpan())
                {
                    if (slot == 0)
                        continue;
                    slot = (nint)this.splitter;
                    this.splitter = default;
                    break;
                }
            }

            if (this.splitter is not null)
                ImGuiNative.ImDrawListSplitter_destroy(this.splitter);

            this.splitter = default;
            this.drawList = default;
            this.returnTo = null;
        }
    }

#pragma warning restore CS0618 // Type or member is obsolete
}

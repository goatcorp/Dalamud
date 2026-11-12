using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.ImGuiBackend;

/// <summary>
/// Owns unmanaged copies of the draw-data header and command, index, and vertex buffers.
/// </summary>
/// <remarks>
/// <para>
/// Readers must be excluded while the snapshot is copied or disposed. Texture handles and callback data remain
/// borrowed and must stay valid until no render can use the snapshot. Draw callbacks may execute repeatedly and
/// on a presentation worker thread.
/// </para>
/// <para>
/// Copied <see cref="ImDrawList"/> headers expose only CmdBuffer, IdxBuffer, and VtxBuffer. All other fields
/// are zeroed, including those seen by callbacks.
/// </para>
/// </remarks>
internal sealed unsafe class DrawDataSnapshot : IDisposable
{
    private readonly ImDrawData* header;

    /// <summary>Owned draw-list headers.</summary>
    private ImDrawList* drawListArray;

    /// <summary>Array of <see cref="ImDrawList"/>* assigned to <see cref="header"/>.CmdLists.</summary>
    private ImDrawList** cmdListPtrArray;

    /// <summary>Number of allocated draw-list slots.</summary>
    private int listCapacity;

    /// <summary>Native vector storage for all allocated slots, including those unused by the current frame.</summary>
    private DrawListBuffers[] listBuffers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DrawDataSnapshot"/> class and allocates its native header.
    /// </summary>
    public DrawDataSnapshot()
    {
        this.header = (ImDrawData*)NativeMemory.AllocZeroed((nuint)sizeof(ImDrawData));
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="DrawDataSnapshot"/> class.
    /// </summary>
    ~DrawDataSnapshot() => this.FreeNativeResources();

    /// <summary>
    /// Gets a pointer to the copied ImDrawData.
    /// </summary>
    public ImDrawData* Handle => this.header;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.FreeNativeResources();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Copies the draw-data header and each command, index, and vertex buffer into owned unmanaged storage.
    /// </summary>
    /// <remarks>
    /// External pointers embedded in draw commands, such as texture handles and callback data, remain borrowed.
    /// </remarks>
    /// <param name="src">
    /// The live draw-data pointer obtained immediately after ImGui.Render().
    /// </param>
    public void CopyFrom(ImDrawData* src)
    {
        var count = src->CmdListsCount;

        // Grow the draw-list headers while retaining each slot's native vectors.
        if (count > this.listCapacity)
        {
            Array.Resize(ref this.listBuffers, count);

            if (this.drawListArray is not null)
            {
                NativeMemory.Free(this.drawListArray);
                this.drawListArray = null;
            }

            if (this.cmdListPtrArray is not null)
            {
                NativeMemory.Free(this.cmdListPtrArray);
                this.cmdListPtrArray = null;
            }

            if (count > 0)
            {
                this.drawListArray =
                    (ImDrawList*)NativeMemory.AllocZeroed((nuint)count, (nuint)sizeof(ImDrawList));
                this.cmdListPtrArray =
                    (ImDrawList**)NativeMemory.AllocZeroed((nuint)count, (nuint)sizeof(nint));
            }

            this.listCapacity = count;
        }

        // Copy scalar frame metadata, redirect CmdLists to owned storage, and clear the unused OwnerViewport.
        NativeMemory.Copy(src, this.header, (nuint)sizeof(ImDrawData));
        this.header->CmdLists = count > 0 ? this.cmdListPtrArray : null;
        this.header->OwnerViewport = null;

        for (var i = 0; i < count; i++)
        {
            var srcList = src->CmdLists[i];
            ref var buffers = ref this.listBuffers[i];

            var cmdCount = srcList->CmdBuffer.Size;
            GrowBuffer(ref buffers.CmdBuffer, ref buffers.CmdCapacity, cmdCount, sizeof(ImDrawCmd));
            new Span<ImDrawCmd>(srcList->CmdBuffer.Data, cmdCount)
                .CopyTo(new Span<ImDrawCmd>((ImDrawCmd*)buffers.CmdBuffer, cmdCount));

            var idxCount = srcList->IdxBuffer.Size;
            GrowBuffer(ref buffers.IdxBuffer, ref buffers.IdxCapacity, idxCount, sizeof(ushort));
            new Span<ushort>(srcList->IdxBuffer.Data, idxCount)
                .CopyTo(new Span<ushort>((ushort*)buffers.IdxBuffer, idxCount));

            var vtxCount = srcList->VtxBuffer.Size;
            GrowBuffer(ref buffers.VtxBuffer, ref buffers.VtxCapacity, vtxCount, sizeof(ImDrawVert));
            new Span<ImDrawVert>(srcList->VtxBuffer.Data, vtxCount)
                .CopyTo(new Span<ImDrawVert>((ImDrawVert*)buffers.VtxBuffer, vtxCount));

            // Reconstruct only the ImDrawList fields consumed by the DX11 renderer.
            var dstList = this.drawListArray + i;
            *dstList = default;
            dstList->CmdBuffer = new ImVector<ImDrawCmd>(cmdCount, buffers.CmdCapacity, (ImDrawCmd*)buffers.CmdBuffer);
            dstList->IdxBuffer = new ImVector<ushort>(idxCount, buffers.IdxCapacity, (ushort*)buffers.IdxBuffer);
            dstList->VtxBuffer = new ImVector<ImDrawVert>(vtxCount, buffers.VtxCapacity, (ImDrawVert*)buffers.VtxBuffer);

            this.cmdListPtrArray![i] = dstList;
        }
    }

    /// <summary>
    /// Ensures <paramref name="buffer"/> can hold at least <paramref name="neededCount"/> elements of
    /// <paramref name="elementSize"/> bytes, reallocating (with geometric growth) as necessary.
    /// </summary>
    private static void GrowBuffer(ref void* buffer, ref int capacity, int neededCount, int elementSize)
    {
        if (neededCount <= capacity)
            return;

        if (buffer is not null)
            NativeMemory.Free(buffer);

        capacity = Math.Max(neededCount, capacity == 0 ? 4 : capacity * 2);
        buffer = NativeMemory.AllocZeroed((nuint)capacity, (nuint)elementSize);
    }

    private void FreeNativeResources()
    {
        NativeMemory.Free(this.header);

        if (this.drawListArray is not null)
        {
            NativeMemory.Free(this.drawListArray);
            this.drawListArray = null;
        }

        if (this.cmdListPtrArray is not null)
        {
            NativeMemory.Free(this.cmdListPtrArray);
            this.cmdListPtrArray = null;
        }

        foreach (var buffers in this.listBuffers)
        {
            if (buffers.CmdBuffer is not null) NativeMemory.Free(buffers.CmdBuffer);
            if (buffers.IdxBuffer is not null) NativeMemory.Free(buffers.IdxBuffer);
            if (buffers.VtxBuffer is not null) NativeMemory.Free(buffers.VtxBuffer);
        }

        this.listBuffers = [];
        this.listCapacity = 0;
    }

    /// <summary>Owned native vectors and their capacities for one reusable draw-list slot.</summary>
    private struct DrawListBuffers
    {
        public void* CmdBuffer;
        public int CmdCapacity;
        public void* IdxBuffer;
        public int IdxCapacity;
        public void* VtxBuffer;
        public int VtxCapacity;
    }
}

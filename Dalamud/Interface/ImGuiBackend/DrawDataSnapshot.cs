using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.ImGuiBackend;

/// <summary>
/// Owns a deep copy of ImGui draw data in unmanaged native memory, safe for reading from a render thread
/// that may run concurrently with the next ImGui update step.
/// </summary>
/// <remarks>
/// <para>
/// The only fields of each <see cref="ImDrawList"/> that are populated are the three that the DX11 renderer
/// reads: CmdBuffer, IdxBuffer and VtxBuffer. All other fields are zeroed.
/// </para>
/// </remarks>
internal sealed unsafe class DrawDataSnapshot : IDisposable
{
    private readonly ImDrawData* header;

    /// <summary>Array of <see cref="ImDrawList"/> structs. Only CmdBuffer/IdxBuffer/VtxBuffer are populated.</summary>
    private ImDrawList* drawListArray;

    /// <summary>Array of <see cref="ImDrawList"/>* assigned to <see cref="header"/>.CmdLists.</summary>
    private ImDrawList** cmdListPtrArray;

    /// <summary>Number of list entries currently allocated in the two arrays above.</summary>
    private int listCapacity;

    private void*[] cmdBuffers = [];
    private int[] cmdCapacities = [];

    private void*[] idxBuffers = [];
    private int[] idxCapacities = [];

    private void*[] vtxBuffers = [];
    private int[] vtxCapacities = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DrawDataSnapshot"/> class and allocate internal resources.
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
    /// Deep-copies all draw data referenced by <paramref name="src"/> into this snapshot's
    /// owned unmanaged buffers.  The source ImGui memory is not accessed after this method returns.
    /// </summary>
    /// <param name="src">
    /// The live draw-data pointer obtained immediately after ImGui.Render().
    /// </param>
    public void CopyFrom(ImDrawData* src)
    {
        var count = src->CmdListsCount;

        // Grow the draw-list arrays if necessary
        if (count > this.listCapacity)
        {
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

            this.cmdBuffers = new void*[count];
            this.cmdCapacities = new int[count];
            this.idxBuffers = new void*[count];
            this.idxCapacities = new int[count];
            this.vtxBuffers = new void*[count];
            this.vtxCapacities = new int[count];

            this.listCapacity = count;
        }

        // Shallow-copy all scalar fields of ImDrawData, then override the pointer fields
        NativeMemory.Copy(src, this.header, (nuint)sizeof(ImDrawData));
        this.header->CmdLists = count > 0 ? this.cmdListPtrArray : null;
        this.header->OwnerViewport = null; // viewport pointer is not used by the renderer

        for (var i = 0; i < count; i++)
        {
            var srcList = src->CmdLists[i];

            // CmdBuffer
            var cmdCount = srcList->CmdBuffer.Size;
            GrowBuffer(ref this.cmdBuffers[i], ref this.cmdCapacities[i], cmdCount, sizeof(ImDrawCmd));
            new Span<ImDrawCmd>(srcList->CmdBuffer.Data, cmdCount)
                .CopyTo(new Span<ImDrawCmd>((ImDrawCmd*)this.cmdBuffers[i], cmdCount));

            // IdxBuffer
            var idxCount = srcList->IdxBuffer.Size;
            GrowBuffer(ref this.idxBuffers[i], ref this.idxCapacities[i], idxCount, sizeof(ushort));
            new Span<ushort>(srcList->IdxBuffer.Data, idxCount)
                .CopyTo(new Span<ushort>((ushort*)this.idxBuffers[i], idxCount));

            // VtxBuffer
            var vtxCount = srcList->VtxBuffer.Size;
            GrowBuffer(ref this.vtxBuffers[i], ref this.vtxCapacities[i], vtxCount, sizeof(ImDrawVert));
            new Span<ImDrawVert>(srcList->VtxBuffer.Data, vtxCount)
                .CopyTo(new Span<ImDrawVert>((ImDrawVert*)this.vtxBuffers[i], vtxCount));

            // Reconstruct a minimal ImDrawList that only has the three fields the renderer reads
            var dstList = this.drawListArray + i;
            *dstList = default;
            dstList->CmdBuffer = new ImVector<ImDrawCmd>(cmdCount, this.cmdCapacities[i], (ImDrawCmd*)this.cmdBuffers[i]);
            dstList->IdxBuffer = new ImVector<ushort>(idxCount, this.idxCapacities[i], (ushort*)this.idxBuffers[i]);
            dstList->VtxBuffer = new ImVector<ImDrawVert>(vtxCount, this.vtxCapacities[i], (ImDrawVert*)this.vtxBuffers[i]);

            // cmdListPtrArray is non-null whenever count > 0, which is the only time we enter this loop
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

        for (var i = 0; i < this.cmdBuffers.Length; i++)
        {
            if (this.cmdBuffers[i] is not null) NativeMemory.Free(this.cmdBuffers[i]);
            if (this.idxBuffers[i] is not null) NativeMemory.Free(this.idxBuffers[i]);
            if (this.vtxBuffers[i] is not null) NativeMemory.Free(this.vtxBuffers[i]);
        }

        this.cmdBuffers = [];
        this.idxBuffers = [];
        this.vtxBuffers = [];
        this.cmdCapacities = [];
        this.idxCapacities = [];
        this.vtxCapacities = [];
        this.listCapacity = 0;
    }
}

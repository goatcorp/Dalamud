using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Utility;

using TerraFX.Interop;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Implementations;

/// <summary>
/// Deals with rendering ImGui using DirectX 12.
/// See https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx12.cpp for the original implementation.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal unsafe partial class Dx12Renderer
{
    private sealed class AutoResourceHeap : IDisposable
    {
        private readonly List<Heap> standardHeaps = new();
        private readonly List<Heap> hugeHeaps = new();
        private readonly D3D12_HEAP_DESC heapDesc;
        private readonly ulong minAlignment;
        private readonly string heapDebugName;
        private readonly long standardHeapRemovalGracePeriodTickCount;
        private readonly long hugeHeapRemovalGracePeriodTickCount;
        private int heapCounter;
        private ComPtr<ID3D12Device> device;

        public AutoResourceHeap(
            ID3D12Device* device,
            D3D12_HEAP_FLAGS heapFlags,
            D3D12_HEAP_PROPERTIES heapProperties,
            ulong standardHeapSize = 1024 * D3D12.D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
            ulong heapAlignment = D3D12.D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
            ulong minAllocationAlignment = D3D12.D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT,
            long standardHeapRemovalGracePeriodTickCount = 5000,
            long hugeHeapRemovalGracePeriodTickCount = 500,
            [CallerMemberName] string debugName = "")
        {
            this.device = new(device);
            this.minAlignment = minAllocationAlignment;
            this.standardHeapRemovalGracePeriodTickCount = standardHeapRemovalGracePeriodTickCount;
            this.hugeHeapRemovalGracePeriodTickCount = hugeHeapRemovalGracePeriodTickCount;
            this.heapDebugName = debugName;
            this.heapDesc = new()
            {
                SizeInBytes = standardHeapSize,
                Properties = heapProperties,
                Alignment = heapAlignment,
                Flags = heapFlags,
            };
        }

        ~AutoResourceHeap() => this.ReleaseUnmanagedResources();

        public void Dispose()
        {
            this.ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public ComPtr<ID3D12Resource> CreateResource(
            out D3D12_RESOURCE_DESC outDesc,
            D3D12_RESOURCE_DESC desc,
            D3D12_RESOURCE_STATES initialState = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON,
            D3D12_CLEAR_VALUE* pOptimizedClearValue = null,
            [CallerMemberName] string debugName = "")
        {
            desc.Alignment = D3D12.D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT;
            var resAlloc = this.device.Get()->GetResourceAllocationInfo(0, 1, &desc);
            if (resAlloc.SizeInBytes == ulong.MaxValue)
            {
                desc.Alignment = D3D12.D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
                resAlloc = this.device.Get()->GetResourceAllocationInfo(0, 1, &desc);
                if (resAlloc.SizeInBytes == ulong.MaxValue)
                    throw new InvalidOperationException($"{nameof(ID3D12Device.GetResourceAllocationInfo)}");
            }

            outDesc = desc;

            var useNormalHeap = resAlloc.SizeInBytes <= this.heapDesc.SizeInBytes;
            var heaps = useNormalHeap ? this.standardHeaps : this.hugeHeaps;

            lock (heaps)
            {
                for (int i = 0, to = heaps.Count; i <= to; i++)
                {
                    if (i == to)
                    {
                        var counter = Interlocked.Increment(ref this.heapCounter);
                        heaps.Add(
                            new(
                                this.device,
                                useNormalHeap
                                    ? this.heapDesc
                                    : this.heapDesc with { SizeInBytes = resAlloc.SizeInBytes },
                                this.minAlignment,
                                useNormalHeap
                                    ? $"{this.heapDebugName}#{counter}"
                                    : $"{this.heapDebugName}#{counter}#Huge"));
                    }

                    var r = heaps[i].CreatePlacedResource(
                        resAlloc,
                        desc,
                        initialState,
                        pOptimizedClearValue,
                        debugName);
                    if (!r.IsEmpty())
                        return r;
                }
            }

            throw new OutOfMemoryException();
        }

        public void ClearEmptyHeaps()
        {
            for (var heapTypeIndex = 0; heapTypeIndex < 2; heapTypeIndex++)
            {
                var expireAt = Environment.TickCount64 -
                               (heapTypeIndex == 0
                                    ? this.standardHeapRemovalGracePeriodTickCount
                                    : this.hugeHeapRemovalGracePeriodTickCount);
                var heaps = heapTypeIndex == 0 ? this.standardHeaps : this.hugeHeaps;
                lock (heaps)
                {
                    for (var i = heaps.Count - 1; i >= 0; i--)
                    {
                        var heap = heaps[i];
                        if (heap.RefCount > 1 || heap.LastNodeReleaseTickCount > expireAt)
                            continue;

                        heap.Dispose();
                        heaps.RemoveAt(i);
                    }
                }
            }
        }

        private void ReleaseUnmanagedResources()
        {
            for (var heapTypeIndex = 0; heapTypeIndex < 2; heapTypeIndex++)
            {
                var heaps = heapTypeIndex == 0 ? this.standardHeaps : this.hugeHeaps;
                lock (heaps)
                {
                    foreach (var heap in heaps)
                        heap.Release();
                    heaps.Clear();
                }
            }

            this.device.Reset();
        }

        /// <summary>
        /// Represents a heap.
        /// </summary>
        private class Heap : ManagedComObjectBase<Heap>, INativeGuid
        {
            public static readonly Guid MyGuid =
                new(0x2a5ba9e8, 0x22a5, 0x4b40, 0xb6, 0x88, 0xc2, 0x0f, 0xdf, 0x50, 0xbe, 0x8e);

            private const ulong OffsetShift = 0x100000000;

            private readonly string heapDebugName;
            private readonly ulong minAlignment;

            private readonly Node head;
            private readonly Node tail;

            private ComPtr<ID3D12Device> device;
            private ComPtr<ID3D12Heap> heap;

            public Heap(
                ID3D12Device* device,
                in D3D12_HEAP_DESC desc,
                ulong minAlignment,
                string debugName)
            {
                this.heapDebugName = debugName;
                this.minAlignment = minAlignment;
                this.LastNodeReleaseTickCount = Environment.TickCount64;
                try
                {
                    this.device = new(device);

                    fixed (Guid* piid = &IID.IID_ID3D12Heap)
                    fixed (D3D12_HEAP_DESC* pDesc = &desc)
                    fixed (ID3D12Heap** ppHeap = &this.heap.GetPinnableReference())
                    fixed (void* pName = $"{debugName} ({Util.FormatBytes((long)desc.SizeInBytes)})")
                    {
                        device->CreateHeap(pDesc, piid, (void**)ppHeap).ThrowOnError();
                        this.heap.Get()->SetName((ushort*)pName).ThrowOnError();
                    }

                    this.head = new(null, 0, OffsetShift);
                    this.tail = new(
                        null,
                        OffsetShift + desc.SizeInBytes,
                        OffsetShift + desc.SizeInBytes);
                    this.head.InsertNext(this.tail);
                }
                catch
                {
                    this.Release();
                    throw;
                }
            }

            public static Guid* NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in MyGuid));

            public object Lock { get; } = new();

            public long LastNodeReleaseTickCount { get; set; }

            public ComPtr<ID3D12Resource> CreatePlacedResource(
                in D3D12_RESOURCE_ALLOCATION_INFO resAlloc,
                in D3D12_RESOURCE_DESC desc,
                D3D12_RESOURCE_STATES initialState = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON,
                D3D12_CLEAR_VALUE* pOptimizedClearValue = null,
                [CallerMemberName] string debugName = "")
            {
                using var node = this.Allocate(resAlloc.Alignment, resAlloc.SizeInBytes);
                if (node is null)
                    return default;

                using var texture = default(ComPtr<ID3D12Resource>);
                fixed (Guid* piidResource = &IID.IID_ID3D12Resource)
                fixed (Guid* piidHeap = &IID.IID_ID3D12Heap)
                fixed (D3D12_RESOURCE_DESC* pDesc = &desc)
                fixed (void* pNameResource =
                           $"{this.heapDebugName}[{debugName}] ({Util.FormatBytes((long)resAlloc.SizeInBytes)})")
                {
                    this.device.Get()->CreatePlacedResource(
                        this.heap,
                        node.From - OffsetShift,
                        pDesc,
                        initialState,
                        pOptimizedClearValue,
                        piidResource,
                        (void**)texture.GetAddressOf()).ThrowOnError();

                    texture.Get()->SetName((ushort*)pNameResource).ThrowOnError();
                    texture.Get()->SetPrivateDataInterface(Node.NativeGuid, node.AsIUnknown()).ThrowOnError();
                    texture.Get()->SetPrivateDataInterface(piidHeap, (IUnknown*)this.heap.Get()).ThrowOnError();
                }

                return new(texture);
            }

            protected override void* DynamicCast(in Guid iid) =>
                iid == MyGuid ? this.AsComInterface() : base.DynamicCast(iid);

            protected override void FinalRelease()
            {
                // Note: nodes between head and tail are owned by ID3D12Resource, with SetPrivateDataInterface.
                this.head.Release();
                this.tail.Release();
                this.heap.Reset();
                this.device.Reset();
            }

            private Node? Allocate(ulong alignment, ulong length)
            {
                length = ((length + this.minAlignment) - 1) & ~(this.minAlignment - 1);
                lock (this.Lock)
                {
                    for (var n = this.head; n != this.tail; n = n.Next)
                    {
                        var from = n.To;
                        var to = n.Next.From;
                        var fromExpected = ((from + alignment) - 1) & ~(alignment - 1);
                        var toExpected = fromExpected + length;
                        if (toExpected > to)
                            continue;

                        var o = new Node(this, fromExpected, toExpected);
                        try
                        {
                            n.InsertNext(o);
                            return o;
                        }
                        catch
                        {
                            o.Release();
                            throw;
                        }
                    }
                }

                return default;
            }
        }

        /// <summary>
        /// Represents an allocated memory region in a <see cref="Heap"/>.
        /// </summary>
        private class Node : ManagedComObjectBase<Node>, INativeGuid
        {
            public static readonly Guid NodeGuid =
                new(0x2a5ba9e8, 0x22a5, 0x4b40, 0xb6, 0x88, 0xc2, 0x0f, 0xdf, 0x50, 0xbe, 0x8f);

            private readonly Heap? owner;

            public Node(Heap? owner, ulong from, ulong to)
            {
                this.owner = owner?.CloneRef();
                this.Prev = this;
                this.Next = this;
                this.From = from;
                this.To = to;
            }

            public static Guid* NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in NodeGuid));

            public ulong From { get; }

            public ulong To { get; }

            public Node Prev { get; set; }

            public Node Next { get; set; }

            public void InsertNext(Node n)
            {
                n.Prev = this;
                n.Next = this.Next;
                this.Next.Prev = n;
                this.Next = n;
                if (n.Prev == this.Prev)
                    this.Prev = n;
            }

            protected override void* DynamicCast(in Guid iid) =>
                iid == NodeGuid ? this.AsComInterface() : base.DynamicCast(iid);

            protected override void FinalRelease()
            {
                if (this.owner is null)
                    return;

                this.owner.LastNodeReleaseTickCount = Environment.TickCount64;

                lock (this.owner.Lock)
                {
                    var prev = this.Prev;
                    var next = this.Next;
                    this.Next = next;
                    this.Prev = prev;
                }

                this.owner.Dispose();
            }
        }
    }
}

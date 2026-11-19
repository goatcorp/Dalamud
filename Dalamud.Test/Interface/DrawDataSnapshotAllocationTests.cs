using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiBackend;

using Xunit;

namespace Dalamud.Test.Interface;

/// <summary>
/// Verifies that failed snapshot allocations preserve owned storage and allow retry.
/// </summary>
public class DrawDataSnapshotAllocationTests
{
    private static readonly GrowBufferCallback GrowBuffer = CreateGrowBufferAdapter();
    private static readonly Action<nint> FreeBuffer = CreateFreeBufferAdapter();
    private static readonly CopyFromCallback CopyFrom = CreateCopyFromAdapter();
    private static readonly Func<nuint, nuint, nint> AllocateZeroed = CreateAllocateZeroedAdapter();
    private static readonly Func<DrawDataSnapshot, nint> ReadHeader = CreatePointerReader("header");
    private static readonly Func<DrawDataSnapshot, nint> ReadDrawLists = CreatePointerReader("drawListArray");
    private static readonly Func<DrawDataSnapshot, nint> ReadListPointers = CreatePointerReader("cmdListPtrArray");

    private delegate void GrowBufferCallback(ref nint buffer, ref int capacity, int neededCount, int elementSize);

    private delegate void CopyFromCallback(DrawDataSnapshot snapshot, nint source, Func<nuint, nuint, nint> allocateHeaders);

    [Fact]
    public void InitialAllocationFailurePreservesEmptyStorageAndAllowsRetry()
    {
        var buffer = nint.Zero;
        var capacity = 0;
        try
        {
            // Force allocation failure with a request larger than the Windows x64 address space.
            Assert.Throws<OutOfMemoryException>(() => GrowBuffer(ref buffer, ref capacity, int.MaxValue, int.MaxValue));

            Assert.Equal(nint.Zero, buffer);
            Assert.Equal(0, capacity);

            GrowBuffer(ref buffer, ref capacity, 1, sizeof(int));

            Assert.NotEqual(nint.Zero, buffer);
            Assert.Equal(4, capacity);
            Assert.Equal(0, Marshal.ReadInt32(buffer));
            Marshal.WriteInt32(buffer, 42);
            Assert.Equal(42, Marshal.ReadInt32(buffer));
        }
        finally
        {
            FreeBuffer(buffer);
        }
    }

    [Fact]
    public void ExpansionFailurePreservesAllocationCapacityAndContentsAndAllowsRetry()
    {
        var buffer = nint.Zero;
        var capacity = 0;
        try
        {
            GrowBuffer(ref buffer, ref capacity, 4, sizeof(int));
            var originalBuffer = buffer;
            var originalCapacity = capacity;
            for (var i = 0; i < capacity; i++)
                Marshal.WriteInt32(buffer, i * sizeof(int), 42 + i);

            var failure = Record.Exception(() => GrowBuffer(ref buffer, ref capacity, int.MaxValue, int.MaxValue));
            var failedBuffer = buffer;
            var failedCapacity = capacity;

            // Avoid a double-free during cleanup if failed growth leaves a stale pointer and a changed capacity.
            if (buffer == originalBuffer && capacity != originalCapacity)
                buffer = nint.Zero;

            Assert.IsType<OutOfMemoryException>(failure);
            Assert.Equal(originalBuffer, failedBuffer);
            Assert.Equal(originalCapacity, failedCapacity);
            for (var i = 0; i < capacity; i++)
                Assert.Equal(42 + i, Marshal.ReadInt32(buffer, i * sizeof(int)));

            GrowBuffer(ref buffer, ref capacity, 3, sizeof(int));
            Assert.Equal(originalBuffer, buffer);
            Assert.Equal(originalCapacity, capacity);
            Assert.Equal(42, Marshal.ReadInt32(buffer));

            GrowBuffer(ref buffer, ref capacity, 5, sizeof(int));

            Assert.NotEqual(nint.Zero, buffer);
            Assert.Equal(8, capacity);
            for (var i = 0; i < capacity; i++)
                Assert.Equal(0, Marshal.ReadInt32(buffer, i * sizeof(int)));
            Marshal.WriteInt32(buffer, 84);
            Assert.Equal(84, Marshal.ReadInt32(buffer));
        }
        finally
        {
            FreeBuffer(buffer);
        }
    }

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(2, false, false)]
    [InlineData(1, false, true)]
    [InlineData(2, false, true)]
    [InlineData(1, true, false)]
    [InlineData(2, true, false)]
    [InlineData(1, true, true)]
    [InlineData(2, true, true)]
    public void HeaderAllocationFailurePreservesStorageAndAllowsCaptureRetry(int failedAllocation, bool captureFirst, bool captureEmptyBeforeRetry)
    {
        using var source = new DrawDataFixture();
        using var snapshot = new DrawDataSnapshot();
        var allocationAttempts = 0;
        var failAt = 0;
        nint AllocateHeaders(nuint count, nuint size)
        {
            if (++allocationAttempts == failAt)
                throw new OutOfMemoryException("Controlled snapshot header allocation failure.");
            return AllocateZeroed(count, size);
        }

        void Capture(int count)
        {
            source.Count = count;
            CopyFrom(snapshot, source.Header, AllocateHeaders);
        }

        if (captureFirst)
        {
            Capture(4);
            source.AssertCopied(snapshot, 4);
        }

        var originalDrawLists = ReadDrawLists(snapshot);
        var originalListPointers = ReadListPointers(snapshot);
        var originalVectors = source.ReadVectorPointers(snapshot, captureFirst ? 4 : 0);
        var originalCapacity = captureFirst ? 4 : 0;
        allocationAttempts = 0;
        failAt = failedAllocation;

        Assert.Throws<OutOfMemoryException>(() => Capture(8));

        Assert.Equal(failedAllocation, allocationAttempts);
        Assert.Equal(originalDrawLists, ReadDrawLists(snapshot));
        Assert.Equal(originalListPointers, ReadListPointers(snapshot));
        Assert.Equal(originalCapacity, ReadListCapacity(snapshot));
        if (captureFirst)
        {
            source.AssertCopied(snapshot, 4);
            Assert.Equal(originalVectors, source.ReadVectorPointers(snapshot, 4));
        }

        // An intervening empty frame must preserve storage for the next capture.
        failAt = 0;
        if (captureEmptyBeforeRetry)
        {
            Capture(0);
            source.AssertCopied(snapshot, 0);
        }

        Capture(1);
        source.AssertCopied(snapshot, 1);
        if (captureFirst)
        {
            Assert.Equal(failedAllocation, allocationAttempts);
            Assert.Equal(originalDrawLists, ReadDrawLists(snapshot));
            Assert.Equal(originalListPointers, ReadListPointers(snapshot));
            Assert.Equal(originalVectors[..3], source.ReadVectorPointers(snapshot, 1));
        }

        Capture(4);
        source.AssertCopied(snapshot, 4);
        if (captureFirst)
            Assert.Equal(originalVectors, source.ReadVectorPointers(snapshot, 4));

        Capture(8);
        source.AssertCopied(snapshot, 8);
        Assert.Equal(8, ReadListCapacity(snapshot));
        if (captureFirst)
            Assert.Equal(originalVectors, source.ReadVectorPointers(snapshot, 4));
    }

    private static int ReadListCapacity(DrawDataSnapshot snapshot) =>
        (int)typeof(DrawDataSnapshot).GetField("listCapacity", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(snapshot)!;

    private static CopyFromCallback CreateCopyFromAdapter()
    {
        // Bridge native pointers through nint to exercise production code without unsafe test blocks.
        var copy = typeof(DrawDataSnapshot).GetMethod("CopyFrom", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var method = new DynamicMethod(
            "CopySnapshotTestAdapter",
            typeof(void),
            [typeof(DrawDataSnapshot), typeof(nint), typeof(Func<nuint, nuint, nint>)],
            typeof(DrawDataSnapshotAllocationTests),
            true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Conv_U);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, copy);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<CopyFromCallback>();
    }

    private static Func<nuint, nuint, nint> CreateAllocateZeroedAdapter()
    {
        var method = new DynamicMethod("AllocateSnapshotHeadersTestAdapter", typeof(nint), [typeof(nuint), typeof(nuint)]);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, typeof(NativeMemory).GetMethod(nameof(NativeMemory.AllocZeroed), [typeof(nuint), typeof(nuint)])!);
        il.Emit(OpCodes.Conv_I);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Func<nuint, nuint, nint>>();
    }

    private static Func<DrawDataSnapshot, nint> CreatePointerReader(string name)
    {
        var method = new DynamicMethod("ReadSnapshotTest" + name, typeof(nint), [typeof(DrawDataSnapshot)], typeof(DrawDataSnapshotAllocationTests), true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, typeof(DrawDataSnapshot).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!);
        il.Emit(OpCodes.Conv_I);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Func<DrawDataSnapshot, nint>>();
    }

    private static GrowBufferCallback CreateGrowBufferAdapter()
    {
        var method = new DynamicMethod(
            "GrowSnapshotBufferTestAdapter",
            typeof(void),
            [typeof(nint).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int), typeof(int)],
            typeof(DrawDataSnapshotAllocationTests),
            true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldarg_3);
        il.Emit(OpCodes.Call, typeof(DrawDataSnapshot).GetMethod("GrowBuffer", BindingFlags.Static | BindingFlags.NonPublic)!);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<GrowBufferCallback>();
    }

    private static Action<nint> CreateFreeBufferAdapter()
    {
        var method = new DynamicMethod("FreeSnapshotBufferTestAdapter", typeof(void), [typeof(nint)]);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Conv_U);
        il.Emit(OpCodes.Call, typeof(NativeMemory).GetMethod(nameof(NativeMemory.Free))!);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Action<nint>>();
    }

    /// <summary>
    /// Owns native draw data with distinct vector contents for copy and reuse assertions.
    /// </summary>
    private sealed class DrawDataFixture : IDisposable
    {
        private static readonly int ListSize = Unsafe.SizeOf<ImDrawList>();
        private static readonly int CountOffset = (int)Marshal.OffsetOf<ImDrawData>(nameof(ImDrawData.CmdListsCount));
        // String field names avoid the unsafe context required by nameof on pointer fields.
        private static readonly int ListsOffset = (int)Marshal.OffsetOf<ImDrawData>("CmdLists");
        private static readonly int OwnerOffset = (int)Marshal.OffsetOf<ImDrawData>("OwnerViewport");
        private static readonly int VectorDataOffset = (int)Marshal.OffsetOf<ImVector>("Data");
        private static readonly int[] VectorOffsets =
        [
            (int)Marshal.OffsetOf<ImDrawList>(nameof(ImDrawList.CmdBuffer)),
            (int)Marshal.OffsetOf<ImDrawList>(nameof(ImDrawList.IdxBuffer)),
            (int)Marshal.OffsetOf<ImDrawList>(nameof(ImDrawList.VtxBuffer)),
        ];

        private static readonly int[] ElementSizes = [Unsafe.SizeOf<ImDrawCmd>(), sizeof(ushort), Unsafe.SizeOf<ImDrawVert>()];

        private readonly List<nint> allocations = [];
        private readonly nint lists;

        public DrawDataFixture()
        {
            this.Header = this.Allocate(Unsafe.SizeOf<ImDrawData>());
            this.lists = this.Allocate(8 * ListSize);
            var listPointers = this.Allocate(8 * nint.Size);
            Marshal.WriteIntPtr(this.Header, ListsOffset, listPointers);
            Marshal.WriteIntPtr(this.Header, OwnerOffset, 42);
            for (var listIndex = 0; listIndex < 8; listIndex++)
            {
                var list = this.lists + (listIndex * ListSize);
                Marshal.WriteIntPtr(listPointers, listIndex * nint.Size, list);
                for (var vectorIndex = 0; vectorIndex < VectorOffsets.Length; vectorIndex++)
                {
                    var vector = list + VectorOffsets[vectorIndex];
                    var data = this.Allocate(ElementSizes[vectorIndex]);
                    var bytes = this.ExpectedBytes(listIndex, vectorIndex);
                    Marshal.Copy(bytes, 0, data, bytes.Length);
                    Marshal.WriteInt32(vector, 1);
                    Marshal.WriteInt32(vector, sizeof(int), 1);
                    Marshal.WriteIntPtr(vector, VectorDataOffset, data);
                }
            }
        }

        public nint Header { get; }

        public int Count
        {
            set => Marshal.WriteInt32(this.Header, CountOffset, value);
        }

        public void AssertCopied(DrawDataSnapshot snapshot, int count)
        {
            var header = ReadHeader(snapshot);
            Assert.Equal(count, Marshal.ReadInt32(header, CountOffset));
            Assert.Equal(nint.Zero, Marshal.ReadIntPtr(header, OwnerOffset));
            var listPointers = Marshal.ReadIntPtr(header, ListsOffset);
            if (count == 0)
            {
                Assert.Equal(nint.Zero, listPointers);
                return;
            }

            Assert.Equal(ReadListPointers(snapshot), listPointers);
            for (var listIndex = 0; listIndex < count; listIndex++)
            {
                var list = Marshal.ReadIntPtr(listPointers, listIndex * nint.Size);
                Assert.Equal(ReadDrawLists(snapshot) + (listIndex * ListSize), list);
                for (var vectorIndex = 0; vectorIndex < VectorOffsets.Length; vectorIndex++)
                {
                    var vector = list + VectorOffsets[vectorIndex];
                    Assert.Equal(1, Marshal.ReadInt32(vector));
                    Assert.Equal(4, Marshal.ReadInt32(vector, sizeof(int)));
                    var data = Marshal.ReadIntPtr(vector, VectorDataOffset);
                    var sourceVector = this.lists + (listIndex * ListSize) + VectorOffsets[vectorIndex];
                    Assert.NotEqual(Marshal.ReadIntPtr(sourceVector, VectorDataOffset), data);
                    var bytes = new byte[ElementSizes[vectorIndex]];
                    Marshal.Copy(data, bytes, 0, bytes.Length);
                    Assert.Equal(this.ExpectedBytes(listIndex, vectorIndex), bytes);
                }
            }
        }

        public nint[] ReadVectorPointers(DrawDataSnapshot snapshot, int count)
        {
            var pointers = new nint[count * VectorOffsets.Length];
            var listPointers = ReadListPointers(snapshot);
            for (var listIndex = 0; listIndex < count; listIndex++)
            {
                var list = Marshal.ReadIntPtr(listPointers, listIndex * nint.Size);
                for (var vectorIndex = 0; vectorIndex < VectorOffsets.Length; vectorIndex++)
                    pointers[(listIndex * VectorOffsets.Length) + vectorIndex] = Marshal.ReadIntPtr(list + VectorOffsets[vectorIndex], VectorDataOffset);
            }

            return pointers;
        }

        public void Dispose()
        {
            foreach (var allocation in this.allocations)
                Marshal.FreeHGlobal(allocation);
        }

        private nint Allocate(int size)
        {
            var allocation = Marshal.AllocHGlobal(size);
            this.allocations.Add(allocation);
            Marshal.Copy(new byte[size], 0, allocation, size);
            return allocation;
        }

        private byte[] ExpectedBytes(int listIndex, int vectorIndex)
        {
            var bytes = new byte[ElementSizes[vectorIndex]];
            Array.Fill(bytes, (byte)(1 + (listIndex * VectorOffsets.Length) + vectorIndex));
            return bytes;
        }
    }
}

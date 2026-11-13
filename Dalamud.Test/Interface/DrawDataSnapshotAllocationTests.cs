using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using Dalamud.Interface.ImGuiBackend;

using Xunit;

namespace Dalamud.Test.Interface;

/// <summary>
/// Verifies that failed snapshot buffer allocations preserve ownership and allow retry.
/// </summary>
public class DrawDataSnapshotAllocationTests
{
    private static readonly GrowBufferCallback GrowBuffer = CreateGrowBufferAdapter();
    private static readonly Action<nint> FreeBuffer = CreateFreeBufferAdapter();

    private delegate void GrowBufferCallback(ref nint buffer, ref int capacity, int neededCount, int elementSize);

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

    private static GrowBufferCallback CreateGrowBufferAdapter()
    {
        // Adapt the production pointer-reference signature without enabling unsafe code in the test project.
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
}

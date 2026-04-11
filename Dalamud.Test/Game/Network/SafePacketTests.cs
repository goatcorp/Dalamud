using System;

using Dalamud.Game.Network;

using Xunit;

namespace Dalamud.Test.Game.Network;

public class SafePacketTests
{
    [Fact]
    public void ConstructFromByteArray_CopiesData()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using var packet = new SafePacket(data);

        Assert.Equal(4, packet.Size);
        Assert.Equal(data, packet.ToArray());
    }

    [Fact]
    public void ConstructFromByteArray_IsolatesFromSource()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using var packet = new SafePacket(data);

        data[0] = 0xFF;
        Assert.Equal(0x01, packet.ToArray()[0]);
    }

    [Fact]
    public void ConstructFromNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SafePacket(null!));
    }

    [Fact]
    public void ConstructFromEmptyArray_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SafePacket(Array.Empty<byte>()));
    }

    [Fact]
    public void OpCode_ReadsFirstTwoBytes()
    {
        var data = new byte[] { 0x34, 0x12, 0x00, 0x00 };
        using var packet = new SafePacket(data);

        Assert.Equal(0x1234, packet.OpCode);
    }

    [Fact]
    public void OpCode_SingleByte_Throws()
    {
        var data = new byte[] { 0x01 };
        using var packet = new SafePacket(data);

        Assert.Throws<InvalidOperationException>(() => packet.OpCode);
    }

    [Fact]
    public void Read_Int32AtOffset()
    {
        var data = new byte[] { 0x00, 0x00, 0x78, 0x56, 0x34, 0x12 };
        using var packet = new SafePacket(data);

        Assert.Equal(0x12345678, packet.Read<int>(2));
    }

    [Fact]
    public void Read_NegativeOffset_Throws()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using var packet = new SafePacket(data);

        Assert.Throws<ArgumentOutOfRangeException>(() => packet.Read<int>(-1));
    }

    [Fact]
    public void Read_ExceedsBounds_Throws()
    {
        var data = new byte[] { 0x01, 0x02 };
        using var packet = new SafePacket(data);

        Assert.Throws<ArgumentOutOfRangeException>(() => packet.Read<int>(0));
    }

    [Fact]
    public void Read_OffsetAtBoundary_Throws()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using var packet = new SafePacket(data);

        Assert.Throws<ArgumentOutOfRangeException>(() => packet.Read<int>(1));
    }

    [Fact]
    public void TryRead_Success()
    {
        var data = new byte[] { 0x34, 0x12, 0x00, 0x00 };
        using var packet = new SafePacket(data);

        Assert.True(packet.TryRead<ushort>(0, out var value));
        Assert.Equal(0x1234, value);
    }

    [Fact]
    public void TryRead_OutOfBounds_ReturnsFalse()
    {
        var data = new byte[] { 0x01, 0x02 };
        using var packet = new SafePacket(data);

        Assert.False(packet.TryRead<int>(0, out var value));
        Assert.Equal(default, value);
    }

    [Fact]
    public void TryRead_NegativeOffset_ReturnsFalse()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using var packet = new SafePacket(data);

        Assert.False(packet.TryRead<int>(-1, out _));
    }

    [Fact]
    public void AsSpan_ReturnsFullData()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };
        using var packet = new SafePacket(data);

        Assert.Equal(data, packet.AsSpan().ToArray());
    }

    [Fact]
    public void AsSpan_WithRange_ReturnsSubset()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        using var packet = new SafePacket(data);

        var span = packet.AsSpan(1, 3);
        Assert.Equal(new byte[] { 0x02, 0x03, 0x04 }, span.ToArray());
    }

    [Fact]
    public void AsSpan_InvalidRange_Throws()
    {
        var data = new byte[] { 0x01, 0x02 };
        using var packet = new SafePacket(data);

        Assert.Throws<ArgumentOutOfRangeException>(() => packet.AsSpan(1, 5));
    }

    [Fact]
    public void AsSpan_NegativeOffset_Throws()
    {
        var data = new byte[] { 0x01, 0x02 };
        using var packet = new SafePacket(data);

        Assert.Throws<ArgumentOutOfRangeException>(() => packet.AsSpan(-1, 1));
    }

    [Fact]
    public void ToArray_ReturnsIndependentCopy()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };
        using var packet = new SafePacket(data);

        var copy = packet.ToArray();
        copy[0] = 0xFF;
        Assert.Equal(0x01, packet.ToArray()[0]);
    }

    [Fact]
    public void Dispose_ClearsData()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var packet = new SafePacket(data);

        packet.Dispose();

        Assert.Throws<ObjectDisposedException>(() => packet.AsSpan());
    }

    [Fact]
    public void Dispose_DoubleDispose_NoThrow()
    {
        var data = new byte[] { 0x01, 0x02 };
        var packet = new SafePacket(data);

        packet.Dispose();
        packet.Dispose();
    }

    [Fact]
    public void Read_AfterDispose_Throws()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var packet = new SafePacket(data);
        packet.Dispose();

        Assert.Throws<ObjectDisposedException>(() => packet.Read<int>(0));
    }

    [Fact]
    public void OpCode_AfterDispose_Throws()
    {
        var data = new byte[] { 0x01, 0x02 };
        var packet = new SafePacket(data);
        packet.Dispose();

        Assert.Throws<ObjectDisposedException>(() => packet.OpCode);
    }

    [Fact]
    public void TryRead_AfterDispose_ReturnsFalse()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var packet = new SafePacket(data);
        packet.Dispose();

        Assert.False(packet.TryRead<int>(0, out _));
    }

    [Fact]
    public void ToArray_AfterDispose_Throws()
    {
        var data = new byte[] { 0x01, 0x02 };
        var packet = new SafePacket(data);
        packet.Dispose();

        Assert.Throws<ObjectDisposedException>(() => packet.ToArray());
    }
}

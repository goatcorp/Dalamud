using System;

using Dalamud.Game.Network;

using Xunit;

namespace Dalamud.Test.Game.Network;

public class NetworkPointerValidatorTests
{
    [Fact]
    public void NullPointer_ReturnsFalse()
    {
        Assert.False(NetworkPointerValidator.IsValidPacketPointer(nint.Zero, 32));
    }

    [Theory]
    [InlineData(0x1)]
    [InlineData(0xFF)]
    [InlineData(0xFFFF)]
    public void BelowMinAddress_ReturnsFalse(long address)
    {
        Assert.False(NetworkPointerValidator.IsValidPacketPointer((nint)address, 32));
    }

    [Theory]
    [InlineData(0x800000000000)]
    [InlineData(0xFFFFFFFFFFFF)]
    public void AboveMaxAddress_ReturnsFalse(long address)
    {
        Assert.False(NetworkPointerValidator.IsValidPacketPointer((nint)address, 32));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void NonPositiveSize_ReturnsFalse(int size)
    {
        Assert.False(NetworkPointerValidator.IsValidPacketPointer((nint)0x10000, size));
    }

    [Theory]
    [InlineData(0x10000, 1)]
    [InlineData(0x100000, 1024)]
    [InlineData(0x7FFFFFFFFFFF, 1)]
    public void ValidPointerAndSize_ReturnsTrue(long address, int size)
    {
        Assert.True(NetworkPointerValidator.IsValidPacketPointer((nint)address, size));
    }

    [Fact]
    public void SafeRead_InvalidPointer_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            NetworkPointerValidator.SafeRead<int>(nint.Zero, 0, 32));
    }

    [Fact]
    public void SafeRead_NegativeOffset_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkPointerValidator.SafeRead<int>((nint)0x10000, -1, 32));
    }

    [Fact]
    public void SafeRead_OffsetExceedsPacket_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkPointerValidator.SafeRead<int>((nint)0x10000, 30, 32));
    }

    [Fact]
    public void TrySafeRead_InvalidPointer_ReturnsFalse()
    {
        Assert.False(NetworkPointerValidator.TrySafeRead<int>(nint.Zero, 0, 32, out _));
    }

    [Fact]
    public void TrySafeRead_NegativeOffset_ReturnsFalse()
    {
        Assert.False(NetworkPointerValidator.TrySafeRead<int>((nint)0x10000, -1, 32, out _));
    }

    [Fact]
    public void TrySafeRead_OffsetExceedsPacket_ReturnsFalse()
    {
        Assert.False(NetworkPointerValidator.TrySafeRead<int>((nint)0x10000, 30, 32, out _));
    }
}

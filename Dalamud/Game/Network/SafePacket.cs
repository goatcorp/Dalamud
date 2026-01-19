using System.Runtime.InteropServices;

namespace Dalamud.Game.Network;

/// <summary>
/// A safe wrapper around network packet data with lifetime and bounds guarantees.
/// </summary>
/// <remarks>
/// <para>
/// This class copies packet data to managed memory, ensuring:
/// </para>
/// <list type="bullet">
/// <item><description>Lifetime safety - data persists as long as this object</description></item>
/// <item><description>Bounds checking - all reads are validated against packet size</description></item>
/// <item><description>Thread safety - the copied data cannot be modified externally</description></item>
/// </list>
/// <para>
/// This class is intended to replace raw pointer access in future API versions.
/// </para>
/// </remarks>
internal sealed class SafePacket : IDisposable
{
    private readonly byte[] data;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SafePacket"/> class by copying data from an unmanaged pointer.
    /// </summary>
    /// <param name="ptr">The source pointer to copy from.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <exception cref="ArgumentException">Thrown when the pointer is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when size is not positive.</exception>
    internal SafePacket(nint ptr, int size)
    {
        if (!NetworkPointerValidator.IsValidPacketPointer(ptr, size))
            throw new ArgumentException("Invalid packet pointer.", nameof(ptr));

        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be positive.");

        this.data = new byte[size];
        Marshal.Copy(ptr, this.data, 0, size);
        this.Size = size;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SafePacket"/> class from existing data.
    /// </summary>
    /// <param name="data">The source data to copy.</param>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="ArgumentException">Thrown when data is empty.</exception>
    internal SafePacket(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0)
            throw new ArgumentException("Data cannot be empty.", nameof(data));

        this.data = new byte[data.Length];
        data.CopyTo(this.data, 0);
        this.Size = data.Length;
    }

    /// <summary>
    /// Gets the total size of the packet data in bytes.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Gets the packet opcode (first two bytes interpreted as ushort).
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the packet has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when packet is too small to contain an opcode.</exception>
    public ushort OpCode
    {
        get
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            if (this.Size < sizeof(ushort))
                throw new InvalidOperationException("Packet too small to contain an opcode.");

            return BitConverter.ToUInt16(this.data, 0);
        }
    }

    /// <summary>
    /// Safely reads a value of type T at the specified byte offset.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to read.</typeparam>
    /// <param name="offset">The byte offset from the start of the packet.</param>
    /// <returns>The value read from the packet data.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the packet has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the read would exceed packet boundaries.</exception>
    public unsafe T Read<T>(int offset) where T : unmanaged
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        var size = sizeof(T);
        if (offset < 0 || offset > this.Size || size > this.Size - offset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                $"Cannot read {size} bytes at offset {offset} from packet of size {this.Size}.");
        }

        return MemoryMarshal.Read<T>(this.data.AsSpan(offset));
    }

    /// <summary>
    /// Attempts to safely read a value of type T at the specified byte offset.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to read.</typeparam>
    /// <param name="offset">The byte offset from the start of the packet.</param>
    /// <param name="value">When this method returns, contains the value read, or default if the read failed.</param>
    /// <returns>True if the read succeeded; false otherwise.</returns>
    public unsafe bool TryRead<T>(int offset, out T value) where T : unmanaged
    {
        value = default;

        if (this.disposed)
            return false;

        var size = sizeof(T);
        if (offset < 0 || offset > this.Size || size > this.Size - offset)
            return false;

        value = MemoryMarshal.Read<T>(this.data.AsSpan(offset));
        return true;
    }

    /// <summary>
    /// Gets a read-only span of the entire packet data.
    /// </summary>
    /// <returns>A read-only span covering all packet data.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the packet has been disposed.</exception>
    public ReadOnlySpan<byte> AsSpan()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        return this.data;
    }

    /// <summary>
    /// Gets a read-only span of a portion of the packet data.
    /// </summary>
    /// <param name="offset">The starting offset.</param>
    /// <param name="length">The number of bytes to include.</param>
    /// <returns>A read-only span covering the specified portion of packet data.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the packet has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the range exceeds packet boundaries.</exception>
    public ReadOnlySpan<byte> AsSpan(int offset, int length)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        if (offset < 0 || length < 0 || offset > this.Size || length > this.Size - offset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                $"Range [{offset}, {offset + length}) exceeds packet size {this.Size}.");
        }

        return this.data.AsSpan(offset, length);
    }

    /// <summary>
    /// Creates a copy of the packet data as a new byte array.
    /// </summary>
    /// <returns>A new byte array containing a copy of the packet data.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the packet has been disposed.</exception>
    public byte[] ToArray()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        var copy = new byte[this.Size];
        this.data.CopyTo(copy, 0);
        return copy;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!this.disposed)
        {
            // Clear the data array to prevent sensitive network data from persisting in memory
            Array.Clear(this.data);
            this.disposed = true;
        }
    }
}

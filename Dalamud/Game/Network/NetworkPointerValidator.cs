using System.Runtime.InteropServices;

namespace Dalamud.Game.Network;

/// <summary>
/// Provides validation utilities for network packet pointers.
/// </summary>
internal static class NetworkPointerValidator
{
    /// <summary>
    /// Minimum address threshold below which pointers are considered invalid.
    /// Addresses below this are typically reserved by the OS.
    /// </summary>
    private const long MinValidAddress = 0x10000;

    /// <summary>
    /// Maximum valid user-mode address for 64-bit Windows.
    /// Addresses above this are kernel-mode and inaccessible from user-mode.
    /// </summary>
    private const long MaxValidAddress = 0x7FFFFFFFFFFF;

    /// <summary>
    /// Validates a network packet pointer before use.
    /// </summary>
    /// <param name="ptr">The pointer to validate.</param>
    /// <param name="minSize">The minimum expected size of the data.</param>
    /// <returns>True if the pointer appears valid; false otherwise.</returns>
    public static bool IsValidPacketPointer(nint ptr, int minSize)
    {
        if (ptr == nint.Zero)
            return false;

        // Ensure pointer is within reasonable memory range
        if (ptr < MinValidAddress)
            return false;

        // Ensure pointer is within user-mode address space
        if (ptr > MaxValidAddress)
            return false;

        // Minimum size must be positive
        if (minSize <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// Safely reads a value from a packet pointer with bounds checking.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to read.</typeparam>
    /// <param name="ptr">The base pointer to read from.</param>
    /// <param name="offset">The byte offset from the base pointer.</param>
    /// <param name="packetSize">The total size of the packet for bounds checking.</param>
    /// <returns>The value read from memory.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the read would exceed packet boundaries.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the pointer is invalid.
    /// </exception>
    public static unsafe T SafeRead<T>(nint ptr, int offset, int packetSize) where T : unmanaged
    {
        if (!IsValidPacketPointer(ptr, packetSize))
            throw new ArgumentException("Invalid packet pointer.", nameof(ptr));

        var size = sizeof(T);
        if (offset < 0 || offset > packetSize || size > packetSize - offset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                $"Cannot read {size} bytes at offset {offset} from packet of size {packetSize}.");
        }

        return *(T*)(ptr + offset);
    }

    /// <summary>
    /// Attempts to safely read a value from a packet pointer with bounds checking.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to read.</typeparam>
    /// <param name="ptr">The base pointer to read from.</param>
    /// <param name="offset">The byte offset from the base pointer.</param>
    /// <param name="packetSize">The total size of the packet for bounds checking.</param>
    /// <param name="value">The value read from memory, or default if the read failed.</param>
    /// <returns>True if the read succeeded; false otherwise.</returns>
    public static unsafe bool TrySafeRead<T>(nint ptr, int offset, int packetSize, out T value) where T : unmanaged
    {
        value = default;

        if (!IsValidPacketPointer(ptr, packetSize))
            return false;

        var size = sizeof(T);
        if (offset < 0 || offset > packetSize || size > packetSize - offset)
            return false;

        value = *(T*)(ptr + offset);
        return true;
    }
}

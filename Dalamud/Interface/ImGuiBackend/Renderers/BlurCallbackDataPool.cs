using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Utility;

namespace Dalamud.Interface.ImGuiBackend.Renderers;

/// <summary>
/// A capped static pool for <see cref="BlurCallbackData"/> structs.
/// </summary>
internal static unsafe class BlurCallbackDataPool
{
    private const int PoolSize = 64;

    private static readonly BlurCallbackData* Pool =
        (BlurCallbackData*)NativeMemory.AllocZeroed((nuint)(sizeof(BlurCallbackData) * PoolSize));

    private static long freeMask = -1L;

    /// <summary>
    /// Rents a <see cref="BlurCallbackData"/> slot from the pool.
    /// Falls back to <see cref="NativeMemory.AllocZeroed(nuint)"/> when all slots are in flight.
    /// Caller must populate every field and return the pointer via <see cref="Return"/> when done.
    /// </summary>
    /// <returns>
    /// Pointer to the rented <see cref="BlurCallbackData"/>.
    /// </returns>
    public static BlurCallbackData* Rent()
    {
        ThreadSafety.AssertMainThread();

        if (freeMask == 0)
        {
            return (BlurCallbackData*)NativeMemory.AllocZeroed((nuint)sizeof(BlurCallbackData));
        }

        var slot = BitOperations.TrailingZeroCount((ulong)freeMask);
        freeMask &= ~(1L << slot);
        return Pool + slot;
    }

    /// <summary>
    /// Returns a <see cref="BlurCallbackData"/> pointer back to the pool.
    /// If the pointer originated from the heap fallback it is freed instead.
    /// </summary>
    /// <param name="ptr">
    /// Pointer previously obtained from <see cref="Rent"/>.
    /// </param>
    public static void Return(BlurCallbackData* ptr)
    {
        ThreadSafety.AssertMainThread();

        var offset = (nuint)((byte*)ptr - (byte*)Pool);
        if (offset < (nuint)(sizeof(BlurCallbackData) * PoolSize))
        {
            var slot = (int)(offset / (nuint)sizeof(BlurCallbackData));
            freeMask |= 1L << slot;
        }
        else
        {
            NativeMemory.Free(ptr);
        }
    }
}

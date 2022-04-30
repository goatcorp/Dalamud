using System;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Gui.ContextMenus;

/// <summary>
/// UI Allocation.
/// </summary>
internal class UiAlloc
{
    private readonly GameAllocDelegate? gameAlloc;
    private readonly GameFreeDelegate? gameFree;
    private readonly GetGameAllocatorDelegate? getGameAllocator;

    /// <summary>
    /// Initializes a new instance of the <see cref="UiAlloc"/> class.
    /// </summary>
    /// <param name="scanner">scanner.</param>
    internal UiAlloc(SigScanner scanner)
    {
        if (scanner.TryScanText(Signatures.GameAlloc, out var gameAllocPtr))
        {
            this.gameAlloc = Marshal.GetDelegateForFunctionPointer<GameAllocDelegate>(gameAllocPtr);
        }
        else
        {
            return;
        }

        if (scanner.TryScanText(Signatures.GameFree, out var gameFreePtr))
        {
            this.gameFree = Marshal.GetDelegateForFunctionPointer<GameFreeDelegate>(gameFreePtr);
        }
        else
        {
            return;
        }

        if (scanner.TryScanText(Signatures.GetGameAllocator, out var getAllocatorPtr))
        {
            this.getGameAllocator = Marshal.GetDelegateForFunctionPointer<GetGameAllocatorDelegate>(getAllocatorPtr);
        }
    }

    private delegate IntPtr GameAllocDelegate(ulong size, IntPtr unk, IntPtr allocator, IntPtr alignment);

    private delegate IntPtr GameFreeDelegate(IntPtr a1);

    private delegate IntPtr GetGameAllocatorDelegate();

    /// <summary>
    /// Allocate by size.
    /// </summary>
    /// <param name="size">size.</param>
    /// <returns>pointer.</returns>
    internal IntPtr Alloc(ulong size)
    {
        if (this.getGameAllocator == null || this.gameAlloc == null)
        {
            throw new InvalidOperationException();
        }

        return this.gameAlloc(size, IntPtr.Zero, this.getGameAllocator(), IntPtr.Zero);
    }

    /// <summary>
    /// Allocate by size.
    /// </summary>
    /// <param name="ptr">pointer.</param>
    internal void Free(IntPtr ptr)
    {
        if (this.gameFree == null)
        {
            throw new InvalidOperationException();
        }

        this.gameFree(ptr);
    }

    private static class Signatures
    {
        internal const string GameAlloc = "E8 ?? ?? ?? ?? 49 83 CF FF 4C 8B F0";
        internal const string GameFree = "E8 ?? ?? ?? ?? 4C 89 7B 60";
        internal const string GetGameAllocator = "E8 ?? ?? ?? ?? 8B 75 08";
    }
}

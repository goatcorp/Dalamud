using System.Runtime.InteropServices;

namespace Dalamud.Game.Libc;

/// <summary>
/// An address wrapper around the <see cref="StdString"/> class.
/// </summary>
public sealed partial class OwnedStdString
{
    private readonly DeallocatorDelegate dealloc;

    /// <summary>
    /// Initializes a new instance of the <see cref="OwnedStdString"/> class.
    /// Construct a wrapper around std::string.
    /// </summary>
    /// <remarks>
    /// Violating any of these might cause an undefined hehaviour.
    /// 1. This function takes the ownership of the address.
    /// 2. A memory pointed by address argument is assumed to be allocated by Marshal.AllocHGlobal thus will try to call Marshal.FreeHGlobal on the address.
    /// 3. std::string object pointed by address must be initialized before calling this function.
    /// </remarks>
    /// <param name="address">The address of the owned std string.</param>
    /// <param name="dealloc">A deallocator function.</param>
    internal OwnedStdString(IntPtr address, DeallocatorDelegate dealloc)
    {
        this.Address = address;
        this.dealloc = dealloc;
    }

    /// <summary>
    /// The delegate type that deallocates a std string.
    /// </summary>
    /// <param name="address">Address to deallocate.</param>
    internal delegate void DeallocatorDelegate(IntPtr address);

    /// <summary>
    /// Gets the address of the std string.
    /// </summary>
    public IntPtr Address { get; private set; }

    /// <summary>
    /// Read the wrapped StdString.
    /// </summary>
    /// <returns>The StdString.</returns>
    public StdString Read() => StdString.ReadFromPointer(this.Address);
}

/// <summary>
/// Implements IDisposable.
/// </summary>
public sealed partial class OwnedStdString : IDisposable
{
    private bool isDisposed;

    /// <summary>
    /// Finalizes an instance of the <see cref="OwnedStdString"/> class.
    /// </summary>
    ~OwnedStdString() => this.Dispose(false);

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.Dispose(true);
    }

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">A value indicating whether this was called via Dispose or finalized.</param>
    public void Dispose(bool disposing)
    {
        if (this.isDisposed)
            return;

        this.isDisposed = true;

        if (disposing)
        {
        }

        if (this.Address == IntPtr.Zero)
        {
            // Something got seriously fucked.
            throw new AccessViolationException();
        }

        // Deallocate inner string first
        this.dealloc(this.Address);

        // Free the heap
        Marshal.FreeHGlobal(this.Address);

        // Better safe (running on a nullptr) than sorry. (running on a dangling pointer)
        this.Address = IntPtr.Zero;
    }
}

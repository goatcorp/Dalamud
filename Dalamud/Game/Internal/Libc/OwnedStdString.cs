using System;
using System.Runtime.InteropServices;

using Serilog;

namespace Dalamud.Game.Internal.Libc
{
    public sealed class OwnedStdString : IDisposable
    {
        internal delegate void DeallocatorDelegate(IntPtr address);

        // ala. the drop flag
        private bool isDisposed;

        private readonly DeallocatorDelegate dealloc;

        public IntPtr Address { get; private set; }

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
        /// <param name="address"></param>
        /// <param name="dealloc">A deallocator function.</param>
        /// <returns></returns>
        internal OwnedStdString(IntPtr address, DeallocatorDelegate dealloc)
        {
            this.Address = address;
            this.dealloc = dealloc;
        }

        ~OwnedStdString()
        {
            this.ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
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

        public void Dispose()
        {
            // No double free plz, kthx.
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            this.ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public StdString Read()
        {
            return StdString.ReadFromPointer(this.Address);
        }
    }
}

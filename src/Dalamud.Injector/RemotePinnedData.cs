using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using static Dalamud.Injector.NativeFunctions;

namespace Dalamud.Injector
{
    /// <summary>
    /// Pin an arbitrary string to a remote process.
    /// </summary>
    internal class RemotePinnedData : IDisposable
    {
        private readonly Process process;
        private readonly byte[] data;
        private readonly IntPtr allocAddr;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemotePinnedData"/> class.
        /// </summary>
        /// <param name="process">Process to write in.</param>
        /// <param name="data">Data to write.</param>
        public unsafe RemotePinnedData(Process process, byte[] data)
        {
            this.process = process;
            this.data = data;

            this.allocAddr = VirtualAllocEx(
                this.process.Handle,
                IntPtr.Zero,
                this.data.Length,
                AllocationType.Commit,
                MemoryProtection.ReadWrite);

            if (this.allocAddr == IntPtr.Zero || Marshal.GetLastWin32Error() != 0)
            {
                throw new Exception("Error allocating memory");
            }

            var result = WriteProcessMemory(
                this.process.Handle,
                this.allocAddr,
                this.data,
                this.data.Length,
                out _);

            if (!result || Marshal.GetLastWin32Error() != 0)
            {
                throw new Exception("Error writing memory");
            }
        }

        /// <summary>
        /// Gets the address of the pinned data.
        /// </summary>
        public IntPtr Address => this.allocAddr;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.allocAddr == IntPtr.Zero)
            {
                return;
            }

            var result = VirtualFreeEx(
                this.process.Handle,
                this.allocAddr,
                0,
                AllocationType.Release);

            if (!result || Marshal.GetLastWin32Error() != 0)
            {
                throw new Exception("Error freeing memory");
            }
        }
    }
}

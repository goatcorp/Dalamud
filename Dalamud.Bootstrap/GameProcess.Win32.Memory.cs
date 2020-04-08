using Dalamud.Bootstrap.OS.Windows.Raw;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Dalamud.Bootstrap
{
    public sealed partial class GameProcess : IDisposable
    {
        /// <summary>
        /// Reads the process memory.
        /// </summary>
        /// <returns>
        /// The number of bytes that is actually read.
        /// </returns>
        private int ReadMemory(IntPtr address, Span<byte> destination)
        {
            unsafe
            {
                fixed (byte* pDest = destination)
                {
                    if (!Kernel32.ReadProcessMemory(m_handle, address, pDest, (IntPtr)destination.Length, out var bytesRead))
                    {
                        ProcessException.ThrowLastOsError();
                    }

                    // This is okay because destination will never be longer than int.Max
                    return bytesRead.ToInt32();
                }
            }
        }

        public void ReadMemoryExact(IntPtr address, Span<byte> destination)
        {
            var totalBytesRead = 0;

            while (totalBytesRead < destination.Length)
            {
                var bytesRead = ReadMemory(address + totalBytesRead, destination[totalBytesRead..]);

                if (bytesRead == 0)
                {
                    // prolly page fault; there's not much we can do here
                    var readBeginAddr = address.ToInt64() + totalBytesRead;
                    var readEndAddr = address.ToInt64() + destination.Length;

                    ProcessException.ThrowLastOsError();
                }

                totalBytesRead += bytesRead;
            }
        }

        private byte[] ReadMemoryExact(IntPtr address, int length)
        {
            var buffer = new byte[length];
            ReadMemoryExact(address, buffer);

            return buffer;
        }

        private void ReadMemoryExact<T>(IntPtr address, ref T value) where T : unmanaged
        {
            var span = MemoryMarshal.CreateSpan(ref value, 1); // span should never leave this function since it has unbounded lifetime.
            var buffer = MemoryMarshal.AsBytes(span);

            ReadMemoryExact(address, buffer);
        }

        private IntPtr GetPebAddress()
        {
            unsafe
            {
                PROCESS_BASIC_INFORMATION info = default;

                var status = Ntdll.NtQueryInformationProcess(m_handle, PROCESSINFOCLASS.ProcessBasicInformation, &info, sizeof(PROCESS_BASIC_INFORMATION), (IntPtr*)IntPtr.Zero);

                if (!status.Success)
                {
                    throw new ProcessException($"Could not query information on process. (Status: {status})");
                }

                return info.PebBaseAddress;
            }
        }
    }
}

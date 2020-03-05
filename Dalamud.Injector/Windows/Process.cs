using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Dalamud.Injector.Windows
{
    internal sealed partial class Process : IDisposable
    {
        private SafeProcessHandle m_handle;
    }

    internal sealed partial class Process
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle">A process handle. Note that this functinon will take the ownership of the handle.</param>
        public Process(SafeProcessHandle handle)
        {
            m_handle = handle;
        }

        public void Dispose()
        {
            m_handle?.Dispose();
            m_handle = null!;
        }

        public static Process Open(uint pid)
        {
            const PROCESS_ACCESS_RIGHT access = PROCESS_ACCESS_RIGHT.PROCESS_VM_OPERATION |
                PROCESS_ACCESS_RIGHT.PROCESS_VM_READ |
                PROCESS_ACCESS_RIGHT.PROCESS_VM_WRITE |
                PROCESS_ACCESS_RIGHT.PROCESS_QUERY_LIMITED_INFORMATION |
                PROCESS_ACCESS_RIGHT.PROCESS_QUERY_INFORMATION |
                PROCESS_ACCESS_RIGHT.PROCESS_CREATE_THREAD |
                PROCESS_ACCESS_RIGHT.PROCESS_TERMINATE;
            
            var handle = Win32.OpenProcess((uint) access, false, pid);
            if (handle.IsInvalid)
            {
                throw new Win32Exception();
            }

            return new Process(handle);
        }

        /// <summary>
        /// Reads the process memory.
        /// </summary>
        /// <returns>
        /// The number of bytes that is actually read and written into the buffer.
        /// </returns>
        public int ReadMemory(IntPtr address, Span<byte> destination)
        {
            unsafe
            {
                fixed (byte* pDest = destination)
                {
                    IntPtr bytesRead;

                    if (!Win32.ReadProcessMemory(m_handle, (void*)address, pDest, (IntPtr)destination.Length, &bytesRead))
                    {
                        throw new Win32Exception();
                    }

                    // this is okay as Span<byte> can't really be longer than int.Max
                    return (int)bytesRead;
                }
            }
        }

        public void ReadMemoryExact(IntPtr address, Span<byte> destination)
        {
            var totalBytesRead = 0;
            while (totalBytesRead < destination.Length)
            {
                var bytesRead = ReadMemory(address + totalBytesRead, destination[totalBytesRead..]);

                // err -> partial read -> page fault?
                //if (bytesRead == 0)
                //{
                //    throw new NotImplementedException("TODO: unexpected EOF");
                //}

                totalBytesRead += bytesRead;
            }
        }

        public IntPtr WriteMemory(IntPtr address, ReadOnlySpan<byte> source)
        {
            unsafe
            {
                fixed (byte* pSrc = source)
                {
                    IntPtr bytesWritten;

                    if (!Win32.WriteProcessMemory(m_handle, (void*)address, pSrc, (IntPtr)source.Length, &bytesWritten))
                    {
                        throw new Win32Exception();
                    }

                    return bytesWritten;
                }
            }
        }

        public void Terminate(uint exitCode = 0)
        {
            if (!Win32.TerminateProcess(m_handle, exitCode))
            {
                throw new Win32Exception();
            }
        }

        private IntPtr ReadPebAddress()
        {
            unsafe
            {
                var info = new PROCESS_BASIC_INFORMATION();
                var status = Win32.NtQueryInformationProcess(m_handle, PROCESSINFOCLASS.ProcessBasicInformation, &info, sizeof(PROCESS_BASIC_INFORMATION), (IntPtr*)IntPtr.Zero);

                if (!status.Success)
                {
                    throw new NtStatusException(status);
                }

                return info.PebBaseAddress;
            }
        }

        public string[] ReadCommandLine()
        {
            unsafe
            {
                var pPeb = ReadPebAddress();

                // Read peb (partially)
                Span<byte> pebBuf = stackalloc byte[sizeof(PEB)];
                ReadMemoryExact(pPeb, pebBuf);
                ref readonly var peb = ref MemoryMarshal.AsRef<PEB>(pebBuf);

                // Read process parameters
                Span<byte> procParamBuf = stackalloc byte[sizeof(RTL_USER_PROCESS_PARAMETERS)];
                ReadMemoryExact(peb.ProcessParameters, procParamBuf);
                ref readonly var procParam = ref MemoryMarshal.AsRef<RTL_USER_PROCESS_PARAMETERS>(procParamBuf);

                // Read commandline
                var commandLineBuf = new byte[procParam.CommandLine.Length]; // arbitrary length; allocate to gc heap
                ReadMemoryExact(procParam.CommandLine.Buffer, commandLineBuf);



            }

            throw new NotImplementedException();
        }

    }
}

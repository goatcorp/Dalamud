using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Dalamud.Injector.Windows
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    internal sealed partial class Process : IDisposable
    {
        private SafeProcessHandle m_handle;
    }

    internal sealed partial class Process {
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
            if (!handle.IsInvalid)
            {
                throw new Win32Exception();
            }

            return new Process(handle);
        }

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

                    return (uint)bytesRead;
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
                    throw new NotImplementedException("TODO: unexpected EOF");
                }

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
                IntPtr _retLen;

                var status = Win32.NtQueryInformationProcess(m_handle, PROCESSINFOCLASS.ProcessBasicInformation, &info, sizeof(PROCESS_BASIC_INFORMATION), &_retLen);
                if (!status.Success)
                {
                    // TODO
                    throw new InvalidOperationException("TODO");
                }

                return info.PebBaseAddress;
            }
        }

        private ReadOnlySpan<char> ReadMemoryAsUtf16(IntPtr address, int length)
        {
            var buffer = new byte[length];
            ReadMemoryExact(address, buffer);

            // TODO..

            return MemoryMarshal.Cast<byte, char>(buffer);
        }

        public string ReadCommandLine()
        {
            unsafe
            {
                var pPeb = ReadPebAddress();
                var pPebLdr = pPeb + (int)Marshal.OffsetOf<PEB>("ProcessParameters");

                Span<byte> procParamBuf = stackalloc byte[sizeof(RTL_USER_PROCESS_PARAMETERS)];
                ReadMemoryExact(pPebLdr, procParamBuf);
                ref var procParam = ref MemoryMarshal.AsRef<RTL_USER_PROCESS_PARAMETERS>(procParamBuf);

                var commandLine = ReadMemoryAsUtf16(procParam.CommandLine.Buffer, procParam.CommandLine.Length);
            }

            throw new NotImplementedException();
        }

    }
}

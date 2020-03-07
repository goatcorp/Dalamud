using Dalamud.Injector.Windows;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Dalamud.Injector
{
    /// <summary>
    /// TODO
    /// </summary>
    internal sealed partial class Process : IDisposable
    {
        private SafeProcessHandle m_handle;
    }

    internal sealed partial class Process
    {
        /// <summary>
        /// Creates a process object that can be used to manipulate process's internal state.
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
            const PROCESS_ACCESS_RIGHT access = PROCESS_ACCESS_RIGHT.PROCESS_VM_OPERATION
                | PROCESS_ACCESS_RIGHT.PROCESS_VM_READ
                | PROCESS_ACCESS_RIGHT.PROCESS_QUERY_LIMITED_INFORMATION
                | PROCESS_ACCESS_RIGHT.PROCESS_QUERY_INFORMATION
                | PROCESS_ACCESS_RIGHT.PROCESS_CREATE_THREAD
                | PROCESS_ACCESS_RIGHT.PROCESS_TERMINATE;
            
            var handle = Win32.OpenProcess((uint) access, false, pid);

            if (handle.IsInvalid)
            {
                throw new Win32Exception();
            }

            return new Process(handle);
        }

        public void Terminate(uint exitCode = 0)
        {
            if (!Win32.TerminateProcess(m_handle, exitCode))
            {
                throw new Win32Exception();
            }
        }

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

        private void ReadMemoryExact(IntPtr address, Span<byte> destination)
        {
            var totalBytesRead = 0;
            while (totalBytesRead < destination.Length)
            {
                var bytesRead = ReadMemory(address + totalBytesRead, destination[totalBytesRead..]);

                if (bytesRead == 0)
                {
                    // prolly page fault; there's not much we can do here
                    throw new Win32Exception();
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

        private ref T ReadMemoryValue<T>(IntPtr address, Span<byte> buffer) where T : unmanaged
        {
            ReadMemoryExact(address, buffer);
            
            return ref MemoryMarshal.AsRef<T>(buffer);
        }

        private T ReadMemoryValue<T>(IntPtr address) where T : unmanaged
        {
            unsafe
            {
                // This assumes that size of T is small enough to be safely allocated on the stack.
                Span<byte> buffer = stackalloc byte[sizeof(T)];
                ReadMemoryExact(address, buffer);

                // I reckon this is still far better than allocating on the heap when T is small enough.
                return MemoryMarshal.Read<T>(buffer);
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
                    throw new NtException(status);
                }

                return info.PebBaseAddress;
            }
        }

        public string[] ReadCommandLine()
        {
            unsafe
            {
                // Find where the command line is allocated
                var pebAddr = ReadPebAddress();
                var peb = ReadMemoryValue<PEB>(pebAddr);
                var procParam = ReadMemoryValue<RTL_USER_PROCESS_PARAMETERS>(peb.ProcessParameters);

                // Read the command line (utf16-like string)
                var commandLine = ReadMemoryExact(procParam.CommandLine.Buffer, procParam.CommandLine.Length);
                return ParseCommandLine(commandLine);
            }
        }

        private static string[] ParseCommandLine(ReadOnlySpan<byte> commandLine)
        {
            unsafe
            {
                char** argv;
                int argc;

                fixed (byte* pCommandLine = commandLine)
                {
                    argv = Win32.CommandLineToArgvW(pCommandLine, &argc);
                }

                if (argv == null)
                {
                    throw new Win32Exception();
                }

                try
                {
                    var arguments = new string[argc];

                    for (var i = 0; i < argc; i++)
                    {
                        arguments[i] = new string(argv[i]);
                    }

                    return arguments;
                }
                finally
                {
                    Win32.LocalFree(argv);
                }
            }
        }
    }
}

using Dalamud.Bootstrap.OS.Windows.Raw;
using Dalamud.Bootstrap.Windows;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Bootstrap.OS
{
    /// <summary>
    /// A class that provides a wrapper over operations on Win32 process.
    /// </summary>
    internal sealed class Process : IDisposable
    {
        private SafeProcessHandle m_handle;

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

        public static Process Create(ProcessCreationOptions options)
        {
            
        }

        public static Process Open(uint pid, PROCESS_ACCESS_RIGHTS access)
        {
            var handle = OpenHandle(pid, access);

            return new Process(handle);
        }

        private static SafeProcessHandle OpenHandle(uint pid, PROCESS_ACCESS_RIGHTS access)
        {
            var handle = Kernel32.OpenProcess((uint)access, false, pid);
            
            if (handle.IsInvalid)
            {
                ProcessException.ThrowLastOsError($"Could not open process {pid}");
            }

            return handle;
        }

        private static uint GetProcessId(SafeProcessHandle handle) => Kernel32.GetProcessId(handle);

        public uint GetProcessId() => GetProcessId(m_handle);

        public void Terminate(int exitCode = 0)
        {
            if (!Kernel32.TerminateProcess(m_handle, exitCode))
            {
                ProcessException.ThrowLastOsError($"Could not terminate process {GetProcessId()}");
            }
        }

        /// <summary>
        /// Reads the process memory.
        /// </summary>
        /// <returns>
        /// The number of bytes that is actually read.
        /// </returns>
        public int ReadMemory(IntPtr address, Span<byte> destination)
        {
            unsafe
            {
                fixed (byte* pDest = destination)
                {
                    if (!Kernel32.ReadProcessMemory(m_handle, address, pDest, (IntPtr)destination.Length, out var bytesRead))
                    {
                        ProcessException.ThrowLastOsError($"Could not read process {GetProcessId()} memory at 0x{address.ToInt64():X8}");
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

                    ProcessException.ThrowLastOsError($"Could not read process {GetProcessId()} memory at 0x{readBeginAddr:X8} .. 0x{readEndAddr:X8}; This likely means that page fault was hit.");
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

        private T ReadMemoryValue<T>(IntPtr address) where T : unmanaged
        {
            unsafe
            {
                // This assumes that size of T is small enough to be safely allocated on the stack.
                Span<byte> buffer = stackalloc byte[sizeof(T)];
                ReadMemoryExact(address, buffer);

                // this is still far better than allocating the temporary buffer on the heap when sizeof(T) is small enough.
                return MemoryMarshal.Read<T>(buffer);
            }
        }

        private IntPtr ReadPebAddress()
        {
            unsafe
            {
                var info = new PROCESS_BASIC_INFORMATION();
                var status = Ntdll.NtQueryInformationProcess(m_handle, PROCESSINFOCLASS.ProcessBasicInformation, &info, sizeof(PROCESS_BASIC_INFORMATION), (IntPtr*)IntPtr.Zero);

                if (!status.Success)
                {
                    throw new ProcessException($"Could not query information on process {GetProcessId()} (Status: {status})");
                }

                return info.PebBaseAddress;
            }
        }

        /// <summary>
        /// Reads command-line arguments from the process.
        /// </summary>
        public string[] GetProcessArguments()
        {
            unsafe
            {
                // Find where the command line is allocated
                var pebAddr = ReadPebAddress();
                var peb = ReadMemoryValue<PEB>(pebAddr);
                var procParam = ReadMemoryValue<RTL_USER_PROCESS_PARAMETERS>(peb.ProcessParameters);

                // Read the command line (which is utf16-like string)
                var commandLine = ReadMemoryExact(procParam.CommandLine.Buffer, procParam.CommandLine.Length);
                
                return ParseCommandLineToArguments(commandLine);
            }
        }

        /// <summary>
        /// Returns a time when the process was started.
        /// </summary>
        public DateTime GetCreationTime()
        {
            if (!Kernel32.GetProcessTimes(m_handle, out var creationTime, out var _, out var _, out var _))
            {
                ProcessException.ThrowLastOsError($"Could not read process creation time from process {GetProcessId()}");
            }

            return creationTime.ToDateTime();
        }

        private string[] ParseCommandLineToArguments(ReadOnlySpan<byte> commandLine)
        {
            unsafe
            {
                char** argv;
                int argc;

                fixed (byte* pCommandLine = commandLine)
                {
                    argv = Shell32.CommandLineToArgvW(pCommandLine, out argc);
                }

                if (argv == null)
                {
                    ProcessException.ThrowLastOsError($"Could not parse a command-line.");
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
                    Kernel32.LocalFree(argv);
                }
            }
        }

        public string GetImageFilePath()
        {
            var buffer = new StringBuilder(300);

            // From docs:
            // On input, specifies the size of the lpExeName buffer, in characters.
            // On success, receives the number of characters written to the buffer, not including the null-terminating character.
            var size = buffer.Capacity;

            if (!Kernel32.QueryFullProcessImageNameW(m_handle, 0, buffer, ref size))
            {
                ProcessException.ThrowLastOsError($"Could not read image path from process {GetProcessId()}");
            }

            return buffer.ToString();
        }
    }
}

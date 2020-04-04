using Dalamud.Bootstrap.Windows;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Bootstrap
{
    /// <summary>
    /// A class that provides a wrapper over operations on Win32 process.
    /// </summary>
    internal class Process : IDisposable
    {
        protected SafeProcessHandle Handle { get; set; }

        /// <summary>
        /// Creates a process object that can be used to manipulate process's internal state.
        /// </summary>
        /// <param name="handle">A process handle. Note that this functinon will take the ownership of the handle.</param>
        public Process(SafeProcessHandle handle)
        {
            Handle = handle;
        }

        ~Process()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Handle?.Dispose();
            Handle = null!;
        }

        public static Process Create(ProcessCreationOptions options)
        {
            //

            if (!Win32.CreateProcessW())
            {
                ProcessException.ThrowLastOsError("Failed to create a new process.");
            }

            //
            //
        }

        protected static SafeProcessHandle OpenHandle(uint pid, PROCESS_ACCESS_RIGHT access)
        {
            var handle = Win32.OpenProcess((uint)access, false, pid);

            if (handle.IsInvalid)
            {
                ProcessException.ThrowLastOsError(pid);
            }

            return handle;
        }

        public static Process Open(uint pid, PROCESS_ACCESS_RIGHT access)
        {
            var handle = OpenHandle(pid, access);

            return new Process(handle);
        }

        public uint GetPid() => Win32.GetProcessId(Handle);

        public void Terminate(uint exitCode = 0)
        {
            if (!Win32.TerminateProcess(Handle, exitCode))
            {
                ProcessException.ThrowLastOsError(GetPid());
            }
        }

        /// <summary>
        /// Reads the process memory.
        /// </summary>
        /// <returns>
        /// The number of bytes that is actually read.
        /// </returns>
        protected int ReadMemory(IntPtr address, Span<byte> destination)
        {
            unsafe
            {
                fixed (byte* pDest = destination)
                {
                    if (!Win32.ReadProcessMemory(Handle, (void*)address, pDest, (IntPtr)destination.Length, out var bytesRead))
                    {
                        ProcessException.ThrowLastOsError(GetPid());
                    }

                    // this is okay as the length of the span can't be longer than int.Max
                    return (int)bytesRead;
                }
            }
        }

        protected void ReadMemoryExact(IntPtr address, Span<byte> destination)
        {
            var totalBytesRead = 0;
            while (totalBytesRead < destination.Length)
            {
                var bytesRead = ReadMemory(address + totalBytesRead, destination[totalBytesRead..]);

                if (bytesRead == 0)
                {
                    // prolly page fault; there's not much we can do here
                    ProcessException.ThrowLastOsError(GetPid());
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
                var status = Win32.NtQueryInformationProcess(Handle, PROCESSINFOCLASS.ProcessBasicInformation, &info, sizeof(PROCESS_BASIC_INFORMATION), (IntPtr*)IntPtr.Zero);

                if (!status.Success)
                {
                    var message = $"A call to NtQueryInformationProcess failed. (Status: {status})";
                    var pid = GetPid();

                    throw new ProcessException(message, pid);
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
            unsafe
            {
                FileTime creationTime, exitTime, kernelTime, userTime;

                if (!Win32.GetProcessTimes(Handle, &creationTime, &exitTime, &kernelTime, &userTime))
                {
                    ProcessException.ThrowLastOsError(GetPid());
                }

                return (DateTime)creationTime;
            }
        }

        private string[] ParseCommandLineToArguments(ReadOnlySpan<byte> commandLine)
        {
            unsafe
            {
                char** argv;
                int argc;

                fixed (byte* pCommandLine = commandLine)
                {
                    argv = Win32.CommandLineToArgvW(pCommandLine, out argc);
                }

                if (argv == null)
                {
                    ProcessException.ThrowLastOsError(GetPid());
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

        public string GetImageFilePath()
        {
            var buffer = new StringBuilder(300);

            // From docs:
            // On input, specifies the size of the lpExeName buffer, in characters.
            // On success, receives the number of characters written to the buffer, not including the null-terminating character.
            var size = buffer.Capacity;

            if (!Win32.QueryFullProcessImageNameW(Handle, 0, buffer, ref size))
            {
                ProcessException.ThrowLastOsError(GetPid());
            }

            return buffer.ToString();
        }
    }
}

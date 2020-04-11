using System;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Bootstrap.OS.Windows.Raw;
using Microsoft.Win32.SafeHandles;

namespace Dalamud.Bootstrap.OS.Windows
{
    /// <summary>
    /// Provides a thin wrapper around process API.
    /// </summary>
    internal sealed class Process : IDisposable
    {
        /// <summary>
        /// A process handle that can be passed to PInvoke. Note that it should never outlive a process object.
        /// This handle will be disposed when the process object is disposed.
        /// </summary>
        public IntPtr Handle { get; private set; }

        public Process(IntPtr handle)
        {
            Handle = handle;
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                Kernel32.CloseHandle(Handle);
            }

            Handle = IntPtr.Zero;
        }

        /// <summary>
        /// Reads process memory.
        /// </summary>
        /// <returns>
        /// A number of bytes that is actually read.
        /// </returns>
        /// <exception cref="ProcessException">
        /// Thrown when failed to read memory. 
        /// </exception>
        public int ReadMemory(IntPtr address, Span<byte> destination)
        {
            unsafe
            {
                fixed (byte* pDest = destination)
                {
                    if (!Kernel32.ReadProcessMemory(Handle, address, pDest, (IntPtr)destination.Length, out var bytesRead))
                    {
                        ProcessException.ThrowLastOsError();
                    }

                    // This is okay because destination will never be longer than int.Max
                    return bytesRead.ToInt32();
                }
            }
        }

        /// <summary>
        /// Reads the exact number of bytes required to fill the buffer.
        /// </summary>
        /// <exception cref="ProcessException">
        /// Thrown when failed to read memory. 
        /// </exception>
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


        /// <exception cref="ProcessException">
        /// Thrown when failed to read memory. 
        /// </exception>
        public byte[] ReadMemoryExact(IntPtr address, int length)
        {
            var buffer = new byte[length];
            ReadMemoryExact(address, buffer);

            return buffer;
        }


        /// <exception cref="ProcessException">
        /// Thrown when failed to read memory. 
        /// </exception>
        public void ReadMemoryExact<T>(IntPtr address, ref T value) where T : unmanaged
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

                var status = Ntdll.NtQueryInformationProcess(Handle, PROCESSINFOCLASS.ProcessBasicInformation, &info, sizeof(PROCESS_BASIC_INFORMATION), (IntPtr*)IntPtr.Zero);

                if (!status.Success)
                {
                    throw new ProcessException($"Could not query information on process. (Status: {status})");
                }

                return info.PebBaseAddress;
            }
        }

        /// <summary>
        /// Reads command-line arguments from the process.
        /// </summary>
        public string[] GetProcessArguments()
        {
            PEB peb = default;
            RTL_USER_PROCESS_PARAMETERS procParam = default;

            // Find where the command line is allocated
            var pebAddr = GetPebAddress();
            ReadMemoryExact(pebAddr, ref peb);
            ReadMemoryExact(peb.ProcessParameters, ref procParam);

            // Read the command line (which is utf16-like string)
            var commandLine = ReadMemoryExact(procParam.CommandLine.Buffer, procParam.CommandLine.Length);

            return ParseCommandLineToArguments(commandLine);
        }

        /// <summary>
        /// Returns a time when the process was started.
        /// </summary>
        public DateTime GetCreationTime()
        {
            if (!Kernel32.GetProcessTimes(Handle, out var creationTime, out var _, out var _, out var _))
            {
                ProcessException.ThrowLastOsError();
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
                    ProcessException.ThrowLastOsError();
                }

                // NOTE: argv must be deallocated via LocalFree when we're done
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

            if (!Kernel32.QueryFullProcessImageNameW(Handle, 0, buffer, ref size))
            {
                ProcessException.ThrowLastOsError();
            }

            return buffer.ToString();
        }
    }
}

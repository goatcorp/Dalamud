using Dalamud.Bootstrap.OS;
using Dalamud.Bootstrap.OS.Windows;
using Dalamud.Bootstrap.OS.Windows.Raw;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Bootstrap
{
    public sealed partial class GameProcess : IDisposable
    {
        private const uint OpenProcessRights = 0;

        private IntPtr m_handle;

        public GameProcess(IntPtr handle)
        {
            m_handle = handle;
        }

        ~GameProcess()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(true);
        }

        private void Dispose(bool disposing)
        {
            if (m_handle != IntPtr.Zero)
            {
                Kernel32.CloseHandle(m_handle);

                m_handle = IntPtr.Zero;
            }
        }

        private static IntPtr OpenProcessHandle(uint pid, uint access)
        {
            var handle = Kernel32.OpenProcess(access, false, pid);

            if (handle == IntPtr.Zero)
            {
                ProcessException.ThrowLastOsError();
            }

            return handle;
        }

        public static GameProcess Open(uint pid)
        {
            var secHandle = OpenProcessHandle(pid, (uint)(PROCESS_ACCESS_RIGHTS.READ_CONTROL | PROCESS_ACCESS_RIGHTS.WRITE_DAC));
            try
            {
                return RelaxProcessHandle(secHandle, (_) =>
                {
                    var handle = OpenProcessHandle(pid, OpenProcessRights);

                    return new GameProcess(handle);
                });
            }
            finally
            {
                Kernel32.CloseHandle(secHandle);
            }
        }

        private static T RelaxProcessHandle<T>(IntPtr handle, Func<IntPtr, T> scope)
        {
            // relax shit
            unsafe
            {
                T result;
                uint error;
                SECURITY_DESCRIPTOR* pSecurityDescOrig;
                ACL* pDaclOrig;

                error = Advapi32.GetSecurityInfo(
                    handle,
                    SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                    SECURITY_INFORMATION.DACL_SECURITY_INFORMATION,
                    null,
                    null,
                    &pDaclOrig,
                    null,
                    &pSecurityDescOrig
                );

                if (error != 0)
                {
                    throw new ProcessException();
                }

                try
                {
                    EXPLICIT_ACCESS_W explictAccess;
                    ACL* pRelaxedAcl;

                    Advapi32.BuildExplicitAccessWithNameW(&explictAccess, "TODO", OpenProcessRights, ACCESS_MODE.GRANT_ACCESS, 0);

                    error = Advapi32.SetEntriesInAclW(1, &explictAccess, null, &pRelaxedAcl);

                    if (error != 0)
                    {
                        throw new ProcessException();
                    }

                    error = Advapi32.SetSecurityInfo(
                        handle,
                        SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                        SECURITY_INFORMATION.DACL_SECURITY_INFORMATION,
                        null,
                        null,
                        pRelaxedAcl,
                        null
                    );

                    if (error != 0)
                    {
                        throw new ProcessException();
                    }

                    result = scope(handle);
                }
                finally
                {
                    // Restore permission; also we don't care about an error for now
                    Advapi32.SetSecurityInfo(
                        handle,
                        SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                        SECURITY_INFORMATION.DACL_SECURITY_INFORMATION,
                        null,
                        null,
                        pDaclOrig,
                        null
                    );

                    Kernel32.LocalFree(pSecurityDescOrig);
                }

                return result;
            }
        }

        public void GetSecurityInfo()
        {

            var error = Advapi32.GetSecurityInfo(m_handle, SE_OBJECT_TYPE.SE_KERNEL_OBJECT, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, );

            if (error != 0 /* ERROR_SUCCESS */)
            {
                throw new ProcessException($"Could not read a security info. (Error {error})");
            }


        }

        private static void AllowDacl(Process process)
        {

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

            if (!Kernel32.QueryFullProcessImageNameW(m_handle, 0, buffer, ref size))
            {
                ProcessException.ThrowLastOsError();
            }

            return buffer.ToString();
        }
    }
}

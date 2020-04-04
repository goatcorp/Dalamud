using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Bootstrap.Windows
{
    internal static unsafe class Win32
    {
        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern SafeProcessHandle OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateProcess(SafeProcessHandle hProcess, uint uExitCode);

        [DllImport("ntdll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern NtStatus NtQueryInformationProcess(SafeProcessHandle processHandle, PROCESSINFOCLASS processInfoClass, void* processInformation, int processInformationLength, out IntPtr returnLength);

        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(SafeProcessHandle hProcess, void* lpBaseAddress, void* lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteProcessMemory(SafeProcessHandle hProcess, void* lpBaseAddress, void* lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern void* LocalFree(void* hMem);

        [DllImport("shell32", CallingConvention = CallingConvention.Winapi, SetLastError = true, ExactSpelling = true)]
        public static extern char** CommandLineToArgvW(void* lpCmdLine, out int pNumArgs);

        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi)]
        public static extern uint GetProcessId(SafeProcessHandle hProcess);

        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetProcessTimes(SafeProcessHandle hProcess, FileTime* lpCreationTime, FileTime* lpExitTime, FileTime* lpKernelTime, FileTime* lpUserTime);

        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryFullProcessImageNameW(SafeProcessHandle hProcess, uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessW(string lpApplicationName, StringBuilder lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles, PROCESS_CREATION_FLAG dwCreationFlags, lpEnvironment, string lpCurrentDirectory, lpStartupInfo, lpProcessInformation);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NtStatus
    {
        public uint Value { get; }

        public NtStatus(uint value)
        {
            Value = value;
        }

        /// <summary>
        /// Equivalent to NT_SUCCESS
        /// </summary>
        public bool Success => Value <= 0x7FFFFFFF;

        /// <summary>
        /// Equivalent to NT_INFORMATION
        /// </summary>
        public bool Information => 0x40000000 <= Value && Value <= 0x7FFFFFFF;

        /// <summary>
        /// Equivalent to NT_WARNING
        /// </summary>
        public bool Warning => 0x80000000 <= Value && Value <= 0xBFFFFFFF;

        /// <summary>
        /// Equivalent to NT_ERROR
        /// </summary>
        public bool Error => 0xC0000000 <= Value;

        public override string ToString() => $"0x{Value:X8}";
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FileTime
    {
        public uint LowDateTime;

        public uint HighDateTime;

        public static explicit operator DateTime(FileTime value)
        {
            var time = ((long)value.HighDateTime << 32) | value.LowDateTime;
            
            return DateTime.FromFileTime(time);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    internal enum PROCESSINFOCLASS : uint
    {
        ProcessBasicInformation = 0,
        ProcessDebugPort = 7,
        ProcessWow64Information = 26,
        ProcessImageFileName = 27,
        ProcessBreakOnTermination = 29,
        ProcessSubsystemInformation = 75,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_BASIC_INFORMATION
    {
        // https://github.com/processhacker/processhacker/blob/e43d7c0513ec5368c3309a58c9f2c2a3ca5de367/phnt/include/ntpsapi.h#L272
        public NtStatus ExitStatus;
        public IntPtr PebBaseAddress;
        public IntPtr AffinityMask;
        public IntPtr BasePriority;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PEB
    {
        // https://github.com/processhacker/processhacker/blob/238287786b80abad647b988e60f69090cab4c8fe/phnt/include/ntpebteb.h#L57-L218
        public byte InheritedAddressSpace;
        public byte ReadImageFileExecOptions;
        public byte BeingDebugged;
        public byte BitField;
        public IntPtr Mutant;
        public IntPtr ImageBaseAddress;
        public IntPtr Ldr;
        public IntPtr ProcessParameters;
        // ..snip..
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RTL_USER_PROCESS_PARAMETERS
    {

        public uint MaximumLength;
        public uint LengthInitialized;
        public uint Flags;
        public uint DebugFlags;
        public IntPtr ConsoleHandle;
        public uint ConsoleFlags;
        public IntPtr StandardInput;
        public IntPtr StandardOutput;
        public IntPtr StandardError;
        public UNICODE_STRING CurrentDirectory_DosPath;
        public IntPtr CurrentDirectory_Handle;
        public UNICODE_STRING DllPath;
        public UNICODE_STRING ImagePathName;
        public UNICODE_STRING CommandLine;
        // ..snip..
    }

    [Flags]
    internal enum PROCESS_ACCESS_RIGHT : uint
    {
        PROCESS_TERMINATE = 0x1,
        PROCESS_CREATE_THREAD = 0x2,
        PROCESS_VM_OPERATION = 0x8,
        PROCESS_VM_READ = 0x10,
        PROCESS_VM_WRITE = 0x20,
        PROCESS_DUP_HANDLE = 0x40,
        PROCESS_CREATE_PROCESS = 0x80,
        PROCESS_SET_QUOTA = 0x100,
        PROCESS_SET_INFORMATION = 0x200,
        PROCESS_QUERY_INFORMATION = 0x400,
        PROCESS_SUSPEND_RESUME = 0x800,
        PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
        SYNCHRONIZE = 0x100000,
    }

    [Flags]
    internal enum PROCESS_CREATION_FLAG : uint
    {
        CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
        CREATE_DEFAULT_ERROR_MODE = 0x04000000,
        CREATE_NEW_CONSOLE = 0x00000010,
        CREATE_NEW_PROCESS_GROUP = 0x00000200,
        CREATE_NO_WINDOW = 0x08000000,
        CREATE_PROTECTED_PROCESS = 0x00040000,
        CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
        CREATE_SECURE_PROCESS = 0x00400000,
        CREATE_SEPARATE_WOW_VDM = 0x00000800,
        CREATE_SHARED_WOW_VDM = 0x00001000,
        CREATE_SUSPENDED = 0x00000004,
        CREATE_UNICODE_ENVIRONMENT = 0x00000400,
        DEBUG_ONLY_THIS_PROCESS = 0x00000002,
        DEBUG_PROCESS = 0x00000001,
        DETACHED_PROCESS = 0x00000008,
        EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
        INHERIT_PARENT_AFFINITY = 0x00010000,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SECURITY_ATTRIBUTES
    {
        public uint Length;

        public SECURITY_DESCRIPTOR* SecurityDescriptor;

        [MarshalAs(UnmanagedType.Bool)]
        public bool InheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_DESCRIPTOR
    {

    }
}

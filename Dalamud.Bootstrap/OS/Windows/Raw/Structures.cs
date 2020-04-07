using System;
using System.Runtime.InteropServices;

namespace Dalamud.Bootstrap.OS.Windows.Raw
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NTSTATUS
    {
        public uint Code;

        public NTSTATUS(uint value)
        {
            Code = value;
        }

        /// <summary>
        /// Equivalent to NT_SUCCESS
        /// </summary>
        public bool Success => Code <= 0x7FFFFFFF;

        /// <summary>
        /// Equivalent to NT_INFORMATION
        /// </summary>
        public bool Information => 0x40000000 <= Code && Code <= 0x7FFFFFFF;

        /// <summary>
        /// Equivalent to NT_WARNING
        /// </summary>
        public bool Warning => 0x80000000 <= Code && Code <= 0xBFFFFFFF;

        /// <summary>
        /// Equivalent to NT_ERROR
        /// </summary>
        public bool Error => 0xC0000000 <= Code;

        public override string ToString() => $"{Code:X8}";

        public static implicit operator uint(NTSTATUS status) => status.Code;

        public static implicit operator NTSTATUS(uint code) => new NTSTATUS(code);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FILETIME
    {
        public uint LowDateTime;

        public uint HighDateTime;

        public long FileTime => ((long)HighDateTime << 32) | LowDateTime;

        public DateTime ToDateTime() => DateTime.FromFileTime(FileTime);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    // https://github.com/processhacker/processhacker/blob/e43d7c0513ec5368c3309a58c9f2c2a3ca5de367/phnt/include/ntpsapi.h#L272
    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_BASIC_INFORMATION
    {
        public NTSTATUS ExitStatus;
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
        // ..snip.. we don't care about others
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
        // ..snip.. don't care
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

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Injector.FFI
{
    internal static class Win32
    {
        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern SafeProcessHandle OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern NtStatus NtQueryInformationProcess(/* TODO */);


    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct NtStatus
    {
        public uint Value { get; }
    }

    internal partial struct NtStatus
    {
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
}

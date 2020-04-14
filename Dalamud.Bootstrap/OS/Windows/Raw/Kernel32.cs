using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Bootstrap.OS.Windows.Raw
{
    internal static unsafe class Kernel32
    {
        private const string Name = "kernel32";

        [DllImport(Name, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [DllImport(Name, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateProcess(IntPtr hProcess, int uExitCode);

        [DllImport(Name, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, void* lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesRead);

        [DllImport(Name, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, void* lpBuffer, IntPtr nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport(Name, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern void* LocalFree(void* hMem);

        [DllImport(Name, CallingConvention = CallingConvention.Winapi)]
        public static extern uint GetProcessId(IntPtr hProcess);

        [DllImport(Name, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetProcessTimes(IntPtr hProcess, FILETIME* lpCreationTime, FILETIME* lpExitTime, FILETIME* lpKernelTime, FILETIME* lpUserTime);

        [DllImport(Name, CallingConvention = CallingConvention.Winapi, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryFullProcessImageNameW(IntPtr hProcess, uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpExeName, ref int lpdwSize);

        [DllImport(Name, CallingConvention = CallingConvention.Winapi, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessW(void* lpApplicationName, void* lpCommandLine, SECURITY_ATTRIBUTES* lpProcessAttributes, SECURITY_ATTRIBUTES* lpThreadAttributes, uint bInheritHandles, uint dwCreationFlags, void* lpEnvironment, void* lpCurrentDirectory, void* lpStartupInfo, void* lpProcessInformation);

        [DllImport(Name, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);
    }
}

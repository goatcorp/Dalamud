using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Dalamud.Bootstrap.OS.Windows.Raw
{
    internal static unsafe class Ntdll
    {
        private const string Name = "ntdll";

        [DllImport(Name, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern NTSTATUS NtQueryInformationProcess(SafeProcessHandle processHandle, PROCESSINFOCLASS processInfoClass, void* processInformation, int processInformationLength, IntPtr* returnLength);
    }
}

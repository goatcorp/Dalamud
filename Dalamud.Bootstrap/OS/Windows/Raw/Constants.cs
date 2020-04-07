using System;

namespace Dalamud.Bootstrap.OS.Windows.Raw
{
    // https://github.com/processhacker/processhacker/blob/c1a8c103f8afa1561dbac416f87523ea8f70b15e/phnt/include/ntpsapi.h#L96-L199
    internal enum PROCESSINFOCLASS : uint
    {
        ProcessBasicInformation = 0,
    }

    // https://github.com/processhacker/processhacker/blob/0e9cf471e06a59cdb3a7c89f0b92b253a6a93999/phnt/include/ntpsapi.h#L5-L17
    [Flags]
    internal enum PROCESS_ACCESS_RIGHTS : uint
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
    internal enum PROCESS_CREATION_FLAGS : uint
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
}

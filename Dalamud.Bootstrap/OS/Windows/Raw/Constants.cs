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

    internal enum SECURITY_DESCRIPTOR_CONTROL : ushort
    {
        SE_DACL_AUTO_INHERIT_REQ = 0x0100,
        SE_DACL_AUTO_INHERITED = 0x0400,
        SE_DACL_DEFAULTED = 0x0008,
        SE_DACL_PRESENT = 0x0004,
        SE_DACL_PROTECTED = 0x1000,
        SE_GROUP_DEFAULTED = 0x0002,
        SE_OWNER_DEFAULTED = 0x0001,
        SE_RM_CONTROL_VALID = 0x4000,
        SE_SACL_AUTO_INHERIT_REQ = 0x0200,
        SE_SACL_AUTO_INHERITED = 0x0800,
        SE_SACL_DEFAULTED = 0x0008,
        SE_SACL_PRESENT = 0x0010,
        SE_SACL_PROTECTED = 0x2000,
        SE_SELF_RELATIVE = 0x8000,
    }

    internal enum ACCESS_MODE : uint
    {
        NOT_USED_ACCESS,
        GRANT_ACCESS,
        SET_ACCESS,
        DENY_ACCESS,
        REVOKE_ACCESS,
        SET_AUDIT_SUCCESS,
        SET_AUDIT_FAILURE,
    }

    internal enum MULTIPLE_TRUSTEE_OPERATION : uint
    {
        NO_MULTIPLE_TRUSTEE,
        TRUSTEE_IS_IMPERSONATE,
    }

    internal enum TRUSTEE_FORM : uint
    {
        TRUSTEE_IS_SID,
        TRUSTEE_IS_NAME,
        TRUSTEE_BAD_FORM,
        TRUSTEE_IS_OBJECTS_AND_SID,
        TRUSTEE_IS_OBJECTS_AND_NAME,
    }

    internal enum TRUSTEE_TYPE : uint
    {
        TRUSTEE_IS_UNKNOWN,
        TRUSTEE_IS_USER,
        TRUSTEE_IS_GROUP,
        TRUSTEE_IS_DOMAIN,
        TRUSTEE_IS_ALIAS,
        TRUSTEE_IS_WELL_KNOWN_GROUP,
        TRUSTEE_IS_DELETED,
        TRUSTEE_IS_INVALID,
        TRUSTEE_IS_COMPUTER,
    }

    internal enum SE_OBJECT_TYPE : uint
    {

        SE_UNKNOWN_OBJECT_TYPE,
        SE_FILE_OBJECT,
        SE_SERVICE,
        SE_PRINTER,
        SE_REGISTRY_KEY,
        SE_LMSHARE,
        SE_KERNEL_OBJECT,
        SE_WINDOW_OBJECT,
        SE_DS_OBJECT,
        SE_DS_OBJECT_ALL,
        SE_PROVIDER_DEFINED_OBJECT,
        SE_WMIGUID_OBJECT,
        SE_REGISTRY_WOW64_32KEY,
        SE_REGISTRY_WOW64_64KEY
    }
}

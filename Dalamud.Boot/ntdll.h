#pragma once

// ntdll exports
enum {
    LDR_DLL_NOTIFICATION_REASON_LOADED = 1,
    LDR_DLL_NOTIFICATION_REASON_UNLOADED = 2,
};

struct LDR_DLL_UNLOADED_NOTIFICATION_DATA {
    ULONG Flags;                    //Reserved.
    const UNICODE_STRING* FullDllName;   //The full path name of the DLL module.
    const UNICODE_STRING* BaseDllName;   //The base file name of the DLL module.
    PVOID DllBase;                  //A pointer to the base address for the DLL in memory.
    ULONG SizeOfImage;              //The size of the DLL image, in bytes.
};

struct LDR_DLL_LOADED_NOTIFICATION_DATA {
    ULONG Flags;                    //Reserved.
    const UNICODE_STRING* FullDllName;   //The full path name of the DLL module.
    const UNICODE_STRING* BaseDllName;   //The base file name of the DLL module.
    PVOID DllBase;                  //A pointer to the base address for the DLL in memory.
    ULONG SizeOfImage;              //The size of the DLL image, in bytes.
};

union LDR_DLL_NOTIFICATION_DATA {
    LDR_DLL_LOADED_NOTIFICATION_DATA Loaded;
    LDR_DLL_UNLOADED_NOTIFICATION_DATA Unloaded;
};

using PLDR_DLL_NOTIFICATION_FUNCTION = VOID CALLBACK(_In_ ULONG NotificationReason, _In_ const LDR_DLL_NOTIFICATION_DATA* NotificationData, _In_opt_ PVOID Context);

NTSTATUS LdrRegisterDllNotification(ULONG Flags, PLDR_DLL_NOTIFICATION_FUNCTION NotificationFunction, PVOID Context, PVOID* Cookie);
NTSTATUS LdrUnregisterDllNotification(PVOID Cookie);

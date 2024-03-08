#include "pch.h"

#include "ntdll.h"

#include "utils.h"

NTSTATUS LdrRegisterDllNotification(ULONG Flags, PLDR_DLL_NOTIFICATION_FUNCTION NotificationFunction, PVOID Context, PVOID* Cookie) {
    static const auto pfn = utils::loaded_module(GetModuleHandleW(L"ntdll.dll")).get_exported_function<NTSTATUS(NTAPI)(ULONG Flags, PLDR_DLL_NOTIFICATION_FUNCTION NotificationFunction, PVOID Context, PVOID* Cookie)>("LdrRegisterDllNotification");
    return pfn(Flags, NotificationFunction, Context, Cookie);
}

NTSTATUS LdrUnregisterDllNotification(PVOID Cookie) {
    static const auto pfn = utils::loaded_module(GetModuleHandleW(L"ntdll.dll")).get_exported_function<NTSTATUS(NTAPI)(PVOID Cookie)>("LdrUnregisterDllNotification");
    return pfn(Cookie);
}

#pragma once

#include "windows.h"

struct exception_info
{
    void* ExceptionPointers; // Cannot dereference!
    DWORD ThreadId;
    DWORD ProcessId;
    BOOL DoFullDump;
    wchar_t DumpPath[1000];

    // For metrics
    DWORD ExceptionCode;
    long long Lifetime;
};

constexpr wchar_t SHARED_INFO_FILE_NAME[] = L"DalamudCrashInfoShare";
constexpr wchar_t CRASHDUMP_EVENT_NAME[] = L"Global\\DalamudRequestWriteDump";

#pragma once

#include <cinttypes>

#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>

struct exception_info
{
    LPEXCEPTION_POINTERS pExceptionPointers;
    EXCEPTION_POINTERS ExceptionPointers;
    EXCEPTION_RECORD ExceptionRecord;
    CONTEXT ContextRecord;
    uint64_t nLifetime;
    HANDLE hThreadHandle;
    HANDLE hEventHandle;
    DWORD dwStackTraceLength;
    DWORD dwTroubleshootingPackDataLength;
};

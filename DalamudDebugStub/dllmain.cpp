// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"

#include <stdlib.h>
#include <tchar.h>
#include <cstdio>
#include <DbgHelp.h>
#include <TlHelp32.h>

BOOL ListProcessThreads(DWORD dwOwnerPID)
{
    HANDLE hThreadSnap = INVALID_HANDLE_VALUE;
    THREADENTRY32 te32;

    // Take a snapshot of all running threads  
    hThreadSnap = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
    if (hThreadSnap == INVALID_HANDLE_VALUE)
        return(FALSE);

    // Fill in the size of the structure before using it. 
    te32.dwSize = sizeof(THREADENTRY32);

    // Retrieve information about the first thread,
    // and exit if unsuccessful
    if (!Thread32First(hThreadSnap, &te32))
    {
        CloseHandle(hThreadSnap);     // Must clean up the snapshot object!
        return(FALSE);
    }

    // Now walk the thread list of the system,
    // and display information about each thread
    // associated with the specified process
    do
    {
        if (te32.th32OwnerProcessID == dwOwnerPID && te32.th32ThreadID != GetCurrentThreadId())
        {
            _tprintf(TEXT("\n     THREAD ID      = 0x%08X"), te32.th32ThreadID);
            _tprintf(TEXT("\n     base priority  = %d"), te32.tpBasePri);
            _tprintf(TEXT("\n     delta priority = %d"), te32.tpDeltaPri);
            SuspendThread(OpenThread(THREAD_ALL_ACCESS, FALSE, te32.th32ThreadID));
        }
    } while (Thread32Next(hThreadSnap, &te32));

    _tprintf(TEXT("\n"));

    //  Don't forget to clean up the snapshot object.
    CloseHandle(hThreadSnap);
    return(TRUE);
}

bool isExcept = false;

LONG WINAPI
VectoredHandlerSkip1(
    struct _EXCEPTION_POINTERS* ExceptionInfo
)
{
    PEXCEPTION_RECORD Record = ExceptionInfo->ExceptionRecord;

    if (Record->ExceptionCode != EXCEPTION_ACCESS_VIOLATION)
        return EXCEPTION_CONTINUE_SEARCH;

    //ListProcessThreads(GetCurrentProcessId());

    if (!isExcept)
    {
        isExcept = true;

        TCHAR pszMessage[1024] = { 0 };
        _stprintf_s(pszMessage, _T("An internal error in Dalamud or a FFXIV plugin occured.\nThe game must close.\n\nDo you wish to save troubleshooting information?\n\nReasoning: 0x%x at 0x%x"), Record->ExceptionCode, Record->ExceptionAddress);

        auto res = MessageBox(NULL, pszMessage, L"Dalamud", MB_YESNO | MB_ICONERROR | MB_TOPMOST);

        if (res == IDYES)
        {

            TCHAR fileName[255] = { 0 };

            SYSTEMTIME t;
            GetSystemTime(&t);
            _stprintf_s(fileName, _T("MD-%d-%d-%d-%d-%d-%d.dmp"), t.wYear, t.wMonth, t.wDay, t.wHour, t.wMinute, t.wSecond);

            HANDLE hFile = CreateFile(fileName, GENERIC_READ | GENERIC_WRITE,
                0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

            if ((hFile != NULL) && (hFile != INVALID_HANDLE_VALUE))
            {
                // Create the minidump 

                MINIDUMP_EXCEPTION_INFORMATION mdei;

                mdei.ThreadId = GetCurrentThreadId();
                mdei.ExceptionPointers = ExceptionInfo;
                mdei.ClientPointers = TRUE;

                BOOL rv = MiniDumpWriteDump(GetCurrentProcess(), GetCurrentProcessId(),
                    hFile, (MINIDUMP_TYPE)(MiniDumpWithFullMemory | MiniDumpWithDataSegs | MiniDumpWithThreadInfo), (ExceptionInfo != 0) ? &mdei : 0, 0, 0);

                if (!rv)
                    _stprintf_s(pszMessage, _T("MiniDumpWriteDump failed. Error: %u \n"), GetLastError());
                else
                    _stprintf_s(pszMessage, _T("Minidump created.\n"));

                MessageBox(NULL, pszMessage, L"Dalamud", MB_OK | MB_ICONINFORMATION | MB_TOPMOST);

                // Close the file 

                CloseHandle(hFile);
            }
            else
            {
                _tprintf(_T("CreateFile failed. Error: %u \n"), GetLastError());
            }
        }

        exit(-1);
    }

    PCONTEXT Context;

    Context = ExceptionInfo->ContextRecord;
#ifdef _AMD64_
    Context->Rip++;
#else
    Context->Eip++;
#endif    
    return EXCEPTION_CONTINUE_EXECUTION;
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
)
{
    TCHAR pszMessage[1024] = { 0 };

    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        _stprintf_s(pszMessage, _T("GetCurrentProcessId() %d, hModule 0x%p, nReason %d\r\n"), GetCurrentProcessId(), hModule, ul_reason_for_call);
        OutputDebugString(pszMessage);
        AddVectoredExceptionHandler(99, VectoredHandlerSkip1);
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}


// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"

#include <stdlib.h>
#include <tchar.h>
#include <cstdio>
#include <DbgHelp.h>

bool isExcept = false;

LONG WINAPI
VectoredHandler(
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

            char* pValue;
            size_t len;
            errno_t err = _dupenv_s(&pValue, &len, "APPDATA");

            wchar_t* fullPath = new wchar_t[2048];

            // Convert char* string to a wchar_t* string.
            size_t convertedChars = 0;
            mbstowcs_s(&convertedChars, fullPath, strlen(pValue) + 1, pValue, _TRUNCATE);

            SYSTEMTIME t;
            GetSystemTime(&t);
            _stprintf_s(fileName, _T("MD-%d-%d-%d-%d-%d-%d.dmp"), t.wYear, t.wMonth, t.wDay, t.wHour, t.wMinute, t.wSecond);

            _tcscat_s(fullPath, 2048, TEXT("\\XIVLauncher\\"));
            _tcscat_s(fullPath, 2048, fileName);

            HANDLE hFile = CreateFileW(fullPath, GENERIC_READ | GENERIC_WRITE,
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
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:

        AddVectoredExceptionHandler(99, VectoredHandler);
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}


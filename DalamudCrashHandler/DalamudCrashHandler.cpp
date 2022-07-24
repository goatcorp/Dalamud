#include <iostream>
#include <sstream>
#include <windows.h>
#include <minidumpapiset.h>
#include <tlhelp32.h>
#include <winhttp.h>

#include "../Dalamud.Boot/crashhandler_shared.h"

DWORD WINAPI ExitCheckThread(LPVOID lpParam)
{
    while (true)
    {
        PROCESSENTRY32 entry;
        entry.dwSize = sizeof(PROCESSENTRY32);

        HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, NULL);

        if (Process32First(snapshot, &entry) == TRUE)
        {
            bool had_xiv = false;

            while (Process32Next(snapshot, &entry) == TRUE)
            {
                // Exit if there's another crash handler
                // TODO(goat): We should make this more robust and ensure that there is one per PID
                if (_wcsicmp(entry.szExeFile, L"DalamudCrashHandler.exe") == 0 &&
                    entry.th32ProcessID != GetCurrentProcessId())
                {
                    ExitProcess(0);
                    break;
                }

                if (_wcsicmp(entry.szExeFile, L"ffxiv_dx11.exe") == 0)
                {
                    had_xiv = true;
                }
            }

            if (!had_xiv)
            {
                ExitProcess(0);
                break;
            }
        }

        CloseHandle(snapshot);

        Sleep(1000);
    }
}

int main()
{
    CreateThread(
        NULL,                   // default security attributes
        0,                      // use default stack size  
        ExitCheckThread,       // thread function name
        NULL,          // argument to thread function 
        0,                      // use default creation flags 
        NULL);   // returns the thread identifier 

    auto file_mapping = CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, sizeof(exception_info), SHARED_INFO_FILE_NAME);
    if (!file_mapping) {
        std::cout << "Could not map info share file.\n";
        return -2;
    }

    auto file_ptr = MapViewOfFile(file_mapping, FILE_MAP_READ, 0, 0, sizeof(exception_info));
    if (!file_ptr) {
        std::cout << "Could not map view of info share file.\n";
        return -3;
    }

    std::cout << "Waiting for crash...\n";

    auto crash_event = CreateEvent(
        NULL,               // default security attributes
        TRUE,               // manual-reset event
        FALSE,              // initial state is nonsignaled
        CRASHDUMP_EVENT_NAME  // object name
    );

    if (!crash_event)
    {
        std::cout << "Couldn't acquire event handle\n";
        return -1;
    }

    auto wait_result = WaitForSingleObject(crash_event, INFINITE);
    std::cout << "Crash triggered, writing dump!\n";

    auto info_share = (exception_info*)file_ptr;

    if (!info_share->ExceptionPointers)
    {
        std::cout << "info_share->ExceptionPointers was nullptr\n";
        return -4;
    }

    MINIDUMP_EXCEPTION_INFORMATION mdmp_info;
    mdmp_info.ClientPointers = true;
    mdmp_info.ExceptionPointers = (PEXCEPTION_POINTERS)info_share->ExceptionPointers;
    mdmp_info.ThreadId = info_share->ThreadId;


    std::cout << "Dump for " << info_share->ProcessId << std::endl;
    HANDLE file = CreateFileW(info_share->DumpPath, GENERIC_WRITE, FILE_SHARE_WRITE, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (!file)
    {
        auto hr = GetLastError();
        std::cout << "Failed to open dump file: " << std::hex << hr << std::endl;
        return -6;
    }

    auto process = OpenProcess(PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_QUERY_INFORMATION, FALSE, info_share->ProcessId);
    if (!process)
    {
        auto hr = GetLastError();
        std::cout << "Failed to open " << info_share->ProcessId << ": " << std::hex << hr << std::endl;
        return -6;
    }

    auto success = MiniDumpWriteDump(process, info_share->ProcessId, file, MiniDumpWithFullMemory, &mdmp_info, NULL, NULL);
    if (!success)
    {
        auto hr = GetLastError();
        std::cout << "Failed: " << std::hex << hr << std::endl;
    }

    // TODO(goat): Technically, we should have another event or a semaphore to block xiv while dumping...

    CloseHandle(file);
    CloseHandle(process);
    CloseHandle(file_mapping);

    if (getenv("DALAMUD_NO_METRIC"))
        return 0;

    HINTERNET internet = WinHttpOpen(L"DALAMUDCRASHHANDLER", WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY, NULL, NULL, WINHTTP_FLAG_SECURE_DEFAULTS);
    HINTERNET connect = NULL, request = NULL;
    if (internet)
    {
        connect = WinHttpConnect(internet, L"kamori.goats.dev", INTERNET_DEFAULT_HTTPS_PORT, 0);
    }

    if (connect)
    {
        std::wstringstream url{ L"/Dalamud/Metric/ReportCrash/" };
        url << "?lt=" << info_share->Lifetime << "&code=" << std::hex << info_share->ExceptionCode;

        request = WinHttpOpenRequest(internet, L"GET", url.str().c_str(), NULL, NULL, NULL, 0);
    }

    if (request)
    {
        bool sent = WinHttpSendRequest(request,
            WINHTTP_NO_ADDITIONAL_HEADERS,
            0, WINHTTP_NO_REQUEST_DATA, 0,
            0, 0);

        if (!sent)
            std::cout << "Failed to send metric: " << std::hex << GetLastError() << std::endl;
    }

    if (request) WinHttpCloseHandle(request);
    if (connect) WinHttpCloseHandle(connect);
    if (internet) WinHttpCloseHandle(internet);

    return 0;
}

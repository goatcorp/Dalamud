#define WIN32_LEAN_AND_MEAN
#define DllExport extern "C" __declspec(dllexport)

#include <filesystem>
#include <Windows.h>
#include <stdlib.h>
#include <tchar.h>
#include <DbgHelp.h>
#include "..\lib\CoreCLR\CoreCLR.h"
#include "..\lib\CoreCLR\boot.h"
#include <fstream>

HMODULE g_hModule;
BOOL g_isExcept = false;

LONG WINAPI VectoredHandler(struct _EXCEPTION_POINTERS* ExceptionInfo)
{
    auto record = ExceptionInfo->ExceptionRecord;
    auto context = ExceptionInfo->ContextRecord;

    if (record->ExceptionCode != EXCEPTION_ACCESS_VIOLATION)
        return EXCEPTION_CONTINUE_SEARCH;

    if (!g_isExcept)
    {
        g_isExcept = true;

        #pragma region Setup base path

        wchar_t base_path_arr[MAX_PATH];
        GetModuleFileNameW(g_hModule, base_path_arr, MAX_PATH);
        std::filesystem::path base_path(base_path_arr);
        base_path = base_path.remove_filename();

        #if defined(NDEBUG) 
        base_path = base_path / L".." / L".." / L"..";
        #endif

        #pragma endregion

        #pragma region Write to dalamud.log

        wchar_t dalamud_log_path_arr[MAX_PATH];
        wcsncpy_s(dalamud_log_path_arr, base_path_arr, MAX_PATH);
        std::filesystem::path dalamud_log_path(dalamud_log_path_arr);
        dalamud_log_path /= L"dalamud.log";

        auto base_address = (unsigned long long)GetModuleHandleW(nullptr);
        auto ex_address = (unsigned long long)record->ExceptionAddress;
        auto actual_address = ex_address - base_address + 0x140000000;

        FILE* log_handle;
        errno_t result = _wfopen_s(&log_handle, dalamud_log_path.c_str(), L"a");
        if (!result)
        {
            MessageBox(NULL, L"Could not open dalamud.log for writing.", L"Dalamud", MB_OK | MB_ICONERROR | MB_TOPMOST);
        }
        else
        {

            fwprintf_s(log_handle, L"--------------------------------------------------------------------------------\n");
            fwprintf_s(log_handle, L"An internal error in Dalamud or a plugin occurred. 0x%X at 0x%p (0x%p)", record->ExceptionCode, record->ExceptionAddress, actual_address);
            fwprintf_s(log_handle, L"Rax=%p R08=%p", context->Rax, context->R8);
            fwprintf_s(log_handle, L"Rcx=%p R09=%p", context->Rcx, context->R9);
            fwprintf_s(log_handle, L"Rdx=%p R10=%p", context->Rdx, context->R10);
            fwprintf_s(log_handle, L"Rbx=%p R11=%p", context->Rbx, context->R11);
            fwprintf_s(log_handle, L"Rsp=%p R12=%p", context->Rsp, context->R12);
            fwprintf_s(log_handle, L"Rbp=%p R13=%p", context->Rbp, context->R13);
            fwprintf_s(log_handle, L"Rsi=%p R14=%p", context->Rsi, context->R14);
            fwprintf_s(log_handle, L"Rdi=%p R15=%p", context->Rdi, context->R15);
            fwprintf_s(log_handle, L"Xmm00=%f Xmm08=%f", context->Xmm0, context->Xmm8);
            fwprintf_s(log_handle, L"Xmm01=%f Xmm09=%f", context->Xmm1, context->Xmm9);
            fwprintf_s(log_handle, L"Xmm02=%f Xmm10=%f", context->Xmm2, context->Xmm10);
            fwprintf_s(log_handle, L"Xmm03=%f Xmm11=%f", context->Xmm3, context->Xmm11);
            fwprintf_s(log_handle, L"Xmm04=%f Xmm12=%f", context->Xmm4, context->Xmm12);
            fwprintf_s(log_handle, L"Xmm05=%f Xmm13=%f", context->Xmm5, context->Xmm13);
            fwprintf_s(log_handle, L"Xmm06=%f Xmm14=%f", context->Xmm6, context->Xmm14);
            fwprintf_s(log_handle, L"Xmm07=%f Xmm15=%f", context->Xmm7, context->Xmm15);
            fwprintf_s(log_handle, L"--------------------------------------------------------------------------------\n");
            fclose(log_handle);
        }

        #pragma endregion

        wchar_t pszMessage[1024] = { 0 };
        swprintf_s(pszMessage, L"An internal error in Dalamud or a plugin occurred.\nThe game must close.\n\nDo you wish to save a memory dump?\n\nReasoning: 0x%X at 0x%p (0x%p)", record->ExceptionCode, record->ExceptionAddress, actual_address);

        auto result = MessageBox(NULL, pszMessage, L"Dalamud", MB_YESNO | MB_ICONERROR | MB_TOPMOST);
        if (result == IDYES)
        {
            #pragma region Write minidump

            SYSTEMTIME t;
            GetSystemTime(&t);

            wchar_t file_name[30] = { 0 };
            swprintf_s(file_name, L"MiniDump-%04d%02d%02d-%02d%02d%02d.dmp", t.wYear, t.wMonth, t.wDay, t.wHour, t.wMinute, t.wSecond);

            wchar_t minidump_path_arr[MAX_PATH];
            wcsncpy_s(minidump_path_arr, base_path_arr, MAX_PATH);
            std::filesystem::path minidump_path(minidump_path_arr);
            minidump_path /= file_name;

            HANDLE hFile = CreateFileW(minidump_path.c_str(), GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

            if (hFile == NULL || hFile == INVALID_HANDLE_VALUE)
            {
                wprintf_s(L"CreateFile failed. Error: %u \n", GetLastError());
            }
            else
            {
                MINIDUMP_EXCEPTION_INFORMATION mdei{};
                mdei.ThreadId = GetCurrentThreadId();
                mdei.ExceptionPointers = ExceptionInfo;
                mdei.ClientPointers = TRUE;

                BOOL success = MiniDumpWriteDump(
                    GetCurrentProcess(), GetCurrentProcessId(),
                    hFile, (MINIDUMP_TYPE)(MiniDumpWithFullMemory | MiniDumpWithDataSegs | MiniDumpWithThreadInfo),
                    ExceptionInfo != 0 ? &mdei : 0, 0, 0);

                if (success)
                    swprintf_s(pszMessage, L"Minidump created.\n");
                else
                    swprintf_s(pszMessage, L"MiniDumpWriteDump failed. Error: %u\n", GetLastError());

                MessageBox(NULL, pszMessage, L"Dalamud", MB_OK | MB_ICONINFORMATION | MB_TOPMOST);

                // Close the file 
                CloseHandle(hFile);
            }

            #pragma endregion
        }

        exit(-1);
    }

    PCONTEXT Context;
    Context = ExceptionInfo->ContextRecord;
    Context->Rip++;

    return EXCEPTION_CONTINUE_EXECUTION;
}


DllExport DWORD WINAPI Initialize(LPVOID lpParam)
{
    #if !defined(NDEBUG)
    ConsoleSetup(L"Dalamud Boot");
    #endif

    wchar_t _module_path[MAX_PATH];
    GetModuleFileNameW(g_hModule, _module_path, sizeof _module_path / 2);
    std::filesystem::path fs_module_path(_module_path);

    std::wstring runtimeconfig_path = _wcsdup(fs_module_path.replace_filename(L"Dalamud.runtimeconfig.json").c_str());
    std::wstring module_path = _wcsdup(fs_module_path.replace_filename(L"Dalamud.dll").c_str());

    // =========================================================================== //

    void* entrypoint_vfn;
    int result = InitializeClrAndGetEntryPoint(
        runtimeconfig_path,
        module_path,
        L"Dalamud.EntryPoint, Dalamud",
        L"Initialize",
        L"Dalamud.EntryPoint+InitDelegate, Dalamud",
        &entrypoint_vfn);

    if (result != 0)
        return result;

    typedef void (CORECLR_DELEGATE_CALLTYPE* custom_component_entry_point_fn)(LPVOID);
    custom_component_entry_point_fn entrypoint_fn = reinterpret_cast<custom_component_entry_point_fn>(entrypoint_vfn);

    printf("Initializing exception handler...");
    AddVectoredExceptionHandler(99, VectoredHandler);
    printf("Done!\n");

    printf("Initializing Dalamud... ");
    entrypoint_fn(lpParam);
    printf("Done!\n");

    // =========================================================================== //

    #if !defined(NDEBUG)
    FreeConsole();
    #endif

    return 0;
}

BOOL APIENTRY DllMain(const HMODULE hModule, const DWORD dwReason, LPVOID lpReserved) {
    DisableThreadLibraryCalls(hModule);

    switch (dwReason)
    {
        case DLL_PROCESS_ATTACH:
            g_hModule = hModule;
            break;
        case DLL_PROCESS_DETACH:
            break;
    }
    return TRUE;
}

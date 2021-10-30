#define WIN32_LEAN_AND_MEAN
#define DllExport extern "C" __declspec(dllexport)

#include <filesystem>
#include <fstream>
#include <Windows.h>
#include <DbgHelp.h>
#include "..\lib\CoreCLR\CoreCLR.h"
#include "..\lib\CoreCLR\boot.h"

HMODULE g_hModule;
PVOID g_hVEH; // VEH Handle to remove the handler later in DLL_PROCESS_DETACH

std::vector g_exception_whitelist({
    STATUS_ACCESS_VIOLATION,
    STATUS_IN_PAGE_ERROR,
    STATUS_INVALID_HANDLE,
    STATUS_INVALID_PARAMETER,
    STATUS_NO_MEMORY,
    STATUS_ILLEGAL_INSTRUCTION,
    STATUS_NONCONTINUABLE_EXCEPTION,
    STATUS_INVALID_DISPOSITION,
    STATUS_ARRAY_BOUNDS_EXCEEDED,
    STATUS_FLOAT_DENORMAL_OPERAND,
    STATUS_FLOAT_DIVIDE_BY_ZERO,
    STATUS_FLOAT_INEXACT_RESULT,
    STATUS_FLOAT_INVALID_OPERATION,
    STATUS_FLOAT_OVERFLOW,
    STATUS_FLOAT_STACK_CHECK,
    STATUS_FLOAT_UNDERFLOW,
    STATUS_INTEGER_DIVIDE_BY_ZERO,
    STATUS_INTEGER_OVERFLOW,
    STATUS_PRIVILEGED_INSTRUCTION,
    STATUS_STACK_OVERFLOW,
    STATUS_DLL_NOT_FOUND,
    STATUS_ORDINAL_NOT_FOUND,
    STATUS_ENTRYPOINT_NOT_FOUND,
    STATUS_DLL_INIT_FAILED,
    STATUS_CONTROL_STACK_VIOLATION,
    STATUS_FLOAT_MULTIPLE_FAULTS,
    STATUS_FLOAT_MULTIPLE_TRAPS,
    STATUS_HEAP_CORRUPTION,
    STATUS_STACK_BUFFER_OVERRUN,
    STATUS_INVALID_CRUNTIME_PARAMETER,
    STATUS_THREAD_NOT_RUNNING,
    STATUS_ALREADY_REGISTERED
});

bool GetModuleFileAndBase(DWORD64 address, PDWORD64 moduleBase, std::filesystem::path& moduleFile)
{
    HMODULE handle;
    if (GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, reinterpret_cast<LPCSTR>(address), &handle))
    {
        if (wchar_t path[1024]; GetModuleFileNameW(handle, path, sizeof path / 2) > 0)
        {
            *moduleBase = reinterpret_cast<DWORD64>(handle);
            moduleFile = path;
            return true;
        }
    }
    return false;
}

bool GetCallStack(PEXCEPTION_POINTERS ex, std::vector<DWORD64>& addressList, DWORD frames = 10)
{
    STACKFRAME64 sf;
    sf.AddrPC.Offset = ex->ContextRecord->Rip;
    sf.AddrPC.Mode = AddrModeFlat;
    sf.AddrStack.Offset = ex->ContextRecord->Rsp;
    sf.AddrStack.Mode = AddrModeFlat;
    sf.AddrFrame.Offset = ex->ContextRecord->Rbp;
    sf.AddrFrame.Mode = AddrModeFlat;
    CONTEXT ctx = *ex->ContextRecord;
    addressList.clear();
    do
    {
        if (!StackWalk64(IMAGE_FILE_MACHINE_AMD64, GetCurrentProcess(), GetCurrentThread(), &sf, &ctx, nullptr, nullptr, nullptr, nullptr))
            return false;
        addressList.push_back(sf.AddrPC.Offset);
    } while (sf.AddrReturn.Offset != 0 && --frames);
    return true;
}

LONG ExceptionHandler(PEXCEPTION_POINTERS ex)
{
    // return if the exception is not in the whitelist
    if (std::ranges::find(g_exception_whitelist, ex->ExceptionRecord->ExceptionCode) == g_exception_whitelist.end())
        return EXCEPTION_CONTINUE_SEARCH;
    
    DWORD64 module_base;
    std::filesystem::path module_path;

    // return if the exception did not happen in ffxiv_dx11.exe
    if (!GetModuleFileAndBase(ex->ContextRecord->Rip, &module_base, module_path) || module_path.filename() != L"ffxiv_dx11.exe")
        return EXCEPTION_CONTINUE_SEARCH;

    wchar_t boot_mod_path[1024];
    GetModuleFileNameW(g_hModule, boot_mod_path, sizeof boot_mod_path / 2);
    std::filesystem::path fs_module_path(boot_mod_path);
#ifndef NDEBUG
    std::wstring dmp_path = _wcsdup(fs_module_path.replace_filename(L"dalamud_appcrashd.dmp").wstring().c_str());
#else
    std::wstring dmp_path = _wcsdup(fs_module_path.replace_filename(L"dalamud_appcrash.dmp").wstring().c_str());
#endif
    std::wstring log_path = _wcsdup(fs_module_path.replace_filename(L"dalamud_appcrash.log").wstring().c_str());
    
    std::wofstream log;
    log.open(log_path, std::ios::app);

    std::wstring time_stamp;

    std::time_t t = std::time(nullptr);
    std::tm tm{};
    localtime_s(&tm, &t);

    log << L"[" << std::put_time(&tm, L"%d/%m/%Y %H:%M:%S") << L"] Exception " << std::uppercase << std::hex << ex->ExceptionRecord->ExceptionCode << " at ";
    if(GetModuleFileAndBase(ex->ContextRecord->Rip, &module_base, module_path))
        log << module_path.filename() << "+" << std::uppercase << std::hex << ex->ContextRecord->Rip - module_base << std::endl;
    else log << std::uppercase << std::hex << ex->ContextRecord->Rip << std::endl;

    std::vector<DWORD64> callStack;

    if(GetCallStack(ex, callStack, -1))
    {
        log << L"Call Stack:" << std::endl;
        for(auto& addr : callStack)
        {
            if (GetModuleFileAndBase(addr, &module_base, module_path))
                log << L"  " << module_path.filename().c_str() << "+" << std::uppercase << std::hex << addr - module_base << std::endl;
            else log << L"  " << std::uppercase << std::hex << addr << std::endl;
        }
    }

    MINIDUMP_EXCEPTION_INFORMATION ex_info;
    ex_info.ClientPointers = true;
    ex_info.ExceptionPointers = ex;
    ex_info.ThreadId = GetCurrentThreadId();

    auto file = CreateFileW(dmp_path.c_str(), GENERIC_WRITE, FILE_SHARE_WRITE, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    MiniDumpWriteDump(GetCurrentProcess(), GetCurrentProcessId(), file, MiniDumpWithDataSegs, &ex_info, nullptr, nullptr);
    CloseHandle(file);

    log << "Crash Dump: " << dmp_path << std::endl;    

    log.close();

    return EXCEPTION_CONTINUE_SEARCH;
}

DllExport DWORD WINAPI Initialize(LPVOID lpParam)
{
    #ifndef NDEBUG
    ConsoleSetup(L"Dalamud Boot");
    #endif

    printf("Dalamud.Boot Injectable, (c) 2021 XIVLauncher Contributors\nBuilt at: %s@%s\n\n", __DATE__, __TIME__);

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

    printf("Initializing Dalamud... ");
    entrypoint_fn(lpParam);
    printf("Done!\n");

    // ============================== VEH ======================================== //

    g_hVEH = AddVectoredExceptionHandler(0, ExceptionHandler);
    if (g_hVEH)
        printf("VEH Installed [%p]\n", g_hVEH);
    else printf("Failed to Install VEH\n");

    // =========================================================================== //

    #ifndef NDEBUG
    fclose(stdin);
    fclose(stdout);
    fclose(stderr);
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
            // remove the VEH on unload
            if (g_hVEH)
                RemoveVectoredExceptionHandler(g_hVEH);
            break;
    }
    return TRUE;
}

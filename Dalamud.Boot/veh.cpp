#define WIN32_LEAN_AND_MEAN
#include "veh.h"
#include <filesystem>
#include <fstream>
#include <Windows.h>
#include <DbgHelp.h>

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

    GetModuleFileAndBase(reinterpret_cast<DWORD64>(&ExceptionHandler), &module_base, module_path);
#ifndef NDEBUG
    std::wstring dmp_path = _wcsdup(module_path.replace_filename(L"dalamud_appcrashd.dmp").wstring().c_str());
#else
    std::wstring dmp_path = _wcsdup(module_path.replace_filename(L"dalamud_appcrash.dmp").wstring().c_str());
#endif
    std::wstring log_path = _wcsdup(module_path.replace_filename(L"dalamud_appcrash.log").wstring().c_str());

    std::wofstream log;
    log.open(log_path, std::ios::app);

    std::wstring time_stamp;

    std::time_t t = std::time(nullptr);
    std::tm tm{};
    localtime_s(&tm, &t);

    log << L"[" << std::put_time(&tm, L"%d/%m/%Y %H:%M:%S") << L"] Exception " << std::uppercase << std::hex << ex->ExceptionRecord->ExceptionCode << " at ";
    if (GetModuleFileAndBase(ex->ContextRecord->Rip, &module_base, module_path))
        log << module_path.filename() << "+" << std::uppercase << std::hex << ex->ContextRecord->Rip - module_base << std::endl;
    else log << std::uppercase << std::hex << ex->ContextRecord->Rip << std::endl;

    if (std::vector<DWORD64> call_stack; GetCallStack(ex, call_stack, -1))
    {
        log << L"Call Stack:" << std::endl;
        for (auto& addr : call_stack)
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

PVOID g_hVEH; // VEH Handle to remove the handler later in DLL_PROCESS_DETACH

bool veh::add_handler()
{
    if (g_hVEH)
        return false;
    g_hVEH = AddVectoredExceptionHandler(0, ExceptionHandler);
    return g_hVEH != nullptr;
}

bool veh::remove_handler()
{
    if (g_hVEH && RemoveVectoredExceptionHandler(g_hVEH) != 0)
    {
        g_hVEH = nullptr;
        return true;
    }
    return false;
}

void* veh::get_handle()
{
    return g_hVEH;
}

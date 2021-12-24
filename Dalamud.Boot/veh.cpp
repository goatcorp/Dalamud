#include "pch.h"

#include "veh.h"

bool is_whitelist_exception(const DWORD code)
{
    switch (code)
    {
    case STATUS_ACCESS_VIOLATION:
    case STATUS_IN_PAGE_ERROR:
    case STATUS_INVALID_HANDLE:
    case STATUS_INVALID_PARAMETER:
    case STATUS_NO_MEMORY:
    case STATUS_ILLEGAL_INSTRUCTION:
    case STATUS_NONCONTINUABLE_EXCEPTION:
    case STATUS_INVALID_DISPOSITION:
    case STATUS_ARRAY_BOUNDS_EXCEEDED:
    case STATUS_FLOAT_DENORMAL_OPERAND:
    case STATUS_FLOAT_DIVIDE_BY_ZERO:
    case STATUS_FLOAT_INEXACT_RESULT:
    case STATUS_FLOAT_INVALID_OPERATION:
    case STATUS_FLOAT_OVERFLOW:
    case STATUS_FLOAT_STACK_CHECK:
    case STATUS_FLOAT_UNDERFLOW:
    case STATUS_INTEGER_DIVIDE_BY_ZERO:
    case STATUS_INTEGER_OVERFLOW:
    case STATUS_PRIVILEGED_INSTRUCTION:
    case STATUS_STACK_OVERFLOW:
    case STATUS_DLL_NOT_FOUND:
    case STATUS_ORDINAL_NOT_FOUND:
    case STATUS_ENTRYPOINT_NOT_FOUND:
    case STATUS_DLL_INIT_FAILED:
    case STATUS_CONTROL_STACK_VIOLATION:
    case STATUS_FLOAT_MULTIPLE_FAULTS:
    case STATUS_FLOAT_MULTIPLE_TRAPS:
    case STATUS_HEAP_CORRUPTION:
    case STATUS_STACK_BUFFER_OVERRUN:
    case STATUS_INVALID_CRUNTIME_PARAMETER:
    case STATUS_THREAD_NOT_RUNNING:
    case STATUS_ALREADY_REGISTERED:
        return true;
    default:
        return false;
    }
}


bool get_module_file_and_base(const DWORD64 address, DWORD64& module_base, std::filesystem::path& module_file)
{
    HMODULE handle;
    if (GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, reinterpret_cast<LPCSTR>(address), &handle))
    {
        std::wstring path(PATHCCH_MAX_CCH, L'\0');
        path.resize(GetModuleFileNameW(handle, &path[0], static_cast<DWORD>(path.size())));
        if (!path.empty())
        {
            module_base = reinterpret_cast<DWORD64>(handle);
            module_file = path;
            return true;
        }
    }
    return false;
}


bool is_ffxiv_address(const DWORD64 address)
{
    DWORD64 module_base;
    if (std::filesystem::path module_path; get_module_file_and_base(address, module_base, module_path))
        return _wcsicmp(module_path.filename().c_str(), L"ffxiv_dx11.exe") == 0;
    return false;
}


bool get_sym_from_addr(const DWORD64 address, DWORD64& displacement, std::wstring& symbol_name)
{
    union {
        char buffer[sizeof(SYMBOL_INFOW) + MAX_SYM_NAME * sizeof(wchar_t)]{};
        SYMBOL_INFOW symbol;
    };
    symbol.SizeOfStruct = sizeof(SYMBOL_INFO);
    symbol.MaxNameLen = MAX_SYM_NAME;

    if (SymFromAddrW(GetCurrentProcess(), address, &displacement, &symbol) && symbol.Name[0])
    {
        symbol_name = symbol.Name;
        return true;
    }
    return false;
}


std::wstring to_address_string(const DWORD64 address, const bool try_ptrderef = true)
{
    DWORD64 module_base;
    std::filesystem::path module_path;
    bool is_mod_addr = get_module_file_and_base(address, module_base, module_path);

    DWORD64 value = 0;
    if(try_ptrderef && address > 0x10000 && address < 0x7FFFFFFE0000)
    {
        ReadProcessMemory(GetCurrentProcess(), reinterpret_cast<void*>(address), &value, sizeof value, nullptr);
    }

    std::wstring addr_str = is_mod_addr ?
        std::format(L"{}+{:X}", module_path.filename().c_str(), address - module_base) :
        std::format(L"{:X}", address);

    DWORD64 displacement;
    if (std::wstring symbol; get_sym_from_addr(address, displacement, symbol))
        return std::format(L"{}\t({})", addr_str, displacement != 0 ? std::format(L"{}+0x{:X}", symbol, displacement) : std::format(L"{}", symbol));
    return value != 0 ? std::format(L"{} [{}]", addr_str, to_address_string(value, false)) : addr_str;
}


void print_exception_info(const EXCEPTION_POINTERS* ex, std::wostringstream& log)
{
    size_t rec_index = 0;
    for (auto rec = ex->ExceptionRecord; rec; rec = rec->ExceptionRecord)
    {
        log << std::format(L"\nException Info #{}\n", ++rec_index);
        log << std::format(L"Address: {:X}\n", rec->ExceptionCode);
        log << std::format(L"Flags: {:X}\n", rec->ExceptionFlags);
        log << std::format(L"Address: {:X}\n", reinterpret_cast<size_t>(rec->ExceptionAddress));
        if (!rec->NumberParameters)
            continue;
        log << L"Parameters: ";
        for (DWORD i = 0; i < rec->NumberParameters; ++i)
        {
            if (i != 0)
                log << L", ";
            log << std::format(L"{:X}", rec->ExceptionInformation[i]);
        }
    }
    
    log << L"\nCall Stack\n{";

    STACKFRAME64 sf;
    sf.AddrPC.Offset = ex->ContextRecord->Rip;
    sf.AddrPC.Mode = AddrModeFlat;
    sf.AddrStack.Offset = ex->ContextRecord->Rsp;
    sf.AddrStack.Mode = AddrModeFlat;
    sf.AddrFrame.Offset = ex->ContextRecord->Rbp;
    sf.AddrFrame.Mode = AddrModeFlat;
    CONTEXT ctx = *ex->ContextRecord;
    int frame_index = 0;

    log << std::format(L"\n  [{}]\t{}", frame_index++, to_address_string(sf.AddrPC.Offset, false));

    do
    {
        if (!StackWalk64(IMAGE_FILE_MACHINE_AMD64, GetCurrentProcess(), GetCurrentThread(), &sf, &ctx, nullptr, SymFunctionTableAccess64, SymGetModuleBase64, nullptr))
            break;

        log << std::format(L"\n  [{}]\t{}", frame_index++, to_address_string(sf.AddrPC.Offset, false));

    } while (sf.AddrReturn.Offset != 0 && sf.AddrPC.Offset != sf.AddrReturn.Offset);

    log << L"\n}\n";

    ctx = *ex->ContextRecord;

    log << L"\nRegisters\n{";

    log << std::format(L"\n  RAX:\t{}", to_address_string(ctx.Rax));
    log << std::format(L"\n  RBX:\t{}", to_address_string(ctx.Rbx));
    log << std::format(L"\n  RCX:\t{}", to_address_string(ctx.Rcx));
    log << std::format(L"\n  RDX:\t{}", to_address_string(ctx.Rdx));
    log << std::format(L"\n  R8:\t{}", to_address_string(ctx.R8));
    log << std::format(L"\n  R9:\t{}", to_address_string(ctx.R9));
    log << std::format(L"\n  R10:\t{}", to_address_string(ctx.R10));
    log << std::format(L"\n  R11:\t{}", to_address_string(ctx.R11));
    log << std::format(L"\n  R12:\t{}", to_address_string(ctx.R12));
    log << std::format(L"\n  R13:\t{}", to_address_string(ctx.R13));
    log << std::format(L"\n  R14:\t{}", to_address_string(ctx.R14));
    log << std::format(L"\n  R15:\t{}", to_address_string(ctx.R15));

    log << std::format(L"\n  RSI:\t{}", to_address_string(ctx.Rsi));
    log << std::format(L"\n  RDI:\t{}", to_address_string(ctx.Rdi));
    log << std::format(L"\n  RBP:\t{}", to_address_string(ctx.Rbp));
    log << std::format(L"\n  RSP:\t{}", to_address_string(ctx.Rsp));
    log << std::format(L"\n  RIP:\t{}", to_address_string(ctx.Rip));

    log << L"\n}" << std::endl;

    if(0x10000 < ctx.Rsp && ctx.Rsp < 0x7FFFFFFE0000)
    {
        log << L"\nStack\n{";

        for(DWORD64 i = 0; i < 16; i++)
            log << std::format(L"\n  [RSP+{:X}]\t{}", i * 8, to_address_string(*reinterpret_cast<DWORD64*>(ctx.Rsp + i * 8ull)));

        log << L"\n}\n";
    }

    log << L"\nModules\n{";

    if(HANDLE snap = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, GetCurrentProcessId()); snap != INVALID_HANDLE_VALUE)
    {
        MODULEENTRY32 mod;
        mod.dwSize = sizeof MODULEENTRY32;
        if(Module32First(snap, &mod))
        {
            do
            {
                log << std::format(L"\n  {:08X}\t{}", reinterpret_cast<DWORD64>(mod.modBaseAddr), mod.szExePath);
            }
            while (Module32Next(snap, &mod));
        }
        CloseHandle(snap);
    }

    log << L"\n}\n";
}

LONG exception_handler(EXCEPTION_POINTERS* ex)
{
    static std::mutex s_exception_handler_mutex;

    if (!is_whitelist_exception(ex->ExceptionRecord->ExceptionCode))
        return EXCEPTION_CONTINUE_SEARCH;

    if (!is_ffxiv_address(ex->ContextRecord->Rip))
        return EXCEPTION_CONTINUE_SEARCH;

    // block any other exceptions hitting the veh while the messagebox is open
    const auto lock = std::lock_guard(s_exception_handler_mutex);

    DWORD64 module_base;
    std::filesystem::path module_path;

    get_module_file_and_base(reinterpret_cast<DWORD64>(&exception_handler), module_base, module_path);
#ifndef NDEBUG
    std::wstring dmp_path = module_path.replace_filename(L"dalamud_appcrashd.dmp").wstring();
#else
    std::wstring dmp_path = module_path.replace_filename(L"dalamud_appcrash.dmp").wstring();
#endif
    std::wstring log_path = module_path.replace_filename(L"dalamud_appcrash.log").wstring();

    std::wostringstream log;
    log << std::format(L"Unhandled native exception occurred at {}", to_address_string(ex->ContextRecord->Rip, false)) << std::endl;
    log << std::format(L"Code: {:X}", ex->ExceptionRecord->ExceptionCode) << std::endl;
    log << std::format(L"Dump at: {}", dmp_path) << std::endl;
    log << L"Time: " << std::chrono::zoned_time{ std::chrono::current_zone(), std::chrono::system_clock::now() } << std::endl;

    SymRefreshModuleList(GetCurrentProcess());
    print_exception_info(ex, log);

    MINIDUMP_EXCEPTION_INFORMATION ex_info;
    ex_info.ClientPointers = false;
    ex_info.ExceptionPointers = ex;
    ex_info.ThreadId = GetCurrentThreadId();

    HANDLE file = CreateFileW(dmp_path.c_str(), GENERIC_WRITE, FILE_SHARE_WRITE, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    MiniDumpWriteDump(GetCurrentProcess(), GetCurrentProcessId(), file, MiniDumpWithDataSegs, &ex_info, nullptr, nullptr);
    CloseHandle(file);
    
    void* fn;
    if (const auto err = static_cast<DWORD>(g_clr->get_function_pointer(
        L"Dalamud.EntryPoint, Dalamud",
        L"VehCallback",
        L"Dalamud.EntryPoint+VehDelegate, Dalamud", 
        nullptr, nullptr, &fn)))
    {
        const auto msg = L"An error within the game has occurred.\n\n"
            L"This may be caused by a faulty plugin, a broken TexTools modification, any other third-party tool or simply a bug in the game.\n"
            L"Please try \"Start Over\" or \"Download Index Backup\" in TexTools, an integrity check in the XIVLauncher settings, and disabling plugins you don't need.\n\n"
            L"The log file is located at:\n"
            L"{1}\n\n"
            L"Press OK to exit the application.\n\nFailed to read stack trace: {2:08x}";

        // show in another thread to prevent messagebox from pumping messages of current thread
        std::thread([&]() {
            MessageBoxW(nullptr, std::format(msg, dmp_path, log_path, err).c_str(), L"Dalamud Error", MB_OK | MB_ICONERROR | MB_TOPMOST);
            }).join();
    }
    else
    {
        ((void(__stdcall*)(const void*, const void*, const void*))fn)(dmp_path.c_str(), log_path.c_str(), log.str().c_str());
    }

    return EXCEPTION_CONTINUE_SEARCH;
}


PVOID g_veh_handle = nullptr;

bool veh::add_handler()
{
    if (g_veh_handle)
        return false;
    g_veh_handle = AddVectoredExceptionHandler(1, exception_handler);
    SetUnhandledExceptionFilter(nullptr);
    return g_veh_handle != nullptr;
}

bool veh::remove_handler()
{
    if (g_veh_handle && RemoveVectoredExceptionHandler(g_veh_handle) != 0)
    {
        g_veh_handle = nullptr;
        return true;
    }
    return false;
}

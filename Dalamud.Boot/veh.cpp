#include "pch.h"

#include "veh.h"

#include <shellapi.h>

#include "logging.h"
#include "utils.h"
#include "hooks.h"

#include "crashhandler_shared.h"
#include "DalamudStartInfo.h"

#pragma comment(lib, "comctl32.lib")

#if defined _M_IX86
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_IA64
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif

PVOID g_veh_handle = nullptr;
bool g_veh_do_full_dump = false;
std::optional<hooks::import_hook<decltype(SetUnhandledExceptionFilter)>> g_HookSetUnhandledExceptionFilter;

HANDLE g_crashhandler_process = nullptr;
HANDLE g_crashhandler_event = nullptr;
HANDLE g_crashhandler_pipe_write = nullptr;

std::recursive_mutex g_exception_handler_mutex;

std::chrono::time_point<std::chrono::system_clock> g_time_start;

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

bool is_ffxiv_address(const wchar_t* module_name, const DWORD64 address)
{
    DWORD64 module_base;
    if (std::filesystem::path module_path; get_module_file_and_base(address, module_base, module_path))
        return _wcsicmp(module_path.filename().c_str(), module_name) == 0;
    return false;
}

static void append_injector_launch_args(std::vector<std::wstring>& args)
{
    args.emplace_back(L"--game=\"" + utils::loaded_module::current_process().path().wstring() + L"\"");
    switch (g_startInfo.DalamudLoadMethod) {
    case DalamudStartInfo::LoadMethod::Entrypoint:
        args.emplace_back(L"--mode=entrypoint");
        break;
    case DalamudStartInfo::LoadMethod::DllInject:
        args.emplace_back(L"--mode=inject");
    }
    args.emplace_back(L"--dalamud-working-directory=\"" + unicode::convert<std::wstring>(g_startInfo.WorkingDirectory) + L"\"");
    args.emplace_back(L"--dalamud-configuration-path=\"" + unicode::convert<std::wstring>(g_startInfo.ConfigurationPath) + L"\"");
    args.emplace_back(L"--logpath=\"" + unicode::convert<std::wstring>(g_startInfo.LogPath) + L"\"");
    args.emplace_back(L"--logname=\"" + unicode::convert<std::wstring>(g_startInfo.LogName) + L"\"");
    args.emplace_back(L"--dalamud-plugin-directory=\"" + unicode::convert<std::wstring>(g_startInfo.PluginDirectory) + L"\"");
    args.emplace_back(L"--dalamud-asset-directory=\"" + unicode::convert<std::wstring>(g_startInfo.AssetDirectory) + L"\"");
    args.emplace_back(std::format(L"--dalamud-client-language={}", static_cast<int>(g_startInfo.Language)));
    args.emplace_back(std::format(L"--dalamud-delay-initialize={}", g_startInfo.DelayInitializeMs));
    // NoLoadPlugins/NoLoadThirdPartyPlugins: supplied from DalamudCrashHandler

    if (g_startInfo.BootShowConsole)
        args.emplace_back(L"--console");
    if (g_startInfo.BootEnableEtw)
        args.emplace_back(L"--etw");
    if (g_startInfo.BootVehEnabled)
        args.emplace_back(L"--veh");
    if (g_startInfo.BootVehFull)
        args.emplace_back(L"--veh-full");
    if ((g_startInfo.BootWaitMessageBox & DalamudStartInfo::WaitMessageboxFlags::BeforeInitialize) != DalamudStartInfo::WaitMessageboxFlags::None)
        args.emplace_back(L"--msgbox1");
    if ((g_startInfo.BootWaitMessageBox & DalamudStartInfo::WaitMessageboxFlags::BeforeDalamudEntrypoint) != DalamudStartInfo::WaitMessageboxFlags::None)
        args.emplace_back(L"--msgbox2");
    if ((g_startInfo.BootWaitMessageBox & DalamudStartInfo::WaitMessageboxFlags::BeforeDalamudConstruct) != DalamudStartInfo::WaitMessageboxFlags::None)
        args.emplace_back(L"--msgbox3");

    args.emplace_back(L"--");

    if (int nArgs; LPWSTR * szArgList = CommandLineToArgvW(GetCommandLineW(), &nArgs)) {
        for (auto i = 1; i < nArgs; i++)
            args.emplace_back(szArgList[i]);
        LocalFree(szArgList);
    }
}

LONG exception_handler(EXCEPTION_POINTERS* ex)
{
    // block any other exceptions hitting the handler while the messagebox is open
    const auto lock = std::lock_guard(g_exception_handler_mutex);

    exception_info exinfo{};
    exinfo.pExceptionPointers = ex;
    exinfo.ExceptionPointers = *ex;
    exinfo.ContextRecord = *ex->ContextRecord;
    exinfo.ExceptionRecord = ex->ExceptionRecord ? *ex->ExceptionRecord : EXCEPTION_RECORD{};
    const auto time_now = std::chrono::system_clock::now();
    auto lifetime = std::chrono::duration_cast<std::chrono::seconds>(
        time_now.time_since_epoch()).count()
        - std::chrono::duration_cast<std::chrono::seconds>(
            g_time_start.time_since_epoch()).count();
    exinfo.nLifetime = lifetime;
    DuplicateHandle(GetCurrentProcess(), GetCurrentThread(), g_crashhandler_process, &exinfo.hThreadHandle, 0, TRUE, DUPLICATE_SAME_ACCESS);
    DuplicateHandle(GetCurrentProcess(), g_crashhandler_event, g_crashhandler_process, &exinfo.hEventHandle, 0, TRUE, DUPLICATE_SAME_ACCESS);

    std::wstring stackTrace;
    if (!g_clr)
    {
        stackTrace = L"(no CLR stack trace available)";
    }
    else if (void* fn; const auto err = static_cast<DWORD>(g_clr->get_function_pointer(
        L"Dalamud.EntryPoint, Dalamud",
        L"VehCallback",
        L"Dalamud.EntryPoint+VehDelegate, Dalamud",
        nullptr, nullptr, &fn)))
    {
        stackTrace = std::format(L"Failed to read stack trace: 0x{:08x}", err);
    }
    else
    {
        stackTrace = static_cast<wchar_t*(*)()>(fn)();
        // Don't free it, as the program's going to be quit anyway
    }

    exinfo.dwStackTraceLength = static_cast<DWORD>(stackTrace.size());
    exinfo.dwTroubleshootingPackDataLength = static_cast<DWORD>(g_startInfo.TroubleshootingPackData.size());
    if (DWORD written; !WriteFile(g_crashhandler_pipe_write, &exinfo, static_cast<DWORD>(sizeof exinfo), &written, nullptr) || sizeof exinfo != written)
        return EXCEPTION_CONTINUE_SEARCH;

    if (const auto nb = static_cast<DWORD>(std::span(stackTrace).size_bytes()))
    {
        if (DWORD written; !WriteFile(g_crashhandler_pipe_write, stackTrace.data(), nb, &written, nullptr) || nb != written)
            return EXCEPTION_CONTINUE_SEARCH;
    }

    if (const auto nb = static_cast<DWORD>(std::span(g_startInfo.TroubleshootingPackData).size_bytes()))
    {
        if (DWORD written; !WriteFile(g_crashhandler_pipe_write, g_startInfo.TroubleshootingPackData.data(), nb, &written, nullptr) || nb != written)
            return EXCEPTION_CONTINUE_SEARCH;
    }

    AllowSetForegroundWindow(GetProcessId(g_crashhandler_process));

    HANDLE waitHandles[] = { g_crashhandler_process, g_crashhandler_event };
    DWORD waitResult = WaitForMultipleObjects(2, waitHandles, FALSE, INFINITE);

    switch (waitResult) {
    case WAIT_OBJECT_0:
        logging::E("DalamudCrashHandler.exe exited unexpectedly");
        break;
    case WAIT_OBJECT_0 + 1:
        logging::I("Crashing thread was resumed");
        break;
    default:
        logging::E("Unexpected WaitForMultipleObjects return code 0x{:x}", waitResult);
    }

    return EXCEPTION_CONTINUE_SEARCH;
}

LONG WINAPI structured_exception_handler(EXCEPTION_POINTERS* ex)
{
    return exception_handler(ex);
}

LONG WINAPI vectored_exception_handler(EXCEPTION_POINTERS* ex)
{
    if (ex->ExceptionRecord->ExceptionCode == 0x12345678)
    {
        // pass
    }
    else
    {
        if (!is_whitelist_exception(ex->ExceptionRecord->ExceptionCode))
            return EXCEPTION_CONTINUE_SEARCH;

        if (!is_ffxiv_address(L"ffxiv_dx11.exe", ex->ContextRecord->Rip) &&
            !is_ffxiv_address(L"cimgui.dll", ex->ContextRecord->Rip))
            return EXCEPTION_CONTINUE_SEARCH;   
    }

    return exception_handler(ex);
}

bool veh::add_handler(bool doFullDump, const std::string& workingDirectory)
{
    if (g_veh_handle)
        return false;

    g_veh_handle = AddVectoredExceptionHandler(TRUE, vectored_exception_handler);

    g_HookSetUnhandledExceptionFilter.emplace("kernel32.dll!SetUnhandledExceptionFilter (lpTopLevelExceptionFilter)", "kernel32.dll", "SetUnhandledExceptionFilter", 0);
    g_HookSetUnhandledExceptionFilter->set_detour([](LPTOP_LEVEL_EXCEPTION_FILTER lpTopLevelExceptionFilter) -> LPTOP_LEVEL_EXCEPTION_FILTER
    {
        logging::I("Overwriting UnhandledExceptionFilter from {} to {}", reinterpret_cast<ULONG_PTR>(lpTopLevelExceptionFilter), reinterpret_cast<ULONG_PTR>(structured_exception_handler));
        return g_HookSetUnhandledExceptionFilter->call_original(structured_exception_handler);
    });
    SetUnhandledExceptionFilter(structured_exception_handler);

    g_veh_do_full_dump = doFullDump;
    g_time_start = std::chrono::system_clock::now();

    std::optional<std::unique_ptr<std::remove_pointer_t<HANDLE>, decltype(&CloseHandle)>> hWritePipe;
    std::optional<std::unique_ptr<std::remove_pointer_t<HANDLE>, decltype(&CloseHandle)>> hReadPipeInheritable;
    if (HANDLE hReadPipeRaw, hWritePipeRaw; CreatePipe(&hReadPipeRaw, &hWritePipeRaw, nullptr, 65536))
    {
        hWritePipe.emplace(hWritePipeRaw, &CloseHandle);
        
        if (HANDLE hReadPipeInheritableRaw; DuplicateHandle(GetCurrentProcess(), hReadPipeRaw, GetCurrentProcess(), &hReadPipeInheritableRaw, 0, TRUE, DUPLICATE_SAME_ACCESS | DUPLICATE_CLOSE_SOURCE))
        {
            hReadPipeInheritable.emplace(hReadPipeInheritableRaw, &CloseHandle);
        }
        else
        {
            logging::W("Failed to launch DalamudCrashHandler.exe: DuplicateHandle(1) error 0x{:x}", GetLastError());
            return false;
        }
    }
    else
    {
        logging::W("Failed to launch DalamudCrashHandler.exe: CreatePipe error 0x{:x}", GetLastError());
        return false;
    }

    // additional information
    STARTUPINFOEXW siex{};     
    PROCESS_INFORMATION pi{};
    
    siex.StartupInfo.cb = sizeof siex;
    siex.StartupInfo.dwFlags = STARTF_USESHOWWINDOW;
    siex.StartupInfo.wShowWindow = g_startInfo.CrashHandlerShow ? SW_SHOW : SW_HIDE;

    // set up list of handles to inherit to child process
    std::vector<char> attributeListBuf;
    if (SIZE_T size = 0; !InitializeProcThreadAttributeList(nullptr, 1, 0, &size))
    {
        if (const auto err = GetLastError(); err != ERROR_INSUFFICIENT_BUFFER)
        {
            logging::W("Failed to launch DalamudCrashHandler.exe: InitializeProcThreadAttributeList(1) error 0x{:x}", err);
            return false;
        }

        attributeListBuf.resize(size);
        siex.lpAttributeList = reinterpret_cast<PPROC_THREAD_ATTRIBUTE_LIST>(&attributeListBuf[0]);
        if (!InitializeProcThreadAttributeList(siex.lpAttributeList, 1, 0, &size))
        {
            logging::W("Failed to launch DalamudCrashHandler.exe: InitializeProcThreadAttributeList(2) error 0x{:x}", GetLastError());
            return false;
        }
    }
    else
    {
        logging::W("Failed to launch DalamudCrashHandler.exe: InitializeProcThreadAttributeList(0) was supposed to fail");
        return false;
    }
    std::unique_ptr<std::remove_pointer_t<LPPROC_THREAD_ATTRIBUTE_LIST>, decltype(&DeleteProcThreadAttributeList)> cleanAttributeList(siex.lpAttributeList, &DeleteProcThreadAttributeList);

	std::vector<HANDLE> handles;

    HANDLE hInheritableCurrentProcess;
    if (!DuplicateHandle(GetCurrentProcess(), GetCurrentProcess(), GetCurrentProcess(), &hInheritableCurrentProcess, 0, TRUE, DUPLICATE_SAME_ACCESS))
    {
        logging::W("Failed to launch DalamudCrashHandler.exe: DuplicateHandle(2) error 0x{:x}", GetLastError());
        return false;
    }
    handles.push_back(hInheritableCurrentProcess);
    handles.push_back(hReadPipeInheritable->get());

    std::vector<std::wstring> args;
    std::wstring argstr;
    args.emplace_back((std::filesystem::path(unicode::convert<std::wstring>(workingDirectory)) / L"DalamudCrashHandler.exe").wstring());
    args.emplace_back(std::format(L"--process-handle={}", reinterpret_cast<size_t>(hInheritableCurrentProcess)));
    args.emplace_back(std::format(L"--exception-info-pipe-read-handle={}", reinterpret_cast<size_t>(hReadPipeInheritable->get())));
    args.emplace_back(std::format(L"--asset-directory={}", unicode::convert<std::wstring>(g_startInfo.AssetDirectory)));
    args.emplace_back(std::format(L"--log-directory={}", g_startInfo.BootLogPath.empty()
        ? utils::loaded_module(g_hModule).path().parent_path().wstring()
        : std::filesystem::path(unicode::convert<std::wstring>(g_startInfo.BootLogPath)).parent_path().wstring()));
    args.emplace_back(L"--");
    append_injector_launch_args(args);

    for (const auto& arg : args)
    {
        argstr.append(utils::escape_shell_arg(arg));
        argstr.push_back(L' ');
    }
    argstr.pop_back();
    
    if (!handles.empty() && !UpdateProcThreadAttribute(siex.lpAttributeList, 0, PROC_THREAD_ATTRIBUTE_HANDLE_LIST, &handles[0], std::span(handles).size_bytes(), nullptr, nullptr))
    {
        logging::W("Failed to launch DalamudCrashHandler.exe: UpdateProcThreadAttribute error 0x{:x}", GetLastError());
        return false;
    }

    if (!CreateProcessW(
        args[0].c_str(),               // The path
        &argstr[0],                    // Command line
        nullptr,                       // Process handle not inheritable
        nullptr,                       // Thread handle not inheritable
        TRUE,                          // Set handle inheritance to FALSE
        EXTENDED_STARTUPINFO_PRESENT,  // lpStartupInfo actually points to a STARTUPINFOEX(W)
        nullptr,                       // Use parent's environment block
        nullptr,                       // Use parent's starting directory 
        &siex.StartupInfo,             // Pointer to STARTUPINFO structure
        &pi                            // Pointer to PROCESS_INFORMATION structure (removed extra parentheses)
        ))
    {
        logging::W("Failed to launch DalamudCrashHandler.exe: CreateProcessW error 0x{:x}", GetLastError());
        return false;
    }

    if (!(g_crashhandler_event = CreateEventW(NULL, FALSE, FALSE, NULL)))
    {
        logging::W("Failed to create crash synchronization event: CreateEventW error 0x{:x}", GetLastError());
        return false;
    }

    CloseHandle(pi.hThread);
    
    g_crashhandler_process = pi.hProcess;
    g_crashhandler_pipe_write = hWritePipe->release();
    logging::I("Launched DalamudCrashHandler.exe: PID {}", pi.dwProcessId);
    return true;
}

bool veh::remove_handler()
{
    if (g_veh_handle && RemoveVectoredExceptionHandler(g_veh_handle) != 0)
    {
        g_veh_handle = nullptr;
        g_HookSetUnhandledExceptionFilter.reset();
        SetUnhandledExceptionFilter(nullptr);
        return true;
    }
    return false;
}

#include <filesystem>
#include <fstream>
#include <iostream>
#include <map>
#include <optional>
#include <ranges>
#include <span>
#include <sstream>
#include <string>
#include <thread>
#include <vector>

#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <Windows.h>

#include <CommCtrl.h>
#include <DbgHelp.h>
#include <minidumpapiset.h>
#include <PathCch.h>
#include <Psapi.h>
#include <shellapi.h>
#include <winhttp.h>

#pragma comment(lib, "comctl32.lib")
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")

#include "resource.h"
#include "../Dalamud.Boot/crashhandler_shared.h"

HANDLE g_hProcess = nullptr;
bool g_bSymbolsAvailable = false;

const std::map<HMODULE, size_t>& get_remote_modules() {
    static const auto data = [] {
        std::map<HMODULE, size_t> data;

        std::vector<HMODULE> buf(8192);
        for (size_t i = 0; i < 64; i++) {
            if (DWORD needed; !EnumProcessModules(g_hProcess, &buf[0], static_cast<DWORD>(std::span(buf).size_bytes()), &needed)) {
                std::cerr << std::format("EnumProcessModules error: 0x{:x}", GetLastError()) << std::endl; 
                break;
            } else if (needed > std::span(buf).size_bytes()) {
                buf.resize(needed / sizeof(HMODULE) + 16);
            } else {
                buf.resize(needed / sizeof(HMODULE));
                break;
            }
        }

        for (const auto& hModule : buf) {
            IMAGE_DOS_HEADER dosh;
            IMAGE_NT_HEADERS64 nth64;
            if (size_t read; !ReadProcessMemory(g_hProcess, hModule, &dosh, sizeof dosh, &read) || read != sizeof dosh) {
                std::cerr << std::format("Failed to read IMAGE_DOS_HEADER for module at 0x{:x}", reinterpret_cast<size_t>(hModule)) << std::endl;
                continue;
            }

            if (size_t read; !ReadProcessMemory(g_hProcess, reinterpret_cast<const char*>(hModule) + dosh.e_lfanew, &nth64, sizeof nth64, &read) || read != sizeof nth64) {
                std::cerr << std::format("Failed to read IMAGE_NT_HEADERS64 for module at 0x{:x}", reinterpret_cast<size_t>(hModule)) << std::endl;
                continue;
            }

            data[hModule] = nth64.OptionalHeader.SizeOfImage;
        }
        
        return data;
    }();

    return data;
}

const std::map<HMODULE, std::filesystem::path>& get_remote_module_paths() {
    static const auto data = [] {
        std::map<HMODULE, std::filesystem::path> data;

        std::wstring buf(PATHCCH_MAX_CCH, L'\0');
        for (const auto& hModule : get_remote_modules() | std::views::keys) {
            buf.resize(PATHCCH_MAX_CCH, L'\0');
            buf.resize(GetModuleFileNameExW(g_hProcess, hModule, &buf[0], PATHCCH_MAX_CCH));
            if (buf.empty()) {
                std::cerr << std::format("Failed to get path for module at 0x{:x}: error 0x{:x}", reinterpret_cast<size_t>(hModule), GetLastError()) << std::endl;
                continue;
            }

            data[hModule] = buf;
        }

        return data;
    }();
    return data;
}

bool get_module_file_and_base(const DWORD64 address, DWORD64& module_base, std::filesystem::path& module_file) {
    for (const auto& [hModule, path] : get_remote_module_paths()) {
        const auto nAddress = reinterpret_cast<DWORD64>(hModule);
        if (address < nAddress)
            continue;

        const auto nAddressTo = nAddress + get_remote_modules().at(hModule);
        if (nAddressTo <= address)
            continue;

        module_base = nAddress;
        module_file = path;
        return true;
    }

    return false;
}

bool is_ffxiv_address(const wchar_t* module_name, const DWORD64 address) {
    DWORD64 module_base;
    if (std::filesystem::path module_path; get_module_file_and_base(address, module_base, module_path))
        return _wcsicmp(module_path.filename().c_str(), module_name) == 0;
    return false;
}

bool get_sym_from_addr(const DWORD64 address, DWORD64& displacement, std::wstring& symbol_name) {
    if (!g_bSymbolsAvailable)
        return false;

    union {
        char buffer[sizeof(SYMBOL_INFOW) + MAX_SYM_NAME * sizeof(wchar_t)]{};
        SYMBOL_INFOW symbol;
    };
    symbol.SizeOfStruct = sizeof(SYMBOL_INFO);
    symbol.MaxNameLen = MAX_SYM_NAME;

    if (SymFromAddrW(g_hProcess, address, &displacement, &symbol) && symbol.Name[0]) {
        symbol_name = symbol.Name;
        return true;
    }
    return false;
}

std::wstring to_address_string(const DWORD64 address, const bool try_ptrderef = true) {
    DWORD64 module_base;
    std::filesystem::path module_path;
    bool is_mod_addr = get_module_file_and_base(address, module_base, module_path);

    DWORD64 value = 0;
    if (try_ptrderef && address > 0x10000 && address < 0x7FFFFFFE0000) {
        ReadProcessMemory(g_hProcess, reinterpret_cast<void*>(address), &value, sizeof value, nullptr);
    }

    std::wstring addr_str = is_mod_addr ? std::format(L"{}+{:X}", module_path.filename().c_str(), address - module_base) : std::format(L"{:X}", address);

    DWORD64 displacement;
    if (std::wstring symbol; get_sym_from_addr(address, displacement, symbol))
        return std::format(L"{}\t({})", addr_str, displacement != 0 ? std::format(L"{}+0x{:X}", symbol, displacement) : std::format(L"{}", symbol));
    return value != 0 ? std::format(L"{} [{}]", addr_str, to_address_string(value, false)) : addr_str;
}

void print_exception_info(HANDLE hThread, const EXCEPTION_POINTERS& ex, const CONTEXT& ctx, std::wostringstream& log) {
    std::vector<EXCEPTION_RECORD> exRecs;
    if (ex.ExceptionRecord) {
        size_t rec_index = 0;
        size_t read;
        exRecs.emplace_back();
        for (auto pRemoteExRec = ex.ExceptionRecord;
             pRemoteExRec
             && rec_index < 64
             && ReadProcessMemory(g_hProcess, pRemoteExRec, &exRecs.back(), sizeof exRecs.back(), &read)
             && read >= offsetof(EXCEPTION_RECORD, ExceptionInformation)
             && read >= static_cast<size_t>(reinterpret_cast<const char*>(&exRecs.back().ExceptionInformation[exRecs.back().NumberParameters]) - reinterpret_cast<const char*>(&exRecs.back()));
             rec_index++) {

            log << std::format(L"\nException Info #{}\n", rec_index);
            log << std::format(L"Address: {:X}\n", exRecs.back().ExceptionCode);
            log << std::format(L"Flags: {:X}\n", exRecs.back().ExceptionFlags);
            log << std::format(L"Address: {:X}\n", reinterpret_cast<size_t>(exRecs.back().ExceptionAddress));
            if (!exRecs.back().NumberParameters)
                continue;
            log << L"Parameters: ";
            for (DWORD i = 0; i < exRecs.back().NumberParameters; ++i) {
                if (i != 0)
                    log << L", ";
                log << std::format(L"{:X}", exRecs.back().ExceptionInformation[i]);
            }

            pRemoteExRec = exRecs.back().ExceptionRecord;
            exRecs.emplace_back();
        }
        exRecs.pop_back();
    }

    log << L"\nCall Stack\n{";

    STACKFRAME64 sf{};
    sf.AddrPC.Offset = ctx.Rip;
    sf.AddrPC.Mode = AddrModeFlat;
    sf.AddrStack.Offset = ctx.Rsp;
    sf.AddrStack.Mode = AddrModeFlat;
    sf.AddrFrame.Offset = ctx.Rbp;
    sf.AddrFrame.Mode = AddrModeFlat;
    int frame_index = 0;

    log << std::format(L"\n  [{}]\t{}", frame_index++, to_address_string(sf.AddrPC.Offset, false));

    const auto appendContextToLog = [&](const CONTEXT& ctxWalk) {
        log << std::format(L"\n  [{}]\t{}", frame_index++, to_address_string(sf.AddrPC.Offset, false));
    };

    const auto tryStackWalk = [&] {
        __try {
            CONTEXT ctxWalk = ctx;
            do {
                if (!StackWalk64(IMAGE_FILE_MACHINE_AMD64, g_hProcess, hThread, &sf, &ctxWalk, nullptr, &SymFunctionTableAccess64, &SymGetModuleBase64, nullptr))
                    break;

                appendContextToLog(ctxWalk);

            } while (sf.AddrReturn.Offset != 0 && sf.AddrPC.Offset != sf.AddrReturn.Offset);
            return true;
        } __except(EXCEPTION_EXECUTE_HANDLER) {
            return false;
        }
    };

    if (!tryStackWalk())
        log << L"\n  Access violation while walking up the stack.";

    log << L"\n}\n";
}

void print_exception_info_extended(const EXCEPTION_POINTERS& ex, const CONTEXT& ctx, std::wostringstream& log)
{
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

        DWORD64 stackData[16];
        size_t read;
        ReadProcessMemory(g_hProcess, reinterpret_cast<void*>(ctx.Rsp), stackData, sizeof stackData, &read);
        for(DWORD64 i = 0; i < 16 && i * sizeof(size_t) < read; i++)
            log << std::format(L"\n  [RSP+{:X}]\t{}", i * 8, to_address_string(stackData[i]));

        log << L"\n}\n";
    }

    log << L"\nModules\n{";

    for (const auto& [hModule, path] : get_remote_module_paths())
        log << std::format(L"\n  {:08X}\t{}", reinterpret_cast<DWORD64>(hModule), path.wstring());

    log << L"\n}\n";
}

std::wstring escape_shell_arg(const std::wstring& arg) {
    // https://docs.microsoft.com/en-us/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way
    
    std::wstring res;
    if (!arg.empty() && arg.find_first_of(L" \t\n\v\"") == std::wstring::npos) {
        res.append(arg);
    } else {
        res.push_back(L'"');
        for (auto it = arg.begin(); ; ++it) {
            size_t bsCount = 0;

            while (it != arg.end() && *it == L'\\') {
                ++it;
                ++bsCount;
            }

            if (it == arg.end()) {
                res.append(bsCount * 2, L'\\');
                break;
            } else if (*it == L'"') {
                res.append(bsCount * 2 + 1, L'\\');
                res.push_back(*it);
            } else {
                res.append(bsCount, L'\\');
                res.push_back(*it);
            }
        }

        res.push_back(L'"');
    }
    return res;
}

enum {
    IdRadioRestartNormal = 101,
    IdRadioRestartWithout3pPlugins,
    IdRadioRestartWithoutPlugins,
    IdRadioRestartWithoutDalamud,

    IdButtonRestart = 201,
    IdButtonHelp = IDHELP,
    IdButtonExit = IDCANCEL,
};

void restart_game_using_injector(int nRadioButton, const std::vector<std::wstring>& launcherArgs)
{
    std::wstring pathStr(PATHCCH_MAX_CCH, L'\0');
    pathStr.resize(GetModuleFileNameExW(GetCurrentProcess(), GetModuleHandleW(nullptr), &pathStr[0], PATHCCH_MAX_CCH));

    std::vector<std::wstring> args;
    args.emplace_back((std::filesystem::path(pathStr).parent_path() / L"Dalamud.Injector.exe").wstring());
    args.emplace_back(L"launch");
    switch (nRadioButton) {
        case IdRadioRestartWithout3pPlugins:
            args.emplace_back(L"--no-3rd-plugin");
        break;
        case IdRadioRestartWithoutPlugins:
            args.emplace_back(L"--no-plugin");
        break;
        case IdRadioRestartWithoutDalamud:
            args.emplace_back(L"--without-dalamud");
        break;
    }
    args.emplace_back(L"--");
    args.insert(args.end(), launcherArgs.begin(), launcherArgs.end());

    std::wstring argstr;
    for (const auto& arg : args) {
        argstr.append(escape_shell_arg(arg));
        argstr.push_back(L' ');
    }
    argstr.pop_back();

    STARTUPINFOW si{};
    si.cb = sizeof si;
    si.dwFlags = STARTF_USESHOWWINDOW;
#ifndef NDEBUG
    si.wShowWindow = SW_HIDE;
#else
    si.wShowWindow = SW_SHOW;
#endif
    PROCESS_INFORMATION pi{};
    if (CreateProcessW(args[0].c_str(), &argstr[0], nullptr, nullptr, FALSE, 0, nullptr, nullptr, &si, &pi)) {
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    } else {
        MessageBoxW(nullptr, std::format(L"Failed to restart: 0x{:x}", GetLastError()).c_str(), L"Dalamud Boot", MB_ICONERROR | MB_OK);
    }
}

int main() {
    enum crash_handler_special_exit_codes {
        InvalidParameter = -101,
        ProcessExitedUnknownExitCode = -102,
    };

    HANDLE hPipeRead = nullptr;
    std::filesystem::path assetDir, logDir;
    std::optional<std::vector<std::wstring>> launcherArgs;
    
    std::vector<std::wstring> args;
    if (int argc = 0; const auto argv = CommandLineToArgvW(GetCommandLineW(), &argc)) {
        for (auto i = 0; i < argc; i++)
            args.emplace_back(argv[i]);
        LocalFree(argv);
    }
    for (size_t i = 1; i < args.size(); i++) {
        const auto arg = std::wstring_view(args[i]);
        if (launcherArgs) {
            launcherArgs->emplace_back(arg);
        } else if (constexpr wchar_t pwszArgPrefix[] = L"--process-handle="; arg.starts_with(pwszArgPrefix)) {
            g_hProcess = reinterpret_cast<HANDLE>(std::wcstoull(&arg[ARRAYSIZE(pwszArgPrefix) - 1], nullptr, 0));
        } else if (constexpr wchar_t pwszArgPrefix[] = L"--exception-info-pipe-read-handle="; arg.starts_with(pwszArgPrefix)) {
            hPipeRead = reinterpret_cast<HANDLE>(std::wcstoull(&arg[ARRAYSIZE(pwszArgPrefix) - 1], nullptr, 0));
        } else if (constexpr wchar_t pwszArgPrefix[] = L"--asset-directory="; arg.starts_with(pwszArgPrefix)) {
            assetDir = arg.substr(ARRAYSIZE(pwszArgPrefix) - 1);
        } else if (constexpr wchar_t pwszArgPrefix[] = L"--log-directory="; arg.starts_with(pwszArgPrefix)) {
            logDir = arg.substr(ARRAYSIZE(pwszArgPrefix) - 1);
        } else if (arg == L"--") {
            launcherArgs.emplace();
        } else {
            std::wcerr << L"Invalid argument: " << arg << std::endl;
            return InvalidParameter;
        }
    }

    if (g_hProcess == nullptr) {
        std::wcerr << L"Target process not specified" << std::endl;
        return InvalidParameter;
    }

    if (hPipeRead == nullptr) {
        std::wcerr << L"Read pipe handle not specified" << std::endl;
        return InvalidParameter;
    }

    const auto dwProcessId = GetProcessId(g_hProcess);
    if (!dwProcessId){
        std::wcerr << L"Target process not specified" << std::endl;
        return InvalidParameter;
    }

    while (true) {
        std::cout << "Waiting for crash...\n";

        exception_info exinfo;
        if (DWORD exsize{}; !ReadFile(hPipeRead, &exinfo, static_cast<DWORD>(sizeof exinfo), &exsize, nullptr) || exsize != sizeof exinfo) {
            if (WaitForSingleObject(g_hProcess, 0) == WAIT_OBJECT_0) {
                auto excode = static_cast<DWORD>(ProcessExitedUnknownExitCode);
                if (!GetExitCodeProcess(g_hProcess, &excode))
                    std::cerr << std::format("Process exited, but failed to read exit code; error: 0x{:x}", GetLastError()) << std::endl;
                else
                    std::cout << std::format("Process exited with exit code {0} (0x{0:x})", excode) << std::endl;
                break;
            }

            const auto err = GetLastError();
            std::cerr << std::format("Failed to read exception information; error: 0x{:x}", err) << std::endl;
            std::cerr << "Terminating target process." << std::endl;
            TerminateProcess(g_hProcess, -1);
            break;
        }

        if (exinfo.ExceptionRecord.ExceptionCode == 0x12345678) {
            std::cout << "Restart requested" << std::endl;
            TerminateProcess(g_hProcess, 0);
            restart_game_using_injector(IdRadioRestartNormal, *launcherArgs);
            break;
        }

        std::cout << "Crash triggered" << std::endl;

        if (g_bSymbolsAvailable) {
            SymRefreshModuleList(g_hProcess);
        } else if (g_bSymbolsAvailable = SymInitialize(g_hProcess, nullptr, true); g_bSymbolsAvailable) {
            if (!assetDir.empty()) {
                if (!SymSetSearchPathW(g_hProcess, std::format(L".;{}", (assetDir / "UIRes" / "pdb").wstring()).c_str()))
                    std::wcerr << std::format(L"SymSetSearchPathW error: 0x{:x}", GetLastError()) << std::endl;
            }
        } else {
            std::wcerr << std::format(L"SymInitialize error: 0x{:x}", GetLastError()) << std::endl;
        }

        std::wstring stackTrace(exinfo.dwStackTraceLength, L'\0');
        if (exinfo.dwStackTraceLength) {
            if (DWORD read; !ReadFile(hPipeRead, &stackTrace[0], 2 * exinfo.dwStackTraceLength, &read, nullptr)) {
                std::cout << std::format("Failed to read supplied stack trace: error 0x{:x}", GetLastError()) << std::endl;
            }
        }

        SYSTEMTIME st;
        GetLocalTime(&st);
        const auto dumpPath = logDir.empty() ? std::filesystem::path() : logDir / std::format("dalamud_appcrash_{:04}{:02}{:02}_{:02}{:02}{:02}_{:03}_{}.dmp", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, dwProcessId);
        const auto logPath = logDir.empty() ? std::filesystem::path() : logDir / std::format("dalamud_appcrash_{:04}{:02}{:02}_{:02}{:02}{:02}_{:03}_{}.log", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, dwProcessId);
        std::wstring dumpError;
        if (dumpPath.empty()) {
            std::cout << "Skipping dump path, as log directory has not been specified" << std::endl;
        } else {
            MINIDUMP_EXCEPTION_INFORMATION mdmp_info{};
            mdmp_info.ThreadId = GetThreadId(exinfo.hThreadHandle);
            mdmp_info.ExceptionPointers = exinfo.pExceptionPointers;
            mdmp_info.ClientPointers = TRUE;

            do {
                const auto hDumpFile = CreateFileW(dumpPath.c_str(), GENERIC_READ | GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, 0, nullptr);
                if (hDumpFile == INVALID_HANDLE_VALUE) {
                    std::wcerr << (dumpError = std::format(L"CreateFileW({}, GENERIC_READ | GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, 0, nullptr) error: 0x{:x}", dumpPath.wstring(), GetLastError())) << std::endl;
                    break;
                }

                std::unique_ptr<std::remove_pointer_t<HANDLE>, decltype(&CloseHandle)> hDumpFilePtr(hDumpFile, &CloseHandle);
                if (!MiniDumpWriteDump(g_hProcess, dwProcessId, hDumpFile, static_cast<MINIDUMP_TYPE>(MiniDumpWithDataSegs | MiniDumpWithModuleHeaders), &mdmp_info, nullptr, nullptr)) {
                    std::wcerr << (dumpError = std::format(L"MiniDumpWriteDump(0x{:x}, {}, 0x{:x}({}), MiniDumpWithFullMemory, ..., nullptr, nullptr) error: 0x{:x}", reinterpret_cast<size_t>(g_hProcess), dwProcessId, reinterpret_cast<size_t>(hDumpFile), dumpPath.wstring(), GetLastError())) << std::endl;
                    break;
                }

                std::wcout << "Dump written to path: " << dumpPath << std::endl;
            } while (false);
        }

        std::wostringstream log;
        log << std::format(L"Unhandled native exception occurred at {}", to_address_string(exinfo.ContextRecord.Rip, false)) << std::endl;
        log << std::format(L"Code: {:X}", exinfo.ExceptionRecord.ExceptionCode) << std::endl;
        if (dumpPath.empty())
            log << L"Dump skipped" << std::endl;
        else if (dumpError.empty())
            log << std::format(L"Dump at: {}", dumpPath.wstring()) << std::endl;
        else
            log << std::format(L"Dump error: {}", dumpError) << std::endl;
        log << L"Time: " << std::chrono::zoned_time{ std::chrono::current_zone(), std::chrono::system_clock::now() } << std::endl;
        log << L"\n" << stackTrace << std::endl;

        SymRefreshModuleList(GetCurrentProcess());
        print_exception_info(exinfo.hThreadHandle, exinfo.ExceptionPointers, exinfo.ContextRecord, log);
        auto window_log_str = log.str();
        print_exception_info_extended(exinfo.ExceptionPointers, exinfo.ContextRecord, log);

        std::wofstream(logPath) << log.str();

        std::thread submitThread;
        if (!getenv("DALAMUD_NO_METRIC")) {
            auto url = std::format(L"/Dalamud/Metric/ReportCrash/?lt={}&code={:x}", exinfo.nLifetime, exinfo.ExceptionRecord.ExceptionCode);

            submitThread = std::thread([url = std::move(url)] {
                const auto hInternet = WinHttpOpen(L"DALAMUDCRASHHANDLER", WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY, nullptr, nullptr, WINHTTP_FLAG_SECURE_DEFAULTS);
                const auto hConnect = !hInternet ? nullptr : WinHttpConnect(hInternet, L"kamori.goats.dev", INTERNET_DEFAULT_HTTPS_PORT, 0);
                const auto hRequest = !hConnect ? nullptr : WinHttpOpenRequest(hConnect, L"GET", url.c_str(), nullptr, nullptr, nullptr, 0);
                const auto bSent = !hRequest ? false : WinHttpSendRequest(hRequest,
                    WINHTTP_NO_ADDITIONAL_HEADERS,
                    0, WINHTTP_NO_REQUEST_DATA, 0,
                    0, 0);

                if (!bSent)
                    std::cerr << std::format("Failed to send metric: 0x{:x}", GetLastError()) << std::endl;

                if (hRequest) WinHttpCloseHandle(hRequest);
                if (hConnect) WinHttpCloseHandle(hConnect);
                if (hInternet) WinHttpCloseHandle(hInternet);
            });
        }

        TASKDIALOGCONFIG config = { 0 };

        const TASKDIALOG_BUTTON radios[]{
            {IdRadioRestartNormal, L"Restart"},
            {IdRadioRestartWithout3pPlugins, L"Restart without 3rd party plugins"},
            {IdRadioRestartWithoutPlugins, L"Restart without any plugins"},
            {IdRadioRestartWithoutDalamud, L"Restart without Dalamud"},
        };

        const TASKDIALOG_BUTTON buttons[]{
            {IdButtonRestart, L"Restart\nRestart the game, optionally without plugins or Dalamud."},
            {IdButtonExit, L"Exit\nExit the game."},
        };

        config.cbSize = sizeof(config);
        config.hInstance = GetModuleHandleW(nullptr);
        config.dwFlags = TDF_ENABLE_HYPERLINKS | TDF_CAN_BE_MINIMIZED | TDF_ALLOW_DIALOG_CANCELLATION | TDF_USE_COMMAND_LINKS;
        config.pszMainIcon = MAKEINTRESOURCE(IDI_ICON1);
        config.pszMainInstruction = L"An error occurred";
        config.pszContent = (L""
            R"aa(This may be caused by a faulty plugin, a broken TexTools modification, any other third-party tool, or simply a bug in the game.)aa" "\n"
            "\n"
            R"aa(Try running integrity check in the XIVLauncher settings, and disabling plugins you don't need.)aa"
        );
        config.pButtons = buttons;
        config.cButtons = ARRAYSIZE(buttons);
        config.nDefaultButton = IdButtonRestart;
        config.pszExpandedInformation = window_log_str.c_str();
        config.pszWindowTitle = L"Dalamud Error";
        config.pRadioButtons = radios;
        config.cRadioButtons = ARRAYSIZE(radios);
        config.nDefaultRadioButton = IdRadioRestartNormal;
        config.cxWidth = 300;
        config.pszFooter = (L""
            R"aa(<a href="help">Help</a> | <a href="logdir">Open log directory</a> | <a href="logfile">Open log file</a> | <a href="resume">Attempt to resume</a>)aa"
        );

        // Can't do this, xiv stops pumping messages here
        //config.hwndParent = FindWindowA("FFXIVGAME", NULL);

        auto attemptResume = false;
        const auto callback = [&](HWND hwnd, UINT uNotification, WPARAM wParam, LPARAM lParam) -> HRESULT {
            switch (uNotification) {
                case TDN_CREATED:
                {
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    return S_OK;
                }
                case TDN_HYPERLINK_CLICKED:
                {
                    const auto link = std::wstring_view(reinterpret_cast<const wchar_t*>(lParam));
                    if (link == L"help") {
                        ShellExecuteW(hwnd, nullptr, L"https://goatcorp.github.io/faq?utm_source=vectored", nullptr, nullptr, SW_SHOW);
                    } else if (link == L"logdir") {
                        ShellExecuteW(hwnd, nullptr, L"explorer.exe", escape_shell_arg(std::format(L"/select,{}", logPath.wstring())).c_str(), nullptr, SW_SHOW);
                    } else if (link == L"logfile") {
                        ShellExecuteW(hwnd, nullptr, logPath.c_str(), nullptr, nullptr, SW_SHOW);
                    } else if (link == L"resume") {
                        attemptResume = true;
                        DestroyWindow(hwnd);
                    }
                    return S_OK;
                }
            }

            return S_OK;
        };

        config.pfCallback = [](HWND hwnd, UINT uNotification, WPARAM wParam, LPARAM lParam, LONG_PTR dwRefData) {
            return (*reinterpret_cast<decltype(callback)*>(dwRefData))(hwnd, uNotification, wParam, lParam);
        };
        config.lpCallbackData = reinterpret_cast<LONG_PTR>(&callback);

        if (submitThread.joinable()) {
            submitThread.join();
            submitThread = {};
        }

        int nButtonPressed = 0, nRadioButton = 0;
        if (FAILED(TaskDialogIndirect(&config, &nButtonPressed, &nRadioButton, nullptr))) {
            ResumeThread(exinfo.hThreadHandle);
        } else {
            switch (nButtonPressed) {
                case IdButtonRestart:
                {
                    TerminateProcess(g_hProcess, exinfo.ExceptionRecord.ExceptionCode);
                    restart_game_using_injector(nRadioButton, *launcherArgs);
                    break;
                }
                default:
                    if (attemptResume)
                        ResumeThread(exinfo.hThreadHandle);
                    else
                        TerminateProcess(g_hProcess, exinfo.ExceptionRecord.ExceptionCode);
            }
        }
    }

    return 0;
}

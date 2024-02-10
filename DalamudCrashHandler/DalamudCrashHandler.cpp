#include <array>
#include <chrono>
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

#include <comdef.h>
#include <CommCtrl.h>
#include <DbgHelp.h>
#include <minidumpapiset.h>
#include <PathCch.h>
#include <Psapi.h>
#include <shellapi.h>
#include <ShlGuid.h>
#include <ShObjIdl.h>
#include <winhttp.h>

#pragma comment(lib, "comctl32.lib")
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")

_COM_SMARTPTR_TYPEDEF(IFileOperation, __uuidof(IFileOperation));
_COM_SMARTPTR_TYPEDEF(IFileSaveDialog, __uuidof(IFileSaveDialog));
_COM_SMARTPTR_TYPEDEF(IShellItem, __uuidof(IShellItem));
_COM_SMARTPTR_TYPEDEF(IBindCtx, __uuidof(IBindCtx));
_COM_SMARTPTR_TYPEDEF(IStream, __uuidof(IStream));

static constexpr GUID Guid_IFileDialog_Tspack{ 0xfc057318, 0xad35, 0x4599, {0xa7, 0x68, 0xdd, 0xaf, 0x70, 0xbe, 0x98, 0x75} };

#include "resource.h"
#include "../Dalamud.Boot/crashhandler_shared.h"
#include "miniz.h"

HANDLE g_hProcess = nullptr;
bool g_bSymbolsAvailable = false;

std::string ws_to_u8(const std::wstring& ws) {
    std::string s(WideCharToMultiByte(CP_UTF8, 0, ws.data(), static_cast<int>(ws.size()), nullptr, 0, nullptr, nullptr), '\0');
    WideCharToMultiByte(CP_UTF8, 0, ws.data(), static_cast<int>(ws.size()), s.data(), static_cast<int>(s.size()), nullptr, nullptr);
    return s;
}

std::wstring u8_to_ws(const std::string& s) {
    std::wstring ws(MultiByteToWideChar(CP_UTF8, 0, s.data(), static_cast<int>(s.size()), nullptr, 0), '\0');
    MultiByteToWideChar(CP_UTF8, 0, s.data(), static_cast<int>(s.size()), ws.data(), static_cast<int>(ws.size()));
    return ws;
}

std::wstring get_window_string(HWND hWnd) {
    std::wstring buf(GetWindowTextLengthW(hWnd), L'\0');
    GetWindowTextW(hWnd, &buf[0], static_cast<int>(buf.size()));
    return buf;
}

[[noreturn]]
void throw_hresult(HRESULT hr, const std::string& clue = {}) {
    wchar_t* pwszMsg = nullptr;
    FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER |
        FORMAT_MESSAGE_FROM_SYSTEM |
        FORMAT_MESSAGE_IGNORE_INSERTS,
        nullptr,
        hr,
        MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_US),
        reinterpret_cast<LPWSTR>(&pwszMsg),
        0,
        nullptr);
    if (!pwszMsg) {
        if (clue.empty())
            throw std::runtime_error(std::format("Error (HRESULT=0x{:08X})", static_cast<uint32_t>(hr)));
        else
            throw std::runtime_error(std::format("Error at {} (HRESULT=0x{:08X})", clue, static_cast<uint32_t>(hr)));
    }

    std::unique_ptr<wchar_t, decltype(LocalFree)*> pszMsgFree(pwszMsg, LocalFree);
    if (clue.empty())
        throw std::runtime_error(std::format("Error (HRESULT=0x{:08X}): {}", static_cast<uint32_t>(hr), ws_to_u8(pwszMsg)));
    else
        throw std::runtime_error(std::format("Error at {} (HRESULT=0x{:08X}): {}", clue, static_cast<uint32_t>(hr), ws_to_u8(pwszMsg)));
}

[[noreturn]]
void throw_last_error(const std::string& clue = {}) {
    throw_hresult(HRESULT_FROM_WIN32(GetLastError()), clue);
}

HRESULT throw_if_failed(HRESULT hr, std::initializer_list<HRESULT> acceptables = {}, const std::string& clue = {}) {
    if (SUCCEEDED(hr))
        return hr;

    for (const auto& h : acceptables) {
        if (h == hr)
            return hr;
    }

    throw_hresult(hr, clue);
}

std::wstring describe_module(const std::filesystem::path& path) {
    DWORD verHandle = 0;
    std::vector<uint8_t> block;
    block.resize(GetFileVersionInfoSizeW(path.c_str(), &verHandle));
    if (block.empty()) {
        if (GetLastError() == ERROR_RESOURCE_TYPE_NOT_FOUND)
            return L"<no information available>";
        return std::format(L"<error: GetFileVersionInfoSizeW#1 returned {}>", GetLastError());
    }
    if (!GetFileVersionInfoW(path.c_str(), 0, static_cast<DWORD>(block.size()), block.data()))
        return std::format(L"<error: GetFileVersionInfoSizeW#2 returned {}>", GetLastError());

    UINT size = 0;
    
    std::wstring version = L"v?.?.?.?";
    if (LPVOID lpBuffer; VerQueryValueW(block.data(), L"\\", &lpBuffer, &size)) {
        const auto& v = *static_cast<const VS_FIXEDFILEINFO*>(lpBuffer);
        if (v.dwSignature != 0xfeef04bd || sizeof v > size) {
            version = L"<invalid version information>";
        } else {
            if (v.dwFileVersionMS == v.dwProductVersionMS && v.dwFileVersionLS == v.dwProductVersionLS) {
                version = std::format(L"v{}.{}.{}.{}",
                    (v.dwProductVersionMS >> 16) & 0xFFFF,
                    (v.dwProductVersionMS >> 0) & 0xFFFF,
                    (v.dwProductVersionLS >> 16) & 0xFFFF,
                    (v.dwProductVersionLS >> 0) & 0xFFFF);
            } else {
                version = std::format(L"file=v{}.{}.{}.{} prod=v{}.{}.{}.{}",
                    (v.dwFileVersionMS >> 16) & 0xFFFF,
                    (v.dwFileVersionMS >> 0) & 0xFFFF,
                    (v.dwFileVersionLS >> 16) & 0xFFFF,
                    (v.dwFileVersionLS >> 0) & 0xFFFF,
                    (v.dwProductVersionMS >> 16) & 0xFFFF,
                    (v.dwProductVersionMS >> 0) & 0xFFFF,
                    (v.dwProductVersionLS >> 16) & 0xFFFF,
                    (v.dwProductVersionLS >> 0) & 0xFFFF);
            }
        }
    }

    std::wstring description = L"<no description>";
    if (LPVOID lpBuffer; VerQueryValueW(block.data(), L"\\VarFileInfo\\Translation", &lpBuffer, &size)) {
        struct LANGANDCODEPAGE {
            WORD wLanguage;
            WORD wCodePage;
        };
        const auto langs = std::span(reinterpret_cast<const LANGANDCODEPAGE*>(lpBuffer), size / sizeof(LANGANDCODEPAGE));
        for (const auto& lang : langs) {
            if (!VerQueryValueW(block.data(), std::format(L"\\StringFileInfo\\{:04x}{:04x}\\FileDescription", lang.wLanguage, lang.wCodePage).c_str(), &lpBuffer, &size))
                continue;
            auto currName = std::wstring_view(static_cast<wchar_t*>(lpBuffer), size);
            while (!currName.empty() && currName.back() == L'\0')
                currName = currName.substr(0, currName.size() - 1);
            if (currName.empty())
                continue;
            description = currName;
            break;
        }
    }

    return std::format(L"{} {}", description, version);
}

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
        log << std::format(L"\n  {:08X}\t{}\t{}", reinterpret_cast<DWORD64>(hModule), path.wstring(), describe_module(path));

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

void export_tspack(HWND hWndParent, const std::filesystem::path& logDir, const std::string& crashLog, const std::string& troubleshootingPackData) {
    static const char* SourceLogFiles[] = {
        "output.log",
        "patcher.log",
        "dalamud.log",
        "dalamud.injector.log",
        "dalamud.boot.log",
        "aria.log",
    };
    static constexpr auto MaxSizePerLog = 1 * 1024 * 1024;
    static constexpr std::array<COMDLG_FILTERSPEC, 2> OutputFileTypeFilterSpec{{
        { L"Dalamud Troubleshooting Pack File (*.tspack)", L"*.tspack" },
        { L"All files (*.*)", L"*" },
    }};

    IShellItemPtr pItem;
    try {
        SYSTEMTIME st;
        GetLocalTime(&st);
        IFileSaveDialogPtr pDialog;
        throw_if_failed(pDialog.CreateInstance(CLSID_FileSaveDialog, nullptr, CLSCTX_INPROC_SERVER), {}, "pDialog.CreateInstance");
        throw_if_failed(pDialog->SetClientGuid(Guid_IFileDialog_Tspack), {}, "pDialog->SetClientGuid");
        throw_if_failed(pDialog->SetFileTypes(static_cast<UINT>(OutputFileTypeFilterSpec.size()), OutputFileTypeFilterSpec.data()), {}, "pDialog->SetFileTypes");
        throw_if_failed(pDialog->SetFileTypeIndex(0), {}, "pDialog->SetFileTypeIndex");
        throw_if_failed(pDialog->SetTitle(L"Export Dalamud Troubleshooting Pack"), {}, "pDialog->SetTitle");
        throw_if_failed(pDialog->SetFileName(std::format(L"crash-{:04}{:02}{:02}{:02}{:02}{:02}.tspack", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond).c_str()), {}, "pDialog->SetFileName");
        throw_if_failed(pDialog->SetDefaultExtension(L"tspack"), {}, "pDialog->SetDefaultExtension");
        switch (throw_if_failed(pDialog->Show(hWndParent), { HRESULT_FROM_WIN32(ERROR_CANCELLED) }, "pDialog->Show")) {
            case HRESULT_FROM_WIN32(ERROR_CANCELLED):
                return;
        }

        throw_if_failed(pDialog->GetResult(&pItem), {}, "pDialog->GetResult");
        
        IBindCtxPtr pBindCtx;
        throw_if_failed(CreateBindCtx(0, &pBindCtx), {}, "CreateBindCtx");

        auto options = BIND_OPTS{.cbStruct = sizeof(BIND_OPTS), .grfMode = STGM_READWRITE | STGM_SHARE_EXCLUSIVE | STGM_CREATE};
        throw_if_failed(pBindCtx->SetBindOptions(&options), {}, "pBindCtx->SetBindOptions");

        IStreamPtr pStream;
        throw_if_failed(pItem->BindToHandler(pBindCtx, BHID_Stream, IID_PPV_ARGS(&pStream)), {}, "pItem->BindToHandler");

        throw_if_failed(pStream->SetSize({}), {}, "pStream->SetSize");
        
        mz_zip_archive zipa{};
        zipa.m_pIO_opaque = &*pStream;
        zipa.m_pRead = [](void* pOpaque, mz_uint64 file_ofs, void* pBuf, size_t n) -> size_t {
            const auto pStream = static_cast<IStream*>(pOpaque);
            throw_if_failed(pStream->Seek({ .QuadPart = static_cast<int64_t>(file_ofs) }, STREAM_SEEK_SET, nullptr), {}, "pStream->Seek");
            ULONG read;
            throw_if_failed(pStream->Read(pBuf, static_cast<ULONG>(n), &read), {}, "pStream->Read");
            return read;
        };
        zipa.m_pWrite = [](void* pOpaque, mz_uint64 file_ofs, const void* pBuf, size_t n) -> size_t {
            const auto pStream = static_cast<IStream*>(pOpaque);
            throw_if_failed(pStream->Seek({ .QuadPart = static_cast<int64_t>(file_ofs) }, STREAM_SEEK_SET, nullptr), {}, "pStream->Seek");
            ULONG written;
            throw_if_failed(pStream->Write(pBuf, static_cast<ULONG>(n), &written), {}, "pStream->Write");
            return written;
        };
        const auto mz_throw_if_failed = [&zipa](mz_bool res, const std::string& clue) {
            if (!res)
                throw std::runtime_error(std::format("Failed to save file at {}: mz_error={} description={}", clue, static_cast<int>(mz_zip_get_last_error(&zipa)), mz_zip_get_error_string(mz_zip_get_last_error(&zipa))));
        };

        mz_throw_if_failed(mz_zip_writer_init_v2(&zipa, 0, 0), "mz_zip_writer_init_v2");
        mz_throw_if_failed(mz_zip_writer_add_mem(&zipa, "trouble.json", troubleshootingPackData.data(), troubleshootingPackData.size(), MZ_ZIP_FLAG_WRITE_HEADER_SET_SIZE | MZ_BEST_COMPRESSION), "mz_zip_writer_add_mem: trouble.json");
        mz_throw_if_failed(mz_zip_writer_add_mem(&zipa, "crash.log", crashLog.data(), crashLog.size(), MZ_ZIP_FLAG_WRITE_HEADER_SET_SIZE | MZ_BEST_COMPRESSION), "mz_zip_writer_add_mem: crash.log");

        struct HandleAndBaseOffset {
            HANDLE h;
            int64_t off;
        };
        const auto fnHandleReader = [](void* pOpaque, mz_uint64 file_ofs, void* pBuf, size_t n) -> size_t {
            const auto& info = *reinterpret_cast<const HandleAndBaseOffset*>(pOpaque);
            if (!SetFilePointerEx(info.h, { .QuadPart = static_cast<int64_t>(info.off + file_ofs) }, nullptr, SEEK_SET))
                throw_last_error("fnHandleReader: SetFilePointerEx");
            if (DWORD read; !ReadFile(info.h, pBuf, static_cast<DWORD>(n), &read, nullptr))
                throw_last_error("fnHandleReader: ReadFile");
            else
                return read;
        };
        for (const auto& pcszLogFileName : SourceLogFiles) {
            const auto logFilePath = logDir / pcszLogFileName;
            if (!exists(logFilePath))
                continue;

            const auto hLogFile = CreateFileW(logFilePath.c_str(), GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, nullptr);
            if (hLogFile == INVALID_HANDLE_VALUE)
                throw_last_error(std::format("indiv. log file: CreateFileW({})", ws_to_u8(logFilePath.wstring())));
            
            std::unique_ptr<void, decltype(&CloseHandle)> hLogFileClose(hLogFile, &CloseHandle);

            LARGE_INTEGER size, baseOffset{};
            if (!SetFilePointerEx(hLogFile, {}, &size, SEEK_END))
                throw_last_error(std::format("indiv. log file: SetFilePointerEx({})", ws_to_u8(logFilePath.wstring())));

            if (size.QuadPart > MaxSizePerLog) {
                if (!SetFilePointerEx(hLogFile, {.QuadPart = -MaxSizePerLog}, &baseOffset, SEEK_END))
                    throw_last_error(std::format("indiv. log file: SetFilePointerEx#2({})", ws_to_u8(logFilePath.wstring())));
            }

            auto handleInfo = HandleAndBaseOffset{.h = hLogFile, .off = baseOffset.QuadPart};
            const auto modt = std::chrono::system_clock::to_time_t(std::chrono::clock_cast<std::chrono::system_clock>(last_write_time(logFilePath))); 
            mz_throw_if_failed(mz_zip_writer_add_read_buf_callback(
                &zipa,
                pcszLogFileName,
                fnHandleReader, &handleInfo,  // callback info
                size.QuadPart - baseOffset.QuadPart,
                &modt,
                nullptr, 0,  // comments
                MZ_ZIP_FLAG_WRITE_HEADER_SET_SIZE | MZ_BEST_COMPRESSION,  // flags and compression ratio
                nullptr, 0,  // user extra data (local)
                nullptr, 0   // user extra data (central)
                ), std::format("mz_zip_writer_add_read_buf_callback({})", ws_to_u8(logFilePath.wstring())));
        }

        mz_throw_if_failed(mz_zip_writer_finalize_archive(&zipa), "mz_zip_writer_finalize_archive");
        mz_throw_if_failed(mz_zip_writer_end(&zipa), "mz_zip_writer_end");

    } catch (const std::exception& e) {
        MessageBoxW(hWndParent, std::format(L"Failed to save file: {}", u8_to_ws(e.what())).c_str(), get_window_string(hWndParent).c_str(), MB_OK | MB_ICONERROR);

        if (pItem) {
            try {
                IFileOperationPtr pFileOps;
                throw_if_failed(pFileOps.CreateInstance(__uuidof(FileOperation), nullptr, CLSCTX_ALL));
                throw_if_failed(pFileOps->SetOperationFlags(FOF_NO_UI));
                throw_if_failed(pFileOps->DeleteItem(pItem, nullptr));
                throw_if_failed(pFileOps->PerformOperations());
            } catch (const std::exception& e2) {
                std::wcerr << std::format(L"Failed to remove temporary file: {}", u8_to_ws(e2.what())) << std::endl;
            }
            pItem.Release();
        }
    }

    if (pItem) {
        PWSTR pwszFileName;
        if (FAILED(pItem->GetDisplayName(SIGDN_FILESYSPATH, &pwszFileName))) {
            if (FAILED(pItem->GetDisplayName(SIGDN_DESKTOPABSOLUTEEDITING, &pwszFileName))) {
                MessageBoxW(hWndParent, L"The file has been saved to the specified path.", get_window_string(hWndParent).c_str(), MB_OK | MB_ICONINFORMATION);
            } else {
                std::unique_ptr<std::remove_pointer<PWSTR>::type, decltype(CoTaskMemFree)*> pszFileNamePtr(pwszFileName, CoTaskMemFree);
                MessageBoxW(hWndParent, std::format(L"The file has been saved to: {}", pwszFileName).c_str(), get_window_string(hWndParent).c_str(), MB_OK | MB_ICONINFORMATION);
            }
        } else {
            std::unique_ptr<std::remove_pointer<PWSTR>::type, decltype(CoTaskMemFree)*> pszFileNamePtr(pwszFileName, CoTaskMemFree);
            ShellExecuteW(hWndParent, nullptr, L"explorer.exe", escape_shell_arg(std::format(L"/select,{}", pwszFileName)).c_str(), nullptr, SW_SHOW);
        }
    }
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
        UnknownError = -99,
        InvalidParameter = -101,
        ProcessExitedUnknownExitCode = -102,
    };

    HANDLE hPipeRead = nullptr;
    std::filesystem::path assetDir, logDir;
    std::optional<std::vector<std::wstring>> launcherArgs;
    auto fullDump = false;
    
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
            if (arg == L"--veh-full") {
                fullDump = true;
            }
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

        auto shutup_mutex = CreateMutex(NULL, false, L"DALAMUD_CRASHES_NO_MORE");
        bool shutup = false;
        if (shutup_mutex == NULL && GetLastError() == ERROR_ALREADY_EXISTS)
            shutup = true;

        /*
        Hard won wisdom: changing symbol path with SymSetSearchPath() after modules
        have been loaded (invadeProcess=TRUE in SymInitialize() or SymRefreshModuleList())
        doesn't work.
        I had to provide symbol path in SymInitialize() (and either invadeProcess=TRUE
        or invadeProcess=FALSE and call SymRefreshModuleList()). There's probably
        a way to force it, but I'm happy I found a way that works.

        https://github.com/sumatrapdfreader/sumatrapdf/blob/master/src/utils/DbgHelpDyn.cpp
        */
        
        if (g_bSymbolsAvailable) {
            SymRefreshModuleList(g_hProcess);
        }
        else if(!assetDir.empty())
        {
            auto symbol_search_path = std::format(L".;{}", (assetDir / "UIRes" / "pdb").wstring());
            
            g_bSymbolsAvailable = SymInitializeW(g_hProcess, symbol_search_path.c_str(), true);
            std::wcout << std::format(L"Init symbols with PDB at {}", symbol_search_path) << std::endl;

            SymRefreshModuleList(g_hProcess);
        }
        else
        {
            g_bSymbolsAvailable = SymInitializeW(g_hProcess, nullptr, true);
            std::cout << "Init symbols without PDB" << std::endl;
        }
        
        if (!g_bSymbolsAvailable) {
            std::wcerr << std::format(L"SymInitialize error: 0x{:x}", GetLastError()) << std::endl;
        }

        std::wstring stackTrace(exinfo.dwStackTraceLength, L'\0');
        if (exinfo.dwStackTraceLength) {
            if (DWORD read; !ReadFile(hPipeRead, &stackTrace[0], 2 * exinfo.dwStackTraceLength, &read, nullptr)) {
                std::cout << std::format("Failed to read supplied stack trace: error 0x{:x}", GetLastError()) << std::endl;
            }
        }

        std::string troubleshootingPackData(exinfo.dwTroubleshootingPackDataLength, '\0');
        if (exinfo.dwTroubleshootingPackDataLength) {
            if (DWORD read; !ReadFile(hPipeRead, &troubleshootingPackData[0], exinfo.dwTroubleshootingPackDataLength, &read, nullptr)) {
                std::cout << std::format("Failed to read troubleshooting pack data: error 0x{:x}", GetLastError()) << std::endl;
            }
        }

        SYSTEMTIME st;
        GetLocalTime(&st);
        const auto dalamudLogPath = logDir.empty() ? std::filesystem::path() : logDir / L"Dalamud.log";
        const auto dalamudBootLogPath = logDir.empty() ? std::filesystem::path() : logDir / L"Dalamud.boot.log";
        const auto dumpPath = logDir.empty() ? std::filesystem::path() : logDir / std::format(L"dalamud_appcrash_{:04}{:02}{:02}_{:02}{:02}{:02}_{:03}_{}.dmp", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, dwProcessId);
        const auto logPath = logDir.empty() ? std::filesystem::path() : logDir / std::format(L"dalamud_appcrash_{:04}{:02}{:02}_{:02}{:02}{:02}_{:03}_{}.log", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, dwProcessId);
        std::wstring dumpError;
        if (dumpPath.empty()) {
            std::cout << "Skipping dump path, as log directory has not been specified" << std::endl;
        } else if (shutup) {
            std::cout << "Skipping dump, was shutdown" << std::endl;
        }
        else
        {
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
                if (!MiniDumpWriteDump(g_hProcess, dwProcessId, hDumpFile, fullDump ? MiniDumpWithFullMemory : static_cast<MINIDUMP_TYPE>(MiniDumpWithDataSegs | MiniDumpWithModuleHeaders), &mdmp_info, nullptr, nullptr)) {
                    std::wcerr << (dumpError = std::format(L"MiniDumpWriteDump(0x{:x}, {}, 0x{:x}({}), MiniDumpWithFullMemory, ..., nullptr, nullptr) error: 0x{:x}", reinterpret_cast<size_t>(g_hProcess), dwProcessId, reinterpret_cast<size_t>(hDumpFile), dumpPath.wstring(), GetLastError())) << std::endl;
                    break;
                }

                std::wcout << "Dump written to path: " << dumpPath << std::endl;
            } while (false);
        }

        std::wostringstream log;
        log << std::format(L"Unhandled native exception occurred at {}", to_address_string(exinfo.ContextRecord.Rip, false)) << std::endl;
        log << std::format(L"Code: {:X}", exinfo.ExceptionRecord.ExceptionCode) << std::endl;

        if (shutup)
            log << L"======= Crash handler was globally muted(shutdown?) =======" << std::endl;
        
        if (dumpPath.empty())
            log << L"Dump skipped" << std::endl;
        else if (dumpError.empty())
            log << std::format(L"Dump at: {}", dumpPath.wstring()) << std::endl;
        else
            log << std::format(L"Dump error: {}", dumpError) << std::endl;
        log << L"System Time: " << std::chrono::system_clock::now() << std::endl;
        log << L"\n" << stackTrace << std::endl;

        SymRefreshModuleList(GetCurrentProcess());
        print_exception_info(exinfo.hThreadHandle, exinfo.ExceptionPointers, exinfo.ContextRecord, log);
        const auto window_log_str = log.str();
        print_exception_info_extended(exinfo.ExceptionPointers, exinfo.ContextRecord, log);
        std::wofstream(logPath) << log.str();

        std::thread submitThread;
        if (!getenv("DALAMUD_NO_METRIC")) {
            auto url = std::format(L"/Dalamud/Metric/ReportCrash?lt={}&code={:x}", exinfo.nLifetime, exinfo.ExceptionRecord.ExceptionCode);

            submitThread = std::thread([url = std::move(url)] {
                const auto hInternet = WinHttpOpen(L"Dalamud Crash Handler/1.0", 
                                     WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
                                     WINHTTP_NO_PROXY_NAME, 
                                     WINHTTP_NO_PROXY_BYPASS, 0);
                const auto hConnect = !hInternet ? nullptr : WinHttpConnect(hInternet, L"kamori.goats.dev", INTERNET_DEFAULT_HTTP_PORT, 0);
                const auto hRequest = !hConnect ? nullptr : WinHttpOpenRequest(hConnect, L"GET", url.c_str(), NULL, WINHTTP_NO_REFERER, 
                                               WINHTTP_DEFAULT_ACCEPT_TYPES,
                                               0);
                if (hRequest) WinHttpAddRequestHeaders(hRequest, L"Host: kamori.goats.dev", (ULONG)-1L, WINHTTP_ADDREQ_FLAG_ADD);
                const auto bSent = !hRequest ? false : WinHttpSendRequest(hRequest, WINHTTP_NO_ADDITIONAL_HEADERS,
                                               0, WINHTTP_NO_REQUEST_DATA, 0, 
                                               0, 0);

                if (!bSent)
                    std::cerr << std::format("Failed to send metric: 0x{:x}", GetLastError()) << std::endl;

                if (WinHttpReceiveResponse(hRequest, nullptr))
                {
                    DWORD dwStatusCode = 0;
                    DWORD dwStatusCodeSize = sizeof(DWORD);

                    WinHttpQueryHeaders(hRequest,
                                        WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
                                        WINHTTP_HEADER_NAME_BY_INDEX,
                                        &dwStatusCode, &dwStatusCodeSize, WINHTTP_NO_HEADER_INDEX);

                    if (dwStatusCode != 200)
                        std::cerr << std::format("Failed to send metric: {}", dwStatusCode) << std::endl;
                }
                
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
        config.pszMainInstruction = L"An error in the game occurred";
        config.pszContent = (L""
            R"aa(The game has to close. This error may be caused by a faulty plugin, a broken mod, any other third-party tool, or simply a bug in the game.)aa" "\n"
            "\n"
            R"aa(Try running a game repair in XIVLauncher by right clicking the login button, and disabling plugins you don't need. Please also check your antivirus, see our <a href="help">help site</a> for more information.)aa" "\n"
            "\n"
            R"aa(Upload <a href="exporttspack">this file (click here)</a> if you want to ask for help in our <a href="discord">Discord server</a>.)aa" "\n"

        );
        config.pButtons = buttons;
        config.cButtons = ARRAYSIZE(buttons);
        config.nDefaultButton = IdButtonRestart;
        config.pszExpandedControlText = L"Hide stack trace";
        config.pszCollapsedControlText = L"Stack trace for plugin developers";
        config.pszExpandedInformation = window_log_str.c_str();
        config.pszWindowTitle = L"Dalamud Error";
        config.pRadioButtons = radios;
        config.cRadioButtons = ARRAYSIZE(radios);
        config.nDefaultRadioButton = IdRadioRestartNormal;
        config.cxWidth = 300;

#if _DEBUG
        config.pszFooter = (L""
            R"aa(<a href="help">Help</a> | <a href="logdir">Open log directory</a> | <a href="logfile">Open log file</a> | <a href="resume">Attempt to resume</a>)aa"
        );
#else
        config.pszFooter = (L""
            R"aa(<a href="help">Help</a> | <a href="logdir">Open log directory</a> | <a href="logfile">Open log file</a>)aa"
        );
#endif
        
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
                    } else if (link == L"exporttspack") {
                        export_tspack(hwnd, logDir, ws_to_u8(log.str()), troubleshootingPackData);
                    } else if (link == L"discord") {
                        ShellExecuteW(hwnd, nullptr, L"https://goat.place", nullptr, nullptr, SW_SHOW);
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

        if (shutup) {
            TerminateProcess(g_hProcess, exinfo.ExceptionRecord.ExceptionCode);
            return 0;
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

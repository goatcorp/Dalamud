#include <algorithm>
#include <array>
#include <chrono>
#include <cstring>
#include <filesystem>
#include <format>
#include <fstream>
#include <map>
#include <optional>
#include <ranges>
#include <span>
#include <sstream>
#include <string>
#include <vector>

#define WIN32_LEAN_AND_MEAN
#undef NOMINMAX
#define NOMINMAX
#include <windows.h>

#include <comdef.h>
#include <commctrl.h>
#include <dbghelp.h>
#include <intrin.h>
#if !defined(__MINGW32__)
// In MinGW, the parts of minidumpapiset needed are available via dbghelp.
#include <minidumpapiset.h>
#endif
#include <pathcch.h>
#include <psapi.h>
#include <shellapi.h>
#include <tlhelp32.h>
#include <shlguid.h>
#include <shobjidl.h>
#if !defined(__MINGW32__)
#include <shlobj_core.h>
#else
#include <shlobj.h>
#endif

#include <dxgi.h>

#if defined(__GNUC__) 
#pragma GCC diagnostic ignored "-Wdeprecated-enum-enum-conversion"
#endif

#if defined(__clang__)
#pragma clang diagnostic ignored "-Wdeprecated-anon-enum-enum-conversion"
#endif

_COM_SMARTPTR_TYPEDEF(IFileOperation, __uuidof(IFileOperation));
_COM_SMARTPTR_TYPEDEF(IFileSaveDialog, __uuidof(IFileSaveDialog));
_COM_SMARTPTR_TYPEDEF(IShellItem, __uuidof(IShellItem));
_COM_SMARTPTR_TYPEDEF(IBindCtx, __uuidof(IBindCtx));
_COM_SMARTPTR_TYPEDEF(IStream, __uuidof(IStream));

static constexpr GUID Guid_IFileDialog_Tspack{ 0xfc057318, 0xad35, 0x4599, {0xa7, 0x68, 0xdd, 0xaf, 0x70, 0xbe, 0x98, 0x75} };

#include "resource.h"
#include "../Dalamud.Boot/crashhandler_shared.h"
#include "../shared/logging.h"
#include "miniz.h"
#include "dac_interfaces.h"

/* mingw SEH try / catch seems to be absolutely cursed beyond belief,
 * with some projects handling it wrong, some projects warning against it,
 * and yet again some other projects pushing through despite its fragility.
 * If anyone's got a better idea at handling the flag than a thread local, please fix, thanks.
 */
#if defined(_MSC_VER)
#define SEH_MSVC
#define SEH_NOOPT

// MSVC: __try { faulty; } __except(EXCEPTION_EXECUTE_HANDLER) { handler; }
#define __seh_try_begin __try
#define __seh_try_end __except(EXCEPTION_EXECUTE_HANDLER) {}

#elif defined(__try1)
#define SEH_MINGW
#if defined(__clang__)
#define SEH_NOOPT __attribute__((optnone))
#else
#define SEH_NOOPT __attribute__((optimize("O0")))
#endif
extern "C" long SEH_NOOPT seh_violation_handler(EXCEPTION_POINTERS* data)
{
    return EXCEPTION_EXECUTE_HANDLER;
}

// MinGW: __try1(handler); faulty; __except1; runs_always;
/* MinGW __try1 / __except1 is great... except for when it decides to use named labels which cannot be reused.
 */
#define __seh_try_begin \
    startw: \
    asm goto volatile ( \
        "\t.seh_handler __C_specific_handler, @except\n" \
        "\t.seh_handlerdata\n" \
        "\t.long 1\n" \
        "\t.rva %l[startw], %l[endw], " __MINGW64_STRINGIFY(__MINGW_USYMBOL(seh_violation_handler)) ", %l[endw]\n" \
        "\t.text" \
        : \
        : \
        : \
        : startw, endw \
    );

#define __seh_try_end \
    endw:

#else
#error "Your compilation environment does not expose SEH try / except unwind helpers"
#endif

// Windows 10 1607, part of modern Windows SDKs and MinGW 12+, but Ubuntu 24.04 ships MinGW 11
typedef HRESULT (WINAPI* PFN_GetThreadDescription)(HANDLE hThread, PWSTR* ppszThreadDescription);
#if defined(__MINGW64_VERSION_MAJOR) && __MINGW64_VERSION_MAJOR < 12
static PFN_GetThreadDescription _GetThreadDescription = reinterpret_cast<PFN_GetThreadDescription>(
    GetProcAddress(GetModuleHandleW(L"kernel32.dll"), "GetThreadDescription"));
#else
static PFN_GetThreadDescription _GetThreadDescription = &GetThreadDescription;
#endif

HANDLE g_hProcess = nullptr;
bool g_bSymbolsAvailable = false;

// DAC state per crash to walk the managed stack
IXCLRDataProcess* g_pClrDataProcess = nullptr;
// ISOSDacInterface used to read JIT code header data for cross-process unwinding of JIT frames
ISOSDacInterface*  g_pSosDac          = nullptr;

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
    std::wstring buf(GetWindowTextLengthW(hWnd) + 1, L'\0');
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
        const auto langs = std::span(static_cast<const LANGANDCODEPAGE*>(lpBuffer), size / sizeof(LANGANDCODEPAGE));
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
                logging::E("EnumProcessModules error: 0x{:x}", GetLastError());
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
                logging::E("Failed to read IMAGE_DOS_HEADER for module at 0x{:x}", reinterpret_cast<size_t>(hModule));
                continue;
            }

            if (size_t read; !ReadProcessMemory(g_hProcess, reinterpret_cast<const char*>(hModule) + dosh.e_lfanew, &nth64, sizeof nth64, &read) || read != sizeof nth64) {
                logging::E("Failed to read IMAGE_NT_HEADERS64 for module at 0x{:x}", reinterpret_cast<size_t>(hModule));
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
                logging::E("Failed to get path for module at 0x{:x}: error 0x{:x}", reinterpret_cast<size_t>(hModule), GetLastError());
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

// Data target for accessing the game process remotely
class CrashHandlerDataTarget : public ICLRDataTarget2
{
    ULONG m_refs;
    DWORD m_crashingThreadOsId;
    CONTEXT m_crashContext;

public:
    CrashHandlerDataTarget(DWORD crashingThreadOsId, const CONTEXT& crashCtx)
        : m_refs(1)
        , m_crashingThreadOsId(crashingThreadOsId)
        , m_crashContext(crashCtx)
    {
    }

    virtual ~CrashHandlerDataTarget() = default;

    // IUnknown
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject) override
    {
        if (riid == IID_IUnknown || riid == __uuidof(ICLRDataTarget) || riid == __uuidof(ICLRDataTarget2))
        {
            *ppvObject = static_cast<ICLRDataTarget2*>(this);
            AddRef();
            return S_OK;
        }
        *ppvObject = nullptr;
        return E_NOINTERFACE;
    }

    ULONG STDMETHODCALLTYPE AddRef() override
    {
        return ++m_refs;
    }

    ULONG STDMETHODCALLTYPE Release() override
    {
        const ULONG refs = --m_refs;
        if (!refs)
            delete this;
        return refs;
    }

    // ICLRDataTarget
    HRESULT STDMETHODCALLTYPE GetMachineType(ULONG32* machineType) override
    {
        *machineType = IMAGE_FILE_MACHINE_AMD64;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetPointerSize(ULONG32* pointerSize) override
    {
        *pointerSize = 8;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetImageBase(LPCWSTR imagePath, CLRDATA_ADDRESS* baseAddress) override
    {
        for (const auto& [hMod, path] : get_remote_module_paths())
        {
            if (_wcsicmp(path.c_str(), imagePath) == 0
                || _wcsicmp(path.filename().c_str(), imagePath) == 0)
            {
                *baseAddress = reinterpret_cast<CLRDATA_ADDRESS>(hMod);
                return S_OK;
            }
        }
        return E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE ReadVirtual(CLRDATA_ADDRESS address, BYTE* buffer, ULONG32 bytesRequested, ULONG32* bytesRead) override
    {
        SIZE_T read = 0;
        if (!ReadProcessMemory(g_hProcess, reinterpret_cast<LPCVOID>(address), buffer, bytesRequested, &read))
        {
            *bytesRead = 0;
            return HRESULT_FROM_WIN32(GetLastError());
        }
        *bytesRead = static_cast<ULONG32>(read);
        // ReadProcessMemory may do a partial read and still succeed, that's fine (I think)
        return read > 0 ? S_OK : E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE WriteVirtual(CLRDATA_ADDRESS, BYTE*, ULONG32, ULONG32*) override
    {
        return E_NOTIMPL;
    }

    HRESULT STDMETHODCALLTYPE GetTLSValue(ULONG32, ULONG32, CLRDATA_ADDRESS*) override
    {
        return E_NOTIMPL;
    }

    HRESULT STDMETHODCALLTYPE SetTLSValue(ULONG32, ULONG32, CLRDATA_ADDRESS) override
    {
        return E_NOTIMPL;
    }

    HRESULT STDMETHODCALLTYPE GetCurrentThreadID(ULONG32* threadID) override
    {
        *threadID = m_crashingThreadOsId;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetThreadContext(ULONG32 threadID, ULONG32 contextFlags, ULONG32 contextSize, BYTE* context) override
    {
        if (contextSize < sizeof(CONTEXT))
            return E_INVALIDARG;

        // For the crashing thread we already have a saved snapshot of the context at crash time
        if (threadID == m_crashingThreadOsId)
        {
            const ULONG32 copySize = std::min<ULONG32>(contextSize, sizeof(CONTEXT));
            memcpy(context, &m_crashContext, copySize);
            return S_OK;
        }

        // For any other thread, open it and get its current context
        HANDLE hThread = OpenThread(THREAD_GET_CONTEXT | THREAD_QUERY_INFORMATION, FALSE, threadID);
        if (!hThread)
            return HRESULT_FROM_WIN32(GetLastError());

        auto* pCtx = reinterpret_cast<CONTEXT*>(context);
        pCtx->ContextFlags = contextFlags;
        const BOOL ok = ::GetThreadContext(hThread, pCtx);
        CloseHandle(hThread);
        return ok ? S_OK : HRESULT_FROM_WIN32(GetLastError());
    }

    HRESULT STDMETHODCALLTYPE SetThreadContext(ULONG32, ULONG32, BYTE*) override
    {
        return E_NOTIMPL;
    }

    HRESULT STDMETHODCALLTYPE Request(ULONG32, ULONG32, BYTE*, ULONG32, BYTE*) override
    {
        return E_NOTIMPL;
    }

    // ICLRDataTarget2
    HRESULT STDMETHODCALLTYPE AllocVirtual(CLRDATA_ADDRESS, ULONG32, ULONG32, ULONG32, CLRDATA_ADDRESS*) override
    {
        return E_NOTIMPL;
    }

    HRESULT STDMETHODCALLTYPE FreeVirtual(CLRDATA_ADDRESS, ULONG32, ULONG32) override
    {
        return E_NOTIMPL;
    }
};

IXCLRDataProcess* try_create_clr_data_process(DWORD crashingThreadOsId, const CONTEXT& crashCtx)
{
    for (const auto& [hMod, path] : get_remote_module_paths())
    {
        if (_wcsicmp(path.filename().c_str(), L"coreclr.dll") != 0)
            continue;

        const auto dacPath = path.parent_path() / L"mscordaccore.dll";

        const HMODULE hDac = LoadLibraryExW(dacPath.c_str(), nullptr, LOAD_WITH_ALTERED_SEARCH_PATH);
        if (!hDac)
        {
            logging::E("[DAC] Failed to load {}: 0x{:x}", dacPath, GetLastError());
            continue;
        }

        const auto pfnCreateInstance = reinterpret_cast<PFN_CLRDataCreateInstance>(
            GetProcAddress(hDac, "CLRDataCreateInstance"));
        if (!pfnCreateInstance)
        {
            logging::E("[DAC] CLRDataCreateInstance not found in mscordaccore.dll");
            FreeLibrary(hDac);
            continue;
        }

        auto* const pTarget = new CrashHandlerDataTarget(crashingThreadOsId, crashCtx);
        IXCLRDataProcess* pProc = nullptr;
        const HRESULT hr = pfnCreateInstance(__uuidof(IXCLRDataProcess), pTarget, reinterpret_cast<void**>(&pProc));
        pTarget->Release();

        if (FAILED(hr) || !pProc)
        {
            logging::E("[DAC] CLRDataCreateInstance failed: 0x{:x}", static_cast<unsigned>(hr));
            FreeLibrary(hDac);
            continue;
        }

        logging::I("[DAC] Initialized successfully");

        ISOSDacInterface* pSos = nullptr;
        if (SUCCEEDED(pProc->QueryInterface(__uuidof(ISOSDacInterface),
                                            reinterpret_cast<void**>(&pSos))))
        {
            g_pSosDac = pSos;
            logging::I("[DAC] ISOSDacInterface obtained");
        }

        return pProc;
    }
    return nullptr;
}

struct DacNameResult
{
    wchar_t nameBuf[2048];
    CLRDATA_ADDRESS displacement;
    // 0 = not found, 1 = managed method, 2 = CLR runtime code
    int kind;
};

static DacNameResult SEH_NOOPT try_get_managed_method_name_seh(IXCLRDataProcess* pClrProc, DWORD64 address)
{
    DacNameResult result{};
    result.nameBuf[0] = L'\0';
    result.displacement = 0;
    result.kind = 0;

    __seh_try_begin
    {
        CLRDataAddressType addrType = CLRDATA_ADDRESS_UNRECOGNIZED;
        if (FAILED(pClrProc->GetAddressType(static_cast<CLRDATA_ADDRESS>(address), &addrType)))
            return result;

        if (addrType == CLRDATA_ADDRESS_MANAGED_METHOD)
        {
            CLRDATA_ENUM enumHandle{};
            if (FAILED(pClrProc->StartEnumMethodInstancesByAddress(
                    static_cast<CLRDATA_ADDRESS>(address), nullptr, &enumHandle)))
                return result;

            IXCLRDataMethodInstance* pMethod = nullptr;
            const HRESULT hrEnum = pClrProc->EnumMethodInstanceByAddress(&enumHandle, &pMethod);
            pClrProc->EndEnumMethodInstancesByAddress(enumHandle);

            if (FAILED(hrEnum) || !pMethod)
                return result;

            ULONG32 nameLen = 0;
            const HRESULT hrName = pMethod->GetName(
                0, static_cast<ULONG32>(ARRAYSIZE(result.nameBuf)), &nameLen, result.nameBuf);
            pMethod->Release();

            if (SUCCEEDED(hrName) && result.nameBuf[0])
                result.kind = 1;

            return result;
        }

        if (addrType == CLRDATA_ADDRESS_RUNTIME_MANAGED_CODE
            || addrType == CLRDATA_ADDRESS_RUNTIME_UNMANAGED_CODE
            || addrType == CLRDATA_ADDRESS_RUNTIME_MANAGED_STUB
            || addrType == CLRDATA_ADDRESS_RUNTIME_UNMANAGED_STUB)
        {
            ULONG32 nameLen = 0;
            if (SUCCEEDED(pClrProc->GetRuntimeNameByAddress(
                    static_cast<CLRDATA_ADDRESS>(address), 0,
                    static_cast<ULONG32>(ARRAYSIZE(result.nameBuf)), &nameLen,
                    result.nameBuf, &result.displacement))
                && result.nameBuf[0])
            {
                result.kind = 2;
            }

            return result;
        }
    }
    __seh_try_end
    {
        // DAC may raise exceptions on corrupt CLR heap
    }

    return result;
}

// Attempt to resolve a managed method name for a given native code address (in compiled JIT code)
std::wstring try_get_managed_method_name(IXCLRDataProcess* pClrProc, DWORD64 address)
{
    if (!pClrProc)
        return {};

    const DacNameResult r = try_get_managed_method_name_seh(pClrProc, address);

    if (r.kind == 1)
        return std::wstring(r.nameBuf);

    if (r.kind == 2)
    {
        if (r.displacement)
            return std::format(L"[clr] {}+0x{:X}", r.nameBuf, static_cast<ULONG64>(r.displacement));
        return std::format(L"[clr] {}", r.nameBuf);
    }

    return {};
}

// Render the address as a string for the call stack, including managed data from DAC
std::wstring to_address_string_mixed(DWORD64 address, IXCLRDataProcess* pClrProc)
{
    // Try the DAC first so managed frames get a human-readable managed name
    if (const auto managed = try_get_managed_method_name(pClrProc, address); !managed.empty())
    {
        DWORD64 moduleBase;
        std::filesystem::path modulePath;
        if (get_module_file_and_base(address, moduleBase, modulePath))
        {
            return std::format(L"{}+{:X}\t({})",
                modulePath.filename().wstring(), address - moduleBase, managed);
        }
        return std::format(L"{:X}\t({})", address, managed);
    }

    // Fallback for native code
    return to_address_string(address, false);
}

static HRESULT SEH_NOOPT seh_get_code_header_data(ISOSDacInterface* pSos,
                                         CLRDATA_ADDRESS    ip,
                                         DacpCodeHeaderData* pOut)
{
    __seh_try_begin { return pSos->GetCodeHeaderData(ip, pOut); }
    __seh_try_end { return E_FAIL; }
}

// Map x64 unwind register index (0-15) to the matching CONTEXT field pointer
static ULONG64* ctx_reg(CONTEXT& ctx, BYTE regIdx)
{
    // 0=RAX 1=RCX 2=RDX 3=RBX 4=RSP 5=RBP 6=RSI 7=RDI
    // 8=R8  9=R9 10=R10 11=R11 12=R12 13=R13 14=R14 15=R15
    static const int offsets[16] = {
        static_cast<int>(offsetof(CONTEXT, Rax)), static_cast<int>(offsetof(CONTEXT, Rcx)),
        static_cast<int>(offsetof(CONTEXT, Rdx)), static_cast<int>(offsetof(CONTEXT, Rbx)),
        static_cast<int>(offsetof(CONTEXT, Rsp)), static_cast<int>(offsetof(CONTEXT, Rbp)),
        static_cast<int>(offsetof(CONTEXT, Rsi)), static_cast<int>(offsetof(CONTEXT, Rdi)),
        static_cast<int>(offsetof(CONTEXT, R8)),  static_cast<int>(offsetof(CONTEXT, R9)),
        static_cast<int>(offsetof(CONTEXT, R10)), static_cast<int>(offsetof(CONTEXT, R11)),
        static_cast<int>(offsetof(CONTEXT, R12)), static_cast<int>(offsetof(CONTEXT, R13)),
        static_cast<int>(offsetof(CONTEXT, R14)), static_cast<int>(offsetof(CONTEXT, R15)),
    };
    if (regIdx >= 16) return nullptr;
    return reinterpret_cast<ULONG64*>(reinterpret_cast<char*>(&ctx) + offsets[regIdx]);
}

static bool rpm_u64(HANDLE hProcess, ULONG64 addr, ULONG64& out)
{
    SIZE_T rd = 0;
    return ReadProcessMemory(hProcess, reinterpret_cast<LPCVOID>(addr), &out, 8, &rd) && rd == 8;
}

// Process one UNWIND_INFO blob on ctx
static bool apply_unwind_codes(
    HANDLE hProcess,
    CONTEXT& ctx,
    const std::vector<BYTE>& buf,
    IMAGE_RUNTIME_FUNCTION_ENTRY& chainedRFOut)
{
    chainedRFOut = {};

    if (buf.size() < 4)
        return false;

    const BYTE uwFlags        = buf[0] >> 3;
    const BYTE uwCountOfCodes = buf[2];
    const BYTE uwFrameReg     = buf[3] & 0xF;
    const BYTE uwFrameOffset  = buf[3] >> 4;

    if (4 + static_cast<SIZE_T>(uwCountOfCodes) * 2 > buf.size())
        return false;

    DWORD i = 0;
    while (i < uwCountOfCodes)
    {
        const BYTE* pNode  = buf.data() + 4 + i * 2;
        const BYTE  op     = pNode[1] & 0xF;
        const BYTE  info   = pNode[1] >> 4;

        switch (op)
        {
        case 0: // UWOP_PUSH_NONVOL
        {
            ULONG64 val = 0;
            if (!rpm_u64(hProcess, ctx.Rsp, val))
            {
                logging::D("[JIT-MAN] PUSH_NONVOL: RPM failed at RSP=0x{:X}", ctx.Rsp);
                return false;
            }
            if (auto* pReg = ctx_reg(ctx, info)) *pReg = val;
            ctx.Rsp += 8;
            ++i;
            break;
        }
        case 1: // UWOP_ALLOC_LARGE
            if (info == 0)
            {
                if (i + 2 > uwCountOfCodes) return false;
                const USHORT scaled = *reinterpret_cast<const USHORT*>(buf.data() + 4 + (i + 1) * 2);
                ctx.Rsp += static_cast<ULONG64>(scaled) * 8;
                i += 2;
            }
            else
            {
                if (i + 3 > uwCountOfCodes) return false;
                const ULONG32 sz = *reinterpret_cast<const ULONG32*>(buf.data() + 4 + (i + 1) * 2);
                ctx.Rsp += sz;
                i += 3;
            }
            break;
        case 2: // UWOP_ALLOC_SMALL
            ctx.Rsp += (static_cast<ULONG64>(info) + 1) * 8;
            ++i;
            break;
        case 3: // UWOP_SET_FPREG
        {
            auto* pFR = ctx_reg(ctx, uwFrameReg);
            if (!pFR) return false;
            ctx.Rsp = *pFR - static_cast<ULONG64>(uwFrameOffset) * 16;
            logging::D(
                "[JIT-MAN] SET_FPREG: RSP <- reg{}=0x{:X} - {}*16 = 0x{:X}",
                uwFrameReg, *pFR, uwFrameOffset, ctx.Rsp);
            ++i;
            break;
        }
        case 4: // UWOP_SAVE_NONVOL (slot * 8 from RSP; no RSP change)
        {
            if (i + 2 > uwCountOfCodes) return false;
            const USHORT slot = *reinterpret_cast<const USHORT*>(buf.data() + 4 + (i + 1) * 2);
            ULONG64 val = 0;
            const ULONG64 addr = ctx.Rsp + static_cast<ULONG64>(slot) * 8;
            if (!rpm_u64(hProcess, addr, val))
            {
                logging::D("[JIT-MAN] SAVE_NONVOL: RPM failed at 0x{:X}", addr);
                return false;
            }
            if (auto* pReg = ctx_reg(ctx, info)) *pReg = val;
            i += 2;
            break;
        }
        case 5: // UWOP_SAVE_NONVOL_FAR (32-bit offset from RSP; no RSP change)
        {
            if (i + 3 > uwCountOfCodes) return false;
            const ULONG32 off = *reinterpret_cast<const ULONG32*>(buf.data() + 4 + (i + 1) * 2);
            ULONG64 val = 0;
            const ULONG64 addr = ctx.Rsp + off;
            if (!rpm_u64(hProcess, addr, val))
            {
                logging::D("[JIT-MAN] SAVE_NONVOL_FAR: RPM failed at 0x{:X}", addr);
                return false;
            }
            if (auto* pReg = ctx_reg(ctx, info)) *pReg = val;
            i += 3;
            break;
        }
        case 6: // UWOP_EPILOG (V2, 2 nodes)
        case 7: // UWOP_SPARE_CODE
            i += 2;
            break;
        case 8: // UWOP_SAVE_XMM128 (2 nodes, XMM not needed for stack walks)
            i += 2;
            break;
        case 9: // UWOP_SAVE_XMM128_FAR (3 nodes)
            i += 3;
            break;
        case 10: // UWOP_PUSH_MACHFRAME (interrupt/exception frame)
        {
            // Layout: [ErrorCode?] RIP CS RFLAGS RSP SS
            if (info) ctx.Rsp += 8; // skip error code if present
            ULONG64 newRip = 0, newRsp = 0;
            if (!rpm_u64(hProcess, ctx.Rsp,      newRip)) return false; // RIP
            if (!rpm_u64(hProcess, ctx.Rsp + 24, newRsp)) return false; // RSP
            ctx.Rip = newRip;
            ctx.Rsp = newRsp;
            ++i;
            return newRip != 0; // terminal: no further unwinding needed
        }
        default:
            logging::D("[JIT-MAN] Unknown UWOP {} at node index {}", op, i);
            return false;
        }
    }

    // If UNW_FLAG_CHAININFO (bit 2 of flags),chained RUNTIME_FUNCTION follows the
    // (aligned) codes section. Populate chainedRFOut for the caller to recurse
    if (uwFlags & 0x4)
    {
        const SIZE_T chainOffset = (4 + static_cast<SIZE_T>(uwCountOfCodes) * 2 + 3) & ~3uLL;
        if (chainOffset + sizeof(IMAGE_RUNTIME_FUNCTION_ENTRY) <= buf.size())
        {
            std::memcpy(&chainedRFOut, buf.data() + chainOffset, sizeof(chainedRFOut));
            logging::D(
                "[JIT-MAN] CHAININFO: chained RF Begin=0x{:X} End=0x{:X} UnwindInfo=0x{:X}",
                chainedRFOut.BeginAddress, chainedRFOut.EndAddress, chainedRFOut.UnwindInfoAddress);
        }
        else
        {
            logging::D("[JIT-MAN] CHAININFO but buffer too small to read chained RF");
        }
    }

    return true;
}

// Read UNWIND_INFO blob from the target process into a vector.
static bool read_unwind_info_blob(HANDLE hProcess, ULONG64 unwindInfoAddr,
                                   std::vector<BYTE>& out)
{
    BYTE hdr[4] = {};
    if (!ReadProcessMemory(hProcess, reinterpret_cast<LPCVOID>(unwindInfoAddr),
                           hdr, 4, nullptr))
        return false;
    // Size = 4-byte header + 2*CountOfCodes (rounded up to DWORD) + 12 (chain/handler trailer)
    const SIZE_T sz = ((4 + static_cast<SIZE_T>(hdr[2]) * 2 + 3) & ~3uLL) + 12;
    out.resize(sz);
    SIZE_T rd = 0;
    return ReadProcessMemory(hProcess, reinterpret_cast<LPCVOID>(unwindInfoAddr),
                              out.data(), sz, &rd) && rd >= 4;
}

// Attempt to unwind one JIT-compiled frame in the game process
// On success, ctx is updated to the caller's context and true is returned.
// On failure (IP not in JIT code, read errors, allocation failure) false is returned
// and ctx is left unchanged.
static bool try_jit_virtual_unwind(HANDLE hProcess, ISOSDacInterface* pSos, CONTEXT& ctx)
{
    if (!pSos || ctx.Rip == 0)
        return false;

    // Step 1: Check if the IP is in a JIT-compiled method, otherwise we shouldn't even bother
    DacpCodeHeaderData codeHdr = {};
    const HRESULT hrHdr = seh_get_code_header_data(
        pSos, ctx.Rip, &codeHdr);

    if (FAILED(hrHdr) || codeHdr.MethodStart == 0)
    {
        logging::D(
            "[JIT] GetCodeHeaderData hr=0x{:X} MethodStart=0x{:X}; IP not in JIT code",
            static_cast<unsigned>(hrHdr), codeHdr.MethodStart);
        return false;
    }

    // Skip ReadyToRun (PJIT) code, handled by StackWalk64
    if (codeHdr.JITType != TYPE_JIT)
    {
        logging::D("[JIT] JITType={} (not TYPE_JIT); skipping",
            static_cast<int>(codeHdr.JITType));
        return false;
    }

    logging::D(
        "[JIT] try_jit_virtual_unwind: Rip=0x{:X} MethodStart=0x{:X} MethodSize=0x{:X} JITType={}",
        ctx.Rip, codeHdr.MethodStart, codeHdr.MethodSize, static_cast<int>(codeHdr.JITType));

    // Step 2: Read CodeHeader (immediately before the JIT code)
    ULONG64 pRealCodeHeader = 0; // pointer into JIT code heap
    SIZE_T  hdrBytesRead    = 0;
    if (!ReadProcessMemory(hProcess,
                           reinterpret_cast<LPCVOID>(codeHdr.MethodStart - 8),
                           &pRealCodeHeader, sizeof(pRealCodeHeader), &hdrBytesRead))
    {
        logging::D(
            "[JIT] ReadProcessMemory for pRealCodeHeader at 0x{:X} failed: error 0x{:X}",
            codeHdr.MethodStart - 8, GetLastError());
        return false;
    }
    logging::D("[JIT] pRealCodeHeader=0x{:X} (read {} bytes)",
        pRealCodeHeader, hdrBytesRead);

    // Guard against stub code blocks (pRealCodeHeader holds a small sentinel enum, not a heap ptr)
    if (pRealCodeHeader <= 0x100)
    {
        logging::D("[JIT] pRealCodeHeader stub sentinel; skipping");
        return false;
    }

    // Step 3: Read nUnwindInfos
    // RealCodeHeader (x64, FEATURE_EH_FUNCLETS)
    //   +0x00  phdrDebugInfo   (8)  +0x08  phdrJitEHInfo (8)
    //   +0x10  phdrJitGCInfo   (8)  +0x18  phdrMDesc     (8)
    //   +0x20  nUnwindInfos    (4)  +0x24  unwindInfos[] (12 bytes each)
    DWORD nUnwindInfos = 0;
    if (!ReadProcessMemory(hProcess,
                           reinterpret_cast<LPCVOID>(pRealCodeHeader + 0x20),
                           &nUnwindInfos, sizeof(nUnwindInfos), nullptr))
    {
        logging::D(
            "[JIT] ReadProcessMemory for nUnwindInfos at 0x{:X}+0x20 failed: error 0x{:X}",
            pRealCodeHeader, GetLastError());
        return false;
    }
    logging::D("[JIT] nUnwindInfos={}", nUnwindInfos);

    if (nUnwindInfos == 0 || nUnwindInfos > 512)
    {
        logging::D("[JIT] nUnwindInfos={} out of range; aborting", nUnwindInfos);
        return false;
    }

    // Step 4: Read RUNTIME_FUNCTION entries
    std::vector<IMAGE_RUNTIME_FUNCTION_ENTRY> rfs(nUnwindInfos);
    if (!ReadProcessMemory(hProcess,
                           reinterpret_cast<LPCVOID>(pRealCodeHeader + 0x24),
                           rfs.data(),
                           static_cast<SIZE_T>(nUnwindInfos) * sizeof(IMAGE_RUNTIME_FUNCTION_ENTRY),
                           nullptr))
    {
        logging::D(
            "[JIT] ReadProcessMemory for RUNTIME_FUNCTIONs at 0x{:X}+0x24 failed: error 0x{:X}",
            pRealCodeHeader, GetLastError());
        return false;
    }

    // codeheapBase: the base from which all BeginAddress / UnwindInfoAddress RVAs are computed
    // rfs[0].BeginAddress == MethodStart - codeheapBase
    const ULONG64 codeheapBase = codeHdr.MethodStart - rfs[0].BeginAddress;
    const DWORD   relIp        = static_cast<DWORD>(ctx.Rip - codeheapBase);

    logging::D("[JIT] codeheapBase=0x{:X} relIp=0x{:X} ({} RUNTIME_FUNCTION entries)",
        codeheapBase, relIp, nUnwindInfos);
    for (DWORD i = 0; i < nUnwindInfos; ++i)
    {
        logging::D(
            "[JIT]   rf[{}]: Begin=0x{:X} End=0x{:X} UnwindInfo=0x{:X}",
            i, rfs[i].BeginAddress, rfs[i].EndAddress, rfs[i].UnwindInfoAddress);
    }

    // Step 5: Find the RUNTIME_FUNCTION entry that covers ctx.Rip
    const IMAGE_RUNTIME_FUNCTION_ENTRY* pEntry = nullptr;
    for (const auto& rf : rfs)
    {
        if (relIp >= rf.BeginAddress && relIp < rf.EndAddress)
        {
            pEntry = &rf;
            break;
        }
    }
    if (!pEntry)
    {
        logging::D(
            "[JIT] No RUNTIME_FUNCTION covers relIp=0x{:X} (codeheapBase=0x{:X})",
            relIp, codeheapBase);
        return false;
    }
    logging::D(
        "[JIT] Matched rf: Begin=0x{:X} End=0x{:X} UnwindInfo=0x{:X}",
        pEntry->BeginAddress, pEntry->EndAddress, pEntry->UnwindInfoAddress);

    // Step 6: Read UNWIND_INFO from target process
    const ULONG64 unwindInfoAddr = codeheapBase + pEntry->UnwindInfoAddress;

    // Read CountOfCodes and Flags
    BYTE hdrUnwind[4] = {};
    if (!ReadProcessMemory(hProcess, reinterpret_cast<LPCVOID>(unwindInfoAddr),
                           hdrUnwind, sizeof(hdrUnwind), nullptr))
    {
        logging::D(
            "[JIT] ReadProcessMemory for UNWIND_INFO header at 0x{:X} failed: error 0x{:X}",
            unwindInfoAddr, GetLastError());
        return false;
    }

    const BYTE uwVersion      = hdrUnwind[0] & 0x7;
    const BYTE uwFlags        = hdrUnwind[0] >> 3;
    const BYTE uwSizeOfProlog = hdrUnwind[1];
    const BYTE uwCountOfCodes = hdrUnwind[2];
    const BYTE uwFrameReg     = hdrUnwind[3] & 0xF;
    const BYTE uwFrameOffset  = hdrUnwind[3] >> 4;
    logging::D(
        "[JIT] UNWIND_INFO at 0x{:X}: Ver={} Flags=0x{:X} SizeOfProlog={} CountOfCodes={} FrameReg={} FrameOff={}",
        unwindInfoAddr, uwVersion, uwFlags, uwSizeOfProlog, uwCountOfCodes, uwFrameReg, uwFrameOffset);
    if (uwFlags & 0x4)
        logging::D("[JIT]   -> UNW_FLAG_CHAININFO set; chained unwind entry present");
    if (uwFlags & 0x1)
        logging::D("[JIT]   -> UNW_FLAG_EHANDLER set");
    if (uwFlags & 0x2)
        logging::D("[JIT]   -> UNW_FLAG_UHANDLER set");

    // Step 7: Manual unwind
    // We can't use RtlVirtualUnwind here because it dereferences RSP into
    // the crash handler and the game's stack is only accessible via ReadProcessMemory
    CONTEXT newCtx = ctx;
    ULONG64 curUnwindAddr = unwindInfoAddr;

    for (int chainDepth = 0; chainDepth < 8; ++chainDepth)
    {
        std::vector<BYTE> uwBuf;
        if (!read_unwind_info_blob(hProcess, curUnwindAddr, uwBuf))
        {
            logging::D("[JIT] Failed to read UNWIND_INFO blob at 0x{:X}", curUnwindAddr);
            return false;
        }

        IMAGE_RUNTIME_FUNCTION_ENTRY chainedRF{};
        if (!apply_unwind_codes(hProcess, newCtx, uwBuf, chainedRF))
            return false;

        // If a chained RF was found, continue with its UNWIND_INFO.
        if (chainedRF.UnwindInfoAddress != 0)
        {
            curUnwindAddr = codeheapBase + chainedRF.UnwindInfoAddress;
            continue;
        }
        break;
    }

    // Read return address from the unwound RSP
    ULONG64 retAddr = 0;
    if (!rpm_u64(hProcess, newCtx.Rsp, retAddr))
    {
        logging::D("[JIT] Failed to read return address from RSP=0x{:X}", newCtx.Rsp);
        return false;
    }
    logging::D("[JIT] RetAddr=0x{:X} from [RSP=0x{:X}]", retAddr, newCtx.Rsp);

    if (retAddr == 0)
    {
        logging::D("[JIT] Return address is zero; stopping");
        return false;
    }

    newCtx.Rip  = retAddr;
    newCtx.Rsp += 8;

    logging::D("[JIT] Unwound: old Rip=0x{:X} -> new Rip=0x{:X} Rsp=0x{:X}",
        ctx.Rip, newCtx.Rip, newCtx.Rsp);

    ctx = newCtx;
    return true;
}

static HRESULT SEH_NOOPT seh_dac_get_task(IXCLRDataProcess* pProc, DWORD osId, IXCLRDataTask** ppTask)
{
    *ppTask = nullptr;
    __seh_try_begin { return pProc->GetTaskByOSThreadID(osId, ppTask); }
    __seh_try_end { return E_FAIL; }
}

static HRESULT SEH_NOOPT seh_dac_create_stack_walk(IXCLRDataTask* pTask, ULONG32 flags, IXCLRDataStackWalk** ppWalk)
{
    *ppWalk = nullptr;
    __seh_try_begin { return pTask->CreateStackWalk(flags, ppWalk); }
    __seh_try_end { return E_FAIL; }
}

static HRESULT SEH_NOOPT seh_dac_walk_get_context(IXCLRDataStackWalk* pWalk, CONTEXT* pCtx)
{
    ULONG32 ctxSize = 0;
    __seh_try_begin { return pWalk->GetContext(CONTEXT_ALL, sizeof(CONTEXT), &ctxSize, reinterpret_cast<BYTE*>(pCtx)); }
    __seh_try_end { return E_FAIL; }
}

static HRESULT SEH_NOOPT seh_dac_walk_get_frame_type(IXCLRDataStackWalk* pWalk,
    CLRDataSimpleFrameType* pSimple, CLRDataDetailedFrameType* pDetailed)
{
    __seh_try_begin { return pWalk->GetFrameType(pSimple, pDetailed); }
    __seh_try_end { return E_FAIL; }
}

static HRESULT SEH_NOOPT seh_dac_walk_next(IXCLRDataStackWalk* pWalk)
{
    __seh_try_begin { return pWalk->Next(); }
    __seh_try_end { return E_FAIL; }
}

static HRESULT SEH_NOOPT seh_dac_walk_set_context2(IXCLRDataStackWalk* pWalk, ULONG32 flags, const CONTEXT* pCtx)
{
    __seh_try_begin
    {
        return pWalk->SetContext2(flags, sizeof(CONTEXT),
            const_cast<BYTE*>(reinterpret_cast<const BYTE*>(pCtx)));
    }
    __seh_try_end { return E_FAIL; }
}

static std::wstring get_thread_name(HANDLE hThread)
{
    PWSTR pName = nullptr;
    if (_GetThreadDescription && SUCCEEDED(_GetThreadDescription(hThread, &pName)) && pName && pName[0] != L'\0')
    {
        std::wstring name(pName);
        LocalFree(pName);
        return name;
    }
    if (pName)
        LocalFree(pName);
    return {};
}

// Walk and log mixed-mode call stack for the passed thread
void print_thread_call_stack(HANDLE hThread, const CONTEXT& ctx, std::wostringstream& log) {
    int frame_index = 0;
    bool dacWalkDone = false;

    if (g_pClrDataProcess)
    {
        const DWORD osThreadId = GetThreadId(hThread);
        IXCLRDataTask* pTask = nullptr;
        if (SUCCEEDED(seh_dac_get_task(g_pClrDataProcess, osThreadId, &pTask)) && pTask)
        {
            IXCLRDataStackWalk* pWalk = nullptr;
            const HRESULT hrCreate = seh_dac_create_stack_walk(
                pTask,
                CLRDATA_SIMPFRAME_UNRECOGNIZED       |
                CLRDATA_SIMPFRAME_MANAGED_METHOD     |
                CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE |
                CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE,
                &pWalk);
            pTask->Release();

            if (SUCCEEDED(hrCreate) && pWalk)
            {
                const auto emit_native_frames = [&](const CONTEXT& startCtx,
                                                    bool skipFirstFrame,
                                                    ULONG64 stopRsp)
                {
                    logging::D(
                        "[NAT] emit_native_frames from Rip=0x{:X} Rsp=0x{:X} skipFirst={} stopRsp=0x{:X}",
                        startCtx.Rip, startCtx.Rsp, skipFirstFrame, stopRsp);

                    STACKFRAME64 sf{};
                    sf.AddrPC.Offset    = startCtx.Rip; sf.AddrPC.Mode    = AddrModeFlat;
                    sf.AddrStack.Offset = startCtx.Rsp; sf.AddrStack.Mode = AddrModeFlat;
                    sf.AddrFrame.Offset = startCtx.Rbp; sf.AddrFrame.Mode = AddrModeFlat;

                    CONTEXT walkCtx = startCtx;
                    bool isFirstResult = true;
                    ULONG64 prevRsp = 0;

                    for (int nativeLimit = 0; nativeLimit < 256; ++nativeLimit)
                    {
                        if (!StackWalk64(IMAGE_FILE_MACHINE_AMD64, g_hProcess, hThread,
                                         &sf, &walkCtx, nullptr,
                                         &SymFunctionTableAccess64, &SymGetModuleBase64, nullptr))
                        {
                            logging::D("[NAT] StackWalk64 returned false, bye bye");
                            break;
                        }

                        const ULONG64 pc  = sf.AddrPC.Offset;
                        const ULONG64 rsp = sf.AddrStack.Offset;

                        logging::D("[NAT] sw64 pc=0x{:X} rsp=0x{:X} ret=0x{:X}",
                            pc, rsp, sf.AddrReturn.Offset);

                        if (pc == 0)                 { logging::D("[NAT] pc==0, stopping");           break; }
                        if (rsp >= stopRsp)           { logging::D("[NAT] rsp>=stopRsp, stopping");    break; }
                        if (rsp != 0 && rsp <= prevRsp){ logging::D("[NAT] rsp went backwards, stopping"); break; }

                        prevRsp = rsp;

                        if (isFirstResult && skipFirstFrame)
                        {
                            isFirstResult = false;
                            logging::D("[NAT] skipping first (already shown)");
                            continue;
                        }
                        isFirstResult = false;

                        log << std::format(L"\n  [{}]\t{}",
                            frame_index++, to_address_string_mixed(pc, g_pClrDataProcess));

                        if (sf.AddrReturn.Offset == 0 || sf.AddrPC.Offset == sf.AddrReturn.Offset)
                        {
                            logging::D("[NAT] AddrReturn==0 or loop, stopping");
                            break;
                        }
                    }
                };

                log << std::format(L"\n  [{}]\t{}", frame_index++,
                    to_address_string_mixed(ctx.Rip, g_pClrDataProcess));

                logging::D("[DAC] crash ctx.Rip=0x{:X} ctx.Rsp=0x{:X} ctx.Rbp=0x{:X}",
                    ctx.Rip, ctx.Rsp, ctx.Rbp);

                const HRESULT hrSetCtx = seh_dac_walk_set_context2(pWalk,
                    static_cast<ULONG32>(CLRDATA_STACK_SET_CURRENT_CONTEXT), &ctx);
                logging::D("[DAC] SetContext2(CURRENT_CONTEXT) hr=0x{:X}", (unsigned)hrSetCtx);

                CONTEXT pendingNativeCtx = ctx;
                bool    pendingNative    = true;
                bool    pendingShown     = true;

                bool    firstFrame       = true;

                CONTEXT frameCtx{};
                bool skipNext = true;

                for (int limit = 0; limit < 512; ++limit)
                {
                    logging::D("[DAC] ---> iteration limit={} skipNext={}", limit, skipNext);

                    if (!skipNext)
                    {
                        const HRESULT hrNext = seh_dac_walk_next(pWalk);
                        logging::D("[DAC] Next() hr=0x{:X}", (unsigned)hrNext);
                        if (hrNext != S_OK)
                        {
                            logging::D("[DAC] Next() stopped walk");
                            break;
                        }
                    }
                    skipNext = false;

                    frameCtx = {};
                    const HRESULT hrGetCtx = seh_dac_walk_get_context(pWalk, &frameCtx);
                    logging::D("[DAC] GetContext hr=0x{:X} Rip=0x{:X} Rsp=0x{:X} Rbp=0x{:X}",
                        (unsigned)hrGetCtx, frameCtx.Rip, frameCtx.Rsp, frameCtx.Rbp);
                    if (FAILED(hrGetCtx) || frameCtx.Rip == 0)
                    {
                        logging::D("[DAC] GetContext failed or Rip==0");
                        break;
                    }

                    CLRDataSimpleFrameType simpleType = CLRDATA_SIMPFRAME_UNRECOGNIZED;
                    CLRDataDetailedFrameType detType  = CLRDATA_DETFRAME_UNRECOGNIZED;
                    const HRESULT hrFT = seh_dac_walk_get_frame_type(pWalk, &simpleType, &detType);
                    logging::D("[DAC] GetFrameType hr=0x{:X} simpleType={} detType={}",
                        (unsigned)hrFT, (int)simpleType, (int)detType);

                    if (simpleType == CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE)
                    {
                        const bool isNativeRip = try_get_managed_method_name(
                            g_pClrDataProcess, frameCtx.Rip).empty();
                        logging::D("[DAC] RUNTIME_UNMANAGED_CODE isNativeRip={}", isNativeRip);

                        if (isNativeRip)
                        {
                            if (!pendingNative || frameCtx.Rsp <= pendingNativeCtx.Rsp)
                            {
                                pendingNativeCtx = frameCtx;
                                pendingShown     = (frameCtx.Rip == ctx.Rip);
                                pendingNative    = true;
                            }
                        }
                        continue;
                    }

                    if (simpleType == CLRDATA_SIMPFRAME_UNRECOGNIZED)
                    {
                        logging::D("[DAC] UNRECOGNIZED frame at 0x{:X}, skipping", frameCtx.Rip);
                        continue;
                    }

                    if (pendingNative)
                    {
                        logging::D("[DAC] Flushing pending native up to Rsp=0x{:X}", frameCtx.Rsp);
                        emit_native_frames(pendingNativeCtx, pendingShown, frameCtx.Rsp);
                        pendingNative = false;
                    }

                    if (firstFrame)
                    {
                        firstFrame = false;
                        if (frameCtx.Rip == ctx.Rip)
                        {
                            logging::D("[DAC] firstFrame managed duplicate, skipping");
                            continue;
                        }
                    }

                    const auto name = try_get_managed_method_name(g_pClrDataProcess, frameCtx.Rip);
                    if (!name.empty())
                    {
                        logging::D("[DAC] managed frame: {}", name);

                        log << std::format(L"\n  [{}]\t{}", frame_index++, name);
                    }
                    else
                    {
                        logging::D("[DAC] managed frame (no name): 0x{:X}", frameCtx.Rip);
                        log << std::format(L"\n  [{}]\t{}", frame_index++,
                            to_address_string(frameCtx.Rip, false));
                    }
                }
                logging::D("[DAC] walk loop ended");

                if (pendingNative)
                {
                    logging::D("[DAC] emitting residual native segment");
                    emit_native_frames(pendingNativeCtx, pendingShown, UINT64_MAX);
                }
                else if (g_pSosDac && frameCtx.Rip != 0)
                {
                    logging::D(
                        "[DAC] last frame was managed (Rip=0x{:X}); attempting JIT unwind "
                        "to recover native frames below reverse-P/Invoke boundary",
                        frameCtx.Rip);

                    CONTEXT jitCtx = frameCtx;
                    bool reachedNative = false;
                    for (int jitLimit = 0; jitLimit < 64 && jitCtx.Rip != 0; ++jitLimit)
                    {
                        const bool isManaged =
                            !try_get_managed_method_name(g_pClrDataProcess, jitCtx.Rip).empty();

                        if (!isManaged)
                        {
                            logging::D("[JIT] Reached native Rip=0x{:X} after {} unwind step(s)",
                                jitCtx.Rip, jitLimit);
                            emit_native_frames(jitCtx, /*skipFirstFrame=*/false, UINT64_MAX);
                            reachedNative = true;
                            break;
                        }

                        if (!try_jit_virtual_unwind(g_hProcess, g_pSosDac, jitCtx))
                        {
                            logging::D("[JIT] try_jit_virtual_unwind failed; stopping JIT unwind");
                            break;
                        }
                    }

                    if (!reachedNative)
                    {
                        logging::D("[JIT] Could not reach native frames via JIT unwind");
                    }
                }
                else
                {
                    logging::D("[DAC] last frame was managed and we have no SOS");
                }

                dacWalkDone = true;
                pWalk->Release();
            }
        }
    }

    if (!dacWalkDone)
    {
        STACKFRAME64 sf{};
        sf.AddrPC.Offset    = ctx.Rip; sf.AddrPC.Mode    = AddrModeFlat;
        sf.AddrStack.Offset = ctx.Rsp; sf.AddrStack.Mode = AddrModeFlat;
        sf.AddrFrame.Offset = ctx.Rbp; sf.AddrFrame.Mode = AddrModeFlat;

        log << std::format(L"\n  [{}]\t{}", frame_index++, to_address_string_mixed(sf.AddrPC.Offset, g_pClrDataProcess));

        const auto appendContextToLog = [&](const CONTEXT& ctxWalk) {
            log << std::format(L"\n  [{}]\t{}", frame_index++, to_address_string_mixed(sf.AddrPC.Offset, g_pClrDataProcess));
        };

        const auto tryStackWalk = [&] SEH_NOOPT {
            __seh_try_begin {
                CONTEXT ctxWalk = ctx;
                do {
                    if (!StackWalk64(IMAGE_FILE_MACHINE_AMD64, g_hProcess, hThread, &sf, &ctxWalk, nullptr, &SymFunctionTableAccess64, &SymGetModuleBase64, nullptr))
                        break;

                    appendContextToLog(ctxWalk);

                } while (sf.AddrReturn.Offset != 0 && sf.AddrPC.Offset != sf.AddrReturn.Offset);
                return true;
            } __seh_try_end {
                return false;
            }
        };

        if (!tryStackWalk())
            log << L"\n  Access violation while walking up the stack.";
    }
}

void print_exception_info(DWORD threadId, HANDLE hThread, const EXCEPTION_POINTERS& ex, const CONTEXT& ctx, std::wostringstream& log) {
    std::vector<EXCEPTION_RECORD> exRecs;
    if (ex.ExceptionRecord)
    {
        size_t rec_index = 0;
        size_t read;

        for (auto pRemoteExRec = ex.ExceptionRecord;
             pRemoteExRec && rec_index < 64;
             rec_index++)
        {
            exRecs.emplace_back();

            if (!ReadProcessMemory(g_hProcess, pRemoteExRec, &exRecs.back(), sizeof exRecs.back(), &read)
                || read < offsetof(EXCEPTION_RECORD, ExceptionInformation)
                || read < static_cast<size_t>(reinterpret_cast<const char*>(&exRecs.back().ExceptionInformation[exRecs.
                    back().NumberParameters]) - reinterpret_cast<const char*>(&exRecs.back())))
            {
                exRecs.pop_back();
                break;
            }

            log << std::format(L"\nException Info #{}\n", rec_index);
            log << std::format(L"Address: {:X}\n", exRecs.back().ExceptionCode);
            log << std::format(L"Flags: {:X}\n", exRecs.back().ExceptionFlags);
            log << std::format(L"Address: {:X}\n", reinterpret_cast<size_t>(exRecs.back().ExceptionAddress));
            if (exRecs.back().NumberParameters)
            {
                log << L"Parameters: ";
                for (DWORD i = 0; i < exRecs.back().NumberParameters; ++i)
                {
                    if (i != 0)
                        log << L", ";
                    log << std::format(L"{:X}", exRecs.back().ExceptionInformation[i]);
                }
            }

            pRemoteExRec = exRecs.back().ExceptionRecord;
        }
    }

    const std::wstring crashThreadName = get_thread_name(hThread);

    log << std::format(L"\nThread: 0x{:X}", threadId);
    if (!crashThreadName.empty())
        log << std::format(L" ({})\n", crashThreadName);

    log << L"\n" << L"Call Stack" << L"\n{";
    print_thread_call_stack(hThread, ctx, log);
    log << L"\n}\n";
}

// Walk and log the call stacks of every thread in the target process except the crashing thread
// Each thread is briefly suspended
void print_all_threads_info(DWORD crashingThreadId, std::wostringstream& log)
{
    const HANDLE hSnap = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
    if (hSnap == INVALID_HANDLE_VALUE)
    {
        log << std::format(L"\n[All Threads] CreateToolhelp32Snapshot failed: 0x{:X}\n",
            GetLastError());
        return;
    }
    std::unique_ptr<std::remove_pointer_t<HANDLE>, decltype(&CloseHandle)>
        hSnapGuard(hSnap, &CloseHandle);

    const DWORD targetPid = GetProcessId(g_hProcess);

    THREADENTRY32 te{};
    te.dwSize = sizeof(te);
    if (!Thread32First(hSnap, &te))
        return;

    do
    {
        if (te.th32OwnerProcessID != targetPid)
            continue;
        if (te.th32ThreadID == crashingThreadId)
            continue;

        const HANDLE hThread = OpenThread(
            THREAD_SUSPEND_RESUME | THREAD_GET_CONTEXT | THREAD_QUERY_INFORMATION,
            FALSE, te.th32ThreadID);
        if (!hThread)
        {
            log << std::format(L"\nThread 0x{:X} Call Stack\n{{\n"
                               L"  (OpenThread failed: error 0x{:X})\n}}\n",
                te.th32ThreadID, GetLastError());
            continue;
        }
        std::unique_ptr<std::remove_pointer_t<HANDLE>, decltype(&CloseHandle)>
            hThreadGuard(hThread, &CloseHandle);

        const DWORD suspendCount = SuspendThread(hThread);
        if (suspendCount == static_cast<DWORD>(-1))
        {
            log << std::format(L"\nThread 0x{:X} Call Stack\n{{\n"
                               L"  (SuspendThread failed: error 0x{:X})\n}}\n",
                te.th32ThreadID, GetLastError());
            continue;
        }

        CONTEXT ctx{};
        ctx.ContextFlags = CONTEXT_ALL;
        const bool gotCtx = GetThreadContext(hThread, &ctx) != 0;

        ResumeThread(hThread);

        if (!gotCtx)
        {
            log << std::format(L"\nThread 0x{:X} Call Stack\n{{\n"
                               L"  (GetThreadContext failed: error 0x{:X})\n}}\n",
                te.th32ThreadID, GetLastError());
            continue;
        }

        const std::wstring threadName = get_thread_name(hThread);
        const std::wstring threadLabel = threadName.empty()
            ? std::format(L"Thread 0x{:X}", te.th32ThreadID)
            : std::format(L"Thread 0x{:X} \"{}\"", te.th32ThreadID, threadName);

        log << std::format(L"\n{} Call Stack\n{{", threadLabel);
        print_thread_call_stack(hThread, ctx, log);
        log << L"\n}\n";

    } while (Thread32Next(hSnap, &te));
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

void open_folder_and_select_items(HWND hwndOpener, const std::wstring& path) {
    const auto piid = ILCreateFromPathW(path.c_str());
    if (!piid
        || FAILED(SHOpenFolderAndSelectItems(piid, 0, nullptr, 0))) {
        const auto args = std::format(L"/select,{}", escape_shell_arg(path));
        SHELLEXECUTEINFOW seiw{
            .cbSize = sizeof seiw,
            .hwnd = hwndOpener,
            .lpFile = L"explorer.exe",
            .lpParameters = args.c_str(),
            .nShow = SW_SHOW,
        };
        if (!ShellExecuteExW(&seiw))
            throw_last_error("ShellExecuteExW");
    }

    if (piid)
        ILFree(piid);
}

std::vector<IDXGIAdapter1*> enum_dxgi_adapters()
{
    std::vector<IDXGIAdapter1*> vAdapters;

    IDXGIFactory1* pFactory = NULL;
    if (FAILED(CreateDXGIFactory1(__uuidof(IDXGIFactory1), (void**)&pFactory)))
    {
        return vAdapters;
    }

    IDXGIAdapter1* pAdapter;
    for (UINT i = 0;
        pFactory->EnumAdapters1(i, &pAdapter) != DXGI_ERROR_NOT_FOUND;
        ++i)
    {
        vAdapters.push_back(pAdapter);
    }

    if (pFactory)
    {
        pFactory->Release();
    }

    return vAdapters;
}

void export_tspack(HWND hWndParent, const std::filesystem::path& logDir, const std::string& crashLog, const std::string& troubleshootingPackData) {
    static const char* SourceLogFiles[] = {
        "output.log", // XIVLauncher for Windows
        "launcher.log", // XIVLauncher.Core for [mostly] Linux
        "patcher.log",
        "dalamud.log",
        "dalamud.troubleshooting.json",
        "dalamud.injector.log",
        "dalamud.boot.log",
        "aria.log",
        "wine.log"
    };
    static constexpr auto MaxSizePerLog = 1 * 1024 * 1024;
    static constexpr std::array<COMDLG_FILTERSPEC, 2> OutputFileTypeFilterSpec{{
        { L"Dalamud Troubleshooting Pack File (*.tspack)", L"*.tspack" },
        { L"All files (*.*)", L"*" },
    }};

    std::optional<std::wstring> filePath;
    try {
        IShellItemPtr pItem;
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

        PWSTR pFilePath = nullptr;
        throw_if_failed(pItem->GetDisplayName(SIGDN_FILESYSPATH, &pFilePath), {}, "pItem->GetDisplayName");
        pItem.Release();
        filePath.emplace(pFilePath);

        std::fstream fileStream(std::filesystem::path(*filePath), std::ios::binary | std::ios::in | std::ios::out | std::ios::trunc);

        mz_zip_archive zipa{};
        zipa.m_pIO_opaque = &fileStream;
        zipa.m_pRead = [](void* pOpaque, mz_uint64 file_ofs, void* pBuf, size_t n) -> size_t {
            const auto pStream = static_cast<std::fstream*>(pOpaque);
            if (!pStream || !pStream->is_open())
                throw std::runtime_error("Read operation failed: Stream is not open");
            pStream->seekg(file_ofs, std::ios::beg);
            if (pStream->fail())
                throw std::runtime_error("Read operation failed: Error seeking in stream");
            pStream->read(static_cast<char*>(pBuf), n);
            if (pStream->fail())
                throw std::runtime_error("Read operation failed: Error reading from stream");
            return pStream->gcount();
        };
        zipa.m_pWrite = [](void* pOpaque, mz_uint64 file_ofs, const void* pBuf, size_t n) -> size_t {
            const auto pStream = static_cast<std::fstream*>(pOpaque);
            if (!pStream || !pStream->is_open())
                throw std::runtime_error("Write operation failed: Stream is not open");
            pStream->seekp(file_ofs, std::ios::beg);
            if (pStream->fail())
                throw std::runtime_error("Write operation failed: Error seeking in stream");
            pStream->write(static_cast<const char*>(pBuf), n);
            if (pStream->fail())
                throw std::runtime_error("Write operation failed: Error writing to stream");
            return n;
        };
        const auto mz_throw_if_failed = [&zipa](mz_bool res, const std::string& clue) {
            if (!res)
                throw std::runtime_error(std::format("Failed to save file at {}: mz_error={} description={}", clue, static_cast<int>(mz_zip_get_last_error(&zipa)), mz_zip_get_error_string(mz_zip_get_last_error(&zipa))));
        };

        mz_throw_if_failed(mz_zip_writer_init_v2(&zipa, 0, 0), "mz_zip_writer_init_v2");
        mz_throw_if_failed(mz_zip_writer_add_mem(&zipa, "trouble.json", troubleshootingPackData.data(), troubleshootingPackData.size(), MZ_ZIP_FLAG_WRITE_HEADER_SET_SIZE | MZ_BEST_COMPRESSION), "mz_zip_writer_add_mem: trouble.json");
        mz_throw_if_failed(mz_zip_writer_add_mem(&zipa, "crash.log", crashLog.data(), crashLog.size(), MZ_ZIP_FLAG_WRITE_HEADER_SET_SIZE | MZ_BEST_COMPRESSION), "mz_zip_writer_add_mem: crash.log");
        std::string logExportLog;

        struct HandleAndBaseOffset {
            HANDLE h;
            int64_t off;
        };
        const auto fnHandleReader = [](void* pOpaque, mz_uint64 file_ofs, void* pBuf, size_t n) -> size_t {
            const auto& info = *static_cast<const HandleAndBaseOffset*>(pOpaque);
            if (!SetFilePointerEx(info.h, { .QuadPart = static_cast<int64_t>(info.off + file_ofs) }, nullptr, SEEK_SET))
                throw_last_error("fnHandleReader: SetFilePointerEx");
            if (DWORD read; !ReadFile(info.h, pBuf, static_cast<DWORD>(n), &read, nullptr))
                throw_last_error("fnHandleReader: ReadFile");
            else
                return read;
        };
        for (const auto& pcszLogFileName : SourceLogFiles) {
            const auto logFilePath = logDir / pcszLogFileName;
            if (!exists(logFilePath)) {
                logExportLog += std::format("File does not exist: {}\n", ws_to_u8(logFilePath.wstring()));
                continue;
            } else {
                logExportLog += std::format("Including: {}\n", ws_to_u8(logFilePath.wstring()));
            }

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
            WIN32_FILE_ATTRIBUTE_DATA fileInfo = { 0 };
            time_t modt = time(nullptr);
            if (GetFileAttributesExW(logFilePath.c_str(), GetFileExInfoStandard, &fileInfo)) {
                ULARGE_INTEGER ull = { 0 };
                ull.LowPart = fileInfo.ftLastWriteTime.dwLowDateTime;
                ull.HighPart = fileInfo.ftLastWriteTime.dwHighDateTime;
                modt = ull.QuadPart / 10000000ULL - 11644473600ULL;
            }
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

        mz_throw_if_failed(mz_zip_writer_add_mem(&zipa, "logexport.log", logExportLog.data(), logExportLog.size(), MZ_ZIP_FLAG_WRITE_HEADER_SET_SIZE | MZ_BEST_COMPRESSION), "mz_zip_writer_add_mem: logexport.log");
        mz_throw_if_failed(mz_zip_writer_finalize_archive(&zipa), "mz_zip_writer_finalize_archive");
        mz_throw_if_failed(mz_zip_writer_end(&zipa), "mz_zip_writer_end");

    } catch (const std::exception& e) {
        MessageBoxW(hWndParent, std::format(L"Failed to save file: {}", u8_to_ws(e.what())).c_str(), get_window_string(hWndParent).c_str(), MB_OK | MB_ICONERROR);
        if (filePath) {
            try {
                std::filesystem::remove(*filePath);
            } catch (const std::filesystem::filesystem_error& e2) {
                logging::E("Failed to remove temporary file: {}", e2.what());
            }
        }
        return;
    }

    if (filePath) {
        // Not sure why, but without the wait, the selected file momentarily disappears and reappears
        Sleep(1000);
        open_folder_and_select_items(hWndParent, *filePath);
    }
}

enum {
    IdRadioRestartNormal = 101,
    IdRadioRestartWithout3pPlugins,
    IdRadioRestartWithoutPlugins,
    IdRadioRestartWithoutDalamud,

    IdButtonRestart = 201,
    IdButtonSaveTsPack = 202,
    IdButtonHelp = IDHELP,
    IdButtonExit = IDCANCEL,
};

void restart_game_using_injector(int nRadioButton, const std::vector<std::wstring>& launcherArgs)
{
    std::wstring pathStr(PATHCCH_MAX_CCH, L'\0');
    pathStr.resize(GetModuleFileNameExW(GetCurrentProcess(), GetModuleHandleW(nullptr), &pathStr[0], PATHCCH_MAX_CCH));

    std::vector<std::wstring> args;
    std::wstring injectorPath = (std::filesystem::path(pathStr).parent_path() / L"Dalamud.Injector.exe").wstring();
    args.emplace_back(L'\"' + injectorPath + L'\"');
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
    args.insert(args.end(), launcherArgs.begin(), launcherArgs.end());

    std::wstring argstr;
    for (const auto& arg : args) {
        argstr.append(arg);
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
    if (CreateProcessW(injectorPath.c_str(), &argstr[0], nullptr, nullptr, FALSE, 0, nullptr, nullptr, &si, &pi)) {
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    } else {
        MessageBoxW(nullptr, std::format(L"Failed to restart: 0x{:x}", GetLastError()).c_str(), L"Dalamud Boot", MB_ICONERROR | MB_OK);
    }
}

void get_cpu_info(wchar_t *vendor, wchar_t *brand)
{
    // Gotten and reformatted to not include all data as listed at https://learn.microsoft.com/en-us/cpp/intrinsics/cpuid-cpuidex?view=msvc-170#example

    // int cpuInfo[4] = {-1};
    std::array<int, 4> cpui;
    int nIds_;
    int nExIds_;
    std::vector<std::array<int, 4>> data_;
    std::vector<std::array<int, 4>> extdata_;
    size_t convertedChars = 0;

    // Calling __cpuid with 0x0 as the function_id argument
    // gets the number of the highest valid function ID.
    __cpuid(cpui.data(), 0);
    nIds_ = cpui[0];

    for (int i = 0; i <= nIds_; ++i)
    {
        __cpuidex(cpui.data(), i, 0);
        data_.push_back(cpui);
    }

    // Capture vendor string
    char vendorA[0x20];
    memset(vendorA, 0, sizeof(vendorA));
    *reinterpret_cast<int *>(vendorA) = data_[0][1];
    *reinterpret_cast<int *>(vendorA + 4) = data_[0][3];
    *reinterpret_cast<int *>(vendorA + 8) = data_[0][2];
    mbstowcs_s(&convertedChars, vendor, 0x20, vendorA, _TRUNCATE);

    // Calling __cpuid with 0x80000000 as the function_id argument
    // gets the number of the highest valid extended ID.
    __cpuid(cpui.data(), 0x80000000);
    nExIds_ = cpui[0];

    for (int i = 0x80000000; i <= nExIds_; ++i)
    {
        __cpuidex(cpui.data(), i, 0);
        extdata_.push_back(cpui);
    }

    // Interpret CPU brand string if reported
    if (nExIds_ >= 0x80000004)
    {
        char brandA[0x40];
        memset(brandA, 0, sizeof(brandA));
        memcpy(brandA, extdata_[2].data(), sizeof(cpui));
        memcpy(brandA + 16, extdata_[3].data(), sizeof(cpui));
        memcpy(brandA + 32, extdata_[4].data(), sizeof(cpui));
        mbstowcs_s(&convertedChars, brand, 0x40, brandA, _TRUNCATE);
    }
}

int main() {
    logging::set_tag("CRASHHANDLER");
    logging::update_dll_load_status(true);

    enum crash_handler_special_exit_codes {
        UnknownError = -99,
        InvalidParameter = -101,
        ProcessExitedUnknownExitCode = -102,
    };

    HANDLE hPipeRead = nullptr;
    std::filesystem::path assetDir, logDir;
    std::filesystem::path bootLogPath;
    bool bootConsole = false;
    std::optional<std::vector<std::wstring>> launcherArgs;
    auto fullDump = false;

    // IFileSaveDialog only works on STA
    CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);

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
        } else if (constexpr wchar_t pwszArgPrefix[] = L"--log-path="; arg.starts_with(pwszArgPrefix)) {
            bootLogPath = arg.substr(ARRAYSIZE(pwszArgPrefix) - 1);
        } else if (arg == L"--console") {
            bootConsole = true;
        } else if (arg == L"--") {
            launcherArgs.emplace();
        } else {
            logging::E("Invalid argument: {}", std::wstring(arg));
            return InvalidParameter;
        }
    }

    if (g_hProcess == nullptr) {
        logging::E("Target process not specified");
        return InvalidParameter;
    }

    if (hPipeRead == nullptr) {
        logging::E("Read pipe handle not specified");
        return InvalidParameter;
    }

    const auto dwProcessId = GetProcessId(g_hProcess);
    if (!dwProcessId){
        logging::E("Target process not specified");
        return InvalidParameter;
    }

    if (!bootLogPath.empty()) {
        const auto crashLogPath = bootLogPath.parent_path() / "dalamud.crashhandler.log";
        try {
            logging::start_file_logging(crashLogPath, !bootConsole);
            logging::I("Logging to file: {}", crashLogPath);
        } catch (const std::exception& e) {
            logging::E("Couldn't open log file {}: {}", crashLogPath, e.what());
        }
    }

    if (logDir.filename().wstring().ends_with(L".log")) {
        logging::W("logDir seems to be pointing to a file; stripping the last path component.");
        logging::I("Previous: {}", logDir);
        logDir = logDir.parent_path();
        logging::I("Stripped: {}", logDir);
    }

    // Only keep the last 3 minidumps
    if (!logDir.empty())
    {
        std::vector<std::pair<std::filesystem::path, std::filesystem::file_time_type>> minidumps;
        for (const auto& entry : std::filesystem::directory_iterator(logDir)) {
            if (entry.path().filename().wstring().ends_with(L".dmp")) {
                minidumps.emplace_back(entry.path(), std::filesystem::last_write_time(entry));
            }
        }

        if (minidumps.size() > 3)
        {
            std::sort(minidumps.begin(), minidumps.end(), [](const auto& a, const auto& b) { return a.second < b.second; });
            for (size_t i = 0; i < minidumps.size() - 3; i++) {
                if (std::filesystem::exists(minidumps[i].first))
                {
                    logging::I("Removing old minidump: {}", minidumps[i].first);
                    std::filesystem::remove(minidumps[i].first);
                }

                // Also remove corresponding .log, if it exists
                if (const auto logPath = minidumps[i].first.replace_extension(L".log"); std::filesystem::exists(logPath)) {
                    logging::I("Removing corresponding log: {}", logPath);
                    std::filesystem::remove(logPath);
                }
            }
        }
    }

    while (true) {
        logging::I("Waiting for crash...");

        exception_info exinfo;
        if (DWORD exsize{}; !ReadFile(hPipeRead, &exinfo, static_cast<DWORD>(sizeof exinfo), &exsize, nullptr) || exsize != sizeof exinfo) {
            if (WaitForSingleObject(g_hProcess, 0) == WAIT_OBJECT_0) {
                auto excode = static_cast<DWORD>(ProcessExitedUnknownExitCode);
                if (!GetExitCodeProcess(g_hProcess, &excode))
                    logging::E("Process exited, but failed to read exit code; error: 0x{:x}", GetLastError());
                else
                    logging::I("Process exited with exit code {0} (0x{0:x})", excode);
                break;
            }

            const auto err = GetLastError();
            logging::E("Failed to read exception information; error: 0x{:x}", err);
            logging::W("Terminating target process.");
            TerminateProcess(g_hProcess, -1);
            break;
        }

        if (exinfo.ExceptionRecord.ExceptionCode == 0x12345678) {
            logging::I("Restart requested");
            TerminateProcess(g_hProcess, 0);
            restart_game_using_injector(IdRadioRestartNormal, *launcherArgs);
            break;
        }

        logging::I("Crash triggered");

        logging::I("Creating progress window");
        IProgressDialog* pProgressDialog = NULL;
        if (SUCCEEDED(CoCreateInstance(CLSID_ProgressDialog, NULL, CLSCTX_ALL, IID_IProgressDialog, (void**)&pProgressDialog)) && pProgressDialog) {
            pProgressDialog->SetTitle(L"Dalamud Crash Handler");
            pProgressDialog->SetLine(1, L"The game has crashed!", FALSE, NULL);
            pProgressDialog->SetLine(2, L"Dalamud is collecting further information...", FALSE, NULL);
            pProgressDialog->SetLine(3, L"Refreshing Game Module List", FALSE, NULL);
            pProgressDialog->StartProgressDialog(NULL, NULL, PROGDLG_MARQUEEPROGRESS | PROGDLG_NOCANCEL | PROGDLG_NOMINIMIZE, NULL);
            IOleWindow* pOleWindow;
            HRESULT hr = pProgressDialog->QueryInterface(IID_IOleWindow, (LPVOID*)&pOleWindow);
            if (SUCCEEDED(hr))
            {
                HWND hwndProgressDialog = NULL;
                hr = pOleWindow->GetWindow(&hwndProgressDialog);
                if (SUCCEEDED(hr))
                {
                    SetWindowPos(hwndProgressDialog, HWND_TOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    SetForegroundWindow(hwndProgressDialog);
                }

                pOleWindow->Release();
            }

        }
        else {
            logging::W("Failed to create progress window");
            pProgressDialog = NULL;
        }

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
            logging::I("Init symbols with PDB at {}", symbol_search_path);

            SymRefreshModuleList(g_hProcess);
        }
        else
        {
            g_bSymbolsAvailable = SymInitializeW(g_hProcess, nullptr, true);
            logging::I("Init symbols without PDB");
        }

        if (!g_bSymbolsAvailable) {
            logging::E("SymInitialize error: 0x{:x}", GetLastError());
        }

        // Initialize (or refresh) the DAC for mixed-mode stack walking.
        // The crashing thread OS ID and its context at crash time are needed to seed the data target.
        if (pProgressDialog)
            pProgressDialog->SetLine(3, L"Initializing DAC for mixed-mode trace", FALSE, NULL);

        const DWORD crashingThreadOsId = GetThreadId(exinfo.hThreadHandle);
        if (g_pClrDataProcess)
        {
            // Flush cached DAC state for a fresh read on this crash.
            g_pClrDataProcess->Flush();
            logging::D("[DAC] Flushed cached state for new crash");
        }
        else
        {
            g_pClrDataProcess = try_create_clr_data_process(crashingThreadOsId, exinfo.ContextRecord);
        }

        if (pProgressDialog)
            pProgressDialog->SetLine(3, L"Reading troubleshooting data", FALSE, NULL);

        std::wstring stackTrace(exinfo.dwStackTraceLength, L'\0');
        if (exinfo.dwStackTraceLength) {
            if (DWORD read; !ReadFile(hPipeRead, &stackTrace[0], 2 * exinfo.dwStackTraceLength, &read, nullptr)) {
                logging::E("Failed to read supplied stack trace: error 0x{:x}", GetLastError());
            }
        }

        std::string troubleshootingPackData(exinfo.dwTroubleshootingPackDataLength, '\0');
        if (exinfo.dwTroubleshootingPackDataLength) {
            if (DWORD read; !ReadFile(hPipeRead, &troubleshootingPackData[0], exinfo.dwTroubleshootingPackDataLength, &read, nullptr)) {
                logging::E("Failed to read troubleshooting pack data: error 0x{:x}", GetLastError());
            }
        }

        if (pProgressDialog)
            pProgressDialog->SetLine(3, fullDump ? L"Creating full dump" : L"Creating minidump", FALSE, NULL);

        SYSTEMTIME st;
        GetLocalTime(&st);
        const auto dalamudLogPath = logDir.empty() ? std::filesystem::path() : logDir / L"Dalamud.log";
        const auto dumpPath = logDir.empty() ? std::filesystem::path() : logDir / std::format(L"dalamud_appcrash_{:04}{:02}{:02}_{:02}{:02}{:02}_{:03}_{}.dmp", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, dwProcessId);
        const auto logPath = logDir.empty() ? std::filesystem::path() : logDir / std::format(L"dalamud_appcrash_{:04}{:02}{:02}_{:02}{:02}{:02}_{:03}_{}.log", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, dwProcessId);
        std::wstring dumpError;
        if (dumpPath.empty()) {
            logging::I("Skipping dump path, as log directory has not been specified");
        } else if (shutup) {
            logging::I("Skipping dump, was shutdown");
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
                    dumpError = std::format(L"CreateFileW({}, GENERIC_READ | GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, 0, nullptr) error: 0x{:x}", dumpPath.wstring(), GetLastError());
                    logging::E(dumpError);
                    break;
                }

                std::unique_ptr<std::remove_pointer_t<HANDLE>, decltype(&CloseHandle)> hDumpFilePtr(hDumpFile, &CloseHandle);
                if (!MiniDumpWriteDump(g_hProcess, dwProcessId, hDumpFile, fullDump ? MiniDumpWithFullMemory : static_cast<MINIDUMP_TYPE>(MiniDumpWithDataSegs | MiniDumpWithModuleHeaders), &mdmp_info, nullptr, nullptr)) {
                    dumpError = std::format(L"MiniDumpWriteDump(0x{:x}, {}, 0x{:x}({}), MiniDumpWithFullMemory, ..., nullptr, nullptr) error: 0x{:x}", reinterpret_cast<size_t>(g_hProcess), dwProcessId, reinterpret_cast<size_t>(hDumpFile), dumpPath.wstring(), GetLastError());
                    logging::E(dumpError);
                    break;
                }

                logging::I("Dump written to path: {}", dumpPath);
            } while (false);
        }

        const bool is_external_event = exinfo.ExceptionRecord.ExceptionCode == CUSTOM_EXCEPTION_EXTERNAL_EVENT;

        std::wostringstream log;
        wchar_t vendor[0x20];
        wchar_t brand[0x40];
        get_cpu_info(vendor, brand);

        if (!is_external_event)
        {
            log << std::format(L"Unhandled native exception occurred at {}", to_address_string(exinfo.ContextRecord.Rip, false)) << std::endl;
            log << std::format(L"Code: {:X}", exinfo.ExceptionRecord.ExceptionCode) << std::endl;
        }
        else
        {
            log << L"CLR error occurred" << std::endl;
        }

        if (shutup)
            log << L"======= Crash handler was globally muted(shutdown?) =======" << std::endl;

        if (dumpPath.empty())
            log << L"Dump skipped" << std::endl;
        else if (dumpError.empty())
            log << std::format(L"Dump at: {}", dumpPath.wstring()) << std::endl;
        else
            log << std::format(L"Dump error: {}", dumpError) << std::endl;
        log << std::format(L"System Time: {0:%F} {0:%T} {0:%Ez}", std::chrono::system_clock::now()) << std::endl;
        log << std::format(L"CPU Vendor: {}", vendor) << std::endl;
        log << std::format(L"CPU Brand: {}", brand) << std::endl;

        for (IDXGIAdapter1* adapter : enum_dxgi_adapters()) {
            DXGI_ADAPTER_DESC1 adapterDescription{};
            adapter->GetDesc1(&adapterDescription);
            log << std::format(L"GPU Desc: {}", adapterDescription.Description) << std::endl;
        }

        if (!stackTrace.empty())
        {
            log << L"\nAdditional Information\n{";
            log << L"\n" << stackTrace;
            log << L"\n}\n";
        }

        if (pProgressDialog)
            pProgressDialog->SetLine(3, L"Refreshing Module List", FALSE, NULL);

        SymRefreshModuleList(GetCurrentProcess());
        print_exception_info(crashingThreadOsId, exinfo.hThreadHandle, exinfo.ExceptionPointers, exinfo.ContextRecord, log);

        // Capture the log content we show in the dialog window (after the call stack is appended).
        const std::wstring window_log_str = log.str();

        print_exception_info_extended(exinfo.ExceptionPointers, exinfo.ContextRecord, log);

        log << L"\n======= All threads follow (except crashing thread) =======\n";
        print_all_threads_info(crashingThreadOsId, log);
        if (const auto temp = ws_to_u8(log.str()); !temp.empty()) {
            std::ofstream(logPath, std::ios::binary).write(temp.data(), temp.size());
        } else {
            // for some reason couldn't be converted to UTF-8; write in UTF-16
            const auto temp2 = log.str();
            const auto temp3 = std::span(reinterpret_cast<const char*>(temp2.data()), temp2.size() * sizeof(temp2[0]));
            std::ofstream(logPath, std::ios::binary).write(temp3.data(), temp3.size());
        }

        TASKDIALOGCONFIG config = { 0 };

        const TASKDIALOG_BUTTON radios[]{
            {IdRadioRestartNormal, L"Restart normally"},
            {IdRadioRestartWithout3pPlugins, L"Restart without custom repository plugins"},
            {IdRadioRestartWithoutPlugins, L"Restart without any plugins"},
            {IdRadioRestartWithoutDalamud, L"Restart without Dalamud"},
        };

        const TASKDIALOG_BUTTON buttons[]{
            {IdButtonRestart, L"Restart\nRestart the game with the above-selected option."},
            {IdButtonSaveTsPack, L"Save Troubleshooting Info\nSave a .tspack file containing information about this crash for analysis."},
            {IdButtonExit, L"Exit\nExit without doing anything."},
        };

        config.cbSize = sizeof(config);
        config.hInstance = GetModuleHandleW(nullptr);
        config.dwFlags = TDF_ENABLE_HYPERLINKS | TDF_CAN_BE_MINIMIZED | TDF_ALLOW_DIALOG_CANCELLATION | TDF_USE_COMMAND_LINKS | TDF_NO_DEFAULT_RADIO_BUTTON;
        config.pszMainIcon = MAKEINTRESOURCE(IDI_ICON1);
        config.pszMainInstruction = L"An error in the game occurred";
        config.pszContent = (L""
            R"aa(The game has to close. This error may be caused by a faulty plugin, a broken mod, any other third-party tool, or simply a bug in the game.)aa" "\n"
            "\n"
            R"aa(Try running a game repair in XIVLauncher by right clicking the login button, and disabling plugins you don't need. Please also check your antivirus, see our <a href="help">help site</a> for more information.)aa" "\n"
            "\n"
            R"aa(For further assistance, please upload <a href="exporttspack">a troubleshooting pack</a> to our <a href="discord">Discord server</a>.)aa" "\n"

        );
        config.pButtons = buttons;
        config.cButtons = ARRAYSIZE(buttons);
        config.nDefaultButton = IdButtonRestart;
        config.pszExpandedControlText = L"Hide further information";
        config.pszCollapsedControlText = L"Further information for developers";
        config.pszExpandedInformation = window_log_str.c_str();
        config.pszWindowTitle = L"Dalamud Crash Handler";
        config.pRadioButtons = radios;
        config.cRadioButtons = ARRAYSIZE(radios);
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
                    SendMessage(hwnd, TDM_ENABLE_BUTTON, IdButtonRestart, 0);
                    return S_OK;
                }
                case TDN_HYPERLINK_CLICKED:
                {
                    const auto link = std::wstring_view(reinterpret_cast<const wchar_t*>(lParam));
                    if (link == L"help") {
                        ShellExecuteW(hwnd, nullptr, L"https://goatcorp.github.io/faq?utm_source=vectored", nullptr, nullptr, SW_SHOW);
                    } else if (link == L"logdir") {
                        open_folder_and_select_items(hwnd, logPath.wstring());
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
                case TDN_RADIO_BUTTON_CLICKED:
                    SendMessage(hwnd, TDM_ENABLE_BUTTON, IdButtonRestart, 1);
                    return S_OK;
                case TDN_BUTTON_CLICKED:
                    const auto button = static_cast<int>(wParam);
                    if (button == IdButtonSaveTsPack)
                    {
                        export_tspack(hwnd, logDir, ws_to_u8(log.str()), troubleshootingPackData);
                        return S_FALSE; // keep the dialog open
                    }

                    return S_OK;
            }

            return S_OK;
        };

        config.pfCallback = [](HWND hwnd, UINT uNotification, WPARAM wParam, LPARAM lParam, LONG_PTR dwRefData) {
            return (*reinterpret_cast<decltype(callback)*>(dwRefData))(hwnd, uNotification, wParam, lParam);
        };
        config.lpCallbackData = reinterpret_cast<LONG_PTR>(&callback);

        if (pProgressDialog) {
            pProgressDialog->StopProgressDialog();
            pProgressDialog->Release();
            pProgressDialog = NULL;
        }

        const auto kill_game = [&] { TerminateProcess(g_hProcess, exinfo.ExceptionRecord.ExceptionCode); };

        if (shutup) {
            kill_game();
            return 0;
        }

#if !_DEBUG
        // In release mode, we can't resume the game, so just kill it. It's not safe to keep it running, as we
        // don't know what state it's in and it may have crashed off-thread.
        // Additionally, if the main thread crashed, Windows will show the ANR dialog, which will block our dialog.
        kill_game();
#endif

        int nButtonPressed = 0, nRadioButton = 0;
        if (FAILED(TaskDialogIndirect(&config, &nButtonPressed, &nRadioButton, nullptr))) {
            SetEvent(exinfo.hEventHandle);
        } else {
            switch (nButtonPressed) {
                case IdButtonRestart:
                {
                    kill_game();
                    restart_game_using_injector(nRadioButton, *launcherArgs);
                    break;
                }
                default:
                    if (attemptResume)
                        SetEvent(exinfo.hEventHandle);
                    else
                        kill_game();
            }
        }
    }

    return 0;
}

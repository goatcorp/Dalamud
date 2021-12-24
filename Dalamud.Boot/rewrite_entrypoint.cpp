#include "pch.h"

DllExport DWORD WINAPI Initialize(LPVOID lpParam);

struct RewrittenEntryPointParameters {
    void* pAllocation;
    char* pEntrypoint;
    char* pEntrypointBytes;
    size_t entrypointLength;
    char* pLoadInfo;
    HANDLE hMainThread;
    HANDLE hMainThreadContinue;
};

#pragma pack(push, 1)
struct EntryPointThunkTemplate {
    struct DUMMYSTRUCTNAME {
        struct {
            const uint8_t op_mov_rdi[2]{ 0x48, 0xbf };
            void* ptr = nullptr;
        } fn;

        const uint8_t op_call_rdi[2]{ 0xff, 0xd7 };
    } CallTrampoline;
};

struct TrampolineTemplate {
    const struct {
        const uint8_t op_sub_rsp_imm[3]{ 0x48, 0x81, 0xec };
        const uint32_t length = 0x80;
    } stack_alloc;

    struct DUMMYSTRUCTNAME {
        struct {
            const uint8_t op_mov_rcx_imm[2]{ 0x48, 0xb9 };
            void* val = nullptr;
        } lpLibFileName;

        struct {
            const uint8_t op_mov_rdi_imm[2]{ 0x48, 0xbf };
            decltype(&LoadLibraryW) ptr = nullptr;
        } fn;

        const uint8_t op_call_rdi[2]{ 0xff, 0xd7 };
    } CallLoadLibrary_nethost;

    struct DUMMYSTRUCTNAME {
        struct {
            const uint8_t op_mov_rcx_imm[2]{ 0x48, 0xb9 };
            void* val = nullptr;
        } lpLibFileName;

        struct {
            const uint8_t op_mov_rdi_imm[2]{ 0x48, 0xbf };
            decltype(&LoadLibraryW) ptr = nullptr;
        } fn;

        const uint8_t op_call_rdi[2]{ 0xff, 0xd7 };
    } CallLoadLibrary_DalamudBoot;

    struct {
        const uint8_t hModule_op_mov_rcx_rax[3]{ 0x48, 0x89, 0xc1 };

        struct {
            const uint8_t op_mov_rdx_imm[2]{ 0x48, 0xba };
            void* val = nullptr;
        } lpProcName;

        struct {
            const uint8_t op_mov_rdi_imm[2]{ 0x48, 0xbf };
            decltype(&GetProcAddress) ptr = nullptr;
        } fn;

        const uint8_t op_call_rdi[2]{ 0xff, 0xd7 };
    } CallGetProcAddress;

    struct {
        const uint8_t op_add_rsp_imm[3]{ 0x48, 0x81, 0xc4 };
        const uint32_t length = 0x80;
    } stack_release;

    struct DUMMYSTRUCTNAME2 {
        // rdi := returned value from GetProcAddress
        const uint8_t op_mov_rdi_rax[3]{ 0x48, 0x89, 0xc7 };
        // rax := return address
        const uint8_t op_pop_rax[1]{ 0x58 };

        // rax := rax - sizeof thunk (last instruction must be call)
        struct {
            const uint8_t op_sub_rax_imm4[2]{ 0x48, 0x2d };
            const uint32_t displacement = static_cast<uint32_t>(sizeof EntryPointThunkTemplate);
        } op_sub_rax_to_entry_point;

        struct {
            const uint8_t op_mov_rcx_imm[2]{ 0x48, 0xb9 };
            void* val = nullptr;
        } param;

        const uint8_t op_push_rax[1]{ 0x50 };
        const uint8_t op_jmp_rdi[2]{ 0xff, 0xe7 };
    } CallInjectEntryPoint;

    const char buf_CallGetProcAddress_lpProcName[20] = "RewrittenEntryPoint";
    uint8_t buf_EntryPointBackup[sizeof EntryPointThunkTemplate]{};

#pragma pack(push, 8)
    RewrittenEntryPointParameters parameters{};
#pragma pack(pop)
};
#pragma pack(pop)

/// @brief Translate any open handle to nt path if a path exists.
/// @param handle Handle to any open kernel object.
/// @return Translated path in nt path (\\?\...)
std::filesystem::path get_path_from_handle(HANDLE handle){
    std::wstring result;
    result.resize(PATHCCH_MAX_CCH);
    result.resize(GetFinalPathNameByHandleW(handle, &result[0], static_cast<DWORD>(result.size()), VOLUME_NAME_DOS | FILE_NAME_NORMALIZED));
    if (result.empty())
        throw std::runtime_error("GetFinalPathNameByHandleW failure");

    return { std::move(result) };
}

/// @brief Convert any path to dos path.
/// @param Path in any format to convert.
/// @return Dos path (L:\...)
/// 
/// Reloaded's FASM locator cannot handle nt paths, resulting in a crash when the DLL is loaded via nt path.
/// 
std::filesystem::path to_dos_path(const std::filesystem::path& path) {
    const auto hFile = CreateFile(path.wstring().c_str(), 0, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, nullptr, OPEN_EXISTING, 0, nullptr);
    if (hFile == INVALID_HANDLE_VALUE)
        throw std::runtime_error("CreateFile failure");
    try {
        auto res = get_path_from_handle(hFile).wstring();
        CloseHandle(hFile);
        if (res.starts_with(LR"(\\?\)"))
            return { res.substr(4) };
        return { std::move(res) };
    } catch (...) {
        CloseHandle(hFile);
        throw;
    }
}

/// @brief Get the underlying file name from an memory-mapped file's virtual address.
/// @param hProcess Process handle.
/// @param lpMem Memory address in target process.
/// @return Converted path in dos path (L:\...)
std::filesystem::path get_mapped_image_path(HANDLE hProcess, void* lpMem) {
    std::wstring result;
    result.resize(PATHCCH_MAX_CCH);
    result.resize(GetMappedFileNameW(hProcess, lpMem, &result[0], static_cast<DWORD>(result.size())));
    if (!result.starts_with(LR"(\Device\)"))
        throw std::runtime_error("GetMappedFileNameW failure");
    return to_dos_path(LR"(\\?\)" + result.substr(8));
}

/// @brief Get the base address of the mapped file/image corresponding to the path given in the target process
/// @param hProcess Process handle.
/// @param path Path to the memory-mapped file to find.
/// @return Base address (lowest address) of the memory mapped file in the target process.
void* get_mapped_image_base_address(HANDLE hProcess, const std::filesystem::path& path) {
    for (MEMORY_BASIC_INFORMATION mbi{};
        VirtualQueryEx(hProcess, mbi.BaseAddress, &mbi, sizeof mbi);
        mbi.BaseAddress = static_cast<char*>(mbi.BaseAddress) + mbi.RegionSize) {
        if (!(mbi.State & MEM_COMMIT) || mbi.Type != MEM_IMAGE)
            continue;

        try {
            const auto imagePath = get_mapped_image_path(hProcess, mbi.BaseAddress);
            if (!imagePath.empty() && equivalent(imagePath, path))
                return mbi.AllocationBase;
        } catch (const std::filesystem::filesystem_error& e) {
            printf("%s", e.what());
            continue;
        }
    }
    throw std::runtime_error("corresponding base address not found");
}

/// @brief Find the game main window.
/// @return Handle to the game main window, or nullptr if it doesn't exist (yet).
HWND try_find_game_window() {
    HWND hwnd = nullptr;
    while ((hwnd = FindWindowExW(nullptr, hwnd, L"FFXIVGAME", nullptr))) {
        DWORD pid;
        GetWindowThreadProcessId(hwnd, &pid);

        if (pid == GetCurrentProcessId() && IsWindowVisible(hwnd))
            break;
    }
    return hwnd;
}

template<typename T>
void read_process_memory_or_throw(HANDLE hProcess, void* pAddress, T& data) {
    SIZE_T read = 0;
    if (!ReadProcessMemory(hProcess, pAddress, &data, sizeof data, &read))
        throw std::runtime_error("ReadProcessMemory failure");
    if (read != sizeof data)
        throw std::runtime_error("ReadProcessMemory read size does not match requested size");
}

void write_process_memory_or_throw(HANDLE hProcess, void* pAddress, const void* data, size_t len) {
    SIZE_T written = 0;
    if (!WriteProcessMemory(hProcess, pAddress, data, len, &written))
        throw std::runtime_error("WriteProcessMemory failure");
    if (written != len)
        throw std::runtime_error("WriteProcessMemory written size does not match requested size");
}

template<typename T>
void write_process_memory_or_throw(HANDLE hProcess, void* pAddress, const T& data) {
    return write_process_memory_or_throw(hProcess, pAddress, &data, sizeof data);
}

std::string from_utf16(const std::wstring& wstr, UINT codePage = CP_UTF8) {
    std::string str(WideCharToMultiByte(codePage, 0, &wstr[0], static_cast<int>(wstr.size()), nullptr, 0, nullptr, nullptr), 0);
    WideCharToMultiByte(codePage, 0, &wstr[0], static_cast<int>(wstr.size()), &str[0], static_cast<int>(str.size()), nullptr, nullptr);
    return str;
}

std::wstring to_utf16(const std::string& str, UINT codePage = CP_UTF8, bool errorOnInvalidChars = false) {
    std::wstring wstr(MultiByteToWideChar(codePage, 0, &str[0], static_cast<int>(str.size()), nullptr, 0), 0);
    MultiByteToWideChar(codePage, errorOnInvalidChars ? MB_ERR_INVALID_CHARS : 0, &str[0], static_cast<int>(str.size()), &wstr[0], static_cast<int>(wstr.size()));
    return wstr;
}

/// @brief Rewrite target process' entry point so that this DLL can be loaded and executed first.
/// @param hProcess Process handle.
/// @param pcwzPath Path to target process.
/// @param pcszLoadInfo JSON string to be passed to Initialize.
/// @return 0 if successful; nonzero if unsuccessful
/// 
/// When the process has just been started up via CreateProcess (CREATE_SUSPENDED), GetModuleFileName and alikes result in an error.
/// Instead, we have to enumerate through all the files mapped into target process' virtual address space and find the base address
/// of memory region corresponding to the path given.
/// 
DllExport DWORD WINAPI RewriteRemoteEntryPointW(HANDLE hProcess, const wchar_t* pcwzPath, const wchar_t* pcwzLoadInfo) {
    try {
        const auto base_address = reinterpret_cast<char*>(get_mapped_image_base_address(hProcess, pcwzPath));

        IMAGE_DOS_HEADER dos_header{};
        union {
            IMAGE_NT_HEADERS32 nt_header32;
            IMAGE_NT_HEADERS64 nt_header64{};
        };

        read_process_memory_or_throw(hProcess, base_address, dos_header);
        read_process_memory_or_throw(hProcess, base_address + dos_header.e_lfanew, nt_header64);
        const auto entrypoint = base_address + (nt_header32.OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC
            ? nt_header32.OptionalHeader.AddressOfEntryPoint
            : nt_header64.OptionalHeader.AddressOfEntryPoint);

        auto path = get_mapped_image_path(GetCurrentProcess(), g_hModule).wstring();
        path.resize(path.size() + 1);  // ensure null termination
        auto path_bytes = std::span(reinterpret_cast<const char*>(&path[0]), std::span(path).size_bytes());

        auto nethost_path = (get_mapped_image_path(GetCurrentProcess(), g_hModule).parent_path() / L"nethost.dll").wstring();
        nethost_path.resize(nethost_path.size() + 1);  // ensure null termination
        auto nethost_path_bytes = std::span(reinterpret_cast<const char*>(&nethost_path[0]), std::span(nethost_path).size_bytes());

        auto load_info = from_utf16(pcwzLoadInfo);
        load_info.resize(load_info.size() + 1);  //ensure null termination

        // Allocate full buffer in advance to keep reference to trampoline valid.
        std::vector<uint8_t> buffer(sizeof TrampolineTemplate + load_info.size() + nethost_path_bytes.size() + path_bytes.size());
        auto& trampoline = *reinterpret_cast<TrampolineTemplate*>(&buffer[0]);
        const auto load_info_buffer = std::span(buffer).subspan(sizeof trampoline, load_info.size());
        const auto nethost_path_buffer = std::span(buffer).subspan(sizeof trampoline + load_info.size(), nethost_path_bytes.size());
        const auto dalamud_path_buffer = std::span(buffer).subspan(sizeof trampoline + load_info.size() + nethost_path_bytes.size(), path_bytes.size());

        new(&trampoline)TrampolineTemplate();  // this line initializes given buffer instead of allocating memory
        memcpy(&load_info_buffer[0], &load_info[0], load_info_buffer.size());
        memcpy(&nethost_path_buffer[0], &nethost_path_bytes[0], nethost_path_buffer.size());
        memcpy(&dalamud_path_buffer[0], &path_bytes[0], dalamud_path_buffer.size());

        // Backup remote process' original entry point.
        read_process_memory_or_throw(hProcess, entrypoint, trampoline.buf_EntryPointBackup);

        // Allocate buffer in remote process, which will be used to fill addresses in the local buffer.
        const auto remote_buffer = reinterpret_cast<char*>(VirtualAllocEx(hProcess, nullptr, buffer.size(), MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE));
    
        // Fill the values to be used in RewrittenEntryPoint
        trampoline.parameters = {
            .pAllocation = remote_buffer,
            .pEntrypoint = entrypoint,
            .pEntrypointBytes = remote_buffer + offsetof(TrampolineTemplate, buf_EntryPointBackup),
            .entrypointLength = sizeof trampoline.buf_EntryPointBackup,
            .pLoadInfo = remote_buffer + (&load_info_buffer[0] - &buffer[0]),
        };

        // Fill the addresses referred in machine code.
        trampoline.CallLoadLibrary_nethost.lpLibFileName.val = remote_buffer + (&nethost_path_buffer[0] - &buffer[0]);
        trampoline.CallLoadLibrary_nethost.fn.ptr = LoadLibraryW;
        trampoline.CallLoadLibrary_DalamudBoot.lpLibFileName.val = remote_buffer + (&dalamud_path_buffer[0] - &buffer[0]);
        trampoline.CallLoadLibrary_DalamudBoot.fn.ptr = LoadLibraryW;
        trampoline.CallGetProcAddress.lpProcName.val = remote_buffer + offsetof(TrampolineTemplate, buf_CallGetProcAddress_lpProcName);
        trampoline.CallGetProcAddress.fn.ptr = GetProcAddress;
        trampoline.CallInjectEntryPoint.param.val = remote_buffer + offsetof(TrampolineTemplate, parameters);

        // Write the local buffer into the buffer in remote process.
        write_process_memory_or_throw(hProcess, remote_buffer, buffer.data(), buffer.size());

        // Overwrite remote process' entry point with a thunk that immediately calls our trampoline function.
        EntryPointThunkTemplate thunk{};
        thunk.CallTrampoline.fn.ptr = remote_buffer;
        write_process_memory_or_throw(hProcess, entrypoint, thunk);

        return 0;
    } catch (const std::exception& e) {
        OutputDebugStringA(std::format("RewriteRemoteEntryPoint failure: {} (GetLastError: {})\n", e.what(), GetLastError()).c_str());
        return 1;
    }
}

/// @deprecated
DllExport DWORD WINAPI RewriteRemoteEntryPoint(HANDLE hProcess, const wchar_t* pcwzPath, const char* pcszLoadInfo) {
    return RewriteRemoteEntryPointW(hProcess, pcwzPath, to_utf16(pcszLoadInfo).c_str());
}

void wait_for_game_window() {
    HWND game_window;
    while (!(game_window = try_find_game_window())) {
        WaitForInputIdle(GetCurrentProcess(), INFINITE);
        Sleep(100);
    };
    SendMessageW(game_window, WM_NULL, 0, 0);
}

/// @brief Entry point function "called" instead of game's original main entry point.
/// @param params Parameters set up from RewriteRemoteEntryPoint.
DllExport void WINAPI RewrittenEntryPoint(RewrittenEntryPointParameters& params) {
    params.hMainThreadContinue = CreateEventW(nullptr, true, false, nullptr);
    if (!params.hMainThreadContinue)
        ExitProcess(-1);

    // Do whatever the work in a separate thread to minimize the stack usage at this context,
    // as this function really should have been a naked procedure but __declspec(naked) isn't supported in x64 version of msvc.
    params.hMainThread = CreateThread(nullptr, 0, [](void* p) -> DWORD {
        try {
            std::string loadInfo;
            {
                auto& params = *reinterpret_cast<RewrittenEntryPointParameters*>(p);

                // Restore original entry point.
                // Use WriteProcessMemory instead of memcpy to avoid having to fiddle with VirtualProtect.
                write_process_memory_or_throw(GetCurrentProcess(), params.pEntrypoint, params.pEntrypointBytes, params.entrypointLength);

                // Make a copy of load info, as the whole params will be freed after this code block.
                loadInfo = params.pLoadInfo;

                // Let the game initialize.
                SetEvent(params.hMainThreadContinue);
            }

            wait_for_game_window();

            Initialize(&loadInfo[0]);
            return 0;
        } catch (const std::exception& e) {
            MessageBoxA(nullptr, std::format("Failed to load Dalamud.\n\nError: {}", e.what()).c_str(), "Dalamud.Boot", MB_OK | MB_ICONERROR);
            ExitProcess(-1);
        }
        }, &params, 0, nullptr);
    if (!params.hMainThread)
        ExitProcess(-1);

    CloseHandle(params.hMainThread);
    WaitForSingleObject(params.hMainThreadContinue, INFINITE);
    CloseHandle(params.hMainThreadContinue);
    VirtualFree(params.pAllocation, 0, MEM_RELEASE);
}

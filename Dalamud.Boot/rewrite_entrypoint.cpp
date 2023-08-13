#include "pch.h"

#include "logging.h"

DWORD WINAPI InitializeImpl(LPVOID lpParam, HANDLE hMainThreadContinue);

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

void read_process_memory_or_throw(HANDLE hProcess, void* pAddress, void* data, size_t len) {
    SIZE_T read = 0;
    if (!ReadProcessMemory(hProcess, pAddress, data, len, &read))
        throw std::runtime_error("ReadProcessMemory failure");
    if (read != len)
        throw std::runtime_error("ReadProcessMemory read size does not match requested size");
}

template<typename T>
void read_process_memory_or_throw(HANDLE hProcess, void* pAddress, T& data) {
    return read_process_memory_or_throw(hProcess, pAddress, &data, sizeof data);
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

std::filesystem::path get_path_from_local_module(HMODULE hModule) {
    std::wstring result;
    result.resize(PATHCCH_MAX_CCH);
    result.resize(GetModuleFileNameW(hModule, &result[0], static_cast<DWORD>(result.size())));
    return result;
}

/// @brief Get the base address of the mapped file/image corresponding to the path given in the target process
/// @param hProcess Process handle.
/// @param path Path to the memory-mapped file to find.
/// @return Base address (lowest address) of the memory mapped file in the target process.
void* get_mapped_image_base_address(HANDLE hProcess, const std::filesystem::path& path) {
    std::ifstream exe(path, std::ios::binary);

    IMAGE_DOS_HEADER exe_dos_header;
    exe.read(reinterpret_cast<char*>(&exe_dos_header), sizeof exe_dos_header);
    if (!exe || exe_dos_header.e_magic != IMAGE_DOS_SIGNATURE)
        throw std::runtime_error("Game executable is corrupt (DOS header).");

    union {
        IMAGE_NT_HEADERS32 exe_nt_header32;
        IMAGE_NT_HEADERS64 exe_nt_header64;
    };
    exe.seekg(exe_dos_header.e_lfanew, std::ios::beg);
    exe.read(reinterpret_cast<char*>(&exe_nt_header64), sizeof exe_nt_header64);
    if (!exe || exe_nt_header64.Signature != IMAGE_NT_SIGNATURE)
        throw std::runtime_error("Game executable is corrupt (NT header).");

    std::vector<IMAGE_SECTION_HEADER> exe_section_headers(exe_nt_header64.FileHeader.NumberOfSections);
    exe.seekg(exe_dos_header.e_lfanew + offsetof(IMAGE_NT_HEADERS32, OptionalHeader) + exe_nt_header64.FileHeader.SizeOfOptionalHeader, std::ios::beg);
    exe.read(reinterpret_cast<char*>(&exe_section_headers[0]), sizeof IMAGE_SECTION_HEADER * exe_section_headers.size());
    if (!exe)
        throw std::runtime_error("Game executable is corrupt (Truncated section header).");
    
    for (MEMORY_BASIC_INFORMATION mbi{};
        VirtualQueryEx(hProcess, mbi.BaseAddress, &mbi, sizeof mbi);
        mbi.BaseAddress = static_cast<char*>(mbi.BaseAddress) + mbi.RegionSize) {
        if (!(mbi.State & MEM_COMMIT) || mbi.Type != MEM_IMAGE)
            continue;

        // Previous Wine versions do not support GetMappedFileName, so we check the content of memory instead.

        try {
            IMAGE_DOS_HEADER compare_dos_header;
            read_process_memory_or_throw(hProcess, mbi.BaseAddress, compare_dos_header);
            if (compare_dos_header.e_magic != exe_dos_header.e_magic)
                continue;

            union {
                IMAGE_NT_HEADERS32 compare_nt_header32;
                IMAGE_NT_HEADERS64 compare_nt_header64;
            };
            read_process_memory_or_throw(hProcess, static_cast<char*>(mbi.BaseAddress) + compare_dos_header.e_lfanew, &compare_nt_header32, offsetof(IMAGE_NT_HEADERS32, OptionalHeader));
            if (compare_nt_header32.Signature != exe_nt_header32.Signature)
                continue;

            if (compare_nt_header32.FileHeader.TimeDateStamp != exe_nt_header32.FileHeader.TimeDateStamp)
                continue;

            if (compare_nt_header32.FileHeader.SizeOfOptionalHeader != exe_nt_header32.FileHeader.SizeOfOptionalHeader)
                continue;

            if (compare_nt_header32.FileHeader.NumberOfSections != exe_nt_header32.FileHeader.NumberOfSections)
                continue;

            if (compare_nt_header32.FileHeader.SizeOfOptionalHeader == sizeof IMAGE_OPTIONAL_HEADER32) {
                read_process_memory_or_throw(hProcess, static_cast<char*>(mbi.BaseAddress) + compare_dos_header.e_lfanew + offsetof(IMAGE_NT_HEADERS32, OptionalHeader), compare_nt_header32.OptionalHeader);
                if (compare_nt_header32.OptionalHeader.SizeOfImage != exe_nt_header32.OptionalHeader.SizeOfImage)
                    continue;
                if (compare_nt_header32.OptionalHeader.CheckSum != exe_nt_header32.OptionalHeader.CheckSum)
                    continue;

                std::vector<IMAGE_SECTION_HEADER> compare_section_headers(exe_nt_header32.FileHeader.NumberOfSections);
                read_process_memory_or_throw(hProcess, static_cast<char*>(mbi.BaseAddress) + compare_dos_header.e_lfanew + sizeof compare_nt_header32, &compare_section_headers[0], sizeof IMAGE_SECTION_HEADER * compare_section_headers.size());
                if (memcmp(&compare_section_headers[0], &exe_section_headers[0], sizeof IMAGE_SECTION_HEADER * compare_section_headers.size()) != 0)
                    continue;

            } else if (compare_nt_header32.FileHeader.SizeOfOptionalHeader == sizeof IMAGE_OPTIONAL_HEADER64) {
                read_process_memory_or_throw(hProcess, static_cast<char*>(mbi.BaseAddress) + compare_dos_header.e_lfanew + offsetof(IMAGE_NT_HEADERS64, OptionalHeader), compare_nt_header64.OptionalHeader);
                if (compare_nt_header64.OptionalHeader.SizeOfImage != exe_nt_header64.OptionalHeader.SizeOfImage)
                    continue;
                if (compare_nt_header64.OptionalHeader.CheckSum != exe_nt_header64.OptionalHeader.CheckSum)
                    continue;

                std::vector<IMAGE_SECTION_HEADER> compare_section_headers(exe_nt_header64.FileHeader.NumberOfSections);
                read_process_memory_or_throw(hProcess, static_cast<char*>(mbi.BaseAddress) + compare_dos_header.e_lfanew + sizeof compare_nt_header64, &compare_section_headers[0], sizeof IMAGE_SECTION_HEADER * compare_section_headers.size());
                if (memcmp(&compare_section_headers[0], &exe_section_headers[0], sizeof IMAGE_SECTION_HEADER * compare_section_headers.size()) != 0)
                    continue;

            } else
                continue;

            // Should be close enough(tm) at this point, as the only two loaded modules should be ntdll.dll and the game executable itself.

            return mbi.AllocationBase;

        } catch (const std::exception& e) {
            logging::W("Failed to check memory block 0x{:X}(len=0x{:X}): {}", mbi.BaseAddress, mbi.RegionSize, e.what());
            continue;
        }
    }
    throw std::runtime_error("corresponding base address not found");
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

        auto path = get_path_from_local_module(g_hModule).wstring();
        path.resize(path.size() + 1);  // ensure null termination
        auto path_bytes = std::span(reinterpret_cast<const char*>(&path[0]), std::span(path).size_bytes());

        auto nethost_path = (get_path_from_local_module(g_hModule).parent_path() / L"nethost.dll").wstring();
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
            auto& params = *reinterpret_cast<RewrittenEntryPointParameters*>(p);
            {
                // Restore original entry point.
                // Use WriteProcessMemory instead of memcpy to avoid having to fiddle with VirtualProtect.
                write_process_memory_or_throw(GetCurrentProcess(), params.pEntrypoint, params.pEntrypointBytes, params.entrypointLength);

                // Make a copy of load info, as the whole params will be freed after this code block.
                loadInfo = params.pLoadInfo;
            }

            if (const auto err = InitializeImpl(&loadInfo[0], params.hMainThreadContinue))
                throw std::exception(std::format("{:08X}", err).c_str());
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
    VirtualFree(params.pAllocation, 0, MEM_RELEASE);
}

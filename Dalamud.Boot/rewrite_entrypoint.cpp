#include "pch.h"

#include "logging.h"

DWORD WINAPI InitializeImpl(LPVOID lpParam, HANDLE hMainThreadContinue);

struct RewrittenEntryPointParameters {
    char* pEntrypoint;
    size_t entrypointLength;
};

namespace thunks {
    constexpr uint64_t Terminator = 0xCCCCCCCCCCCCCCCCu;
    constexpr uint64_t Placeholder = 0x0606060606060606u;
    
    extern "C" void EntryPointReplacement();
    extern "C" void RewrittenEntryPoint_Standalone();

    void* resolve_thunk_address(void (*pfn)()) {
        const auto ptr = reinterpret_cast<uint8_t*>(pfn);
        if (*ptr == 0xe9)
            return ptr + 5 + *reinterpret_cast<int32_t*>(ptr + 1);
        return ptr;
    }

    size_t get_thunk_length(void (*pfn)()) {
        size_t length = 0;
        for (auto ptr = reinterpret_cast<char*>(resolve_thunk_address(pfn)); *reinterpret_cast<uint64_t*>(ptr) != Terminator; ptr++)
            length++;
        return length;
    }

    template<typename T>
    void* fill_placeholders(void* pfn, const T& value) {
        auto ptr = static_cast<char*>(pfn);

        while (*reinterpret_cast<uint64_t*>(ptr) != Placeholder)
            ptr++;

        *reinterpret_cast<uint64_t*>(ptr) = 0;
        *reinterpret_cast<T*>(ptr) = value;
        return ptr + sizeof(value);
    }

    template<typename T, typename...TArgs>
    void* fill_placeholders(void* ptr, const T& value, TArgs&&...more_values) {
        return fill_placeholders(fill_placeholders(ptr, value), std::forward<TArgs>(more_values)...);
    }

    std::vector<char> create_entrypointreplacement() {
        std::vector<char> buf(get_thunk_length(&EntryPointReplacement));
        memcpy(buf.data(), resolve_thunk_address(&EntryPointReplacement), buf.size());
        return buf;
    }

    std::vector<char> create_standalone_rewrittenentrypoint(const std::filesystem::path& dalamud_path) {
        const auto nethost_path = std::filesystem::path(dalamud_path).replace_filename(L"nethost.dll");

        // These are null terminated, since pointers are returned from .c_str()
        const auto dalamud_path_wview = std::wstring_view(dalamud_path.c_str());
        const auto nethost_path_wview = std::wstring_view(nethost_path.c_str());

        // +2 is for null terminator
        const auto dalamud_path_view = std::span(reinterpret_cast<const char*>(dalamud_path_wview.data()), dalamud_path_wview.size() * 2 + 2);
        const auto nethost_path_view = std::span(reinterpret_cast<const char*>(nethost_path_wview.data()), nethost_path_wview.size() * 2 + 2);

        std::vector<char> buffer;
        const auto thunk_template_length = thunks::get_thunk_length(&thunks::RewrittenEntryPoint_Standalone);
        buffer.reserve(thunk_template_length + dalamud_path_view.size() + nethost_path_view.size());
        buffer.resize(thunk_template_length);
        memcpy(buffer.data(), resolve_thunk_address(&thunks::RewrittenEntryPoint_Standalone), thunk_template_length);

        // &::GetProcAddress will return Dalamud.dll's import table entry.
        // GetProcAddress(..., "GetProcAddress") returns the address inside kernel32.dll.
        const auto kernel32 = GetModuleHandleA("kernel32.dll");

        thunks::fill_placeholders(buffer.data(),
            /* pfnLoadLibraryW = */ GetProcAddress(kernel32, "LoadLibraryW"),
            /* pfnGetProcAddress = */ GetProcAddress(kernel32, "GetProcAddress"),
            /* pRewrittenEntryPointParameters = */ Placeholder,
            /* nNethostOffset = */ 0,
            /* nDalamudOffset = */ nethost_path_view.size_bytes()
        );
        buffer.insert(buffer.end(), nethost_path_view.begin(), nethost_path_view.end());
        buffer.insert(buffer.end(), dalamud_path_view.begin(), dalamud_path_view.end());
        return buffer;
    }
}

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

    SYSTEM_INFO sysinfo;
    GetSystemInfo(&sysinfo);
    
    for (MEMORY_BASIC_INFORMATION mbi{};
        VirtualQueryEx(hProcess, mbi.BaseAddress, &mbi, sizeof mbi);
        mbi.BaseAddress = static_cast<char*>(mbi.BaseAddress) + mbi.RegionSize) {

        // wine: apparently there exists a RegionSize of 0xFFF
        mbi.RegionSize = (mbi.RegionSize + sysinfo.dwPageSize - 1) / sysinfo.dwPageSize * sysinfo.dwPageSize;

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
extern "C" DWORD WINAPI RewriteRemoteEntryPointW(HANDLE hProcess, const wchar_t* pcwzPath, const wchar_t* pcwzLoadInfo) {
    try {
        const auto base_address = static_cast<char*>(get_mapped_image_base_address(hProcess, pcwzPath));

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

        auto standalone_rewrittenentrypoint = thunks::create_standalone_rewrittenentrypoint(get_path_from_local_module(g_hModule));
        auto entrypoint_replacement = thunks::create_entrypointreplacement();

        auto load_info = unicode::convert<std::string>(pcwzLoadInfo);
        load_info.resize(load_info.size() + 1);  //ensure null termination

        std::vector<uint8_t> buffer(sizeof(RewrittenEntryPointParameters) + entrypoint_replacement.size() + load_info.size() + standalone_rewrittenentrypoint.size());

        // Allocate buffer in remote process, which will be used to fill addresses in the local buffer.
        const auto remote_buffer = static_cast<char*>(VirtualAllocEx(hProcess, nullptr, buffer.size(), MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE));
        
        auto& params = *reinterpret_cast<RewrittenEntryPointParameters*>(buffer.data());
        params.entrypointLength = entrypoint_replacement.size();
        params.pEntrypoint = entrypoint;

        // Backup original entry point.
        read_process_memory_or_throw(hProcess, entrypoint, &buffer[sizeof params], entrypoint_replacement.size());

        memcpy(&buffer[sizeof params + entrypoint_replacement.size()], load_info.data(), load_info.size());

        thunks::fill_placeholders(standalone_rewrittenentrypoint.data(), remote_buffer);
        memcpy(&buffer[sizeof params + entrypoint_replacement.size() + load_info.size()], standalone_rewrittenentrypoint.data(), standalone_rewrittenentrypoint.size());

        // Write the local buffer into the buffer in remote process.
        write_process_memory_or_throw(hProcess, remote_buffer, buffer.data(), buffer.size());

        thunks::fill_placeholders(entrypoint_replacement.data(), remote_buffer + sizeof params + entrypoint_replacement.size() + load_info.size());
        // Overwrite remote process' entry point with a thunk that will load our DLLs and call our trampoline function.
        write_process_memory_or_throw(hProcess, entrypoint, entrypoint_replacement.data(), entrypoint_replacement.size());

        return 0;
    } catch (const std::exception& e) {
        OutputDebugStringA(std::format("RewriteRemoteEntryPoint failure: {} (GetLastError: {})\n", e.what(), GetLastError()).c_str());
        return 1;
    }
}

static void AbortRewrittenEntryPoint(DWORD err, const std::wstring& clue) {
    wchar_t* pwszMsg = nullptr;
    FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER |
        FORMAT_MESSAGE_FROM_SYSTEM |
        FORMAT_MESSAGE_IGNORE_INSERTS,
        nullptr,
        err,
        MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_US),
        reinterpret_cast<LPWSTR>(&pwszMsg),
        0,
        nullptr);

    if (MessageBoxW(nullptr, std::format(
        L"Failed to load Dalamud. Load game without Dalamud(yes) or abort(no)?\n\nError: 0x{:08X} {}\n\n{}",
        err, pwszMsg ? pwszMsg : L"", clue).c_str(),
        L"Dalamud.Boot", MB_OK | MB_YESNO) == IDNO)
        ExitProcess(-1);
}

/// @brief Entry point function "called" instead of game's original main entry point.
/// @param params Parameters set up from RewriteRemoteEntryPoint.
extern "C" void WINAPI RewrittenEntryPoint_AdjustedStack(RewrittenEntryPointParameters & params) {
    const auto pOriginalEntryPointBytes = reinterpret_cast<char*>(&params) + sizeof(params);
    const auto pLoadInfo = pOriginalEntryPointBytes + params.entrypointLength;

    // Restore original entry point.
    // Use WriteProcessMemory instead of memcpy to avoid having to fiddle with VirtualProtect.
    if (SIZE_T written; !WriteProcessMemory(GetCurrentProcess(), params.pEntrypoint, pOriginalEntryPointBytes, params.entrypointLength, &written))
        return AbortRewrittenEntryPoint(GetLastError(), L"WriteProcessMemory(entrypoint restoration)");

    const auto hMainThreadContinue = CreateEventW(nullptr, true, false, nullptr);
    if (!hMainThreadContinue)
        return AbortRewrittenEntryPoint(GetLastError(), L"CreateEventW");

    if (const auto result = InitializeImpl(pLoadInfo, hMainThreadContinue))
        return AbortRewrittenEntryPoint(result, L"InitializeImpl");

    WaitForSingleObject(hMainThreadContinue, INFINITE);
    VirtualFree(&params, 0, MEM_RELEASE);
}

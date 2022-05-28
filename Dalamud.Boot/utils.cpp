#include "pch.h"

#include "utils.h"

utils::signature_finder& utils::signature_finder::look_in(const void* pFirst, size_t length) {
    if (length)
        m_ranges.emplace_back(std::span(reinterpret_cast<const char*>(pFirst), length));

    return *this;
}

utils::signature_finder& utils::signature_finder::look_in(const void* pFirst, const void* pLast) {
    return look_in(pFirst, reinterpret_cast<const char*>(pLast) - reinterpret_cast<const char*>(pFirst));
}

utils::signature_finder& utils::signature_finder::look_in(HMODULE hModule, const char* sectionName) {
    const auto pcBaseAddress = reinterpret_cast<char*>(hModule);
    const auto& dosHeader = *reinterpret_cast<const IMAGE_DOS_HEADER*>(&pcBaseAddress[0]);
    const auto& ntHeader32 = *reinterpret_cast<const IMAGE_NT_HEADERS32*>(&pcBaseAddress[dosHeader.e_lfanew]);
    // Since this does not refer to OptionalHeader32/64 else than its offset, we can use either.
    const auto sections = std::span(IMAGE_FIRST_SECTION(&ntHeader32), ntHeader32.FileHeader.NumberOfSections);
    for (const auto& section : sections) {
        if (strncmp(reinterpret_cast<const char*>(section.Name), sectionName, IMAGE_SIZEOF_SHORT_NAME) == 0)
            look_in(pcBaseAddress + section.VirtualAddress, section.Misc.VirtualSize);
    }
    return *this;
}

utils::signature_finder& utils::signature_finder::look_for(std::string_view pattern, std::string_view mask, char cExactMatch, char cWildcard) {
    if (pattern.size() != mask.size())
        throw std::runtime_error("Length of pattern does not match the length of mask.");

    std::string buf;
    buf.reserve(pattern.size() * 4);
    for (size_t i = 0; i < pattern.size(); i++) {
        const auto c = pattern[i];
        if (mask[i] == cWildcard) {
            buf.push_back('.');
        } else if (mask[i] == cExactMatch) {
            buf.push_back('\\');
            buf.push_back('x');
            buf.push_back((c >> 4) < 10 ? (c >> 4) - 10 : 'A' + (c >> 4) - 10);
            buf.push_back((c & 15) < 10 ? (c & 15) - 10 : 'A' + (c & 15) - 10);
        }
    }
    m_patterns.emplace_back(buf);
    return *this;
}

utils::signature_finder& utils::signature_finder::look_for(std::string_view pattern, char wildcardMask) {
    std::string buf;
    buf.reserve(pattern.size() * 4);
    for (const auto& c : pattern) {
        if (c == wildcardMask) {
            buf.push_back('.');
        } else {
            buf.push_back('\\');
            buf.push_back('x');
            buf.push_back((c >> 4) < 10 ? '0' + (c >> 4) : 'A' + (c >> 4) - 10);
            buf.push_back((c & 15) < 10 ? '0' + (c & 15) : 'A' + (c & 15) - 10);
        }
    }
    m_patterns.emplace_back(buf);
    return *this;
}

utils::signature_finder& utils::signature_finder::look_for(std::string_view pattern) {
    std::string buf;
    buf.reserve(pattern.size() * 4);
    for (const auto& c : pattern) {
        buf.push_back('\\');
        buf.push_back('x');
        buf.push_back((c >> 4) < 10 ? '0' + (c >> 4) : 'A' + (c >> 4) - 10);
        buf.push_back((c & 15) < 10 ? '0' + (c & 15) : 'A' + (c & 15) - 10);
    }
    m_patterns.emplace_back(buf);
    return *this;
}

utils::signature_finder& utils::signature_finder::look_for_hex(std::string_view pattern) {
    std::string buf;
    buf.reserve(pattern.size());
    bool bHighByte = true;
    for (size_t i = 0; i < pattern.size(); i++) {
        int n = -1;
        if ('0' <= pattern[i] && pattern[i] <= '9')
            n = pattern[i] - '0';
        else if ('a' <= pattern[i] && pattern[i] <= 'f')
            n = 10 + pattern[i] - 'A';
        else if ('A' <= pattern[i] && pattern[i] <= 'F')
            n = 10 + pattern[i] - 'A';
        else if (pattern[i] == '?' && i + 1 < pattern.size() && pattern[i + 1] == '?') {
            i++;
            n = -2;
        } else if (pattern[i] == '?')
            n = -2;

        if (n == -1)
            continue;
        else if (n == -2) {
            if (!bHighByte) {
                buf.insert(buf.begin() + buf.size() - 1, '0');
                bHighByte = true;
            }
            buf.push_back('.');
            continue;
        }

        if (bHighByte) {
            buf.push_back('\\');
            buf.push_back('x');
        }
        buf.push_back(pattern[i]);
        bHighByte = !bHighByte;
    }
    m_patterns.emplace_back(buf);
    return *this;
}

std::vector<utils::signature_finder::result> utils::signature_finder::find(size_t minCount, size_t maxCount, bool bErrorOnMoreThanMaximum) const {
    std::vector<result> res;

    for (const auto& rangeSpan : m_ranges) {
        for (size_t patternIndex = 0; patternIndex < m_patterns.size(); patternIndex++) {
            srell::match_results<std::span<const char>::iterator> matches;
            auto ptr = rangeSpan.begin();
            for (size_t matchIndex = 0;; ptr = matches[0].first + 1, matchIndex++) {
                if (!m_patterns[patternIndex].search(ptr, rangeSpan.end(), rangeSpan.begin(), matches, srell::regex_constants::match_flag_type::match_default))
                    break;

                for (size_t captureIndex = 0; captureIndex < matches.size(); captureIndex++) {
                    const auto& capture = matches[captureIndex];
                    res.emplace_back(
                        std::span(capture.first, capture.second),
                        patternIndex,
                        matchIndex,
                        captureIndex);

                    if (bErrorOnMoreThanMaximum) {
                        if (res.size() > maxCount)
                            throw std::runtime_error(std::format("Found {} result(s), wanted at most {} results", res.size(), maxCount));
                    } else if (res.size() == maxCount)
                        return res;
                }
            }
        }
    }

    if (res.size() < minCount)
        throw std::runtime_error(std::format("Found {} result(s), wanted at least {} results", res.size(), minCount));

    return res;
}

std::span<const char> utils::signature_finder::find_one() const {
    return find(1, 1, false).front().Match;
}

utils::memory_tenderizer::memory_tenderizer(const void* pAddress, size_t length, DWORD dwNewProtect) : m_data(reinterpret_cast<char*>(const_cast<void*>(pAddress)), length) {
    try {
        for (auto pCoveredAddress = &m_data[0];
            pCoveredAddress < &m_data[0] + m_data.size();
            pCoveredAddress = reinterpret_cast<char*>(m_regions.back().BaseAddress) + m_regions.back().RegionSize) {

            MEMORY_BASIC_INFORMATION region{};
            if (!VirtualQuery(pCoveredAddress, &region, sizeof region)) {
                throw std::runtime_error(std::format(
                    "VirtualQuery(addr=0x{:X}, ..., cb={}) failed with Win32 code 0x{:X}",
                    reinterpret_cast<size_t>(pCoveredAddress),
                    sizeof region,
                    GetLastError()));
            }

            if (!VirtualProtect(region.BaseAddress, region.RegionSize, dwNewProtect, &region.Protect)) {
                throw std::runtime_error(std::format(
                    "(Change)VirtualProtect(addr=0x{:X}, size=0x{:X}, ..., ...) failed with Win32 code 0x{:X}",
                    reinterpret_cast<size_t>(region.BaseAddress),
                    region.RegionSize,
                    GetLastError()));
            }

            m_regions.emplace_back(region);
        }

    } catch (...) {
        for (auto& region : std::ranges::reverse_view(m_regions)) {
            if (!VirtualProtect(region.BaseAddress, region.RegionSize, region.Protect, &region.Protect)) {
                // Could not restore; fast fail
                __fastfail(GetLastError());
            }
        }

        throw;
    }
}

utils::memory_tenderizer::~memory_tenderizer() {
    for (auto& region : std::ranges::reverse_view(m_regions)) {
        if (!VirtualProtect(region.BaseAddress, region.RegionSize, region.Protect, &region.Protect)) {
            // Could not restore; fast fail
            __fastfail(GetLastError());
        }
    }
}

std::shared_ptr<void> utils::allocate_executable_heap(size_t len) {
    static std::weak_ptr<void> s_hHeap;

    std::shared_ptr<void> hHeap;
    if (hHeap = s_hHeap.lock(); !hHeap) {
        static std::mutex m_mtx;
        const auto lock = std::lock_guard(m_mtx);

        if (hHeap = s_hHeap.lock(); !hHeap) {
            if (const auto hHeapRaw = HeapCreate(HEAP_CREATE_ENABLE_EXECUTE, 0, 0); hHeapRaw)
                s_hHeap = hHeap = std::shared_ptr<void>(hHeapRaw, HeapDestroy);
            else
                throw std::runtime_error("Failed to create heap.");
        }
    }

    const auto pAllocRaw = HeapAlloc(hHeap.get(), 0, len);
    if (!pAllocRaw)
        throw std::runtime_error("Failed to allocate memory.");

    return {
        pAllocRaw,
        [hHeap = std::move(hHeap)](void* pAddress) { HeapFree(hHeap.get(), 0, pAddress); },
    };
}

void* utils::resolve_unconditional_jump_target(void* pfn) {
    const auto bytes = reinterpret_cast<uint8_t*>(pfn);

    // JMP QWORD PTR [RIP + int32]
    // 48 FF 25 ?? ?? ?? ??
    if (bytes[0] == 0x48 && bytes[1] == 0xFF && bytes[2] == 0x25)
        return *reinterpret_cast<void**>(&bytes[7 + *reinterpret_cast<int*>(&bytes[3])]);

    throw std::runtime_error("Unexpected thunk bytes.");
}

template<typename TEntryType>
static bool find_imported_function_pointer_helper(const char* pcBaseAddress, const IMAGE_IMPORT_DESCRIPTOR& desc, const IMAGE_DATA_DIRECTORY& dir, std::string_view reqFunc, uint32_t hintOrOrdinal, void*& ppFunctionAddress) {
    const auto importLookupsOversizedSpan = std::span(reinterpret_cast<const TEntryType*>(&pcBaseAddress[desc.OriginalFirstThunk]), (dir.Size - desc.OriginalFirstThunk) / sizeof TEntryType);
    const auto importAddressesOversizedSpan = std::span(reinterpret_cast<const TEntryType*>(&pcBaseAddress[desc.FirstThunk]), (dir.Size - desc.FirstThunk) / sizeof TEntryType);

    for (size_t i = 0, i_ = (std::min)(importLookupsOversizedSpan.size(), importAddressesOversizedSpan.size()); i < i_ && importLookupsOversizedSpan[i] && importAddressesOversizedSpan[i]; i++) {
        const auto& importLookup = importLookupsOversizedSpan[i];
        const auto& importAddress = importAddressesOversizedSpan[i];
        const auto& importByName = *reinterpret_cast<const IMAGE_IMPORT_BY_NAME*>(&pcBaseAddress[importLookup]);

        // Is this entry importing by ordinals? A lot of socket functions are the case.
        if (IMAGE_SNAP_BY_ORDINAL32(importLookup)) {

            // Is this the entry?
            if (!hintOrOrdinal || IMAGE_ORDINAL32(importLookup) != hintOrOrdinal)
                continue;

            // Is this entry not importing by ordinals, and are we using hint exclusively to find the entry?
        } else if (reqFunc.empty()) {

            // Is this the entry?
            if (importByName.Hint != hintOrOrdinal)
                continue;

        } else {

            // Name must be contained in this directory.
            auto currFunc = std::string_view(importByName.Name, (std::min<size_t>)(&pcBaseAddress[dir.Size] - importByName.Name, reqFunc.size()));
            currFunc = currFunc.substr(0, strnlen(currFunc.data(), currFunc.size()));

            // Is this the entry? (Case sensitive)
            if (reqFunc != currFunc)
                continue;
        }

        // Found the entry; return the address of the pointer to the target function.
        ppFunctionAddress = const_cast<void*>(reinterpret_cast<const void*>(&importAddress));
        return true;
    }

    return false;
}

bool utils::find_imported_function_pointer(HMODULE hModule, const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal, void*& ppFunctionAddress) {
    const auto requestedDllName = std::string_view(pcszDllName, strlen(pcszDllName));
    const auto requestedFunctionName = pcszFunctionName ? std::string_view(pcszFunctionName, strlen(pcszFunctionName)) : std::string_view();

    ppFunctionAddress = nullptr;

    const auto pcBaseAddress = reinterpret_cast<char*>(hModule);
    const auto& dosHeader = *reinterpret_cast<const IMAGE_DOS_HEADER*>(&pcBaseAddress[0]);
    const auto& ntHeader32 = *reinterpret_cast<const IMAGE_NT_HEADERS32*>(&pcBaseAddress[dosHeader.e_lfanew]);
    const auto& ntHeader64 = *reinterpret_cast<const IMAGE_NT_HEADERS64*>(&pcBaseAddress[dosHeader.e_lfanew]);
    const auto bPE32 = ntHeader32.OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC;
    const auto pDirectory = bPE32
        ? &ntHeader32.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT]
        : &ntHeader64.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];

    // There should always be an import directory, but the world may break down anytime nowadays.
    if (!pDirectory)
        return false;

    // This span might be too long in terms of meaningful data; it only serves to prevent accessing memory outsides boundaries.
    const auto importDescriptorsOversizedSpan = std::span(reinterpret_cast<const IMAGE_IMPORT_DESCRIPTOR*>(&pcBaseAddress[pDirectory->VirtualAddress]), pDirectory->Size / sizeof IMAGE_IMPORT_DESCRIPTOR);
    for (const auto& importDescriptor : importDescriptorsOversizedSpan) {

        // Having all zero values signals the end of the table. We didn't find anything.
        if (!importDescriptor.OriginalFirstThunk && !importDescriptor.TimeDateStamp && !importDescriptor.ForwarderChain && !importDescriptor.FirstThunk)
            return false;

        // Skip invalid entries, just in case.
        if (!importDescriptor.Name || !importDescriptor.OriginalFirstThunk)
            continue;

        // Name must be contained in this directory.
        if (importDescriptor.Name < pDirectory->VirtualAddress)
            continue;
        auto currentDllName = std::string_view(&pcBaseAddress[importDescriptor.Name], (std::min<size_t>)(pDirectory->Size - importDescriptor.Name, requestedDllName.size()));
        currentDllName = currentDllName.substr(0, strnlen(currentDllName.data(), currentDllName.size()));

        // Is this entry about the DLL that we're looking for? (Case insensitive)
        if (requestedDllName.size() != currentDllName.size() || _strcmpi(requestedDllName.data(), currentDllName.data()))
            continue;

        if (bPE32 && find_imported_function_pointer_helper<uint32_t>(pcBaseAddress, importDescriptor, *pDirectory, requestedFunctionName, hintOrOrdinal, ppFunctionAddress))
            return true;
        else if (!bPE32 && find_imported_function_pointer_helper<uint64_t>(pcBaseAddress, importDescriptor, *pDirectory, requestedFunctionName, hintOrOrdinal, ppFunctionAddress))
            return true;
    }

    // Found nothing.
    return false;
}

void* utils::get_imported_function_pointer(HMODULE hModule, const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal) {
    if (void* ppImportTableItem{}; find_imported_function_pointer(GetModuleHandleW(nullptr), pcszDllName, pcszFunctionName, hintOrOrdinal, ppImportTableItem))
        return ppImportTableItem;

    throw std::runtime_error("Failed to find import for kernel32!OpenProcess.");
}

std::shared_ptr<void> utils::create_thunk(void* pfnFunction, void* pThis, uint64_t placeholderValue) {
    const auto pcBaseFn = reinterpret_cast<const uint8_t*>(pfnFunction);
    auto sourceCode = std::vector<uint8_t>(pcBaseFn, pcBaseFn + 256);

    size_t i = 0;
    auto placeholderFound = false;
    for (nmd_x86_instruction instruction{}; ; i += instruction.length) {
        if (i == sourceCode.size() || !nmd_x86_decode(&sourceCode[i], sourceCode.size() - i, &instruction, NMD_X86_MODE_64, NMD_X86_DECODER_FLAGS_ALL)) {
            sourceCode.insert(sourceCode.end(), &pcBaseFn[sourceCode.size()], &pcBaseFn[sourceCode.size() + 512]);
            if (!nmd_x86_decode(&sourceCode[i], sourceCode.size() - i, &instruction, NMD_X86_MODE_64, NMD_X86_DECODER_FLAGS_ALL))
                throw std::runtime_error("Failed to find detour function");
        }

        if (instruction.opcode == 0xCC)
            throw std::runtime_error("Failed to find detour function");

        // msvc debugger related
        if ((instruction.group & NMD_GROUP_CALL) && (instruction.imm_mask & NMD_X86_IMM_ANY))
            std::fill_n(&sourceCode[i], instruction.length, 0x90);

        if ((instruction.group & NMD_GROUP_JUMP) || (instruction.group & NMD_GROUP_RET)) {
            sourceCode.resize(i + instruction.length);
            break;
        }

        if (instruction.opcode == 0xB8  // mov <register>, <thunk placeholder 64bit value>
            && (instruction.imm_mask & NMD_X86_IMM64)
            && instruction.immediate == placeholderValue) {
            *reinterpret_cast<void**>(&sourceCode[i + instruction.length - 8]) = pThis;
            placeholderFound = true;
        }
    }

    if (!placeholderFound)
        throw std::runtime_error("Failed to find detour function");

    return allocate_executable_heap(std::span(sourceCode));
}

template<>
std::wstring utils::get_env(const wchar_t* pcwzName) {
    std::wstring buf(GetEnvironmentVariableW(pcwzName, nullptr, 0) + 1, L'\0');
    buf.resize(GetEnvironmentVariableW(pcwzName, &buf[0], static_cast<DWORD>(buf.size())));
    return buf;
}

template<>
std::string utils::get_env(const wchar_t* pcwzName) {
	return unicode::convert<std::string>(get_env<std::wstring>(pcwzName));
}

template<>
bool utils::get_env(const wchar_t* pcwzName) {
    auto env = get_env<std::wstring>(pcwzName);
    const auto trimmed = trim(std::wstring_view(env));
    for (auto& c : env) {
        if (c < 255)
            c = std::tolower(c);
    }
    return trimmed == L"1"
        || trimmed == L"true"
        || trimmed == L"t"
        || trimmed == L"yes"
        || trimmed == L"y";
}

bool utils::is_running_on_linux() {
    if (get_env<bool>(L"XL_WINEONLINUX"))
        return true;
    HMODULE hntdll = GetModuleHandleW(L"ntdll.dll");
    if (!hntdll)
        return true;
    if (GetProcAddress(hntdll, "wine_get_version"))
        return true;
    if (GetProcAddress(hntdll, "wine_get_host_version"))
        return true;
    return false;
}

std::filesystem::path utils::get_module_path(HMODULE hModule) {
    std::wstring buf(MAX_PATH, L'\0');
    while (true) {
        if (const auto res = GetModuleFileNameW(hModule, &buf[0], static_cast<int>(buf.size())); !res)
            throw std::runtime_error(std::format("GetModuleFileName failure: 0x{:X}", GetLastError()));
        else if (res < buf.size()) {
            buf.resize(res);
            return buf;
        } else
            buf.resize(buf.size() * 2);
    }
}

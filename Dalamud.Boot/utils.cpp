#include "pch.h"

#include "utils.h"

std::filesystem::path utils::loaded_module::path() const {
    std::wstring buf(MAX_PATH, L'\0');
    for (;;) {
        if (const auto len = GetModuleFileNameExW(GetCurrentProcess(), m_hModule, &buf[0], static_cast<DWORD>(buf.size())); len != buf.size()) {
            if (buf.empty())
                throw std::runtime_error(std::format("Failed to resolve module path: Win32 error {}", GetLastError()));
            buf.resize(len);
            return buf;
        }

        if (buf.size() * 2 < PATHCCH_MAX_CCH)
            buf.resize(buf.size() * 2);
        else if (auto p = std::filesystem::path(buf); exists(p))
            return p;
        else
            throw std::runtime_error("Failed to resolve module path: no amount of buffer size would fit the data");
    }
}

bool utils::loaded_module::owns_address(const void* pAddress) const {
    const auto pcAddress = reinterpret_cast<const char*>(pAddress);
    const auto pcModule = reinterpret_cast<const char*>(m_hModule);
    return pcModule <= pcAddress && pcAddress <= pcModule + (is_pe64() ? nt_header64().OptionalHeader.SizeOfImage : nt_header32().OptionalHeader.SizeOfImage);
}

std::span<IMAGE_SECTION_HEADER> utils::loaded_module::section_headers() const {
    const auto& dosHeader = ref_as<IMAGE_DOS_HEADER>(0);
    const auto& ntHeader32 = ref_as<IMAGE_NT_HEADERS32>(dosHeader.e_lfanew);
    // Since this does not refer to OptionalHeader32/64 else than its offset, we can use either.
    return { IMAGE_FIRST_SECTION(&ntHeader32), ntHeader32.FileHeader.NumberOfSections };
}

IMAGE_SECTION_HEADER& utils::loaded_module::section_header(const char* pcszSectionName) const {
    for (auto& section : section_headers()) {
        if (strncmp(reinterpret_cast<const char*>(section.Name), pcszSectionName, IMAGE_SIZEOF_SHORT_NAME) == 0)
            return section;
    }

    throw std::out_of_range(std::format("Section [{}] not found", pcszSectionName));
}

std::span<char> utils::loaded_module::section(size_t index) const {
    auto& sectionHeader = section_headers()[index];
    return { address(sectionHeader.VirtualAddress), sectionHeader.Misc.VirtualSize };
}

std::span<char> utils::loaded_module::section(const char* pcszSectionName) const {
    auto& sectionHeader = section_header(pcszSectionName);
    return { address(sectionHeader.VirtualAddress), sectionHeader.Misc.VirtualSize };
}

template<typename TEntryType>
static bool find_imported_function_pointer_helper(const char* pcBaseAddress, const IMAGE_IMPORT_DESCRIPTOR& desc, const IMAGE_DATA_DIRECTORY& dir, std::string_view reqFunc, uint32_t hintOrOrdinal, void*& ppFunctionAddress) {
    const auto importLookupsOversizedSpan = std::span(reinterpret_cast<const TEntryType*>(&pcBaseAddress[desc.OriginalFirstThunk]), (dir.Size - desc.OriginalFirstThunk) / sizeof(TEntryType));
    const auto importAddressesOversizedSpan = std::span(reinterpret_cast<const TEntryType*>(&pcBaseAddress[desc.FirstThunk]), (dir.Size - desc.FirstThunk) / sizeof(TEntryType));

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

bool utils::loaded_module::find_imported_function_pointer(const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal, void*& ppFunctionAddress) const {
    const auto requestedDllName = std::string_view(pcszDllName, strlen(pcszDllName));
    const auto requestedFunctionName = pcszFunctionName ? std::string_view(pcszFunctionName, strlen(pcszFunctionName)) : std::string_view();
    const auto& directory = data_directory(IMAGE_DIRECTORY_ENTRY_IMPORT);
    ppFunctionAddress = nullptr;

    // This span might be too long in terms of meaningful data; it only serves to prevent accessing memory outsides boundaries.
    for (const auto& importDescriptor : span_as<IMAGE_IMPORT_DESCRIPTOR>(directory.VirtualAddress, directory.Size / sizeof IMAGE_IMPORT_DESCRIPTOR)) {

        // Having all zero values signals the end of the table. We didn't find anything.
        if (!importDescriptor.OriginalFirstThunk && !importDescriptor.TimeDateStamp && !importDescriptor.ForwarderChain && !importDescriptor.FirstThunk)
            return false;

        // Skip invalid entries, just in case.
        if (!importDescriptor.Name || !importDescriptor.OriginalFirstThunk)
            continue;

        // Name must be contained in this directory.
        if (importDescriptor.Name < directory.VirtualAddress)
            continue;
        auto currentDllName = std::string_view(address_as<char>(importDescriptor.Name), (std::min<size_t>)(directory.Size - importDescriptor.Name, requestedDllName.size()));
        currentDllName = currentDllName.substr(0, strnlen(currentDllName.data(), currentDllName.size()));

        // Is this entry about the DLL that we're looking for? (Case insensitive)
        if (requestedDllName.size() != currentDllName.size() || _strcmpi(requestedDllName.data(), currentDllName.data()))
            continue;

        if (is_pe64()) {
            if (find_imported_function_pointer_helper<uint64_t>(address(), importDescriptor, directory, requestedFunctionName, hintOrOrdinal, ppFunctionAddress))
                return true;
        } else {
            if (find_imported_function_pointer_helper<uint32_t>(address(), importDescriptor, directory, requestedFunctionName, hintOrOrdinal, ppFunctionAddress))
                return true;
        }
    }

    // Found nothing.
    return false;
}

void* utils::loaded_module::get_imported_function_pointer(const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal) const {
    if (void* ppImportTableItem{}; find_imported_function_pointer(pcszDllName, pcszFunctionName, hintOrOrdinal, ppImportTableItem))
        return ppImportTableItem;

    throw std::runtime_error(std::format("Failed to find import for {}!{} ({}).", pcszDllName, pcszFunctionName ? pcszFunctionName : "<unnamed>", hintOrOrdinal));
}

std::unique_ptr<std::remove_pointer_t<HGLOBAL>, decltype(&FreeResource)> utils::loaded_module::get_resource(LPCWSTR lpName, LPCWSTR lpType) const {
    const auto hres = FindResourceW(m_hModule, lpName, lpType);
    if (!hres)
        throw std::runtime_error("No such resource");
    
    const auto hRes = LoadResource(m_hModule, hres);
    if (!hRes)
        throw std::runtime_error("LoadResource failure");

    return {hRes, &FreeResource};
}

std::wstring utils::loaded_module::get_description() const {
    const auto rsrc = get_resource(MAKEINTRESOURCE(VS_VERSION_INFO), RT_VERSION);
    const auto pBlock = LockResource(rsrc.get());
    
    struct LANGANDCODEPAGE {
        WORD wLanguage;
        WORD wCodePage;
    } * lpTranslate;
    UINT cbTranslate;
    if (!VerQueryValueW(pBlock,
        TEXT("\\VarFileInfo\\Translation"),
        reinterpret_cast<LPVOID*>(&lpTranslate),
        &cbTranslate)) {
        throw std::runtime_error("Invalid version information (1)");
    }

    for (size_t i = 0; i < (cbTranslate / sizeof(LANGANDCODEPAGE)); i++) {
        wchar_t* buf = nullptr;
        UINT size = 0;
        if (!VerQueryValueW(pBlock,
            std::format(L"\\StringFileInfo\\{:04x}{:04x}\\FileDescription",
                lpTranslate[i].wLanguage,
                lpTranslate[i].wCodePage).c_str(),
            reinterpret_cast<LPVOID*>(&buf),
            &size)) {
            continue;
        }
        auto currName = std::wstring_view(buf, size);
        while (!currName.empty() && currName.back() == L'\0')
            currName = currName.substr(0, currName.size() - 1);
        if (currName.empty())
            continue;
        return std::wstring(currName);
    }
    
    throw std::runtime_error("Invalid version information (2)");
}

VS_FIXEDFILEINFO utils::loaded_module::get_file_version() const {
    const auto rsrc = get_resource(MAKEINTRESOURCE(VS_VERSION_INFO), RT_VERSION);
    const auto pBlock = LockResource(rsrc.get());
    UINT size = 0;
    LPVOID lpBuffer = nullptr;
    if (!VerQueryValueW(pBlock, L"\\", &lpBuffer, &size))
		throw std::runtime_error("Failed to query version information.");
    const VS_FIXEDFILEINFO& versionInfo = *static_cast<const VS_FIXEDFILEINFO*>(lpBuffer);
    if (versionInfo.dwSignature != 0xfeef04bd)
		throw std::runtime_error("Invalid version info found.");
    return versionInfo;
}

utils::loaded_module utils::loaded_module::current_process() {
    return { GetModuleHandleW(nullptr) };
}

std::vector<utils::loaded_module> utils::loaded_module::all_modules() {
    std::vector<HMODULE> hModules(128);
    for (DWORD dwNeeded{}; EnumProcessModules(GetCurrentProcess(), &hModules[0], static_cast<DWORD>(std::span(hModules).size_bytes()), &dwNeeded) && hModules.size() < dwNeeded;)
        hModules.resize(hModules.size() + 128);

    std::vector<loaded_module> modules;
    modules.reserve(hModules.size());
    for (const auto hModule : hModules) {
        if (!hModule)
            break;
        modules.emplace_back(hModule);
    }

    return modules;
}

std::wstring utils::format_file_version(const VS_FIXEDFILEINFO& v) {
    if (v.dwFileVersionMS == v.dwProductVersionMS && v.dwFileVersionLS == v.dwProductVersionLS) {
        return std::format(L"{}.{}.{}.{}",
            (v.dwProductVersionMS >> 16) & 0xFFFF,
            (v.dwProductVersionMS >> 0) & 0xFFFF,
            (v.dwProductVersionLS >> 16) & 0xFFFF,
            (v.dwProductVersionLS >> 0) & 0xFFFF);
    } else {
        return std::format(L"file={}.{}.{}.{} prod={}.{}.{}.{}",
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

utils::signature_finder& utils::signature_finder::look_in(const void* pFirst, size_t length) {
    if (length)
        m_ranges.emplace_back(std::span(reinterpret_cast<const char*>(pFirst), length));

    return *this;
}

utils::signature_finder& utils::signature_finder::look_in(const loaded_module& m, const char* sectionName) {
    return look_in(m.section(sectionName));
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

const char* utils::signature_finder::result::resolve_jump_target(size_t instructionOffset) const {
    nmd_x86_instruction instruction{};
    if (!nmd_x86_decode(&Match[instructionOffset], NMD_X86_MAXIMUM_INSTRUCTION_LENGTH, &instruction, NMD_X86_MODE_64, NMD_X86_DECODER_FLAGS_ALL))
        throw std::runtime_error("Matched address does not have a valid assembly instruction");
    
    size_t numExplicitOperands = 0;
    for (size_t i = 0; i < instruction.num_operands; i++)
        numExplicitOperands += instruction.operands[i].is_implicit ? 0 : 1;
    if (numExplicitOperands != 1)
        throw std::runtime_error("Number of operands at the instruction at matched address is not 1");

    if (!(instruction.group & NMD_GROUP_CALL) && !(instruction.group & NMD_GROUP_JUMP))
        throw std::runtime_error("The instruction at matched address is not a call or jump instruction");

    const auto& arg1 = instruction.operands[0];
    if (arg1.type != NMD_X86_OPERAND_TYPE_IMMEDIATE)
        throw std::runtime_error("The first operand for the instruction at matched address is not an immediate value");

    return &Match[instructionOffset] + instruction.length + arg1.fields.imm;
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

utils::signature_finder::result utils::signature_finder::find_one() const {
    return find(1, 1, false).front();
}

utils::memory_tenderizer::memory_tenderizer(const void* pAddress, size_t length, DWORD dwNewProtect)
    : memory_tenderizer(GetCurrentProcess(), pAddress, length, dwNewProtect) {
}

utils::memory_tenderizer::memory_tenderizer(HANDLE hProcess, const void* pAddress, size_t length, DWORD dwNewProtect)
: m_process(hProcess)
, m_data(static_cast<char*>(const_cast<void*>(pAddress)), length) {
    try {
        for (auto pCoveredAddress = m_data.data();
            pCoveredAddress < m_data.data() + m_data.size();
            pCoveredAddress = static_cast<char*>(m_regions.back().BaseAddress) + m_regions.back().RegionSize) {

            MEMORY_BASIC_INFORMATION region{};
            if (!VirtualQueryEx(hProcess, pCoveredAddress, &region, sizeof region)) {
                throw std::runtime_error(std::format(
                    "VirtualQuery(addr=0x{:X}, ..., cb={}) failed with Win32 code 0x{:X}",
                    reinterpret_cast<size_t>(pCoveredAddress),
                    sizeof region,
                    GetLastError()));
            }

            if (!VirtualProtectEx(hProcess, region.BaseAddress, region.RegionSize, dwNewProtect, &region.Protect)) {
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
            if (!VirtualProtectEx(hProcess, region.BaseAddress, region.RegionSize, region.Protect, &region.Protect)) {
                // Could not restore; fast fail
                __fastfail(GetLastError());
            }
        }

        throw;
    }
}

utils::memory_tenderizer::~memory_tenderizer() {
    for (auto& region : std::ranges::reverse_view(m_regions)) {
        if (!VirtualProtectEx(m_process, region.BaseAddress, region.RegionSize, region.Protect, &region.Protect)) {
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
int utils::get_env(const wchar_t* pcwzName) {
    auto env = get_env<std::wstring>(pcwzName);
    const auto trimmed = trim(std::wstring_view(env));
    if (trimmed.empty())
        return 0;
    return std::wcstol(&trimmed[0], nullptr, 0);
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

template<>
std::vector<std::wstring> utils::get_env_list(const wchar_t* pcszName) {
    const auto src = utils::get_env<std::wstring>(pcszName);
    auto res = utils::split(src, L",");
    for (auto& s : res)
        s = utils::trim(s);
    if (res.size() == 1 && res[0].empty())
        return {};
    return res;
}

template<>
std::vector<std::string> utils::get_env_list(const wchar_t* pcszName) {
    const auto src = utils::get_env<std::string>(pcszName);
    auto res = utils::split(src, ",");
    for (auto& s : res)
        s = utils::trim(s);
    if (res.size() == 1 && res[0].empty())
        return {};
    return res;
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

HWND utils::try_find_game_window() {
    HWND hwnd = nullptr;
    while ((hwnd = FindWindowExW(nullptr, hwnd, L"FFXIVGAME", nullptr))) {
        DWORD pid;
        GetWindowThreadProcessId(hwnd, &pid);

        if (pid == GetCurrentProcessId() && IsWindowVisible(hwnd))
            break;
    }
    return hwnd;
}

void utils::wait_for_game_window() {
	HWND game_window;
	while (!(game_window = try_find_game_window())) {
		WaitForInputIdle(GetCurrentProcess(), INFINITE);
		Sleep(100);
	};
	SendMessageW(game_window, WM_NULL, 0, 0);
}

std::wstring utils::escape_shell_arg(const std::wstring& arg) {
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

std::wstring utils::format_win32_error(DWORD err) {
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
    if (pwszMsg) {
        std::wstring result = std::format(L"Win32 error ({}=0x{:X}): {}", err, err, pwszMsg);
        while (!result.empty() && std::isspace(result.back()))
            result.pop_back();
        LocalFree(pwszMsg);
        return result;
    }

    return std::format(L"Win32 error ({}=0x{:X})", err, err);
}

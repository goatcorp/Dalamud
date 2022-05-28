#include "pch.h"

#include "budget_hooks.h"

namespace budget_hooks::utils {
    budget_hooks::utils::signature_finder& signature_finder::look_in(const void* pFirst, size_t length) {
        if (length)
            m_ranges.emplace_back(std::span(reinterpret_cast<const char*>(pFirst), length));

        return *this;
    }

    budget_hooks::utils::signature_finder& signature_finder::look_in(const void* pFirst, const void* pLast) {
        return look_in(pFirst, reinterpret_cast<const char*>(pLast) - reinterpret_cast<const char*>(pFirst));
    }

    budget_hooks::utils::signature_finder& signature_finder::look_in(HMODULE hModule, const char* sectionName) {
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

    budget_hooks::utils::signature_finder& signature_finder::look_for(std::string_view pattern, std::string_view mask, char cExactMatch, char cWildcard) {
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

    budget_hooks::utils::signature_finder& signature_finder::look_for(std::string_view pattern, char wildcardMask) {
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

    budget_hooks::utils::signature_finder& signature_finder::look_for(std::string_view pattern) {
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

    budget_hooks::utils::signature_finder& signature_finder::look_for_hex(std::string_view pattern) {
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

    std::vector<budget_hooks::utils::signature_finder::result> signature_finder::find(size_t minCount, size_t maxCount, bool bErrorOnMoreThanMaximum) const {
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

    std::span<const char> signature_finder::find_one() const {
        return find(1, 1, false).front().Match;
    }

    memory_tenderizer::memory_tenderizer(const void* pAddress, size_t length, DWORD dwNewProtect) : m_data(reinterpret_cast<char*>(const_cast<void*>(pAddress)), length) {
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

    memory_tenderizer::~memory_tenderizer() {
        for (auto& region : std::ranges::reverse_view(m_regions)) {
            if (!VirtualProtect(region.BaseAddress, region.RegionSize, region.Protect, &region.Protect)) {
                // Could not restore; fast fail
                __fastfail(GetLastError());
            }
        }
    }

    std::shared_ptr<void> allocate_executable_heap(size_t len) {
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

    void* resolve_unconditional_jump_target(void* pfn) {
        const auto bytes = reinterpret_cast<uint8_t*>(pfn);

        // JMP QWORD PTR [RIP + int32]
        // 48 FF 25 ?? ?? ?? ??
        if (bytes[0] == 0x48 && bytes[1] == 0xFF && bytes[2] == 0x25)
            return *reinterpret_cast<void**>(&bytes[7 + *reinterpret_cast<int*>(&bytes[3])]);

        throw std::runtime_error("Unexpected thunk bytes.");
    }

    template<typename TEntryType>
    bool find_imported_function_pointer_helper(const char* pcBaseAddress, const IMAGE_IMPORT_DESCRIPTOR& desc, const IMAGE_DATA_DIRECTORY& dir, std::string_view reqFunc, uint32_t hintOrOrdinal, void*& ppFunctionAddress) {
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

    bool find_imported_function_pointer(HMODULE hModule, const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal, void*& ppFunctionAddress) {
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

    void* get_imported_function_pointer(HMODULE hModule, const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal) {
        if (void* ppImportTableItem{}; find_imported_function_pointer(GetModuleHandleW(nullptr), pcszDllName, pcszFunctionName, hintOrOrdinal, ppImportTableItem))
            return ppImportTableItem;

        throw std::runtime_error("Failed to find import for kernel32!OpenProcess.");
    }

    std::shared_ptr<void> create_thunk(void* pfnFunction, void* pThis) {
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
                && instruction.immediate == ThunkTemplateFunctionThisPointerPlaceholder) {
                *reinterpret_cast<void**>(&sourceCode[i + instruction.length - 8]) = pThis;
                placeholderFound = true;
            }
        }

        if (!placeholderFound)
            throw std::runtime_error("Failed to find detour function");

        return allocate_executable_heap(std::span(sourceCode));
    }

    template<typename>
    class thunk;

    template<typename TReturn, typename ... TArgs>
    class thunk<TReturn(TArgs...)> {
        using TFn = TReturn(TArgs...);

        const std::shared_ptr<void> m_pThunk;
        std::function<TFn> m_fnTarget;

    public:
        thunk(std::function<TFn> target)
            : m_pThunk(utils::create_thunk(&detour_static, this))
            , m_fnTarget(std::move(target)) {
        }

        void set_target(std::function<TFn> detour) {
            m_fnTarget = std::move(detour);
        }

        TFn* get_thunk() const {
            return reinterpret_cast<TFn*>(m_pThunk.get());
        }

    private:
        // mark it as virtual to prevent compiler from inlining
        virtual TReturn detour(TArgs... args) {
            return m_fnTarget(std::forward<TArgs>(args)...);
        }

        static TReturn detour_static(TArgs... args) {
            const volatile auto pThis = reinterpret_cast<thunk<TFn>*>(ThunkTemplateFunctionThisPointerPlaceholder);
            return pThis->detour(args...);
        }
    };
}

namespace budget_hooks::hooks {
    template<typename>
    class base_hook;

    template<typename TReturn, typename ... TArgs>
    class base_hook<TReturn(TArgs...)> {
        using TFn = TReturn(TArgs...);

    private:
        TFn* const m_pfnOriginal;
        utils::thunk<TReturn(TArgs...)> m_thunk;

    public:
        base_hook(TFn* pfnOriginal)
            : m_pfnOriginal(pfnOriginal)
            , m_thunk(m_pfnOriginal) {
        }

        virtual ~base_hook() = default;

        virtual void set_detour(std::function<TFn> fn) {
            if (!fn)
                m_thunk.set_target(m_pfnOriginal);
            else
                m_thunk.set_target(std::move(fn));
        }

        virtual TReturn call_original(TArgs... args) {
            return m_pfnOriginal(std::forward<TArgs>(args)...);
        }

    protected:
        TFn* get_original() const {
            return m_pfnOriginal;
        }

        TFn* get_thunk() const {
            return m_thunk.get_thunk();
        }
    };

    template<typename TFn>
    class import_hook : public base_hook<TFn> {
        using Base = base_hook<TFn>;

        TFn** const m_ppfnImportTableItem;

    public:
        import_hook(TFn** ppfnImportTableItem)
            : Base(*ppfnImportTableItem)
            , m_ppfnImportTableItem(ppfnImportTableItem) {

            const utils::memory_tenderizer tenderizer(ppfnImportTableItem, sizeof * ppfnImportTableItem, PAGE_READWRITE);
            *ppfnImportTableItem = Base::get_thunk();
        }

        import_hook(const char* pcszDllName, const char* pcszFunctionName, int hintOrOrdinal)
            : import_hook(utils::get_imported_function_pointer<TFn>(GetModuleHandleW(nullptr), pcszDllName, pcszFunctionName, hintOrOrdinal)) {
        }

        ~import_hook() override {
            const utils::memory_tenderizer tenderizer(m_ppfnImportTableItem, sizeof * m_ppfnImportTableItem, PAGE_READWRITE);

            *m_ppfnImportTableItem = Base::get_original();
        }
    };

    template<typename TFn>
    class export_hook : public base_hook<TFn> {
        using Base = base_hook<TFn>;

        static constexpr uint8_t DetouringThunkTemplate[12]{
            0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  // movabs rax, 0x0000000000000000
            0xFF, 0xE0, // jmp rax
        };

        TFn* const m_pfnExportThunk;
        uint8_t s_originalThunk[sizeof DetouringThunkTemplate]{};

    public:
        export_hook(TFn* pfnExportThunk)
            : Base(reinterpret_cast<TFn*>(utils::resolve_unconditional_jump_target(pfnExportThunk)))
            , m_pfnExportThunk(pfnExportThunk) {
            auto pExportThunk = reinterpret_cast<uint8_t*>(pfnExportThunk);

            // Make it writeable.
            const utils::memory_tenderizer tenderizer(pfnExportThunk, sizeof DetouringThunkTemplate, PAGE_EXECUTE_READWRITE);

            // Back up original thunk bytes.
            memcpy(s_originalThunk, pExportThunk, sizeof s_originalThunk);

            // Write thunk template.
            memcpy(pExportThunk, DetouringThunkTemplate, sizeof DetouringThunkTemplate);

            // Write target address.
            *reinterpret_cast<TFn**>(&pExportThunk[2]) = Base::get_thunk();
        }

        ~export_hook() override {
            const utils::memory_tenderizer tenderizer(m_pfnExportThunk, sizeof DetouringThunkTemplate, PAGE_EXECUTE_READWRITE);

            // Restore original thunk bytes.
            memcpy(m_pfnExportThunk, s_originalThunk, sizeof s_originalThunk);

            // Clear state.
            memset(s_originalThunk, 0, sizeof s_originalThunk);
        }
    };

    class wndproc_hook : public base_hook<std::remove_pointer_t<WNDPROC>> {
        using Base = base_hook<std::remove_pointer_t<WNDPROC>>;

        const HWND s_hwnd;

    public:
        wndproc_hook(HWND hwnd)
            : Base(reinterpret_cast<WNDPROC>(GetWindowLongPtrW(hwnd, GWLP_WNDPROC)))
            , s_hwnd(hwnd) {
            SetWindowLongPtrW(hwnd, GWLP_WNDPROC, reinterpret_cast<LONG_PTR>(Base::get_thunk()));
        }

        ~wndproc_hook() override {
            SetWindowLongPtrW(s_hwnd, GWLP_WNDPROC, reinterpret_cast<LONG_PTR>(Base::get_original()));
        }

        LRESULT call_original(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) override {
            return CallWindowProcW(Base::get_original(), hwnd, msg, wParam, lParam);
        }
    };
}

using TFnGetInputDeviceManager = void* ();
static TFnGetInputDeviceManager* GetGetInputDeviceManager(HWND hwnd) {
    static TFnGetInputDeviceManager* pCached = nullptr;
    if (pCached)
        return pCached;

    char szClassName[256];
    GetClassNameA(hwnd, szClassName, static_cast<int>(sizeof szClassName));

    WNDCLASSEXA wcx{};
    GetClassInfoExA(g_hGameInstance, szClassName, &wcx);
    const auto match = budget_hooks::utils::signature_finder()
        .look_in(g_hGameInstance, ".text")
        .look_for_hex("41 81 fe 19 02 00 00 0f 87 ?? ?? 00 00 0f 84 ?? ?? 00 00")
        .find_one();

    auto ptr = match.data() + match.size() + *reinterpret_cast<const int*>(match.data() + match.size() - 4);
    ptr += 4;  // CMP RBX, 0x7
    ptr += 2;  // JNZ <giveup>
    ptr += 7;  // MOV RCX, <Framework::Instance>
    ptr += 3;  // TEST RCX, RCX
    ptr += 2;  // JZ <giveup>
    ptr += 5;  // CALL <GetInputDeviceManagerInstance()>
    ptr += *reinterpret_cast<const int*>(ptr - 4);

    return pCached = reinterpret_cast<TFnGetInputDeviceManager*>(ptr);
}

void budget_hooks::fixes::prevent_devicechange_crashes(bool bApply) {
    static std::optional<hooks::import_hook<decltype(CreateWindowExA)>> s_hookCreateWindow;
    static std::optional<hooks::wndproc_hook> s_hookWndProc;

    if (bApply) {
        s_hookCreateWindow.emplace("user32.dll", "CreateWindowExA", 0);
        s_hookCreateWindow->set_detour([](DWORD dwExStyle, LPCSTR lpClassName, LPCSTR lpWindowName, DWORD dwStyle, int X, int Y, int nWidth, int nHeight, HWND hWndParent, HMENU hMenu, HINSTANCE hInstance, LPVOID lpParam)->HWND {
            const auto hWnd = s_hookCreateWindow->call_original(dwExStyle, lpClassName, lpWindowName, dwStyle, X, Y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);

            if (!hWnd
                || hInstance != g_hGameInstance
                || 0 != strcmp(lpClassName, "FFXIVGAME"))
                return hWnd;

            s_hookCreateWindow.reset();

            s_hookWndProc.emplace(hWnd);
            s_hookWndProc->set_detour([](HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam) -> LRESULT {
                if (uMsg == WM_DEVICECHANGE && wParam == DBT_DEVNODES_CHANGED) {
                    if (!GetGetInputDeviceManager(hWnd)())
                        return 0;
                }

                return s_hookWndProc->call_original(hWnd, uMsg, wParam, lParam);
            });

            return hWnd;
        });

    } else {
        s_hookCreateWindow.reset();

        // This will effectively revert any other WndProc alterations, including Dalamud.
        s_hookWndProc.reset();
    }
}

void budget_hooks::fixes::disable_game_openprocess_access_check(bool bApply) {
    static std::optional<hooks::import_hook<decltype(OpenProcess)>> hook;

    if (bApply) {
        hook.emplace("kernel32.dll", "OpenProcess", 0);
        hook->set_detour([](DWORD dwDesiredAccess, BOOL bInheritHandle, DWORD dwProcessId)->HANDLE {
            if (dwProcessId == GetCurrentProcessId()) {
                // Prevent game from feeling unsafe that it restarts
                if (dwDesiredAccess & PROCESS_VM_WRITE) {
                    SetLastError(ERROR_ACCESS_DENIED);
                    return {};
                }
            }

            return hook->call_original(dwDesiredAccess, bInheritHandle, dwProcessId);
        });

    } else
        hook.reset();
}

void budget_hooks::fixes::redirect_openprocess_currentprocess_to_duplicatehandle_currentprocess(bool bApply) {
    static std::optional<hooks::export_hook<decltype(OpenProcess)>> hook;

    if (bApply) {
        hook.emplace(::OpenProcess);
        hook->set_detour([](DWORD dwDesiredAccess, BOOL bInheritHandle, DWORD dwProcessId)->HANDLE {
            if (dwProcessId == GetCurrentProcessId()) {
                if (HANDLE res; DuplicateHandle(GetCurrentProcess(), GetCurrentProcess(), GetCurrentProcess(), &res, dwDesiredAccess, bInheritHandle, 0))
                    return res;

                return nullptr;
            }
            return hook->call_original(dwDesiredAccess, bInheritHandle, dwProcessId);
        });

    } else
        hook.reset();
}

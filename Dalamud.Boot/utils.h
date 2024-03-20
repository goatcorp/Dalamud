#pragma once

#include <filesystem>
#include <functional>
#include <span>
#include <string>
#include <memory>
#include <vector>

#include "unicode.h"

namespace utils {
    class loaded_module {
        HMODULE m_hModule;
    public:
        loaded_module() : m_hModule(nullptr) {}
        loaded_module(const void* hModule) : m_hModule(reinterpret_cast<HMODULE>(const_cast<void*>(hModule))) {}
        loaded_module(void* hModule) : m_hModule(reinterpret_cast<HMODULE>(hModule)) {}
        loaded_module(size_t hModule) : m_hModule(reinterpret_cast<HMODULE>(hModule)) {}

        std::filesystem::path path() const;

        bool is_current_process() const { return m_hModule == GetModuleHandleW(nullptr); }
        bool owns_address(const void* pAddress) const;

        operator HMODULE() const {
            return m_hModule;
        }

        size_t address_int() const { return reinterpret_cast<size_t>(m_hModule); }
        size_t image_size() const { return is_pe64() ? nt_header64().OptionalHeader.SizeOfImage : nt_header32().OptionalHeader.SizeOfImage; }
        char* address(size_t offset = 0) const { return reinterpret_cast<char*>(m_hModule) + offset; }
        template<typename T> T* address_as(size_t offset) const { return reinterpret_cast<T*>(address(offset)); }
        template<typename T> std::span<T> span_as(size_t offset, size_t count) const { return std::span<T>(reinterpret_cast<T*>(address(offset)), count); }
        template<typename T> T& ref_as(size_t offset) const { return *reinterpret_cast<T*>(address(offset)); }

        IMAGE_DOS_HEADER& dos_header() const { return ref_as<IMAGE_DOS_HEADER>(0); }
        IMAGE_NT_HEADERS32& nt_header32() const { return ref_as<IMAGE_NT_HEADERS32>(dos_header().e_lfanew); }
        IMAGE_NT_HEADERS64& nt_header64() const { return ref_as<IMAGE_NT_HEADERS64>(dos_header().e_lfanew); }
        bool is_pe64() const { return nt_header32().OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC; }

        std::span<IMAGE_DATA_DIRECTORY> data_directories() const { return is_pe64() ? nt_header64().OptionalHeader.DataDirectory : nt_header32().OptionalHeader.DataDirectory; }
        IMAGE_DATA_DIRECTORY& data_directory(size_t index) const { return data_directories()[index]; }

        std::span<IMAGE_SECTION_HEADER> section_headers() const;
        IMAGE_SECTION_HEADER& section_header(const char* pcszSectionName) const;
        std::span<char> section(size_t index) const;
        std::span<char> section(const char* pcszSectionName) const;

        template<typename TFn> TFn* get_exported_function(const char* pcszFunctionName) {
            const auto pAddress = GetProcAddress(m_hModule, pcszFunctionName);
            if (!pAddress)
                throw std::out_of_range(std::format("Exported function \"{}\" not found.", pcszFunctionName));
            return reinterpret_cast<TFn*>(pAddress);
        }

        bool find_imported_function_pointer(const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal, void*& ppFunctionAddress) const;
        void* get_imported_function_pointer(const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal) const;
        template<typename TFn> TFn** get_imported_function_pointer(const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal) { return reinterpret_cast<TFn**>(get_imported_function_pointer(pcszDllName, pcszFunctionName, hintOrOrdinal)); }

        [[nodiscard]] std::unique_ptr<std::remove_pointer_t<HGLOBAL>, decltype(&FreeResource)> get_resource(LPCWSTR lpName, LPCWSTR lpType) const;
        [[nodiscard]] std::wstring get_description() const;
        [[nodiscard]] VS_FIXEDFILEINFO get_file_version() const;

        static loaded_module current_process();
        static std::vector<loaded_module> all_modules();
    };

    std::wstring format_file_version(const VS_FIXEDFILEINFO& v);

    class signature_finder {
        std::vector<std::span<const char>> m_ranges;
        std::vector<srell::regex> m_patterns;

    public:
        signature_finder& look_in(const void* pFirst, size_t length);
        signature_finder& look_in(const loaded_module& m, const char* sectionName);

        template<typename T>
        signature_finder& look_in(std::span<T> s) {
            return look_in(s.data(), s.size());
        }

        signature_finder& look_for(std::string_view pattern, std::string_view mask, char cExactMatch = 'x', char cWildcard = '.');
        signature_finder& look_for(std::string_view pattern, char wildcardMask);
        signature_finder& look_for(std::string_view pattern);
        signature_finder& look_for_hex(std::string_view pattern);

        template<size_t len>
        signature_finder& look_for(char pattern[len]) {
            static_assert(len == 5);
        }

        struct result {
            std::span<const char> Match;
            size_t PatternIndex;
            size_t MatchIndex;
            size_t CaptureIndex;

            const char* resolve_jump_target(size_t instructionOffset = 0) const;

            template<typename T>
            T resolve_jump_target(size_t instructionOffset = 0) const {
                return reinterpret_cast<T>(const_cast<char*>(resolve_jump_target(instructionOffset)));
            }
        };

        std::vector<result> find(size_t minCount, size_t maxCount, bool bErrorOnMoreThanMaximum) const;

        result find_one() const;
    };

    class memory_tenderizer {
        HANDLE m_process;
        std::span<char> m_data;
        std::vector<MEMORY_BASIC_INFORMATION> m_regions;

    public:
        memory_tenderizer(HANDLE hProcess, const void* pAddress, size_t length, DWORD dwNewProtect);

        memory_tenderizer(const void* pAddress, size_t length, DWORD dwNewProtect);

        template<typename T, typename = std::enable_if_t<std::is_trivial_v<T>&& std::is_standard_layout_v<T>>>
        memory_tenderizer(const T& object, DWORD dwNewProtect) : memory_tenderizer(&object, sizeof T, dwNewProtect) {}

        template<typename T>
        memory_tenderizer(std::span<const T> s, DWORD dwNewProtect) : memory_tenderizer(&s[0], s.size(), dwNewProtect) {}

        template<typename T>
        memory_tenderizer(std::span<T> s, DWORD dwNewProtect) : memory_tenderizer(&s[0], s.size(), dwNewProtect) {}

        ~memory_tenderizer();
    };

    std::shared_ptr<void> allocate_executable_heap(size_t len);

    template<typename T>
    std::shared_ptr<void> allocate_executable_heap(std::span<T> data) {
        auto res = allocate_executable_heap(data.size_bytes());
        memcpy(res.get(), data.data(), data.size_bytes());
        return res;
    }

    std::shared_ptr<void> create_thunk(void* pfnFunction, void* pThis, uint64_t placeholderValue);

    template<typename>
    class thunk;

    template<typename TReturn, typename ... TArgs>
    class thunk<TReturn(TArgs...)> {
        using TFn = TReturn(TArgs...);

        static constexpr uint64_t Placeholder = 0xCC90CC90CC90CC90ULL;

        const std::shared_ptr<void> m_pThunk;
        std::string m_name;
        std::function<TFn> m_fnTarget;

    public:
        thunk(std::string name, std::function<TFn> target)
            : m_pThunk(utils::create_thunk(&detour_static, this, Placeholder))
            , m_fnTarget(std::move(target))
            , m_name(name) {
        }

        void set_target(std::function<TFn> detour) {
            m_fnTarget = std::move(detour);
        }

        TFn* get_thunk() const {
            return reinterpret_cast<TFn*>(m_pThunk.get());
        }

        const std::string& name() const {
            return m_name;
        }

    private:
        // mark it as virtual to prevent compiler from inlining
        virtual TReturn detour(TArgs... args) {
            return m_fnTarget(std::forward<TArgs>(args)...);
        }

        static TReturn detour_static(TArgs... args) {
            const volatile auto pThis = reinterpret_cast<thunk<TFn>*>(Placeholder);
            return pThis->detour(args...);
        }
    };

    template<class TElem, class TTraits = std::char_traits<TElem>>
    std::basic_string_view<TElem, TTraits> trim(std::basic_string_view<TElem, TTraits> view, bool left = true, bool right = true) {
        if (left) {
            while (!view.empty() && (view.front() < 255 && std::isspace(view.front())))
                view = view.substr(1);
        }
        if (right) {
            while (!view.empty() && (view.back() < 255 && std::isspace(view.back())))
                view = view.substr(0, view.size() - 1);
        }
        return view;
    }

    template<class TElem, class TTraits = std::char_traits<TElem>, class TAlloc = std::allocator<TElem>>
    std::basic_string<TElem, TTraits> trim(std::basic_string<TElem, TTraits> view, bool left = true, bool right = true) {
        return std::basic_string<TElem, TTraits, TAlloc>(trim(std::basic_string_view<TElem, TTraits>(view), left, right));
    }

    template<class TElem, class TTraits = std::char_traits<TElem>, class TAlloc = std::allocator<TElem>>
    [[nodiscard]] std::vector<std::basic_string<TElem, TTraits, TAlloc>> split(const std::basic_string<TElem, TTraits, TAlloc>& str, const std::basic_string_view<TElem, TTraits>& delimiter, size_t maxSplit = SIZE_MAX) {
        std::vector<std::basic_string<TElem, TTraits, TAlloc>> result;
        if (delimiter.empty()) {
            for (size_t i = 0; i < str.size(); ++i)
                result.push_back(str.substr(i, 1));
        } else {
            size_t previousOffset = 0, offset;
            while (maxSplit && (offset = str.find(delimiter, previousOffset)) != std::string::npos) {
                result.push_back(str.substr(previousOffset, offset - previousOffset));
                previousOffset = offset + delimiter.length();
                --maxSplit;
            }
            result.push_back(str.substr(previousOffset));
        }
        return result;
    }

    template<class TElem, class TTraits = std::char_traits<TElem>, class TAlloc = std::allocator<TElem>>
    [[nodiscard]] std::vector<std::basic_string<TElem, TTraits, TAlloc>> split(const std::basic_string<TElem, TTraits, TAlloc>& str, const std::basic_string<TElem, TTraits, TAlloc>& delimiter, size_t maxSplit = SIZE_MAX) {
        return split(str, std::basic_string_view<TElem, TTraits>(delimiter), maxSplit);
    }

    template<class TElem, class TTraits = std::char_traits<TElem>, class TAlloc = std::allocator<TElem>>
    [[nodiscard]] std::vector<std::basic_string<TElem, TTraits, TAlloc>> split(const std::basic_string<TElem, TTraits, TAlloc>& str, const TElem* pcszDelimiter, size_t maxSplit = SIZE_MAX) {
        return split(str, std::basic_string_view<TElem, TTraits>(pcszDelimiter), maxSplit);
    }

    template<typename T>
    T get_env(const wchar_t* pcwzName) = delete;

    template<>
    std::wstring get_env(const wchar_t* pcwzName);

    template<>
    std::string get_env(const wchar_t* pcwzName);

    template<>
    int get_env(const wchar_t* pcwzName);

    template<>
    bool get_env(const wchar_t* pcwzName);

    template<typename T>
    T get_env(const char* pcszName) {
        return get_env<T>(unicode::convert<std::wstring>(pcszName).c_str());
    }

    template<typename T>
    std::vector<T> get_env_list(const wchar_t* pcwzName) = delete;

    template<>
    std::vector<std::wstring> get_env_list(const wchar_t* pcwzName);

    template<>
    std::vector<std::string> get_env_list(const wchar_t* pcwzName);

    template<typename T>
    std::vector<T> get_env_list(const char* pcszName) {
        return get_env_list<T>(unicode::convert<std::wstring>(pcszName).c_str());
    }

    std::filesystem::path get_module_path(HMODULE hModule);

    /// @brief Find the game main window.
    /// @return Handle to the game main window, or nullptr if it doesn't exist (yet).
    HWND try_find_game_window();

    void wait_for_game_window();

	std::wstring escape_shell_arg(const std::wstring& arg);

    std::wstring format_win32_error(DWORD err);
}

#pragma once

#include <filesystem>
#include <functional>
#include <span>
#include <string>
#include <memory>
#include <vector>

#include "unicode.h"

namespace utils {
    class signature_finder {
        std::vector<std::span<const char>> m_ranges;
        std::vector<srell::regex> m_patterns;

    public:
        signature_finder& look_in(const void* pFirst, size_t length);
        signature_finder& look_in(const void* pFirst, const void* pLast);
        signature_finder& look_in(HMODULE hModule, const char* sectionName);

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
        };

        std::vector<result> find(size_t minCount, size_t maxCount, bool bErrorOnMoreThanMaximum) const;

        std::span<const char> find_one() const;
    };

    class memory_tenderizer {
        std::span<char> m_data;
        std::vector<MEMORY_BASIC_INFORMATION> m_regions;

    public:
        memory_tenderizer(const void* pAddress, size_t length, DWORD dwNewProtect);

        template<typename T, typename = std::enable_if_t<std::is_trivial_v<T>&& std::is_standard_layout_v<T>>>
        memory_tenderizer(const T& object, DWORD dwNewProtect) : memory_tenderizer(&object, sizeof T, dwNewProtect) {}

        template<typename T>
        memory_tenderizer(std::span<const T> s, DWORD dwNewProtect) : memory_tenderizer(&s[0], s.size(), dwNewProtect) {}

        ~memory_tenderizer();
    };

    bool find_imported_function_pointer(HMODULE hModule, const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal, void*& ppFunctionAddress);

    void* get_imported_function_pointer(HMODULE hModule, const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal);

    template<typename TFn>
    TFn** get_imported_function_pointer(HMODULE hModule, const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal) {
        return reinterpret_cast<TFn**>(get_imported_function_pointer(hModule, pcszDllName, pcszFunctionName, hintOrOrdinal));
    }

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
        std::function<TFn> m_fnTarget;

    public:
        thunk(std::function<TFn> target)
            : m_pThunk(utils::create_thunk(&detour_static, this, Placeholder))
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
            const volatile auto pThis = reinterpret_cast<thunk<TFn>*>(Placeholder);
            return pThis->detour(args...);
        }
    };

    template<class TElem, class TTraits>
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

    template<typename T>
    T get_env(const wchar_t* pcwzName) {
        static_assert(false);
    }

    template<>
    std::wstring get_env(const wchar_t* pcwzName);

    template<>
    std::string get_env(const wchar_t* pcwzName);

    template<>
    bool get_env(const wchar_t* pcwzName);

    template<typename T>
    T get_env(const char* pcszName) {
        return get_env<T>(unicode::convert<std::wstring>(pcszName).c_str());
    }

    bool is_running_on_linux();

    std::filesystem::path get_module_path(HMODULE hModule);
}

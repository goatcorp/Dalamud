#pragma once

#include <limits>

namespace budget_hooks {
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

        void* resolve_unconditional_jump_target(void* pfn);

        bool find_imported_function_pointer(HMODULE hModule, const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal, void*& ppFunctionAddress);

        void* get_imported_function_pointer(HMODULE hModule, const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal);

        template<typename TFn>
        TFn** get_imported_function_pointer(HMODULE hModule, const char* pcszDllName, const char* pcszFunctionName, uint32_t hintOrOrdinal) {
            return reinterpret_cast<TFn**>(get_imported_function_pointer(hModule, pcszDllName, pcszFunctionName, hintOrOrdinal));
        }

        static constexpr uint64_t ThunkTemplateFunctionThisPointerPlaceholder = 0xCC90CC90CC90CC90ULL;

        std::shared_ptr<void> allocate_executable_heap(size_t len);

        template<typename T>
        std::shared_ptr<void> allocate_executable_heap(std::span<T> data) {
            auto res = allocate_executable_heap(data.size_bytes());
            memcpy(res.get(), data.data(), data.size_bytes());
            return res;
        }

        std::shared_ptr<void> create_thunk(void* pfnFunction, void* pThis);
    }

    namespace hooks {
    }

    namespace fixes {
        void prevent_devicechange_crashes(bool bApply);
        void disable_game_openprocess_access_check(bool bApply);
        void redirect_openprocess_currentprocess_to_duplicatehandle_currentprocess(bool bApply);

        inline void apply_all(bool bApply) {
            prevent_devicechange_crashes(bApply);
            disable_game_openprocess_access_check(bApply);
            redirect_openprocess_currentprocess_to_duplicatehandle_currentprocess(bApply);
        }
    }
}

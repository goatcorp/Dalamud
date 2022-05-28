#pragma once

#include <limits>

#include "utils.h"

namespace hooks {
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

namespace xivfixes {
    void prevent_devicechange_crashes(bool bApply);
    void disable_game_openprocess_access_check(bool bApply);
    void redirect_openprocess(bool bApply);

    inline void apply_all(bool bApply) {
        prevent_devicechange_crashes(bApply);
        disable_game_openprocess_access_check(bApply);
        redirect_openprocess(bApply);
    }
}


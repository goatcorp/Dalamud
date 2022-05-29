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

    template<typename>
    class direct_hook;

    template<typename TReturn, typename ... TArgs>
    class direct_hook<TReturn(TArgs...)> : public base_hook<TReturn(TArgs...)> {
        using TFn = TReturn(TArgs...);
        using Base = base_hook<TFn>;

        TFn* m_pfnMinHookBridge;

    public:
        direct_hook(TFn* pfnFunction)
            : Base(pfnFunction) {
            if (const auto mhStatus = MH_CreateHook(pfnFunction, Base::get_thunk(), reinterpret_cast<void**>(&m_pfnMinHookBridge)); mhStatus != MH_OK)
                throw std::runtime_error(std::format("MH_CreateHook(0x{:X}, ...) failure: {}", static_cast<int>(mhStatus)));

            MH_EnableHook(Base::get_original());
        }

        ~direct_hook() override {
            MH_DisableHook(Base::get_original());
        }

        TReturn call_original(TArgs... args) override {
            return m_pfnMinHookBridge(std::forward<TArgs>(args)...);
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

#pragma once

#include <limits>
#include <map>

#include "utils.h"

namespace hooks {
    class base_untyped_hook {
    public:
        virtual ~base_untyped_hook() = default;
    };

    template<typename>
    class base_hook;

    template<typename TReturn, typename ... TArgs>
    class base_hook<TReturn(TArgs...)> : public base_untyped_hook {
        using TFn = TReturn(TArgs...);

    private:
        TFn* const m_pfnOriginal;
        utils::thunk<TReturn(TArgs...)> m_thunk;

    public:
        base_hook(TFn* pfnOriginal)
            : m_pfnOriginal(pfnOriginal)
            , m_thunk(m_pfnOriginal) {
        }

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
            : import_hook(utils::loaded_module::current_process().get_imported_function_pointer<TFn>(pcszDllName, pcszFunctionName, hintOrOrdinal)) {
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
                throw std::runtime_error(std::format("MH_CreateHook(0x{:X}, ...) failure: {}", reinterpret_cast<size_t>(pfnFunction), static_cast<int>(mhStatus)));

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

    class untyped_import_hook : public base_untyped_hook {
        void** const m_ppfnImportTableItem;
        void* const m_pfnOriginalImport;

    public:
        untyped_import_hook(void** ppfnImportTableItem, void* pThunk)
            : m_pfnOriginalImport(*ppfnImportTableItem)
            , m_ppfnImportTableItem(ppfnImportTableItem) {

            const utils::memory_tenderizer tenderizer(ppfnImportTableItem, sizeof * ppfnImportTableItem, PAGE_READWRITE);
            *ppfnImportTableItem = pThunk;
        }

        ~untyped_import_hook() override {
            MEMORY_BASIC_INFORMATION mbi{};
            VirtualQuery(m_ppfnImportTableItem, &mbi, sizeof mbi);
            if (mbi.State != MEM_COMMIT)
                return;

            const utils::memory_tenderizer tenderizer(m_ppfnImportTableItem, sizeof * m_ppfnImportTableItem, PAGE_READWRITE);
            *m_ppfnImportTableItem = m_pfnOriginalImport;
        }
    };

    class getprocaddress_singleton_import_hook : public std::enable_shared_from_this<getprocaddress_singleton_import_hook> {
        static inline const char* LogTag = "[global_import_hook]";

        decltype(GetProcAddress)* const m_pfnGetProcAddress;
        
        utils::thunk<decltype(GetProcAddress)> m_thunk;
        std::shared_ptr<void> m_getProcAddressHandler;

        void* m_ldrDllNotificationCookie{};
        std::map<HMODULE, std::string> m_dllNameMap;
        std::map<HMODULE, std::map<std::string, void*>> m_targetFns;
        std::map<HMODULE, std::map<std::string, std::map<HMODULE, std::optional<untyped_import_hook>>>> m_hooks;

    public:
        getprocaddress_singleton_import_hook();
        ~getprocaddress_singleton_import_hook();

        std::shared_ptr<void> set_handler(std::wstring dllName, std::string functionName, void* pfnDetour);

        static std::shared_ptr<getprocaddress_singleton_import_hook> get_instance();

    private:
        void initialize();

        FARPROC get_proc_address_handler(HMODULE hModule, LPCSTR lpProcName);

        void hook_module(const utils::loaded_module& mod);
    };

    template<typename>
    class global_import_hook;

    template<typename TReturn, typename ... TArgs>
    class global_import_hook<TReturn(TArgs...)> : public base_untyped_hook {
        using TFn = TReturn(TArgs...);
        utils::thunk<TFn> m_thunk;
        std::shared_ptr<void> m_singleImportHook;

    public:
        global_import_hook(std::wstring dllName, std::string functionName)
            : m_thunk(nullptr) {

            m_singleImportHook = getprocaddress_singleton_import_hook::get_instance()->set_handler(dllName, functionName, m_thunk.get_thunk());
            m_thunk.set_target(reinterpret_cast<TFn*>(m_singleImportHook.get()));
        }

        virtual void set_detour(std::function<TFn> fn) {
            if (!fn)
                m_thunk.set_target(reinterpret_cast<TFn*>(m_singleImportHook.get()));
            else
                m_thunk.set_target(std::move(fn));
        }

        virtual TReturn call_original(TArgs... args) {
            return reinterpret_cast<TFn*>(m_singleImportHook.get())(std::forward<TArgs>(args)...);
        }
    };
}

#include "pch.h"

#include "hooks.h"

#include "ntdll.h"
#include "logging.h"

hooks::getprocaddress_singleton_import_hook::getprocaddress_singleton_import_hook()
    : m_pfnGetProcAddress(GetProcAddress)
    , m_thunk("kernel32!GetProcAddress(Singleton Import Hook)",
        [this](HMODULE hModule, LPCSTR lpProcName) { return get_proc_address_handler(hModule, lpProcName); }) {
}

hooks::getprocaddress_singleton_import_hook::~getprocaddress_singleton_import_hook() {
    LdrUnregisterDllNotification(m_ldrDllNotificationCookie);
}

std::shared_ptr<void> hooks::getprocaddress_singleton_import_hook::set_handler(std::wstring dllName, std::string functionName, void* pfnDetour, std::function<void(void*)> fnOnOriginalAddressAvailable) {
    const auto hModule = GetModuleHandleW(dllName.c_str());
    if (!hModule)
        throw std::out_of_range("Specified DLL is not found.");

    const auto pfn = m_pfnGetProcAddress(hModule, functionName.c_str());
    if (!pfn)
        throw std::out_of_range("Could not find the specified function.");

    fnOnOriginalAddressAvailable(pfn);

    auto& target = m_targetFns[hModule][functionName];
    if (target)
        throw std::runtime_error("Specified function has already been hooked.");

    target = pfnDetour;
    m_dllNameMap[hModule] = unicode::convert<std::string>(dllName);
    for (const auto& mod : utils::loaded_module::all_modules())
        hook_module(mod);

    return { pfn,[pThis = this->shared_from_this(), hModule, functionName](void*) {
        auto& modFns = pThis->m_targetFns[hModule];
        auto& hooks = pThis->m_hooks[hModule];
        modFns.erase(functionName);
        hooks.erase(functionName);
        if (modFns.empty()) {
            pThis->m_targetFns.erase(hModule);
            pThis->m_hooks.erase(hModule);
            pThis->m_dllNameMap.erase(hModule);
        }
    } };
}

std::shared_ptr<hooks::getprocaddress_singleton_import_hook> hooks::getprocaddress_singleton_import_hook::get_instance() {
    static std::weak_ptr<hooks::getprocaddress_singleton_import_hook> s_instance;
    std::shared_ptr<hooks::getprocaddress_singleton_import_hook> res;

    res = s_instance.lock();
    if (res)
        return res;

    static std::mutex m_mtx;
    const auto lock = std::lock_guard(m_mtx);
    res = s_instance.lock();
    if (res)
        return res;

    s_instance = res = std::make_shared<getprocaddress_singleton_import_hook>();
    res->initialize();
    return res;
}

void hooks::getprocaddress_singleton_import_hook::initialize() {
    m_getProcAddressHandler = set_handler(L"kernel32.dll", "GetProcAddress", m_thunk.get_thunk(), [this](void*) {});

    LdrRegisterDllNotification(0, [](ULONG notiReason, const LDR_DLL_NOTIFICATION_DATA* pData, void* context) {
        if (notiReason == LDR_DLL_NOTIFICATION_REASON_LOADED) {
            const auto dllName = unicode::convert<std::string>(pData->Loaded.FullDllName->Buffer);

            utils::loaded_module mod(pData->Loaded.DllBase);
            std::wstring version, description;
            try {
                version = utils::format_file_version(mod.get_file_version());
            } catch (...) {
                version = L"<unknown>";
            }
            
            try {
                description = mod.get_description();
            } catch (...) {
                description = L"<unknown>";
            }
            
            logging::I(R"({} "{}" ("{}" ver {}) has been loaded at 0x{:X} ~ 0x{:X} (0x{:X}); finding import table items to hook.)",
                LogTag, dllName, description, version,
                reinterpret_cast<size_t>(pData->Loaded.DllBase),
                reinterpret_cast<size_t>(pData->Loaded.DllBase) + pData->Loaded.SizeOfImage,
                pData->Loaded.SizeOfImage);
            reinterpret_cast<getprocaddress_singleton_import_hook*>(context)->hook_module(utils::loaded_module(pData->Loaded.DllBase));
        } else if (notiReason == LDR_DLL_NOTIFICATION_REASON_UNLOADED) {
            const auto dllName = unicode::convert<std::string>(pData->Unloaded.FullDllName->Buffer);
            logging::I(R"({} "{}" has been unloaded.)", LogTag, dllName);
        }
    }, this, &m_ldrDllNotificationCookie);
}

FARPROC hooks::getprocaddress_singleton_import_hook::get_proc_address_handler(HMODULE hModule, LPCSTR lpProcName) {
    if (const auto it1 = m_targetFns.find(hModule); it1 != m_targetFns.end()) {
        if (const auto it2 = it1->second.find(lpProcName); it2 != it1->second.end()) {
            logging::I(R"({} Redirecting GetProcAddress("{}", "{}"))", LogTag, m_dllNameMap[hModule], lpProcName);

            return reinterpret_cast<FARPROC>(it2->second);
        }
    }
    return this->m_pfnGetProcAddress(hModule, lpProcName);
}

void hooks::getprocaddress_singleton_import_hook::hook_module(const utils::loaded_module& mod) {
    if (mod.is_current_process())
        return;

    const auto path = unicode::convert<std::string>(mod.path().wstring());

    for (const auto& [hModule, targetFns] : m_targetFns) {
        for (const auto& [targetFn, pfnThunk] : targetFns) {
            const auto& dllName = m_dllNameMap[hModule];
            if (void* pGetProcAddressImport; mod.find_imported_function_pointer(dllName.c_str(), targetFn.c_str(), 0, pGetProcAddressImport)) {
                auto& hook = m_hooks[hModule][targetFn][mod];
                if (!hook) {
                    logging::I("{} Hooking {}!{} imported by {}", LogTag, dllName, targetFn, unicode::convert<std::string>(mod.path().wstring()));

                    hook.emplace(std::format("getprocaddress_singleton_import_hook::hook_module({}!{})", dllName, targetFn), static_cast<void**>(pGetProcAddressImport), pfnThunk);
                }
            }
        }
    }
}

#pragma once

#include "utils.h"

namespace bootconfig {
    enum WaitMessageboxFlags : int {
        None = 0,
        BeforeInitialize = 1 << 0,
        BeforeDalamudEntrypoint = 1 << 1,
    };

    inline WaitMessageboxFlags wait_messagebox() {
        return static_cast<WaitMessageboxFlags>(utils::get_env<int>(L"DALAMUD_WAIT_MESSAGEBOX"));
    }

    enum DotNetOpenProcessHookMode : int {
        ImportHooks = 0,
        DirectHook = 1,
    };

    inline DotNetOpenProcessHookMode dotnet_openprocess_hook_mode() {
        return static_cast<DotNetOpenProcessHookMode>(utils::get_env<int>(L"DALAMUD_DOTNET_OPENPROCESS_HOOKMODE"));
    }

    inline bool is_show_console() {
        return utils::get_env<bool>(L"DALAMUD_SHOW_CONSOLE");
    }

    inline bool is_disable_fallback_console() {
        return utils::get_env<bool>(L"DALAMUD_DISABLE_FALLBACK_CONSOLE");
    }

    inline bool is_wait_debugger() {
        return utils::get_env<bool>(L"DALAMUD_WAIT_DEBUGGER");
    }

    inline bool is_veh_enabled() {
        return utils::get_env<bool>(L"DALAMUD_IS_VEH");
    }

    inline bool is_veh_full() {
        return utils::get_env<bool>(L"DALAMUD_IS_VEH_FULL");
    }

    inline bool gamefix_is_enabled(const wchar_t* name) {
        static const auto list = utils::get_env_list<std::wstring>(L"DALAMUD_GAMEFIX_LIST");
        for (const auto& item : list)
            if (item == name)
                return true;
        return false;
    }

    inline std::vector<std::wstring> gamefix_unhookdll_list() {
        return utils::get_env_list<std::wstring>(L"DALAMUD_UNHOOK_DLLS");
    }
}

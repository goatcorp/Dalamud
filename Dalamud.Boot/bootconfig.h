#pragma once

#include "utils.h"

namespace bootconfig {
    inline bool is_wait_debugger() {
        return utils::get_env<bool>(L"DALAMUD_WAIT_DEBUGGER");
    }

    inline bool is_veh_enabled() {
        return utils::get_env<bool>(L"DALAMUD_IS_VEH");
    }

    inline bool is_veh_full() {
        return utils::get_env<bool>("DALAMUD_IS_VEH_FULL");
    }
}

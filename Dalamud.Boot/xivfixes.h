#pragma once

namespace xivfixes {
    void unhook_dll(bool bApply);
    void prevent_devicechange_crashes(bool bApply);
    void disable_game_openprocess_access_check(bool bApply);
    void redirect_openprocess(bool bApply);
    void backup_userdata_save(bool bApply);
    void clr_failfast_hijack(bool bApply);

    void apply_all(bool bApply);
}

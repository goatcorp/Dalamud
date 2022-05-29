#include "pch.h"

#include "bootconfig.h"
#include "logging.h"
#include "veh.h"
#include "xivfixes.h"

HMODULE g_hModule;
HINSTANCE g_hGameInstance = GetModuleHandleW(nullptr);

DllExport DWORD WINAPI Initialize(LPVOID lpParam, HANDLE hMainThreadContinue) {
    if (bootconfig::is_show_console())
        ConsoleSetup(L"Dalamud Boot");

    if (const auto logFilePath = utils::get_env<std::wstring>("DALAMUD_BOOT_LOGFILE"); logFilePath.empty())
        logging::print<logging::I>("No log file path given; not logging to file.");
    else {
        try {
            logging::start_file_logging(logFilePath);
            logging::print<logging::I>(L"Logging to file: {}", logFilePath);
        } catch (const std::exception& e) {
            logging::print<logging::E>(L"Couldn't open log file: {}", logFilePath);
            logging::print<logging::E>("Error: {} / {}", errno, e.what());
        }
    }
    
    logging::print<logging::I>("Dalamud.Boot Injectable, (c) 2021 XIVLauncher Contributors");
    logging::print<logging::I>("Built at: " __DATE__ "@" __TIME__);

    if (bootconfig::is_wait_messagebox())
        MessageBoxW(nullptr, L"Press OK to continue", L"Dalamud Boot", MB_OK);

    xivfixes::apply_all(true);

    if (bootconfig::is_wait_debugger()) {
        logging::print<logging::I>("Waiting for debugger to attach...");
        while (!IsDebuggerPresent())
            Sleep(100);
        logging::print<logging::I>("Debugger attached.");
    }

    const auto fs_module_path = utils::get_module_path(g_hModule);
    const auto runtimeconfig_path = std::filesystem::path(fs_module_path).replace_filename(L"Dalamud.runtimeconfig.json").wstring();
    const auto module_path = std::filesystem::path(fs_module_path).replace_filename(L"Dalamud.dll").wstring();

    // ============================== CLR ========================================= //

    void* entrypoint_vfn;
    int result = InitializeClrAndGetEntryPoint(
        g_hModule,
        runtimeconfig_path,
        module_path,
        L"Dalamud.EntryPoint, Dalamud",
        L"Initialize",
        L"Dalamud.EntryPoint+InitDelegate, Dalamud",
        &entrypoint_vfn);

    if (result != 0)
        return result;

    using custom_component_entry_point_fn = void (CORECLR_DELEGATE_CALLTYPE*)(LPVOID, HANDLE);
    const auto entrypoint_fn = reinterpret_cast<custom_component_entry_point_fn>(entrypoint_vfn);

    // ============================== VEH ======================================== //

    logging::print<logging::I>("Initializing VEH...");
    if (utils::is_running_on_linux()) {
        logging::print<logging::I>("=> VEH was disabled, running on linux");
    } else if (bootconfig::is_veh_enabled()) {
        if (veh::add_handler(bootconfig::is_veh_full()))
            logging::print<logging::I>("=> Done!");
        else
            logging::print<logging::I>("=> Failed!");
    } else {
        logging::print<logging::I>("VEH was disabled manually");
    }

    // ============================== Dalamud ==================================== //

    logging::print<logging::I>("Initializing Dalamud...");
    entrypoint_fn(lpParam, hMainThreadContinue);
    logging::print<logging::I>("Done!");

    return 0;
}

BOOL APIENTRY DllMain(const HMODULE hModule, const DWORD dwReason, LPVOID lpReserved) {
    DisableThreadLibraryCalls(hModule);

    switch (dwReason) {
        case DLL_PROCESS_ATTACH:
            g_hModule = hModule;
            if (const auto mhStatus = MH_Initialize(); MH_OK != mhStatus) {
                logging::print<logging::E>("Failed to initialize MinHook (status={})", static_cast<int>(mhStatus));
                return FALSE;
            }

            logging::update_dll_load_status(true);
            break;

        case DLL_PROCESS_DETACH:
            logging::update_dll_load_status(false);

            xivfixes::apply_all(false);

            if (const auto mhStatus = MH_Uninitialize(); MH_OK != mhStatus) {
                logging::print<logging::E>("Failed to uninitialize MinHook (status={})", static_cast<int>(mhStatus));
                __fastfail(logging::MinHookUnload);
            }

            veh::remove_handler();
            //logging::log_file.close();
            break;
    }
    return TRUE;
}

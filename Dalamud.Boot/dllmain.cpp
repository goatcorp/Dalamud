#include "pch.h"

#include "bootconfig.h"
#include "veh.h"
#include "xivfixes.h"

HMODULE g_hModule;
HINSTANCE g_hGameInstance = GetModuleHandleW(nullptr);

DllExport DWORD WINAPI Initialize(LPVOID lpParam, HANDLE hMainThreadContinue) {
#ifndef NDEBUG
    ConsoleSetup(L"Dalamud Boot");
#endif
    try {
        xivfixes::apply_all(true);
    } catch (const std::exception& e) {
        std::cerr << std::format("Failed to do general fixups. Some things might not work.\nError: {}\n", e.what());
    }

    std::cerr << "Dalamud.Boot Injectable, (c) 2021 XIVLauncher Contributors\nBuilt at: " __DATE__ "@" __TIME__ "\n\n";

    if (bootconfig::is_wait_debugger()) {
        std::cerr << "Waiting for debugger to attach...\n";
        while (!IsDebuggerPresent())
            Sleep(100);
        std::cerr << "Debugger attached.\n";
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

    std::cerr << "Initializing VEH...\n";
    if (utils::is_running_on_linux()) {
        std::cerr << "=> VEH was disabled, running on linux\n";
    } else if (bootconfig::is_veh_enabled()) {
        if (veh::add_handler(bootconfig::is_veh_full()))
            std::cerr << "=> Done!\n";
        else
            std::cerr << "=> Failed!\n";
    } else {
        std::cerr << "VEH was disabled manually\n";
    }

    // ============================== Dalamud ==================================== //

    std::cerr << "Initializing Dalamud... ";
    entrypoint_fn(lpParam, hMainThreadContinue);
    std::cerr << "Done!\n";

#ifndef NDEBUG
    fclose(stdin);
    fclose(stdout);
    fclose(stderr);
    FreeConsole();
#endif

    return 0;
}

BOOL APIENTRY DllMain(const HMODULE hModule, const DWORD dwReason, LPVOID lpReserved) {
    DisableThreadLibraryCalls(hModule);

    switch (dwReason) {
        case DLL_PROCESS_ATTACH:
            g_hModule = hModule;
            break;
        case DLL_PROCESS_DETACH:
            xivfixes::apply_all(false);
            veh::remove_handler();
            break;
    }
    return TRUE;
}

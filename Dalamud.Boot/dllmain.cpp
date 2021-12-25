#include "pch.h"

#include "veh.h"

HMODULE g_hModule;

bool is_running_on_linux()
{
    size_t required_size;
    getenv_s(&required_size, nullptr, 0, "XL_WINEONLINUX");
    if (required_size > 0)
    {
        if (char* is_wine_on_linux = static_cast<char*>(malloc(required_size * sizeof(char))))
        {
            getenv_s(&required_size, is_wine_on_linux, required_size, "XL_WINEONLINUX");
            auto result = _stricmp(is_wine_on_linux, "true");
            free(is_wine_on_linux);
            if (result == 0)
                return true;
        }
    }

    HMODULE hntdll = GetModuleHandleW(L"ntdll.dll");
    if (!hntdll) // not running on NT
        return true;

    FARPROC pwine_get_version = GetProcAddress(hntdll, "wine_get_version");
    FARPROC pwine_get_host_version = GetProcAddress(hntdll, "wine_get_host_version");

    return pwine_get_version != nullptr || pwine_get_host_version != nullptr;
}

bool is_veh_enabled()
{
    size_t required_size;
    getenv_s(&required_size, nullptr, 0, "DALAMUD_IS_STAGING");
    if (required_size > 0)
    {
        if (char* is_no_veh = static_cast<char*>(malloc(required_size * sizeof(char))))
        {
            getenv_s(&required_size, is_no_veh, required_size, "DALAMUD_IS_STAGING");
            auto result = _stricmp(is_no_veh, "true");
            free(is_no_veh);
            if (result == 0)
                return true;
        }
    }

    return false;
}

DllExport DWORD WINAPI Initialize(LPVOID lpParam)
{
    #ifndef NDEBUG
    ConsoleSetup(L"Dalamud Boot");
    #endif

    printf("Dalamud.Boot Injectable, (c) 2021 XIVLauncher Contributors\nBuilt at: %s@%s\n\n", __DATE__, __TIME__);

    wchar_t _module_path[MAX_PATH];
    GetModuleFileNameW(g_hModule, _module_path, sizeof _module_path / 2);
    std::filesystem::path fs_module_path(_module_path);

    std::wstring runtimeconfig_path = _wcsdup(fs_module_path.replace_filename(L"Dalamud.runtimeconfig.json").c_str());
    std::wstring module_path = _wcsdup(fs_module_path.replace_filename(L"Dalamud.dll").c_str());

    // ============================== CLR ========================================= //

    void* entrypoint_vfn;
    int result = InitializeClrAndGetEntryPoint(
        runtimeconfig_path,
        module_path,
        L"Dalamud.EntryPoint, Dalamud",
        L"Initialize",
        L"Dalamud.EntryPoint+InitDelegate, Dalamud",
        &entrypoint_vfn);

    if (result != 0)
        return result;

    typedef void (CORECLR_DELEGATE_CALLTYPE* custom_component_entry_point_fn)(LPVOID);
    custom_component_entry_point_fn entrypoint_fn = reinterpret_cast<custom_component_entry_point_fn>(entrypoint_vfn);

    // ============================== VEH ======================================== //

    printf("Initializing VEH... ");
    if(is_running_on_linux())
    {
        printf("VEH was disabled, running on linux\n");
    }
    else if (is_veh_enabled())
    {
        if (veh::add_handler())
            printf("Done!\n");
        else printf("Failed!\n");
    }
    else
    {
        printf("VEH was disabled manually\n");
    }

    // ============================== Dalamud ==================================== //

    printf("Initializing Dalamud... ");
    entrypoint_fn(lpParam);
    printf("Done!\n");

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

    switch (dwReason)
    {
        case DLL_PROCESS_ATTACH:
            g_hModule = hModule;
            break;
        case DLL_PROCESS_DETACH:
            veh::remove_handler();
            break;
    }
    return TRUE;
}

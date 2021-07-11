#define WIN32_LEAN_AND_MEAN
#define DllExport extern "C" __declspec(dllexport)

#include <filesystem>
#include <Windows.h>
#include "..\lib\CoreCLR\CoreCLR.h"
#include "..\lib\CoreCLR\boot.h"

HMODULE g_hModule;

DllExport DWORD WINAPI Initialize(LPVOID lpParam)
{
    #if defined(_DEBUG)
    ConsoleSetup(L"Dalamud Boot");
    #endif

    wchar_t _module_path[MAX_PATH];
    GetModuleFileNameW(g_hModule, _module_path, sizeof _module_path / 2);
    std::filesystem::path fs_module_path(_module_path);

    std::wstring runtimeconfig_path = _wcsdup(fs_module_path.replace_filename(L"Dalamud.runtimeconfig.json").c_str());
    std::wstring module_path = _wcsdup(fs_module_path.replace_filename(L"Dalamud.dll").c_str());

    // =========================================================================== //

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

    printf("Initializing Dalamud... ");
    entrypoint_fn(lpParam);
    printf("Done!\n");

    // =========================================================================== //
    
    #if defined(_DEBUG)
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
            break;
    }
    return TRUE;
}

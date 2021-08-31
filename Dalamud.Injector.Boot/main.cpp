#define WIN32_LEAN_AND_MEAN

#include <filesystem>
#include <Windows.h>
#include "..\lib\CoreCLR\CoreCLR.h"
#include "..\lib\CoreCLR\boot.h"

int wmain(int argc, char** argv)
{
    #if !defined(NDEBUG)
    ConsoleSetup(L"Dalamud.Injector");
    #endif

    printf("Dalamud.Injector, (c) 2021 XIVLauncher Contributors\nBuilt at: %s@%s\n\n", __DATE__, __TIME__);

    wchar_t _module_path[MAX_PATH];
    GetModuleFileNameW(NULL, _module_path, sizeof _module_path / 2);
    std::filesystem::path fs_module_path(_module_path);

    std::wstring runtimeconfig_path = _wcsdup(fs_module_path.replace_filename(L"Dalamud.Injector.runtimeconfig.json").c_str());
    std::wstring module_path = _wcsdup(fs_module_path.replace_filename(L"Dalamud.Injector.dll").c_str());

    // =========================================================================== //

    void* entrypoint_vfn;
    int result = InitializeClrAndGetEntryPoint(
        runtimeconfig_path,
        module_path,
        L"Dalamud.Injector.EntryPoint, Dalamud.Injector",
        L"Main",
        L"Dalamud.Injector.EntryPoint+MainDelegate, Dalamud.Injector",
        &entrypoint_vfn);

    if (result != 0)
        return result;

    typedef void (CORECLR_DELEGATE_CALLTYPE* custom_component_entry_point_fn)(int, char**);
    custom_component_entry_point_fn entrypoint_fn = reinterpret_cast<custom_component_entry_point_fn>(entrypoint_vfn);

    printf("Running Dalamud Injector... ");
    entrypoint_fn(argc, argv);
    printf("Done!\n");

    // =========================================================================== //

    #if !defined(NDEBUG)
    FreeConsole();
    #endif

    return 0;
}

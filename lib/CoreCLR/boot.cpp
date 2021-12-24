#define WIN32_LEAN_AND_MEAN

#include <cstdio>
#include <filesystem>
#include <Windows.h>
#include <Shlobj.h>
#include "CoreCLR.h"

FILE* g_CmdStream;
void ConsoleSetup(const std::wstring console_name)
{
    if (!AllocConsole())
        return;

    SetConsoleTitleW(console_name.c_str());
    freopen_s(&g_CmdStream, "CONOUT$", "w", stdout);
    freopen_s(&g_CmdStream, "CONOUT$", "w", stderr);
    freopen_s(&g_CmdStream, "CONIN$", "r", stdin);
}

void ConsoleTeardown()
{
    FreeConsole();
}

std::optional<CoreCLR> g_clr;

int InitializeClrAndGetEntryPoint(
    std::wstring runtimeconfig_path,
    std::wstring module_path,
    std::wstring entrypoint_assembly_name,
    std::wstring entrypoint_method_name,
    std::wstring entrypoint_delegate_type_name,
    void** entrypoint_fn)
{
    g_clr = CoreCLR();

    int result;
    SetEnvironmentVariable(L"DOTNET_MULTILEVEL_LOOKUP", L"0");
    //SetEnvironmentVariable(L"COMPlus_legacyCorruptedStateExceptionsPolicy", L"1");
    SetEnvironmentVariable(L"DOTNET_legacyCorruptedStateExceptionsPolicy", L"1");
    SetEnvironmentVariable(L"COMPLUS_ForceENC", L"1");

    wchar_t* dotnet_path;
    wchar_t* _appdata;

    std::wstring buffer;
    buffer.resize(0);
    result = GetEnvironmentVariableW(L"DALAMUD_RUNTIME", &buffer[0], 0);

    if (result)
    {
        buffer.resize(result); // The first pass returns the required length
        result = GetEnvironmentVariableW(L"DALAMUD_RUNTIME", &buffer[0], result);
        dotnet_path = _wcsdup(buffer.c_str());
    }
    else
    {
        result = SHGetKnownFolderPath(FOLDERID_RoamingAppData, KF_FLAG_DEFAULT, nullptr, &_appdata);

        if (result != 0)
        {
            printf("Error: Unable to get RoamingAppData path (err=%d)\n", result);
            return result;
        }

        std::filesystem::path fs_app_data(_appdata);
        dotnet_path = _wcsdup(fs_app_data.append("XIVLauncher").append("runtime").c_str());
    }

    // =========================================================================== //

    wprintf(L"with dotnet_path: %s\n", dotnet_path);
    wprintf(L"with config_path: %s\n", runtimeconfig_path.c_str());
    wprintf(L"with module_path: %s\n", module_path.c_str());

    if (!std::filesystem::exists(dotnet_path))
    {
        printf("Error: Unable to find .NET runtime path\n");
        return 1;
    }

    get_hostfxr_parameters init_parameters
    {
        sizeof(get_hostfxr_parameters),
        nullptr,
        dotnet_path,
    };

    printf("Loading hostfxr... ");
    if ((result = g_clr->load_hostfxr(&init_parameters)) != 0)
    {
        printf("\nError: Failed to load the `hostfxr` library (err=%d)\n", result);
        return result;
    }
    printf("Done!\n");

    // =========================================================================== //

    hostfxr_initialize_parameters runtime_parameters
    {
        sizeof(hostfxr_initialize_parameters),
        module_path.c_str(),
        dotnet_path,
    };

    printf("Loading coreclr... ");;
    if ((result = g_clr->load_runtime(runtimeconfig_path, &runtime_parameters)) != 0)
    {
        printf("\nError: Failed to load coreclr (err=%d)\n", result);
        return result;
    }
    printf("Done!\n");

    // =========================================================================== //

    printf("Loading module... ");
    if ((result = g_clr->load_assembly_and_get_function_pointer(
        module_path.c_str(),
        entrypoint_assembly_name.c_str(),
        entrypoint_method_name.c_str(),
        entrypoint_delegate_type_name.c_str(),
        nullptr, entrypoint_fn)) != 0)
    {
        printf("\nError: Failed to load module (err=%d)\n", result);
        return result;
    }
    printf("Done!\n");

    // =========================================================================== //

    return 0;
}

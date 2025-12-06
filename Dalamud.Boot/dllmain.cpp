#include "pch.h"

#include <d3d11.h>
#include <dxgi1_3.h>

#include "DalamudStartInfo.h"
#include "hooks.h"
#include "logging.h"
#include "utils.h"
#include "veh.h"
#include "xivfixes.h"
#include "resource.h"

HMODULE g_hModule;
HINSTANCE g_hGameInstance = GetModuleHandleW(nullptr);

static void CheckMsvcrtVersion() {
    // Commit introducing inline mutex ctor: tagged vs-2022-17.14 (2024-06-18)
    // - https://github.com/microsoft/STL/commit/22a88260db4d754bbc067e2002430144d6ec5391
    // MSVC Redist versions:
    // - https://github.com/abbodi1406/vcredist/blob/master/source_links/README.md
    // - 14.40.33810.0 dsig 2024-04-28
    // - 14.40.33816.0 dsig 2024-09-11

    constexpr WORD RequiredMsvcrtVersionComponents[] = {14, 40, 33816, 0};
    constexpr auto RequiredMsvcrtVersion = 0ULL
        | (static_cast<uint64_t>(RequiredMsvcrtVersionComponents[0]) << 48)
        | (static_cast<uint64_t>(RequiredMsvcrtVersionComponents[1]) << 32)
        | (static_cast<uint64_t>(RequiredMsvcrtVersionComponents[2]) << 16)
        | (static_cast<uint64_t>(RequiredMsvcrtVersionComponents[3]) << 0);

    constexpr const wchar_t* RuntimeDllNames[] = {
#ifdef _DEBUG
        L"msvcp140d.dll",
        L"vcruntime140d.dll",
        L"vcruntime140_1d.dll",
#else
        L"msvcp140.dll",
        L"vcruntime140.dll",
        L"vcruntime140_1.dll",
#endif
    };

    uint64_t lowestVersion = 0;
    for (const auto& runtimeDllName : RuntimeDllNames) {
        const utils::loaded_module mod(GetModuleHandleW(runtimeDllName));
        if (!mod) {
            logging::E("MSVCRT DLL not found: {}", runtimeDllName);
            continue;
        }

        const auto path = mod.path()
            .transform([](const auto& p) { return p.wstring(); })
            .value_or(runtimeDllName);

        if (const auto versionResult = mod.get_file_version()) {
            const auto& versionFull = versionResult->get();
            logging::I("MSVCRT DLL {} has version {}.", path, utils::format_file_version(versionFull));

            const auto version = 0ULL |
                (static_cast<uint64_t>(versionFull.dwFileVersionMS) << 32) |
                (static_cast<uint64_t>(versionFull.dwFileVersionLS) << 0);

            if (version < RequiredMsvcrtVersion && (lowestVersion == 0 || lowestVersion > version))
                lowestVersion = version;
        } else {
            logging::E("Failed to detect MSVCRT DLL version for {}: {}", path, versionResult.error().describe());
        }
    }

    if (!lowestVersion)
        return;

    enum IdTaskDialogAction {
        IdTaskDialogActionOpenDownload = 101,
        IdTaskDialogActionIgnore,
    };

    const TASKDIALOG_BUTTON buttons[]{
        {IdTaskDialogActionOpenDownload, MAKEINTRESOURCEW(IDS_MSVCRT_ACTION_OPENDOWNLOAD)},
        {IdTaskDialogActionIgnore, MAKEINTRESOURCEW(IDS_MSVCRT_ACTION_IGNORE)},
    };

    const WORD lowestVersionComponents[]{
        static_cast<WORD>(lowestVersion >> 48),
        static_cast<WORD>(lowestVersion >> 32),
        static_cast<WORD>(lowestVersion >> 16),
        static_cast<WORD>(lowestVersion >> 0),
    };

    const auto dialogContent = std::vformat(
        utils::get_string_resource(IDS_MSVCRT_DIALOG_CONTENT),
        std::make_wformat_args(
            lowestVersionComponents[0],
            lowestVersionComponents[1],
            lowestVersionComponents[2],
            lowestVersionComponents[3]));

    const TASKDIALOGCONFIG config{
        .cbSize = sizeof config,
        .hInstance = g_hModule,
        .dwFlags = TDF_CAN_BE_MINIMIZED | TDF_ALLOW_DIALOG_CANCELLATION | TDF_USE_COMMAND_LINKS,
        .pszWindowTitle = MAKEINTRESOURCEW(IDS_APPNAME),
        .pszMainIcon = MAKEINTRESOURCEW(IDI_ICON1),
        .pszMainInstruction = MAKEINTRESOURCEW(IDS_MSVCRT_DIALOG_MAININSTRUCTION),
        .pszContent = dialogContent.c_str(),
        .cButtons = _countof(buttons),
        .pButtons = buttons,
        .nDefaultButton = IdTaskDialogActionOpenDownload,
    };

    int buttonPressed;
    if (utils::scoped_dpi_awareness_context ctx;
        FAILED(TaskDialogIndirect(&config, &buttonPressed, nullptr, nullptr)))
        buttonPressed = IdTaskDialogActionOpenDownload;

    switch (buttonPressed) {
        case IdTaskDialogActionOpenDownload:
            ShellExecuteW(
                nullptr,
                L"open",
                utils::get_string_resource(IDS_MSVCRT_DOWNLOADURL).c_str(),
                nullptr,
                nullptr,
                SW_SHOW);
            ExitProcess(0);
            break;
    }
}

HRESULT WINAPI InitializeImpl(LPVOID lpParam, HANDLE hMainThreadContinue) {
    g_startInfo.from_envvars();

    std::string jsonParseError;
    try {
        from_json(nlohmann::json::parse(std::string_view(static_cast<char*>(lpParam))), g_startInfo);
    } catch (const std::exception& e) {
        jsonParseError = e.what();
    }

    if (g_startInfo.BootShowConsole)
        ConsoleSetup(utils::get_string_resource(IDS_APPNAME).c_str());

    logging::update_dll_load_status(true);

    const auto logFilePath = unicode::convert<std::wstring>(g_startInfo.BootLogPath);

    auto attemptFallbackLog = false;
    if (logFilePath.empty()) {
        attemptFallbackLog = true;

        logging::I("No log file path given; not logging to file.");
    } else {
        try {
            logging::start_file_logging(logFilePath, !g_startInfo.BootShowConsole);
            logging::I("Logging to file: {}", logFilePath);

        } catch (const std::exception& e) {
            attemptFallbackLog = true;

            logging::E("Couldn't open log file: {}", logFilePath);
            logging::E("Error: {} / {}", errno, e.what());
        }
    }

    if (!jsonParseError.empty())
        logging::E("Couldn't parse input JSON: {}", jsonParseError);

    if (attemptFallbackLog) {
        std::wstring logFilePath(PATHCCH_MAX_CCH + 1, L'\0');
        logFilePath.resize(GetTempPathW(static_cast<DWORD>(logFilePath.size()), &logFilePath[0]));
        if (logFilePath.empty()) {
            logFilePath.resize(PATHCCH_MAX_CCH + 1);
            logFilePath.resize(GetCurrentDirectoryW(static_cast<DWORD>(logFilePath.size()), &logFilePath[0]));
        }
        if (!logFilePath.empty() && logFilePath.back() != '/' && logFilePath.back() != '\\')
            logFilePath += L"\\";
        SYSTEMTIME st;
        GetLocalTime(&st);
        logFilePath += std::format(L"Dalamud.Boot.{:04}{:02}{:02}.{:02}{:02}{:02}.{:03}.{}.log", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, GetCurrentProcessId());

        try {
            logging::start_file_logging(logFilePath, !g_startInfo.BootShowConsole);
            logging::I("Logging to fallback log file: {}", logFilePath);

        } catch (const std::exception& e) {
            if (!g_startInfo.BootShowConsole && !g_startInfo.BootDisableFallbackConsole)
                ConsoleSetup(L"Dalamud Boot - Fallback Console");

            logging::E("Couldn't open fallback log file: {}", logFilePath);
            logging::E("Error: {} / {}", errno, e.what());
        }
    }

    auto minHookLoaded = false;
    if (const auto mhStatus = MH_Initialize(); mhStatus == MH_OK) {
        logging::I("MinHook initialized.");
        minHookLoaded = true;
    } else if (mhStatus == MH_ERROR_ALREADY_INITIALIZED) {
        logging::I("MinHook already initialized.");
        minHookLoaded = true;
    } else {
        logging::E("Failed to initialize MinHook (status={}({}))", MH_StatusToString(mhStatus), static_cast<int>(mhStatus));
    }

    logging::I("Dalamud.Boot Injectable, (c) 2021 XIVLauncher Contributors");
    logging::I("Built at: " __DATE__ "@" __TIME__);

    if ((g_startInfo.BootWaitMessageBox & DalamudStartInfo::WaitMessageboxFlags::BeforeInitialize) != DalamudStartInfo::WaitMessageboxFlags::None)
        MessageBoxW(nullptr, L"Press OK to continue (BeforeInitialize)", L"Dalamud Boot", MB_OK);

    CheckMsvcrtVersion();

    if (g_startInfo.BootDebugDirectX) {
        logging::I("Enabling DirectX Debugging.");

        const auto hD3D11 = GetModuleHandleW(L"d3d11.dll");
        const auto hDXGI = GetModuleHandleW(L"dxgi.dll");
        const auto pfnD3D11CreateDevice = static_cast<decltype(&D3D11CreateDevice)>(
            hD3D11 ? static_cast<void*>(GetProcAddress(hD3D11, "D3D11CreateDevice")) : nullptr);
        if (pfnD3D11CreateDevice) {
            static hooks::direct_hook<decltype(D3D11CreateDevice)> s_hookD3D11CreateDevice(
                "d3d11.dll!D3D11CreateDevice",
                pfnD3D11CreateDevice);
            s_hookD3D11CreateDevice.set_detour([](
                IDXGIAdapter* pAdapter,
                D3D_DRIVER_TYPE DriverType,
                HMODULE Software,
                UINT Flags,
                const D3D_FEATURE_LEVEL* pFeatureLevels,
                UINT FeatureLevels,
                UINT SDKVersion,
                ID3D11Device** ppDevice,
                D3D_FEATURE_LEVEL* pFeatureLevel,
                ID3D11DeviceContext** ppImmediateContext
            ) -> HRESULT {
                return s_hookD3D11CreateDevice.call_original(
                    pAdapter,
                    DriverType,
                    Software,
                    (Flags & ~D3D11_CREATE_DEVICE_PREVENT_ALTERING_LAYER_SETTINGS_FROM_REGISTRY) | D3D11_CREATE_DEVICE_DEBUG,
                    pFeatureLevels,
                    FeatureLevels,
                    SDKVersion,
                    ppDevice,
                    pFeatureLevel,
                    ppImmediateContext);
            });
        } else {
            logging::W("Could not find d3d11!D3D11CreateDevice.");
        }

        const auto pfnCreateDXGIFactory = static_cast<decltype(&CreateDXGIFactory)>(
            hDXGI ? static_cast<void*>(GetProcAddress(hDXGI, "CreateDXGIFactory")) : nullptr);
        const auto pfnCreateDXGIFactory1 = static_cast<decltype(&CreateDXGIFactory1)>(
            hDXGI ? static_cast<void*>(GetProcAddress(hDXGI, "CreateDXGIFactory1")) : nullptr);
        static const auto pfnCreateDXGIFactory2 = static_cast<decltype(&CreateDXGIFactory2)>(
            hDXGI ? static_cast<void*>(GetProcAddress(hDXGI, "CreateDXGIFactory2")) : nullptr);
        if (pfnCreateDXGIFactory2) {
            static hooks::direct_hook<decltype(CreateDXGIFactory)> s_hookCreateDXGIFactory(
                "dxgi.dll!CreateDXGIFactory",
                pfnCreateDXGIFactory);
            static hooks::direct_hook<decltype(CreateDXGIFactory1)> s_hookCreateDXGIFactory1(
                "dxgi.dll!CreateDXGIFactory1",
                pfnCreateDXGIFactory1);
            s_hookCreateDXGIFactory.set_detour([](REFIID riid, _COM_Outptr_ void **ppFactory) -> HRESULT {
                return pfnCreateDXGIFactory2(DXGI_CREATE_FACTORY_DEBUG, riid, ppFactory);
            });
            s_hookCreateDXGIFactory1.set_detour([](REFIID riid, _COM_Outptr_ void **ppFactory) -> HRESULT {
                return pfnCreateDXGIFactory2(DXGI_CREATE_FACTORY_DEBUG, riid, ppFactory);
            });
        } else {
            logging::W("Could not find dxgi!CreateDXGIFactory2.");
        }
    }

    if (minHookLoaded) {
        logging::I("Applying fixes...");
        std::thread([] { xivfixes::apply_all(true); }).join();
        logging::I("Fixes OK");
    } else {
        logging::W("Skipping fixes, as MinHook has failed to load.");
    }

    if (g_startInfo.BootWaitDebugger) {
        logging::I("Waiting for debugger to attach...");
        while (!IsDebuggerPresent())
            Sleep(100);
        logging::I("Debugger attached.");
        __debugbreak();
    }

    const auto fs_module_path = utils::loaded_module(g_hModule).path();
    if (!fs_module_path)
        return fs_module_path.error();
    const auto runtimeconfig_path = std::filesystem::path(*fs_module_path).replace_filename(L"Dalamud.runtimeconfig.json").wstring();
    const auto module_path = std::filesystem::path(*fs_module_path).replace_filename(L"Dalamud.dll").wstring();

    // ============================== CLR ========================================= //

    logging::I("Calling InitializeClrAndGetEntryPoint");

    void* entrypoint_vfn;
    const auto result = InitializeClrAndGetEntryPoint(
        g_hModule,
        g_startInfo.BootEnableEtw,
        runtimeconfig_path,
        module_path,
        L"Dalamud.EntryPoint, Dalamud",
        L"Initialize",
        L"Dalamud.EntryPoint+InitDelegate, Dalamud",
        &entrypoint_vfn);

    if (FAILED(result))
        return result;

    using custom_component_entry_point_fn = void (CORECLR_DELEGATE_CALLTYPE*)(LPVOID, HANDLE);
    const auto entrypoint_fn = reinterpret_cast<custom_component_entry_point_fn>(entrypoint_vfn);

    // ============================== VEH ======================================== //

    logging::I("Initializing VEH...");
    if (g_startInfo.UnhandledException == DalamudStartInfo::UnhandledExceptionHandlingMode::None) {
        logging::W("=> Exception handlers are disabled from DalamudStartInfo.");
    } else if (g_startInfo.BootVehEnabled) {
        if (veh::add_handler(g_startInfo.BootVehFull, g_startInfo.WorkingDirectory))
            logging::I("=> Done!");
        else
            logging::I("=> Failed!");
    } else {
        logging::I("VEH was disabled manually");
    }

    // ============================== CLR Reporting =================================== //

    // This is pretty horrible - CLR just doesn't provide a way for us to handle these events, and the API for it
    // was pushed back to .NET 11, so we have to hook ReportEventW and catch them ourselves for now.
    // Ideally all of this will go away once they get to it.
    static std::shared_ptr<hooks::global_import_hook<decltype(ReportEventW)>> s_report_event_hook;
    s_report_event_hook = std::make_shared<hooks::global_import_hook<decltype(ReportEventW)>>(
        "advapi32.dll!ReportEventW (global import, hook_clr_report_event)", L"advapi32.dll", "ReportEventW");
    s_report_event_hook->set_detour([hook = s_report_event_hook.get()](
        HANDLE hEventLog,
        WORD wType,
        WORD wCategory,
        DWORD dwEventID,
        PSID lpUserSid,
        WORD wNumStrings,
        DWORD dwDataSize,
        LPCWSTR* lpStrings,
        LPVOID lpRawData)-> BOOL {

        // Check for CLR Error Event IDs
        // https://github.com/dotnet/runtime/blob/v10.0.0/src/coreclr/vm/eventreporter.cpp#L370
        if (dwEventID != 1026 && // ERT_UnhandledException: The process was terminated due to an unhandled exception
            dwEventID != 1025 && // ERT_ManagedFailFast: The application requested process termination through System.Environment.FailFast
            dwEventID != 1023 && // ERT_UnmanagedFailFast: The process was terminated due to an internal error in the .NET Runtime
            dwEventID != 1027 && // ERT_StackOverflow: The process was terminated due to a stack overflow
            dwEventID != 1028)   // ERT_CodeContractFailed: The application encountered a bug.  A managed code contract (precondition, postcondition, object invariant, or assert) failed
        {
            return hook->call_original(hEventLog, wType, wCategory, dwEventID, lpUserSid, wNumStrings, dwDataSize, lpStrings, lpRawData);
        }

        if (wNumStrings == 0 || lpStrings == nullptr) {
            logging::W("ReportEventW called with no strings.");
            return hook->call_original(hEventLog, wType, wCategory, dwEventID, lpUserSid, wNumStrings, dwDataSize, lpStrings, lpRawData);
        }

        // In most cases, DalamudCrashHandler will kill us now, so call original here to make sure we still write to the event log.
        const BOOL original_ret = hook->call_original(hEventLog, wType, wCategory, dwEventID, lpUserSid, wNumStrings, dwDataSize, lpStrings, lpRawData);

        const std::wstring error_details(lpStrings[0]);
        veh::raise_external_event(error_details);

        return original_ret;
    });
    logging::I("ReportEventW hook installed.");

    // ============================== Dalamud ==================================== //

    if (static_cast<int>(g_startInfo.BootWaitMessageBox) & static_cast<int>(DalamudStartInfo::WaitMessageboxFlags::BeforeDalamudEntrypoint))
        MessageBoxW(nullptr, L"Press OK to continue (BeforeDalamudEntrypoint)", L"Dalamud Boot", MB_OK);

    // We don't need to do this anymore, Dalamud now loads without needing the window to be there. Speed!
    // utils::wait_for_game_window();

    logging::I("Initializing Dalamud...");
    entrypoint_fn(lpParam, hMainThreadContinue);
    logging::I("Done!");

    return S_OK;
}

extern "C" DWORD WINAPI Initialize(LPVOID lpParam) {
    return InitializeImpl(lpParam, CreateEvent(nullptr, TRUE, FALSE, nullptr));
}

BOOL APIENTRY DllMain(const HMODULE hModule, const DWORD dwReason, LPVOID lpReserved) {
    DisableThreadLibraryCalls(hModule);

    switch (dwReason) {
        case DLL_PROCESS_ATTACH:
            g_hModule = hModule;
            break;

        case DLL_PROCESS_DETACH:
            // do not show debug message boxes on abort() here
            _set_abort_behavior(0, _WRITE_ABORT_MSG);

            // process is terminating; don't bother cleaning up
            if (lpReserved)
                return TRUE;

            logging::update_dll_load_status(false);

            xivfixes::apply_all(false);

            MH_DisableHook(MH_ALL_HOOKS);
            if (const auto mhStatus = MH_Uninitialize(); MH_OK != mhStatus && MH_ERROR_NOT_INITIALIZED != mhStatus) {
                logging::E("Failed to uninitialize MinHook (status={})", static_cast<int>(mhStatus));
                __fastfail(logging::MinHookUnload);
            }

            veh::remove_handler();
            //logging::log_file.close();
            break;
    }
    return TRUE;
}

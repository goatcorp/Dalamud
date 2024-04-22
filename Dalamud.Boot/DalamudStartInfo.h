#pragma once

struct DalamudStartInfo {
    enum class WaitMessageboxFlags : int {
        None = 0,
        BeforeInitialize = 1 << 0,
        BeforeDalamudEntrypoint = 1 << 1,
        BeforeDalamudConstruct = 1 << 2,
    };
    friend void from_json(const nlohmann::json&, WaitMessageboxFlags&);
    friend WaitMessageboxFlags operator &(WaitMessageboxFlags a, WaitMessageboxFlags b) {
        return static_cast<WaitMessageboxFlags>(static_cast<int>(a) & static_cast<int>(b));
    }

    enum class DotNetOpenProcessHookMode : int {
        ImportHooks = 0,
        DirectHook = 1,
    };
    friend void from_json(const nlohmann::json&, DotNetOpenProcessHookMode&);
    
    enum class ClientLanguage : int {
        Japanese,
        English,
        German,
        French,
    };
    friend void from_json(const nlohmann::json&, ClientLanguage&);

    enum class LoadMethod : int {
        Entrypoint,
        DllInject,
    };
    friend void from_json(const nlohmann::json&, LoadMethod&);

    LoadMethod DalamudLoadMethod = LoadMethod::Entrypoint;
    std::string WorkingDirectory;
    std::string ConfigurationPath;
    std::string LogPath;
    std::string LogName;
    std::string PluginDirectory;
    std::string AssetDirectory;
    ClientLanguage Language = ClientLanguage::English;
    std::string GameVersion;
    std::string TroubleshootingPackData;
    int DelayInitializeMs = 0;
    bool NoLoadPlugins;
    bool NoLoadThirdPartyPlugins;

    std::string BootLogPath;
    bool BootShowConsole = false;
    bool BootDisableFallbackConsole = false;
    WaitMessageboxFlags BootWaitMessageBox = WaitMessageboxFlags::None;
    bool BootWaitDebugger = false;
    bool BootVehEnabled = false;
    bool BootVehFull = false;
    bool BootEnableEtw = false;
    DotNetOpenProcessHookMode BootDotnetOpenProcessHookMode = DotNetOpenProcessHookMode::ImportHooks;
    std::set<std::string> BootEnabledGameFixes{};
    std::set<std::string> BootUnhookDlls{};

    bool CrashHandlerShow = false;
    bool NoExceptionHandlers = false;

    friend void from_json(const nlohmann::json&, DalamudStartInfo&);
    void from_envvars();
};

extern DalamudStartInfo g_startInfo;

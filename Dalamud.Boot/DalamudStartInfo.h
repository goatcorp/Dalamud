#pragma once

struct DalamudStartInfo {
    enum class WaitMessageboxFlags : int {
        None = 0,
        BeforeInitialize = 1 << 0,
        BeforeDalamudEntrypoint = 1 << 1,
    };
    friend void from_json(const nlohmann::json&, WaitMessageboxFlags&);

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

    std::string WorkingDirectory;
    std::string ConfigurationPath;
    std::string PluginDirectory;
    std::string DefaultPluginDirectory;
    std::string AssetDirectory;
    ClientLanguage Language = ClientLanguage::English;
    std::string GameVersion;
    int DelayInitializeMs = 0;

    bool BootShowConsole = false;
    bool BootDisableFallbackConsole = false;
    WaitMessageboxFlags BootWaitMessageBox = WaitMessageboxFlags::None;
    bool BootWaitDebugger = false;
    bool BootVehEnabled = false;
    bool BootVehFull = false;
    DotNetOpenProcessHookMode BootDotnetOpenProcessHookMode = DotNetOpenProcessHookMode::ImportHooks;
    std::set<std::string> BootEnabledGameFixes{};
    std::set<std::string> BootUnhookDlls{};

    friend void from_json(const nlohmann::json&, DalamudStartInfo&);
    void from_envvars();
};

extern DalamudStartInfo g_startInfo;

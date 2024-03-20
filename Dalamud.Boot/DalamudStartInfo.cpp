#include "pch.h"
#include "DalamudStartInfo.h"

#include "utils.h"

DalamudStartInfo g_startInfo;

void from_json(const nlohmann::json& json, DalamudStartInfo::WaitMessageboxFlags& value) {
    if (json.is_number_integer()) {
        value = static_cast<DalamudStartInfo::WaitMessageboxFlags>(json.get<int>());

    } else if (json.is_array()) {
        value = DalamudStartInfo::WaitMessageboxFlags::None;
        for (const auto& item : json) {
            if (item.is_number_integer()) {
                value = static_cast<DalamudStartInfo::WaitMessageboxFlags>(static_cast<int>(value) | item.get<int>());

            } else if (item.is_string()) {
                const auto iteml = unicode::convert<std::string>(item.get<std::string>(), &unicode::lower);
                if (item == "beforeinitialize")
                    value = static_cast<DalamudStartInfo::WaitMessageboxFlags>(static_cast<int>(value) | static_cast<int>(DalamudStartInfo::WaitMessageboxFlags::BeforeInitialize));
                else if (item == "beforedalamudentrypoint")
                    value = static_cast<DalamudStartInfo::WaitMessageboxFlags>(static_cast<int>(value) | static_cast<int>(DalamudStartInfo::WaitMessageboxFlags::BeforeDalamudEntrypoint));
                else if (item == "beforedalamudconstruct")
                    value = static_cast<DalamudStartInfo::WaitMessageboxFlags>(static_cast<int>(value) | static_cast<int>(DalamudStartInfo::WaitMessageboxFlags::BeforeDalamudConstruct));
            }
        }

    } else if (json.is_string()) {
        value = DalamudStartInfo::WaitMessageboxFlags::None;
        for (const auto& item : utils::split(json.get<std::string>(), ",")) {
            const auto iteml = unicode::convert<std::string>(item, &unicode::lower);
            if (iteml == "beforeinitialize")
                value = static_cast<DalamudStartInfo::WaitMessageboxFlags>(static_cast<int>(value) | static_cast<int>(DalamudStartInfo::WaitMessageboxFlags::BeforeInitialize));
            else if (iteml == "beforedalamudentrypoint")
                value = static_cast<DalamudStartInfo::WaitMessageboxFlags>(static_cast<int>(value) | static_cast<int>(DalamudStartInfo::WaitMessageboxFlags::BeforeDalamudEntrypoint));
        }
    }
}

void from_json(const nlohmann::json& json, DalamudStartInfo::DotNetOpenProcessHookMode& value) {
    if (json.is_number_integer()) {
        value = static_cast<DalamudStartInfo::DotNetOpenProcessHookMode>(json.get<int>());

    } else if (json.is_string()) {
        const auto langstr = unicode::convert<std::string>(json.get<std::string>(), &unicode::lower);
        if (langstr == "importhooks")
            value = DalamudStartInfo::DotNetOpenProcessHookMode::ImportHooks;
        else if (langstr == "directhook")
            value = DalamudStartInfo::DotNetOpenProcessHookMode::DirectHook;
    }
}

void from_json(const nlohmann::json& json, DalamudStartInfo::ClientLanguage& value) {
    if (json.is_number_integer()) {
        value = static_cast<DalamudStartInfo::ClientLanguage>(json.get<int>());

    } else if (json.is_string()) {
        const auto langstr = unicode::convert<std::string>(json.get<std::string>(), &unicode::lower);
        if (langstr == "japanese")
            value = DalamudStartInfo::ClientLanguage::Japanese;
        else if (langstr == "english")
            value = DalamudStartInfo::ClientLanguage::English;
        else if (langstr == "german")
            value = DalamudStartInfo::ClientLanguage::German;
        else if (langstr == "french")
            value = DalamudStartInfo::ClientLanguage::French;
    }
}

void from_json(const nlohmann::json& json, DalamudStartInfo::LoadMethod& value) {
    if (json.is_number_integer()) {
        value = static_cast<DalamudStartInfo::LoadMethod>(json.get<int>());

    }
    else if (json.is_string()) {
        const auto langstr = unicode::convert<std::string>(json.get<std::string>(), &unicode::lower);
        if (langstr == "entrypoint")
            value = DalamudStartInfo::LoadMethod::Entrypoint;
        else if (langstr == "inject")
            value = DalamudStartInfo::LoadMethod::DllInject;
    }
}

void from_json(const nlohmann::json& json, DalamudStartInfo& config) {
    if (!json.is_object())
        return;

    config.DalamudLoadMethod = json.value("LoadMethod", config.DalamudLoadMethod);
    config.WorkingDirectory = json.value("WorkingDirectory", config.WorkingDirectory);
    config.ConfigurationPath = json.value("ConfigurationPath", config.ConfigurationPath);
    config.LogPath = json.value("LogPath", config.LogPath);
    config.LogName = json.value("LogName", config.LogName);
    config.PluginDirectory = json.value("PluginDirectory", config.PluginDirectory);
    config.AssetDirectory = json.value("AssetDirectory", config.AssetDirectory);
    config.Language = json.value("Language", config.Language);
    config.GameVersion = json.value("GameVersion", config.GameVersion);
    config.TroubleshootingPackData = json.value("TroubleshootingPackData", std::string{});
    config.DelayInitializeMs = json.value("DelayInitializeMs", config.DelayInitializeMs);
    config.NoLoadPlugins = json.value("NoLoadPlugins", config.NoLoadPlugins);
    config.NoLoadThirdPartyPlugins = json.value("NoLoadThirdPartyPlugins", config.NoLoadThirdPartyPlugins);

    config.BootLogPath = json.value("BootLogPath", config.BootLogPath);
    config.BootShowConsole = json.value("BootShowConsole", config.BootShowConsole);
    config.BootDisableFallbackConsole = json.value("BootDisableFallbackConsole", config.BootDisableFallbackConsole);
    config.BootWaitMessageBox = json.value("BootWaitMessageBox", config.BootWaitMessageBox);
    config.BootWaitDebugger = json.value("BootWaitDebugger", config.BootWaitDebugger);
    config.BootVehEnabled = json.value("BootVehEnabled", config.BootVehEnabled);
    config.BootVehFull = json.value("BootVehFull", config.BootVehFull);
    config.BootEnableEtw = json.value("BootEnableEtw", config.BootEnableEtw);
    config.BootDotnetOpenProcessHookMode = json.value("BootDotnetOpenProcessHookMode", config.BootDotnetOpenProcessHookMode);
    if (const auto it = json.find("BootEnabledGameFixes"); it != json.end() && it->is_array()) {
        config.BootEnabledGameFixes.clear();
        for (const auto& val : *it)
            config.BootEnabledGameFixes.insert(unicode::convert<std::string>(val.get<std::string>(), &unicode::lower));
    }
    if (const auto it = json.find("BootUnhookDlls"); it != json.end() && it->is_array()) {
        config.BootUnhookDlls.clear();
        for (const auto& val : *it)
            config.BootUnhookDlls.insert(unicode::convert<std::string>(val.get<std::string>(), &unicode::lower));
    }

    config.CrashHandlerShow = json.value("CrashHandlerShow", config.CrashHandlerShow);
    config.NoExceptionHandlers = json.value("NoExceptionHandlers", config.NoExceptionHandlers);
}

void DalamudStartInfo::from_envvars() {
    BootLogPath = utils::get_env<std::string>(L"DALAMUD_BOOT_LOGFILE");
    BootShowConsole = utils::get_env<bool>(L"DALAMUD_SHOW_CONSOLE");
    BootDisableFallbackConsole = utils::get_env<bool>(L"DALAMUD_DISABLE_FALLBACK_CONSOLE");
    BootWaitMessageBox = static_cast<WaitMessageboxFlags>(utils::get_env<int>(L"DALAMUD_WAIT_MESSAGEBOX"));
    BootWaitDebugger = utils::get_env<bool>(L"DALAMUD_WAIT_DEBUGGER");
    BootVehEnabled = utils::get_env<bool>(L"DALAMUD_IS_VEH");
    BootVehFull = utils::get_env<bool>(L"DALAMUD_IS_VEH_FULL");
    BootEnableEtw = utils::get_env<bool>(L"DALAMUD_ENABLE_ETW");
    BootDotnetOpenProcessHookMode = static_cast<DotNetOpenProcessHookMode>(utils::get_env<int>(L"DALAMUD_DOTNET_OPENPROCESS_HOOKMODE"));
    for (const auto& item : utils::get_env_list<std::string>(L"DALAMUD_GAMEFIX_LIST"))
        BootEnabledGameFixes.insert(unicode::convert<std::string>(item, &unicode::lower));
    for (const auto& item : utils::get_env_list<std::string>(L"DALAMUD_UNHOOK_DLLS"))
        BootUnhookDlls.insert(unicode::convert<std::string>(item, &unicode::lower));
}

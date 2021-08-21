using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.Sanitizer;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Internal;

namespace Dalamud.Plugin
{
    /// <summary>
    /// This class acts as an interface to various objects needed to interact with Dalamud and the game.
    /// </summary>
    public sealed class DalamudPluginInterface : IDisposable
    {
        private readonly string pluginName;
        private readonly PluginConfigurations configs;

        /// <summary>
        /// Initializes a new instance of the <see cref="DalamudPluginInterface"/> class.
        /// Set up the interface and populate all fields needed.
        /// </summary>
        /// <param name="pluginName">The internal name of the plugin.</param>
        /// <param name="reason">The reason the plugin was loaded.</param>
        internal DalamudPluginInterface(string pluginName, PluginLoadReason reason)
        {
            var configuration = Service<DalamudConfiguration>.Get();
            var dataManager = Service<DataManager>.Get();
            var localization = Service<Localization>.Get();

            this.UiBuilder = new UiBuilder(pluginName);

            this.pluginName = pluginName;
            this.configs = Service<PluginManager>.Get().PluginConfigs;
            this.Reason = reason;

            this.GeneralChatType = configuration.GeneralChatType;
            this.Sanitizer = new Sanitizer(dataManager.Language);
            if (configuration.LanguageOverride != null)
            {
                this.UiLanguage = configuration.LanguageOverride;
            }
            else
            {
                var currentUiLang = CultureInfo.CurrentUICulture;
                if (Localization.ApplicableLangCodes.Any(langCode => currentUiLang.TwoLetterISOLanguageName == langCode))
                    this.UiLanguage = currentUiLang.TwoLetterISOLanguageName;
                else
                    this.UiLanguage = "en";
            }

            localization.OnLocalizationChanged += this.OnLocalizationChanged;
            configuration.OnDalamudConfigurationSaved += this.OnDalamudConfigurationSaved;
        }

        /// <summary>
        /// Delegate for localization change with two-letter iso lang code.
        /// </summary>
        /// <param name="langCode">The new language code.</param>
        public delegate void LanguageChangedDelegate(string langCode);

        /// <summary>
        /// Event that gets fired when loc is changed
        /// </summary>
        public event LanguageChangedDelegate OnLanguageChanged;

        /// <summary>
        /// Gets the reason this plugin was loaded.
        /// </summary>
        public PluginLoadReason Reason { get; }

        /// <summary>
        /// Gets the directory Dalamud assets are stored in.
        /// </summary>
        public DirectoryInfo DalamudAssetDirectory => Service<Dalamud>.Get().AssetDirectory;

        /// <summary>
        /// Gets the directory your plugin configurations are stored in.
        /// </summary>
        public DirectoryInfo ConfigDirectory => new(this.GetPluginConfigDirectory());

        /// <summary>
        /// Gets the config file of your plugin.
        /// </summary>
        public FileInfo ConfigFile => this.configs.GetConfigFile(this.pluginName);

        /// <summary>
        /// Gets the <see cref="UiBuilder"/> instance which allows you to draw UI into the game via ImGui draw calls.
        /// </summary>
        public UiBuilder UiBuilder { get; private set; }

        /// <summary>
        /// Gets a value indicating whether Dalamud is running in Debug mode or the /xldev menu is open. This can occur on release builds.
        /// </summary>
#if DEBUG
        public bool IsDebugging => true;
#else
        public bool IsDebugging => Service<DalamudInterface>.Get().IsDevMenuOpen;
#endif

        /// <summary>
        /// Gets the current UI language in two-letter iso format.
        /// </summary>
        public string UiLanguage { get; private set; }

        /// <summary>
        /// Gets serializer class with functions to remove special characters from strings.
        /// </summary>
        public ISanitizer Sanitizer { get; }

        /// <summary>
        /// Gets the chat type used by default for plugin messages.
        /// </summary>
        public XivChatType GeneralChatType { get; private set; }

        #region Configuration

        /// <summary>
        /// Save a plugin configuration(inheriting IPluginConfiguration).
        /// </summary>
        /// <param name="currentConfig">The current configuration.</param>
        public void SavePluginConfig(IPluginConfiguration currentConfig)
        {
            if (currentConfig == null)
                return;

            this.configs.Save(currentConfig, this.pluginName);
        }

        /// <summary>
        /// Get a previously saved plugin configuration or null if none was saved before.
        /// </summary>
        /// <returns>A previously saved config or null if none was saved before.</returns>
        public IPluginConfiguration GetPluginConfig()
        {
            // This is done to support json deserialization of plugin configurations
            // even after running an in-game update of plugins, where the assembly version
            // changes.
            // Eventually it might make sense to have a separate method on this class
            // T GetPluginConfig<T>() where T : IPluginConfiguration
            // that can invoke LoadForType() directly instead of via reflection
            // This is here for now to support the current plugin API
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                if (type.IsAssignableTo(typeof(IPluginConfiguration)))
                {
                    var mi = this.configs.GetType().GetMethod("LoadForType");
                    var fn = mi.MakeGenericMethod(type);
                    return (IPluginConfiguration)fn.Invoke(this.configs, new object[] { this.pluginName });
                }
            }

            // this shouldn't be a thing, I think, but just in case
            return this.configs.Load(this.pluginName);
        }

        /// <summary>
        /// Get the config directory.
        /// </summary>
        /// <returns>directory with path of AppData/XIVLauncher/pluginConfig/PluginInternalName.</returns>
        public string GetPluginConfigDirectory() => this.configs.GetDirectory(this.pluginName);

        /// <summary>
        /// Get the loc directory.
        /// </summary>
        /// <returns>directory with path of AppData/XIVLauncher/pluginConfig/PluginInternalName/loc.</returns>
        public string GetPluginLocDirectory() => this.configs.GetDirectory(Path.Combine(this.pluginName, "loc"));

        #endregion

        #region Chat Links

        /// <summary>
        /// Register a chat link handler.
        /// </summary>
        /// <param name="commandId">The ID of the command.</param>
        /// <param name="commandAction">The action to be executed.</param>
        /// <returns>Returns an SeString payload for the link.</returns>
        public DalamudLinkPayload AddChatLinkHandler(uint commandId, Action<uint, SeString> commandAction)
        {
            return Service<ChatGui>.Get().AddChatLinkHandler(this.pluginName, commandId, commandAction);
        }

        /// <summary>
        /// Remove a chat link handler.
        /// </summary>
        /// <param name="commandId">The ID of the command.</param>
        public void RemoveChatLinkHandler(uint commandId)
        {
            Service<ChatGui>.Get().RemoveChatLinkHandler(this.pluginName, commandId);
        }

        /// <summary>
        /// Removes all chat link handlers registered by the plugin.
        /// </summary>
        public void RemoveChatLinkHandler()
        {
            Service<ChatGui>.Get().RemoveChatLinkHandler(this.pluginName);
        }
        #endregion

        /// <summary>
        /// Unregister your plugin and dispose all references. You have to call this when your IDalamudPlugin is disposed.
        /// </summary>
        public void Dispose()
        {
            this.UiBuilder.Dispose();
            Service<ChatGui>.Get().RemoveChatLinkHandler(this.pluginName);
            Service<Localization>.Get().OnLocalizationChanged -= this.OnLocalizationChanged;
            Service<DalamudConfiguration>.Get().OnDalamudConfigurationSaved -= this.OnDalamudConfigurationSaved;
        }

        private void OnLocalizationChanged(string langCode)
        {
            this.UiLanguage = langCode;
            this.OnLanguageChanged?.Invoke(langCode);
        }

        private void OnDalamudConfigurationSaved(DalamudConfiguration dalamudConfiguration)
        {
            this.GeneralChatType = dalamudConfiguration.GeneralChatType;
        }
    }
}

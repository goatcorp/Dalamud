using System;
using System.Collections.Generic;
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

            localization.LocalizationChanged += this.OnLocalizationChanged;
            configuration.DalamudConfigurationSaved += this.OnDalamudConfigurationSaved;
        }

        /// <summary>
        /// Delegate for localization change with two-letter iso lang code.
        /// </summary>
        /// <param name="langCode">The new language code.</param>
        public delegate void LanguageChangedDelegate(string langCode);

        /// <summary>
        /// Event that gets fired when loc is changed
        /// </summary>
        public event LanguageChangedDelegate LanguageChanged;

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

        /// <summary>
        /// Gets a list of installed plugin names.
        /// </summary>
        public List<string> PluginNames => Service<PluginManager>.Get().InstalledPlugins.Select(p => p.Manifest.Name).ToList();

        /// <summary>
        /// Gets a list of installed plugin internal names.
        /// </summary>
        public List<string> PluginInternalNames => Service<PluginManager>.Get().InstalledPlugins.Select(p => p.Manifest.InternalName).ToList();

        #region IPC

        /// <summary>
        /// Gets an IPC publisher.
        /// </summary>
        /// <typeparam name="TRet">The return type for funcs. Use object if this is unused.</typeparam>
        /// <param name="name">The name of the IPC registration.</param>
        /// <returns>An IPC publisher.</returns>
        /// <exception cref="IpcTypeMismatchError">This is thrown when the requested types do not match the previously registered types are different.</exception>
        public ICallGatePub<TRet> GetIpcPub<TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<TRet>(name);

        /// <inheritdoc cref="ICallGatePub{TRet}"/>
        public ICallGatePub<T1, TRet> GetIpcPub<T1, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, TRet>(name);

        /// <inheritdoc cref="ICallGatePub{TRet}"/>
        public ICallGatePub<T1, T2, TRet> GetIpcPub<T1, T2, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, TRet>(name);

        /// <inheritdoc cref="ICallGatePub{TRet}"/>
        public ICallGatePub<T1, T2, T3, TRet> GetIpcPub<T1, T2, T3, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, T3, TRet>(name);

        /// <inheritdoc cref="ICallGatePub{TRet}"/>
        public ICallGatePub<T1, T2, T3, T4, TRet> GetIpcPub<T1, T2, T3, T4, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, T3, T4, TRet>(name);

        /// <inheritdoc cref="ICallGatePub{TRet}"/>
        public ICallGatePub<T1, T2, T3, T4, T5, TRet> GetIpcPub<T1, T2, T3, T4, T5, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, T3, T4, T5, TRet>(name);

        /// <inheritdoc cref="ICallGatePub{TRet}"/>
        public ICallGatePub<T1, T2, T3, T4, T5, T6, TRet> GetIpcPub<T1, T2, T3, T4, T5, T6, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, T3, T4, T5, T6, TRet>(name);

        /// <inheritdoc cref="ICallGatePub{TRet}"/>
        public ICallGatePub<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcPub<T1, T2, T3, T4, T5, T6, T7, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, T3, T4, T5, T6, T7, TRet>(name);

        /// <inheritdoc cref="ICallGatePub{TRet}"/>
        public ICallGatePub<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcPub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(name);

        /// <summary>
        /// Gets an IPC subscriber.
        /// </summary>
        /// <typeparam name="TRet">The return type for funcs. Use object if this is unused.</typeparam>
        /// <param name="name">The name of the IPC registration.</param>
        /// <returns>An IPC publisher.</returns>
        public ICallGateSub<TRet> GetIpcSub<TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<TRet>(name);

        /// <inheritdoc cref="ICallGateSub{TRet}"/>
        public ICallGateSub<T1, TRet> GetIpcSub<T1, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, TRet>(name);

        /// <inheritdoc cref="ICallGateSub{TRet}"/>
        public ICallGateSub<T1, T2, TRet> GetIpcSub<T1, T2, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, TRet>(name);

        /// <inheritdoc cref="ICallGateSub{TRet}"/>
        public ICallGateSub<T1, T2, T3, TRet> GetIpcSub<T1, T2, T3, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, T3, TRet>(name);

        /// <inheritdoc cref="ICallGateSub{TRet}"/>
        public ICallGateSub<T1, T2, T3, T4, TRet> GetIpcSub<T1, T2, T3, T4, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, T3, T4, TRet>(name);

        /// <inheritdoc cref="ICallGateSub{TRet}"/>
        public ICallGateSub<T1, T2, T3, T4, T5, TRet> GetIpcSub<T1, T2, T3, T4, T5, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, T3, T4, T5, TRet>(name);

        /// <inheritdoc cref="ICallGateSub{TRet}"/>
        public ICallGateSub<T1, T2, T3, T4, T5, T6, TRet> GetIpcSub<T1, T2, T3, T4, T5, T6, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, T3, T4, T5, T6, TRet>(name);

        /// <inheritdoc cref="ICallGateSub{TRet}"/>
        public ICallGateSub<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcSub<T1, T2, T3, T4, T5, T6, T7, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, T3, T4, T5, T6, T7, TRet>(name);

        /// <inheritdoc cref="ICallGateSub{TRet}"/>
        public ICallGateSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name)
            => Service<CallGate>.Get().GetIpcPubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(name);

        #endregion

        #region Configuration

        /// <summary>
        /// Save a plugin configuration(inheriting IPluginConfiguration).
        /// </summary>
        /// <param name="currentConfig">The current configuration.</param>
        public void SavePluginConfig(IPluginConfiguration? currentConfig)
        {
            if (currentConfig == null)
                return;

            this.configs.Save(currentConfig, this.pluginName);
        }

        /// <summary>
        /// Get a previously saved plugin configuration or null if none was saved before.
        /// </summary>
        /// <returns>A previously saved config or null if none was saved before.</returns>
        public IPluginConfiguration? GetPluginConfig()
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
        /// Unregister your plugin and dispose all references.
        /// You have to call this when your IDalamudPlugin is disposed.
        /// </summary>
        public void Dispose()
        {
            this.UiBuilder.Dispose();
            Service<ChatGui>.Get().RemoveChatLinkHandler(this.pluginName);
            Service<Localization>.Get().LocalizationChanged -= this.OnLocalizationChanged;
            Service<DalamudConfiguration>.Get().DalamudConfigurationSaved -= this.OnDalamudConfigurationSaved;
        }

        private void OnLocalizationChanged(string langCode)
        {
            this.UiLanguage = langCode;
            this.LanguageChanged?.Invoke(langCode);
        }

        private void OnDalamudConfigurationSaved(DalamudConfiguration dalamudConfiguration)
        {
            this.GeneralChatType = dalamudConfiguration.GeneralChatType;
        }
    }
}

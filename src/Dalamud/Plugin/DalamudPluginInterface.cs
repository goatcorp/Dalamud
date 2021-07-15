using System;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Internal;
using Dalamud.Game.Text;
using Dalamud.Game.Text.Sanitizer;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;

namespace Dalamud.Plugin
{
    /// <summary>
    /// This class acts as an interface to various objects needed to interact with Dalamud and the game.
    /// </summary>
    public sealed class DalamudPluginInterface : IDisposable
    {
        private readonly Dalamud dalamud;
        private readonly string pluginName;
        private readonly PluginConfigurations configs;

        /// <summary>
        /// Initializes a new instance of the <see cref="DalamudPluginInterface"/> class.
        /// Set up the interface and populate all fields needed.
        /// </summary>
        /// <param name="dalamud">The dalamud instance to expose.</param>
        /// <param name="pluginName">The internal name of the plugin.</param>
        /// <param name="assemblyLocation">The equivalent of what Assembly.GetExecutingAssembly().Location should return.</param>
        /// <param name="reason">The reason the plugin was loaded.</param>
        internal DalamudPluginInterface(Dalamud dalamud, string pluginName, string assemblyLocation, PluginLoadReason reason)
        {
            this.CommandManager = dalamud.CommandManager;
            this.Framework = dalamud.Framework;
            this.ClientState = dalamud.ClientState;
            this.UiBuilder = new UiBuilder(dalamud, pluginName);
            this.TargetModuleScanner = dalamud.SigScanner;
            this.Data = dalamud.Data;
            this.SeStringManager = dalamud.SeStringManager;

            this.dalamud = dalamud;
            this.pluginName = pluginName;
            this.configs = dalamud.PluginManager.PluginConfigs;
            this.AssemblyLocation = assemblyLocation;
            this.Reason = reason;

            this.GeneralChatType = this.dalamud.Configuration.GeneralChatType;
            this.Sanitizer = new Sanitizer(this.Data.Language);
            if (this.dalamud.Configuration.LanguageOverride != null)
            {
                this.UiLanguage = this.dalamud.Configuration.LanguageOverride;
            }
            else
            {
                var currentUiLang = CultureInfo.CurrentUICulture;
                if (Localization.ApplicableLangCodes.Any(langCode => currentUiLang.TwoLetterISOLanguageName == langCode))
                    this.UiLanguage = currentUiLang.TwoLetterISOLanguageName;
                else
                    this.UiLanguage = "en";
            }

            dalamud.LocalizationManager.OnLocalizationChanged += this.OnLocalizationChanged;
            dalamud.Configuration.OnDalamudConfigurationSaved += this.OnDalamudConfigurationSaved;
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
        /// Gets the plugin assembly location.
        /// </summary>
        public string AssemblyLocation { get; private set; }

        /// <summary>
        /// Gets the directory Dalamud assets are stored in.
        /// </summary>
        public DirectoryInfo DalamudAssetDirectory => this.dalamud.AssetDirectory;

        /// <summary>
        /// Gets the directory your plugin configurations are stored in.
        /// </summary>
        public DirectoryInfo ConfigDirectory => new(this.GetPluginConfigDirectory());

        /// <summary>
        /// Gets the config file of your plugin.
        /// </summary>
        public FileInfo ConfigFile => this.configs.GetConfigFile(this.pluginName);

        /// <summary>
        /// Gets the CommandManager object that allows you to add and remove custom chat commands.
        /// </summary>
        public CommandManager CommandManager { get; private set; }

        /// <summary>
        /// Gets the ClientState object that allows you to access current client memory information like actors, territories, etc.
        /// </summary>
        public ClientState ClientState { get; private set; }

        /// <summary>
        /// Gets the Framework object that allows you to interact with the client.
        /// </summary>
        public Framework Framework { get; private set; }

        /// <summary>
        /// Gets the <see cref="UiBuilder">UiBuilder</see> instance which allows you to draw UI into the game via ImGui draw calls.
        /// </summary>
        public UiBuilder UiBuilder { get; private set; }

        /// <summary>
        /// Gets the <see cref="SigScanner">SigScanner</see> instance targeting the main module of the FFXIV process.
        /// </summary>
        public SigScanner TargetModuleScanner { get; private set; }

        /// <summary>
        /// Gets the <see cref="DataManager">DataManager</see> instance which allows you to access game data needed by the main dalamud features.
        /// </summary>
        public DataManager Data { get; private set; }

        /// <summary>
        /// Gets the <see cref="SeStringManager">SeStringManager</see> instance which allows creating and parsing SeString payloads.
        /// </summary>
        public SeStringManager SeStringManager { get; private set; }

        /// <summary>
        /// Gets a value indicating whether Dalamud is running in Debug mode or the /xldev menu is open. This can occur on release builds.
        /// </summary>
#if DEBUG
        public bool IsDebugging => true;
#else
        public bool IsDebugging => this.dalamud.DalamudUi.IsDevMenuOpen;
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
        /// Gets the action that should be executed when any plugin sends a message.
        /// </summary>
        internal Action<string, ExpandoObject> AnyPluginIpcAction { get; private set; }

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

        #region Chat Links

        /// <summary>
        /// Register a chat link handler.
        /// </summary>
        /// <param name="commandId">The ID of the command.</param>
        /// <param name="commandAction">The action to be executed.</param>
        /// <returns>Returns an SeString payload for the link.</returns>
        public DalamudLinkPayload AddChatLinkHandler(uint commandId, Action<uint, SeString> commandAction)
        {
            return this.Framework.Gui.Chat.AddChatLinkHandler(this.pluginName, commandId, commandAction);
        }

        /// <summary>
        /// Remove a chat link handler.
        /// </summary>
        /// <param name="commandId">The ID of the command.</param>
        public void RemoveChatLinkHandler(uint commandId)
        {
            this.Framework.Gui.Chat.RemoveChatLinkHandler(this.pluginName, commandId);
        }

        /// <summary>
        /// Removes all chat link handlers registered by the plugin.
        /// </summary>
        public void RemoveChatLinkHandler()
        {
            this.Framework.Gui.Chat.RemoveChatLinkHandler(this.pluginName);
        }
        #endregion

        #region IPC

        /// <summary>
        /// Subscribe to an IPC message by any plugin.
        /// </summary>
        /// <param name="action">The action to take when a message was received.</param>
        [Obsolete("The current IPC mechanism is obsolete and scheduled to be replaced after API level 3.")]
        public void SubscribeAny(Action<string, ExpandoObject> action)
        {
            if (this.AnyPluginIpcAction != null)
                throw new InvalidOperationException("Can't subscribe multiple times.");

            this.AnyPluginIpcAction = action;
        }

        /// <summary>
        /// Subscribe to an IPC message by a plugin.
        /// </summary>
        /// <param name="pluginName">The InternalName of the plugin to subscribe to.</param>
        /// <param name="action">The action to take when a message was received.</param>
        [Obsolete("The current IPC mechanism is obsolete and scheduled to be replaced after API level 3.")]
        public void Subscribe(string pluginName, Action<ExpandoObject> action)
        {
            if (this.dalamud.PluginManager.IpcSubscriptions.Any(x => x.SourcePluginName == this.pluginName && x.SubPluginName == pluginName))
                throw new InvalidOperationException("Can't add multiple subscriptions for the same plugin.");

            this.dalamud.PluginManager.IpcSubscriptions.Add(new(this.pluginName, pluginName, action));
        }

        /// <summary>
        /// Unsubscribe from messages from any plugin.
        /// </summary>
        [Obsolete("The current IPC mechanism is obsolete and scheduled to be replaced after API level 3.")]
        public void UnsubscribeAny()
        {
            if (this.AnyPluginIpcAction == null)
                throw new InvalidOperationException("Wasn't subscribed to this plugin.");

            this.AnyPluginIpcAction = null;
        }

        /// <summary>
        /// Unsubscribe from messages from a plugin.
        /// </summary>
        /// <param name="pluginName">The InternalName of the plugin to unsubscribe from.</param>
        [Obsolete("The current IPC mechanism is obsolete and scheduled to be replaced after API level 3.")]
        public void Unsubscribe(string pluginName)
        {
            var sub = this.dalamud.PluginManager.IpcSubscriptions.FirstOrDefault(x => x.SourcePluginName == this.pluginName && x.SubPluginName == pluginName);
            if (sub.SubAction == null)
                throw new InvalidOperationException("Wasn't subscribed to this plugin.");

            this.dalamud.PluginManager.IpcSubscriptions.Remove(sub);
        }

        /// <summary>
        /// Send a message to all subscribed plugins.
        /// </summary>
        /// <param name="message">The message to send.</param>
        [Obsolete("The current IPC mechanism is obsolete and scheduled to be replaced after API level 3.")]
        public void SendMessage(ExpandoObject message)
        {
            var subs = this.dalamud.PluginManager.IpcSubscriptions.Where(x => x.SubPluginName == this.pluginName);
            foreach (var sub in subs.Select(x => x.SubAction))
            {
                sub.Invoke(message);
            }
        }

        /// <summary>
        /// Send a message to a specific plugin.
        /// </summary>
        /// <param name="pluginName">The InternalName of the plugin to send the message to.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>True if the corresponding plugin was present and received the message.</returns>
        [Obsolete("The current IPC mechanism is obsolete and scheduled to be replaced after API level 3.")]
        public bool SendMessage(string pluginName, ExpandoObject message)
        {
            var plugin = this.dalamud.PluginManager.InstalledPlugins.FirstOrDefault(x => x.Manifest.InternalName == pluginName);

            if (plugin == default)
                return false;

            if (plugin.DalamudInterface?.AnyPluginIpcAction == null)
                return false;

            plugin.DalamudInterface.AnyPluginIpcAction.Invoke(this.pluginName, message);

            return true;
        }

        #endregion

        #region Logging

        /// <summary>
        /// Log a templated message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        [Obsolete("Use PluginLog")]
        public void Log(string messageTemplate, params object[] values) => Serilog.Log.Information(messageTemplate, values);

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        [Obsolete("Use PluginLog")]
        public void LogError(string messageTemplate, params object[] values) => Serilog.Log.Error(messageTemplate, values);

        /// <summary>
        /// Log a templated error message to the in-game debug log.
        /// </summary>
        /// <param name="exception">The exception that caused the error.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="values">Values to log.</param>
        [Obsolete("Use PluginLog")]
        public void LogError(Exception exception, string messageTemplate, params object[] values) => Serilog.Log.Error(exception, messageTemplate, values);

        #endregion

        /// <summary>
        /// Unregister your plugin and dispose all references. You have to call this when your IDalamudPlugin is disposed.
        /// </summary>
        public void Dispose()
        {
            this.UiBuilder.Dispose();
            this.Framework.Gui.Chat.RemoveChatLinkHandler(this.pluginName);
            this.dalamud.LocalizationManager.OnLocalizationChanged -= this.OnLocalizationChanged;
            this.dalamud.Configuration.OnDalamudConfigurationSaved -= this.OnDalamudConfigurationSaved;
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

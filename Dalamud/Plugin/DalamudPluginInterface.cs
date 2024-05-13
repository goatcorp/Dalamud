using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.Sanitizer;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Windows.PluginInstaller;
using Dalamud.Interface.Internal.Windows.Settings;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using Dalamud.Plugin.Ipc.Internal;
using Dalamud.Utility;

using static Dalamud.Interface.Internal.Windows.PluginInstaller.PluginInstallerWindow;

namespace Dalamud.Plugin;

/// <summary>
/// This class acts as an interface to various objects needed to interact with Dalamud and the game.
/// </summary>
public sealed class DalamudPluginInterface : IDisposable
{
    private readonly LocalPlugin plugin;
    private readonly PluginConfigurations configs;

    /// <summary>
    /// Initializes a new instance of the <see cref="DalamudPluginInterface"/> class.
    /// Set up the interface and populate all fields needed.
    /// </summary>
    /// <param name="plugin">The plugin this interface belongs to.</param>
    /// <param name="reason">The reason the plugin was loaded.</param>
    internal DalamudPluginInterface(
        LocalPlugin plugin,
        PluginLoadReason reason)
    {
        this.plugin = plugin;
        var configuration = Service<DalamudConfiguration>.Get();
        var dataManager = Service<DataManager>.Get();
        var localization = Service<Localization>.Get();

        this.UiBuilder = new UiBuilder(plugin.Name, plugin);

        this.configs = Service<PluginManager>.Get().PluginConfigs;
        this.Reason = reason;
        this.SourceRepository = this.IsDev ? SpecialPluginSource.DevPlugin : plugin.Manifest.InstalledFromUrl;
        this.IsTesting = plugin.IsTesting;

        this.LoadTime = DateTime.Now;
        this.LoadTimeUTC = DateTime.UtcNow;

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
    /// Delegate for events that listen to changes to the list of active plugins.
    /// </summary>
    /// <param name="kind">What action caused this event to be fired.</param>
    /// <param name="affectedThisPlugin">If this plugin was affected by the change.</param>
    public delegate void ActivePluginsChangedDelegate(PluginListInvalidationKind kind, bool affectedThisPlugin);

    /// <summary>
    /// Event that gets fired when loc is changed
    /// </summary>
    public event LanguageChangedDelegate LanguageChanged;

    /// <summary>
    /// Event that is fired when the active list of plugins is changed.
    /// </summary>
    public event ActivePluginsChangedDelegate ActivePluginsChanged;

    /// <summary>
    /// Gets the reason this plugin was loaded.
    /// </summary>
    public PluginLoadReason Reason { get; }

    /// <summary>
    /// Gets a value indicating whether or not auto-updates have already completed this session.
    /// </summary>
    public bool IsAutoUpdateComplete => Service<ChatHandlers>.Get().IsAutoUpdateComplete;

    /// <summary>
    /// Gets the repository from which this plugin was installed.
    ///
    /// If a plugin was installed from the official/main repository, this will return the value of
    /// <see cref="SpecialPluginSource.MainRepo"/>. Developer plugins will return the value of
    /// <see cref="SpecialPluginSource.DevPlugin"/>.
    /// </summary>
    public string SourceRepository { get; }

    /// <summary>
    /// Gets the current internal plugin name.
    /// </summary>
    public string InternalName => this.plugin.InternalName;

    /// <summary>
    /// Gets the plugin's manifest.
    /// </summary>
    public IPluginManifest Manifest => this.plugin.Manifest;

    /// <summary>
    /// Gets a value indicating whether this is a dev plugin.
    /// </summary>
    public bool IsDev => this.plugin.IsDev;

    /// <summary>
    /// Gets a value indicating whether this is a testing release of a plugin.
    /// </summary>
    /// <remarks>
    /// Dev plugins have undefined behavior for this value, but can be expected to return <c>false</c>.
    /// </remarks>
    public bool IsTesting { get; }

    /// <summary>
    /// Gets the time that this plugin was loaded.
    /// </summary>
    public DateTime LoadTime { get; }

    /// <summary>
    /// Gets the UTC time that this plugin was loaded.
    /// </summary>
    public DateTime LoadTimeUTC { get; }

    /// <summary>
    /// Gets the timespan delta from when this plugin was loaded.
    /// </summary>
    public TimeSpan LoadTimeDelta => DateTime.Now - this.LoadTime;

    /// <summary>
    /// Gets the directory Dalamud assets are stored in.
    /// </summary>
    public DirectoryInfo DalamudAssetDirectory => Service<Dalamud>.Get().AssetDirectory;

    /// <summary>
    /// Gets the location of your plugin assembly.
    /// </summary>
    public FileInfo AssemblyLocation => this.plugin.DllFile;

    /// <summary>
    /// Gets the directory your plugin configurations are stored in.
    /// </summary>
    public DirectoryInfo ConfigDirectory => new(this.GetPluginConfigDirectory());

    /// <summary>
    /// Gets the config file of your plugin.
    /// </summary>
    public FileInfo ConfigFile => this.configs.GetConfigFile(this.plugin.InternalName);

    /// <summary>
    /// Gets the <see cref="UiBuilder"/> instance which allows you to draw UI into the game via ImGui draw calls.
    /// </summary>
    public UiBuilder UiBuilder { get; private set; }

    /// <summary>
    /// Gets a value indicating whether Dalamud is running in Debug mode or the /xldev menu is open. This can occur on release builds.
    /// </summary>
    public bool IsDevMenuOpen => Service<DalamudInterface>.GetNullable() is { IsDevMenuOpen: true }; // Can be null during boot

    /// <summary>
    /// Gets a value indicating whether a debugger is attached.
    /// </summary>
    public bool IsDebugging => Debugger.IsAttached;

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
    /// Gets a list of installed plugins along with their current state.
    /// </summary>
    public IEnumerable<InstalledPluginState> InstalledPlugins => Service<PluginManager>.Get().InstalledPlugins.Select(p => new InstalledPluginState(p.Name, p.Manifest.InternalName, p.IsLoaded, p.EffectiveVersion));

    /// <summary>
    /// Opens the <see cref="PluginInstallerWindow"/> with the plugin name set as search target.
    /// </summary>
    /// <returns>Returns false if the DalamudInterface was null.</returns>
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    public bool OpenPluginInstaller()
    {
        return this.OpenPluginInstallerTo(PluginInstallerOpenKind.InstalledPlugins, this.plugin.InternalName);
    }

    /// <summary>
    /// Opens the <see cref="PluginInstallerWindow"/>, with an optional search term.
    /// </summary>
    /// <param name="openTo">The page to open the installer to. Defaults to the "All Plugins" page.</param>
    /// <param name="searchText">An optional search text to input in the search box.</param>
    /// <returns>Returns false if the DalamudInterface was null.</returns>
    public bool OpenPluginInstallerTo(PluginInstallerOpenKind openTo = PluginInstallerOpenKind.AllPlugins, string searchText = null)
    {
        var dalamudInterface = Service<DalamudInterface>.GetNullable(); // Can be null during boot
        if (dalamudInterface == null)
        {
            return false;
        }

        dalamudInterface.OpenPluginInstallerTo(openTo);
        dalamudInterface.SetPluginInstallerSearchText(searchText);

        return true;
    }

    /// <summary>
    /// Opens the <see cref="SettingsWindow"/>, with an optional search term.
    /// </summary>
    /// <param name="openTo">The tab to open the settings to. Defaults to the "General" tab.</param>
    /// <param name="searchText">An optional search text to input in the search box.</param>
    /// <returns>Returns false if the DalamudInterface was null.</returns>
    public bool OpenDalamudSettingsTo(SettingsOpenKind openTo = SettingsOpenKind.General, string searchText = null)
    {
        var dalamudInterface = Service<DalamudInterface>.GetNullable(); // Can be null during boot
        if (dalamudInterface == null)
        {
            return false;
        }

        dalamudInterface.OpenSettingsTo(openTo);
        dalamudInterface.SetSettingsSearchText(searchText);

        return true;
    }

    /// <summary>
    /// Opens the dev menu bar.
    /// </summary>
    /// <returns>Returns false if the DalamudInterface was null.</returns>
    public bool OpenDeveloperMenu()
    {
        var dalamudInterface = Service<DalamudInterface>.GetNullable(); // Can be null during boot
        if (dalamudInterface == null)
        {
            return false;
        }

        dalamudInterface.OpenDevMenu();
        return true;
    }

    #region IPC

    /// <inheritdoc cref="DataShare.GetOrCreateData{T}"/>
    public T GetOrCreateData<T>(string tag, Func<T> dataGenerator) where T : class
        => Service<DataShare>.Get().GetOrCreateData(tag, dataGenerator);

    /// <inheritdoc cref="DataShare.RelinquishData"/>
    public void RelinquishData(string tag)
        => Service<DataShare>.Get().RelinquishData(tag);

    /// <inheritdoc cref="DataShare.TryGetData{T}"/>
    public bool TryGetData<T>(string tag, [NotNullWhen(true)] out T? data) where T : class
        => Service<DataShare>.Get().TryGetData(tag, out data);

    /// <inheritdoc cref="DataShare.GetData{T}"/>
    public T? GetData<T>(string tag) where T : class
        => Service<DataShare>.Get().GetData<T>(tag);

    /// <summary>
    /// Gets an IPC provider.
    /// </summary>
    /// <typeparam name="TRet">The return type for funcs. Use object if this is unused.</typeparam>
    /// <param name="name">The name of the IPC registration.</param>
    /// <returns>An IPC provider.</returns>
    /// <exception cref="IpcTypeMismatchError">This is thrown when the requested types do not match the previously registered types are different.</exception>
    public ICallGateProvider<TRet> GetIpcProvider<TRet>(string name)
        => new CallGatePubSub<TRet>(name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    public ICallGateProvider<T1, TRet> GetIpcProvider<T1, TRet>(string name)
        => new CallGatePubSub<T1, TRet>(name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    public ICallGateProvider<T1, T2, TRet> GetIpcProvider<T1, T2, TRet>(string name)
        => new CallGatePubSub<T1, T2, TRet>(name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    public ICallGateProvider<T1, T2, T3, TRet> GetIpcProvider<T1, T2, T3, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, TRet>(name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    public ICallGateProvider<T1, T2, T3, T4, TRet> GetIpcProvider<T1, T2, T3, T4, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, TRet>(name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    public ICallGateProvider<T1, T2, T3, T4, T5, TRet> GetIpcProvider<T1, T2, T3, T4, T5, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, TRet>(name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    public ICallGateProvider<T1, T2, T3, T4, T5, T6, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, T6, TRet>(name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    public ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, TRet>(name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    public ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(name);

    /// <summary>
    /// Gets an IPC subscriber.
    /// </summary>
    /// <typeparam name="TRet">The return type for funcs. Use object if this is unused.</typeparam>
    /// <param name="name">The name of the IPC registration.</param>
    /// <returns>An IPC subscriber.</returns>
    public ICallGateSubscriber<TRet> GetIpcSubscriber<TRet>(string name)
        => new CallGatePubSub<TRet>(name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    public ICallGateSubscriber<T1, TRet> GetIpcSubscriber<T1, TRet>(string name)
        => new CallGatePubSub<T1, TRet>(name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    public ICallGateSubscriber<T1, T2, TRet> GetIpcSubscriber<T1, T2, TRet>(string name)
        => new CallGatePubSub<T1, T2, TRet>(name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    public ICallGateSubscriber<T1, T2, T3, TRet> GetIpcSubscriber<T1, T2, T3, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, TRet>(name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    public ICallGateSubscriber<T1, T2, T3, T4, TRet> GetIpcSubscriber<T1, T2, T3, T4, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, TRet>(name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    public ICallGateSubscriber<T1, T2, T3, T4, T5, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, TRet>(name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    public ICallGateSubscriber<T1, T2, T3, T4, T5, T6, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, T6, TRet>(name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    public ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, TRet>(name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    public ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(name);

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

        this.configs.Save(currentConfig, this.plugin.InternalName, this.plugin.EffectiveWorkingPluginId);
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
                return (IPluginConfiguration)fn.Invoke(this.configs, new object[] { this.plugin.InternalName });
            }
        }

        // this shouldn't be a thing, I think, but just in case
        return this.configs.Load(this.plugin.InternalName, this.plugin.EffectiveWorkingPluginId);
    }

    /// <summary>
    /// Get the config directory.
    /// </summary>
    /// <returns>directory with path of AppData/XIVLauncher/pluginConfig/PluginInternalName.</returns>
    public string GetPluginConfigDirectory() => this.configs.GetDirectory(this.plugin.InternalName);

    /// <summary>
    /// Get the loc directory.
    /// </summary>
    /// <returns>directory with path of AppData/XIVLauncher/pluginConfig/PluginInternalName/loc.</returns>
    public string GetPluginLocDirectory() => this.configs.GetDirectory(Path.Combine(this.plugin.InternalName, "loc"));

    #endregion

    #region Chat Links

    // TODO API9: Move to chatgui, don't allow passing own commandId

    /// <summary>
    /// Register a chat link handler.
    /// </summary>
    /// <param name="commandId">The ID of the command.</param>
    /// <param name="commandAction">The action to be executed.</param>
    /// <returns>Returns an SeString payload for the link.</returns>
    public DalamudLinkPayload AddChatLinkHandler(uint commandId, Action<uint, SeString> commandAction)
    {
        return Service<ChatGui>.Get().AddChatLinkHandler(this.plugin.InternalName, commandId, commandAction);
    }

    /// <summary>
    /// Remove a chat link handler.
    /// </summary>
    /// <param name="commandId">The ID of the command.</param>
    public void RemoveChatLinkHandler(uint commandId)
    {
        Service<ChatGui>.Get().RemoveChatLinkHandler(this.plugin.InternalName, commandId);
    }

    /// <summary>
    /// Removes all chat link handlers registered by the plugin.
    /// </summary>
    public void RemoveChatLinkHandler()
    {
        Service<ChatGui>.Get().RemoveChatLinkHandler(this.plugin.InternalName);
    }
    #endregion

    #region Dependency Injection

    /// <summary>
    /// Create a new object of the provided type using its default constructor, then inject objects and properties.
    /// </summary>
    /// <param name="scopedObjects">Objects to inject additionally.</param>
    /// <typeparam name="T">The type to create.</typeparam>
    /// <returns>The created and initialized type.</returns>
    public T? Create<T>(params object[] scopedObjects) where T : class
    {
        var svcContainer = Service<IoC.Internal.ServiceContainer>.Get();

        return (T)this.plugin.ServiceScope!.CreateAsync(
            typeof(T),
            this.GetPublicIocScopes(scopedObjects)).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Inject services into properties on the provided object instance.
    /// </summary>
    /// <param name="instance">The instance to inject services into.</param>
    /// <param name="scopedObjects">Objects to inject additionally.</param>
    /// <returns>Whether or not the injection succeeded.</returns>
    public bool Inject(object instance, params object[] scopedObjects)
    {
        return this.plugin.ServiceScope!.InjectPropertiesAsync(
            instance,
            this.GetPublicIocScopes(scopedObjects)).GetAwaiter().GetResult();
    }

    #endregion

    /// <inheritdoc cref="Dispose"/>
    void IDisposable.Dispose()
    {
    }

    /// <summary>This function will do nothing. Dalamud will dispose this object on plugin unload.</summary>
    [Obsolete("This function will do nothing. Dalamud will dispose this object on plugin unload.", true)]
    public void Dispose()
    {
        // ignored
    }

    /// <summary>Unregister the plugin and dispose all references.</summary>
    /// <remarks>Dalamud internal use only.</remarks>
    internal void DisposeInternal()
    {
        Service<ChatGui>.Get().RemoveChatLinkHandler(this.plugin.InternalName);
        Service<Localization>.Get().LocalizationChanged -= this.OnLocalizationChanged;
        Service<DalamudConfiguration>.Get().DalamudConfigurationSaved -= this.OnDalamudConfigurationSaved;
        this.UiBuilder.DisposeInternal();
    }

    /// <summary>
    /// Dispatch the active plugins changed event.
    /// </summary>
    /// <param name="kind">What action caused this event to be fired.</param>
    /// <param name="affectedThisPlugin">If this plugin was affected by the change.</param>
    internal void NotifyActivePluginsChanged(PluginListInvalidationKind kind, bool affectedThisPlugin)
    {
        this.ActivePluginsChanged?.Invoke(kind, affectedThisPlugin);
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

    private object[] GetPublicIocScopes(IEnumerable<object> scopedObjects)
    {
        return scopedObjects.Append(this).ToArray();
    }
}

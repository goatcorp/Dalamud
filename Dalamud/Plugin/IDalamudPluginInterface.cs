using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Game.Text.Sanitizer;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Windows.PluginInstaller;
using Dalamud.Interface.Internal.Windows.Settings;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using Dalamud.Plugin.Ipc.Internal;

namespace Dalamud.Plugin;

/// <summary>
/// This interface acts as an interface to various objects needed to interact with Dalamud and the game.
/// </summary>
public interface IDalamudPluginInterface
{
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
    event LanguageChangedDelegate LanguageChanged;

    /// <summary>
    /// Event that is fired when the active list of plugins is changed.
    /// </summary>
    event ActivePluginsChangedDelegate ActivePluginsChanged;

    /// <summary>
    /// Gets the reason this plugin was loaded.
    /// </summary>
    PluginLoadReason Reason { get; }

    /// <summary>
    /// Gets a value indicating whether auto-updates have already completed this session.
    /// </summary>
    bool IsAutoUpdateComplete { get; }

    /// <summary>
    /// Gets the repository from which this plugin was installed.
    ///
    /// If a plugin was installed from the official/main repository, this will return the value of
    /// <see cref="SpecialPluginSource.MainRepo"/>. Developer plugins will return the value of
    /// <see cref="SpecialPluginSource.DevPlugin"/>.
    /// </summary>
    string SourceRepository { get; }

    /// <summary>
    /// Gets the current internal plugin name.
    /// </summary>
    string InternalName { get; }

    /// <summary>
    /// Gets the plugin's manifest.
    /// </summary>
    IPluginManifest Manifest { get; }

    /// <summary>
    /// Gets a value indicating whether this is a dev plugin.
    /// </summary>
    bool IsDev { get; }

    /// <summary>
    /// Gets a value indicating whether this is a testing release of a plugin.
    /// </summary>
    /// <remarks>
    /// Dev plugins have undefined behavior for this value, but can be expected to return <c>false</c>.
    /// </remarks>
    bool IsTesting { get; }

    /// <summary>
    /// Gets the time that this plugin was loaded.
    /// </summary>
    DateTime LoadTime { get; }

    /// <summary>
    /// Gets the UTC time that this plugin was loaded.
    /// </summary>
    DateTime LoadTimeUTC { get; }

    /// <summary>
    /// Gets the timespan delta from when this plugin was loaded.
    /// </summary>
    TimeSpan LoadTimeDelta { get; }

    /// <summary>
    /// Gets the directory Dalamud assets are stored in.
    /// </summary>
    DirectoryInfo DalamudAssetDirectory { get; }

    /// <summary>
    /// Gets the location of your plugin assembly.
    /// </summary>
    FileInfo AssemblyLocation { get; }

    /// <summary>
    /// Gets the directory your plugin configurations are stored in.
    /// </summary>
    DirectoryInfo ConfigDirectory { get; }

    /// <summary>
    /// Gets the config file of your plugin.
    /// </summary>
    FileInfo ConfigFile { get; }

    /// <summary>
    /// Gets the <see cref="UiBuilder"/> instance which allows you to draw UI into the game via ImGui draw calls.
    /// </summary>
    IUiBuilder UiBuilder { get; }

    /// <summary>
    /// Gets a value indicating whether Dalamud is running in Debug mode or the /xldev menu is open. This can occur on release builds.
    /// </summary>
    bool IsDevMenuOpen { get; }

    /// <summary>
    /// Gets a value indicating whether a debugger is attached.
    /// </summary>
    bool IsDebugging { get; }

    /// <summary>
    /// Gets the current UI language in two-letter iso format.
    /// </summary>
    string UiLanguage { get; }

    /// <summary>
    /// Gets serializer class with functions to remove special characters from strings.
    /// </summary>
    ISanitizer Sanitizer { get; }

    /// <summary>
    /// Gets the chat type used by default for plugin messages.
    /// </summary>
    XivChatType GeneralChatType { get; }

    /// <summary>
    /// Gets a list of installed plugins along with their current state.
    /// </summary>
    IEnumerable<IExposedPlugin> InstalledPlugins { get; }

    /// <summary>
    /// Opens the <see cref="PluginInstallerWindow"/>, with an optional search term.
    /// </summary>
    /// <param name="openTo">The page to open the installer to. Defaults to the "All Plugins" page.</param>
    /// <param name="searchText">An optional search text to input in the search box.</param>
    /// <returns>Returns false if the DalamudInterface was null.</returns>
    bool OpenPluginInstallerTo(PluginInstallerOpenKind openTo = PluginInstallerOpenKind.AllPlugins, string? searchText = null);

    /// <summary>
    /// Opens the <see cref="SettingsWindow"/>, with an optional search term.
    /// </summary>
    /// <param name="openTo">The tab to open the settings to. Defaults to the "General" tab.</param>
    /// <param name="searchText">An optional search text to input in the search box.</param>
    /// <returns>Returns false if the DalamudInterface was null.</returns>
    bool OpenDalamudSettingsTo(SettingsOpenKind openTo = SettingsOpenKind.General, string? searchText = null);

    /// <summary>
    /// Opens the dev menu bar.
    /// </summary>
    /// <returns>Returns false if the DalamudInterface was null.</returns>
    bool OpenDeveloperMenu();

    /// <inheritdoc cref="DataShare.GetOrCreateData{T}"/>
    T GetOrCreateData<T>(string tag, Func<T> dataGenerator) where T : class;

    /// <inheritdoc cref="DataShare.RelinquishData"/>
    void RelinquishData(string tag);

    /// <inheritdoc cref="DataShare.TryGetData{T}"/>
    bool TryGetData<T>(string tag, [NotNullWhen(true)] out T? data) where T : class;

    /// <inheritdoc cref="DataShare.GetData{T}"/>
    T? GetData<T>(string tag) where T : class;

    /// <summary>
    /// Gets an IPC provider.
    /// </summary>
    /// <typeparam name="TRet">The return type for funcs. Use object if this is unused.</typeparam>
    /// <param name="name">The name of the IPC registration.</param>
    /// <returns>An IPC provider.</returns>
    /// <exception cref="IpcTypeMismatchError">This is thrown when the requested types do not match the previously registered types are different.</exception>
    ICallGateProvider<TRet> GetIpcProvider<TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, TRet> GetIpcProvider<T1, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, TRet> GetIpcProvider<T1, T2, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, T3, TRet> GetIpcProvider<T1, T2, T3, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, T3, T4, TRet> GetIpcProvider<T1, T2, T3, T4, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, T3, T4, T5, TRet> GetIpcProvider<T1, T2, T3, T4, T5, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, T3, T4, T5, T6, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, TRet>(string name);

    /// <inheritdoc cref="ICallGateProvider{TRet}"/>
    ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name);

    /// <summary>
    /// Gets an IPC subscriber.
    /// </summary>
    /// <typeparam name="TRet">The return type for funcs. Use object if this is unused.</typeparam>
    /// <param name="name">The name of the IPC registration.</param>
    /// <returns>An IPC subscriber.</returns>
    ICallGateSubscriber<TRet> GetIpcSubscriber<TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, TRet> GetIpcSubscriber<T1, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, TRet> GetIpcSubscriber<T1, T2, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, T3, TRet> GetIpcSubscriber<T1, T2, T3, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, T3, T4, TRet> GetIpcSubscriber<T1, T2, T3, T4, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, T3, T4, T5, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, T3, T4, T5, T6, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet>(string name);

    /// <inheritdoc cref="ICallGateSubscriber{TRet}"/>
    ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name);

    /// <summary>
    /// Save a plugin configuration(inheriting IPluginConfiguration).
    /// </summary>
    /// <param name="currentConfig">The current configuration.</param>
    void SavePluginConfig(IPluginConfiguration? currentConfig);

    /// <summary>
    /// Get a previously saved plugin configuration or null if none was saved before.
    /// </summary>
    /// <returns>A previously saved config or null if none was saved before.</returns>
    IPluginConfiguration? GetPluginConfig();

    /// <summary>
    /// Get the config directory.
    /// </summary>
    /// <returns>directory with path of AppData/XIVLauncher/pluginConfig/PluginInternalName.</returns>
    string GetPluginConfigDirectory();

    /// <summary>
    /// Get the loc directory.
    /// </summary>
    /// <returns>directory with path of AppData/XIVLauncher/pluginConfig/PluginInternalName/loc.</returns>
    string GetPluginLocDirectory();

    /// <summary>
    /// Create a new object of the provided type using its default constructor, then inject objects and properties.
    /// </summary>
    /// <param name="scopedObjects">Objects to inject additionally.</param>
    /// <typeparam name="T">The type to create.</typeparam>
    /// <returns>The created and initialized type, or <c>null</c> on failure.</returns>
    T? Create<T>(params object[] scopedObjects) where T : class;

    /// <summary>
    /// Create a new object of the provided type using its default constructor, then inject objects and properties.
    /// </summary>
    /// <param name="scopedObjects">Objects to inject additionally.</param>
    /// <typeparam name="T">The type to create.</typeparam>
    /// <returns>A task representing the created and initialized type.</returns>
    Task<T> CreateAsync<T>(params object[] scopedObjects) where T : class;

    /// <summary>
    /// Inject services into properties on the provided object instance.
    /// </summary>
    /// <param name="instance">The instance to inject services into.</param>
    /// <param name="scopedObjects">Objects to inject additionally.</param>
    /// <returns>Whether the injection succeeded.</returns>
    bool Inject(object instance, params object[] scopedObjects);

    /// <summary>
    /// Inject services into properties on the provided object instance.
    /// </summary>
    /// <param name="instance">The instance to inject services into.</param>
    /// <param name="scopedObjects">Objects to inject additionally.</param>
    /// <returns>A <see cref="ValueTask"/> representing the status of the operation.</returns>
    Task InjectAsync(object instance, params object[] scopedObjects);
}

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.Sanitizer;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Windows.PluginInstaller;
using Dalamud.Interface.Internal.Windows.SelfTest;
using Dalamud.Interface.Internal.Windows.Settings;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.AutoUpdate;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using Dalamud.Plugin.Ipc.Internal;
using Dalamud.Plugin.Services;

using Serilog;

namespace Dalamud.Plugin;

/// <summary>
/// This class acts as an interface to various objects needed to interact with Dalamud and the game.
/// </summary>
internal sealed class DalamudPluginInterface : IDalamudPluginInterface, IDisposable
{
    private readonly LocalPlugin plugin;
    private readonly PluginConfigurations configs;
    private readonly UiBuilder uiBuilder;

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

        this.UiBuilder = this.uiBuilder = new(plugin, plugin.Name);

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

    /// <inheritdoc/>
    public event IDalamudPluginInterface.LanguageChangedDelegate? LanguageChanged;

    /// <inheritdoc/>
    public event IDalamudPluginInterface.ActivePluginsChangedDelegate? ActivePluginsChanged;

    /// <inheritdoc/>
    public PluginLoadReason Reason { get; }

    /// <inheritdoc/>
    public bool IsAutoUpdateComplete => Service<AutoUpdateManager>.GetNullable()?.IsAutoUpdateComplete ?? false;

    /// <inheritdoc/>
    public string SourceRepository { get; }

    /// <inheritdoc/>
    public string InternalName => this.plugin.InternalName;

    /// <inheritdoc/>
    public IPluginManifest Manifest => this.plugin.Manifest;

    /// <inheritdoc/>
    public bool IsDev => this.plugin.IsDev;

    /// <inheritdoc/>
    public bool IsTesting { get; }

    /// <inheritdoc/>
    public DateTime LoadTime { get; }

    /// <inheritdoc/>
    public DateTime LoadTimeUTC { get; }

    /// <inheritdoc/>
    public TimeSpan LoadTimeDelta => DateTime.Now - this.LoadTime;

    /// <inheritdoc/>
    public DirectoryInfo DalamudAssetDirectory => Service<Dalamud>.Get().AssetDirectory;

    /// <inheritdoc/>
    public FileInfo AssemblyLocation => this.plugin.DllFile;

    /// <inheritdoc/>
    public DirectoryInfo ConfigDirectory => new(this.GetPluginConfigDirectory());

    /// <inheritdoc/>
    public FileInfo ConfigFile => this.configs.GetConfigFile(this.plugin.InternalName);

    /// <inheritdoc/>
    public IUiBuilder UiBuilder { get; private set; }

    /// <inheritdoc/>
    public bool IsDevMenuOpen => Service<DalamudInterface>.GetNullable() is { IsDevMenuOpen: true }; // Can be null during boot

    /// <inheritdoc/>
    public bool IsDebugging => Debugger.IsAttached;

    /// <inheritdoc/>
    public string UiLanguage { get; private set; }

    /// <inheritdoc/>
    public ISanitizer Sanitizer { get; }

    /// <inheritdoc/>
    public XivChatType GeneralChatType { get; private set; }

    /// <inheritdoc/>
    public IEnumerable<IExposedPlugin> InstalledPlugins =>
        Service<PluginManager>.Get().InstalledPlugins.Select(p => new ExposedPlugin(p));

    /// <summary>
    /// Gets the <see cref="UiBuilder"/> internal implementation.
    /// </summary>
    internal UiBuilder LocalUiBuilder => this.uiBuilder;

    /// <inheritdoc/>
    public bool OpenPluginInstallerTo(PluginInstallerOpenKind openTo = PluginInstallerOpenKind.AllPlugins, string? searchText = null)
    {
        var dalamudInterface = Service<DalamudInterface>.GetNullable(); // Can be null during boot
        if (dalamudInterface == null)
        {
            return false;
        }

        dalamudInterface.OpenPluginInstallerTo(openTo);
        dalamudInterface.SetPluginInstallerSearchText(searchText ?? string.Empty);

        return true;
    }

    /// <inheritdoc/>
    public bool OpenDalamudSettingsTo(SettingsOpenKind openTo = SettingsOpenKind.General, string? searchText = null)
    {
        var dalamudInterface = Service<DalamudInterface>.GetNullable(); // Can be null during boot
        if (dalamudInterface == null)
        {
            return false;
        }

        dalamudInterface.OpenSettingsTo(openTo);
        dalamudInterface.SetSettingsSearchText(searchText ?? string.Empty);

        return true;
    }

    /// <inheritdoc/>
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

    /// <summary>
    /// Gets the plugin the given assembly is part of.
    /// </summary>
    /// <param name="assembly">The assembly to check.</param>
    /// <returns>The plugin the given assembly is part of, or null if this is a shared assembly or if this information cannot be determined.</returns>
    public IExposedPlugin? GetPlugin(Assembly assembly)
        => AssemblyLoadContext.GetLoadContext(assembly) switch
        {
            null => null,
            var context => this.GetPlugin(context),
        };

    /// <summary>
    /// Gets the plugin that loads in the given context.
    /// </summary>
    /// <param name="context">The context to check.</param>
    /// <returns>The plugin that loads in the given context, or null if this isn't a plugin's context or if this information cannot be determined.</returns>
    public IExposedPlugin? GetPlugin(AssemblyLoadContext context)
        => Service<PluginManager>.Get().InstalledPlugins.FirstOrDefault(p => p.LoadsIn(context)) switch
        {
            null => null,
            var p => new ExposedPlugin(p),
        };

    #region IPC

    /// <inheritdoc/>
    public T GetOrCreateData<T>(string tag, Func<T> dataGenerator) where T : class
        => Service<DataShare>.Get().GetOrCreateData(tag, dataGenerator);

    /// <inheritdoc/>
    public void RelinquishData(string tag)
        => Service<DataShare>.Get().RelinquishData(tag);

    /// <inheritdoc/>
    public bool TryGetData<T>(string tag, [NotNullWhen(true)] out T? data) where T : class
        => Service<DataShare>.Get().TryGetData(tag, out data);

    /// <inheritdoc/>
    public T? GetData<T>(string tag) where T : class
        => Service<DataShare>.Get().GetData<T>(tag);

    /// <inheritdoc/>
    public ICallGateProvider<TRet> GetIpcProvider<TRet>(string name)
        => new CallGatePubSub<TRet>(name);

    /// <inheritdoc/>
    public ICallGateProvider<T1, TRet> GetIpcProvider<T1, TRet>(string name)
        => new CallGatePubSub<T1, TRet>(name);

    /// <inheritdoc/>
    public ICallGateProvider<T1, T2, TRet> GetIpcProvider<T1, T2, TRet>(string name)
        => new CallGatePubSub<T1, T2, TRet>(name);

    /// <inheritdoc/>
    public ICallGateProvider<T1, T2, T3, TRet> GetIpcProvider<T1, T2, T3, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, TRet>(name);

    /// <inheritdoc/>
    public ICallGateProvider<T1, T2, T3, T4, TRet> GetIpcProvider<T1, T2, T3, T4, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, TRet>(name);

    /// <inheritdoc/>
    public ICallGateProvider<T1, T2, T3, T4, T5, TRet> GetIpcProvider<T1, T2, T3, T4, T5, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, TRet>(name);

    /// <inheritdoc/>
    public ICallGateProvider<T1, T2, T3, T4, T5, T6, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, T6, TRet>(name);

    /// <inheritdoc/>
    public ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, TRet>(name);

    /// <inheritdoc/>
    public ICallGateProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcProvider<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(name);

    /// <inheritdoc/>
    public ICallGateSubscriber<TRet> GetIpcSubscriber<TRet>(string name)
        => new CallGatePubSub<TRet>(name);

    /// <inheritdoc/>
    public ICallGateSubscriber<T1, TRet> GetIpcSubscriber<T1, TRet>(string name)
        => new CallGatePubSub<T1, TRet>(name);

    /// <inheritdoc/>
    public ICallGateSubscriber<T1, T2, TRet> GetIpcSubscriber<T1, T2, TRet>(string name)
        => new CallGatePubSub<T1, T2, TRet>(name);

    /// <inheritdoc/>
    public ICallGateSubscriber<T1, T2, T3, TRet> GetIpcSubscriber<T1, T2, T3, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, TRet>(name);

    /// <inheritdoc/>
    public ICallGateSubscriber<T1, T2, T3, T4, TRet> GetIpcSubscriber<T1, T2, T3, T4, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, TRet>(name);

    /// <inheritdoc/>
    public ICallGateSubscriber<T1, T2, T3, T4, T5, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, TRet>(name);

    /// <inheritdoc/>
    public ICallGateSubscriber<T1, T2, T3, T4, T5, T6, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, T6, TRet>(name);

    /// <inheritdoc/>
    public ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, TRet>(name);

    /// <inheritdoc/>
    public ICallGateSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet> GetIpcSubscriber<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(string name)
        => new CallGatePubSub<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(name);

    #endregion

    #region Configuration

    /// <inheritdoc/>
    public void SavePluginConfig(IPluginConfiguration? currentConfig)
    {
        if (currentConfig == null)
            return;

        this.configs.Save(currentConfig, this.plugin.InternalName, this.plugin.EffectiveWorkingPluginId);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public string GetPluginConfigDirectory() => this.configs.GetDirectory(this.plugin.InternalName);

    /// <inheritdoc/>
    public string GetPluginLocDirectory() => this.configs.GetDirectory(Path.Combine(this.plugin.InternalName, "loc"));

    #endregion

    #region Dependency Injection

    /// <inheritdoc/>
    public object? GetService(Type serviceType)
    {
        return this.plugin.ServiceScope.GetService(serviceType);
    }

    /// <inheritdoc/>
    public T? Create<T>(params object[] scopedObjects) where T : class
    {
        var t = this.CreateAsync<T>(scopedObjects);
        t.Wait();

        if (t.Exception is { } e)
        {
            Log.Error(
                e,
                "{who}: Exception during {where}: {what}",
                this.plugin.Name,
                nameof(this.Create),
                typeof(T).FullName ?? typeof(T).Name);
        }

        return t.IsCompletedSuccessfully ? t.Result : null;
    }

    /// <inheritdoc/>
    public async Task<T> CreateAsync<T>(params object[] scopedObjects) where T : class =>
        (T)await this.plugin.ServiceScope!.CreateAsync(typeof(T), ObjectInstanceVisibility.ExposedToPlugins, this.GetPublicIocScopes(scopedObjects));

    /// <inheritdoc/>
    public bool Inject(object instance, params object[] scopedObjects)
    {
        var t = this.InjectAsync(instance, scopedObjects);
        t.Wait();

        if (t.Exception is { } e)
        {
            Log.Error(
                e,
                "{who}: Exception during {where}: {what}",
                this.plugin.Name,
                nameof(this.Inject),
                instance.GetType().FullName ?? instance.GetType().Name);
        }

        return t.IsCompletedSuccessfully;
    }

    /// <inheritdoc/>
    public Task InjectAsync(object instance, params object[] scopedObjects) =>
        this.plugin.ServiceScope!.InjectPropertiesAsync(instance, this.GetPublicIocScopes(scopedObjects));

    #endregion

    /// <inheritdoc/>
    public void Dispose()
    {
        Service<ChatGui>.Get().RemoveChatLinkHandler(this.plugin.InternalName);
        Service<Localization>.Get().LocalizationChanged -= this.OnLocalizationChanged;
        Service<DalamudConfiguration>.Get().DalamudConfigurationSaved -= this.OnDalamudConfigurationSaved;
        this.uiBuilder.DisposeInternal();
    }

    /// <summary>
    /// Dispatch the active plugins changed event.
    /// </summary>
    /// <param name="args">The event arguments containing information about the change.</param>
    internal void NotifyActivePluginsChanged(IActivePluginsChangedEventArgs args)
    {
        foreach (var action in Delegate.EnumerateInvocationList(this.ActivePluginsChanged))
        {
            try
            {
                action(args);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", action.Method);
            }
        }
    }

    private void OnLocalizationChanged(string langCode)
    {
        this.UiLanguage = langCode;

        foreach (var action in Delegate.EnumerateInvocationList(this.LanguageChanged))
        {
            try
            {
                action(langCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", action.Method);
            }
        }
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

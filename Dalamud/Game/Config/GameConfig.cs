using System.Threading.Tasks;

using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using Serilog;

namespace Dalamud.Game.Config;

/// <summary>
/// This class represents the game's configuration.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal sealed class GameConfig : IInternalDisposableService, IGameConfig
{
    private readonly TaskCompletionSource tcsInitialization = new();
    private readonly TaskCompletionSource<GameConfigSection> tcsSystem = new();
    private readonly TaskCompletionSource<GameConfigSection> tcsUiConfig = new();
    private readonly TaskCompletionSource<GameConfigSection> tcsUiControl = new(); 

    private readonly GameConfigAddressResolver address = new();
    private Hook<ConfigChangeDelegate>? configChangeHook;

    [ServiceManager.ServiceConstructor]
    private unsafe GameConfig(Framework framework, TargetSigScanner sigScanner)
    {
        framework.RunOnTick(() =>
        {
            try
            {
                Log.Verbose("[GameConfig] Initializing");
                var csFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
                var commonConfig = &csFramework->SystemConfig.CommonSystemConfig;
                this.tcsSystem.SetResult(new("System", framework, &commonConfig->ConfigBase));
                this.tcsUiConfig.SetResult(new("UiConfig", framework, &commonConfig->UiConfig));
                this.tcsUiControl.SetResult(
                    new(
                        "UiControl",
                        framework,
                        () => this.UiConfig.TryGetBool("PadMode", out var padMode) && padMode
                                  ? &commonConfig->UiControlGamepadConfig
                                  : &commonConfig->UiControlConfig));

                this.address.Setup(sigScanner);
                this.configChangeHook = Hook<ConfigChangeDelegate>.FromAddress(
                    this.address.ConfigChangeAddress,
                    this.OnConfigChanged);
                this.configChangeHook.Enable();
                this.tcsInitialization.SetResult();
            }
            catch (Exception ex)
            {
                this.tcsInitialization.SetExceptionIfIncomplete(ex);
            }
        });
    }

    private unsafe delegate nint ConfigChangeDelegate(ConfigBase* configBase, ConfigEntry* configEntry);
    
    /// <inheritdoc/>
    public event EventHandler<ConfigChangeEvent>? Changed;

#pragma warning disable 67
    /// <summary>
    /// Unused internally, used as a proxy for System.Changed via GameConfigPluginScoped
    /// </summary>
    public event EventHandler<ConfigChangeEvent>? SystemChanged;
    
    /// <summary>
    /// Unused internally, used as a proxy for UiConfig.Changed via GameConfigPluginScoped
    /// </summary>
    public event EventHandler<ConfigChangeEvent>? UiConfigChanged;
    
    /// <summary>
    /// Unused internally, used as a proxy for UiControl.Changed via GameConfigPluginScoped
    /// </summary>
    public event EventHandler<ConfigChangeEvent>? UiControlChanged;
#pragma warning restore 67

    /// <summary>
    /// Gets a task representing the initialization state of this class.
    /// </summary>
    public Task InitializationTask => this.tcsInitialization.Task;

    /// <inheritdoc/>
    public GameConfigSection System => this.tcsSystem.Task.Result;

    /// <inheritdoc/>
    public GameConfigSection UiConfig => this.tcsUiConfig.Task.Result;

    /// <inheritdoc/>
    public GameConfigSection UiControl => this.tcsUiControl.Task.Result;

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out bool value) => this.System.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out uint value) => this.System.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out float value) => this.System.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out string value) => this.System.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out UIntConfigProperties? properties) => this.System.TryGetProperties(option.GetName(), out properties);
    
    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out FloatConfigProperties? properties) => this.System.TryGetProperties(option.GetName(), out properties);
    
    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out StringConfigProperties? properties) => this.System.TryGetProperties(option.GetName(), out properties);
    
    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out bool value) => this.UiConfig.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out uint value) => this.UiConfig.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out float value) => this.UiConfig.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out string value) => this.UiConfig.TryGet(option.GetName(), out value);
    
    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out UIntConfigProperties properties) => this.UiConfig.TryGetProperties(option.GetName(), out properties);
    
    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out FloatConfigProperties properties) => this.UiConfig.TryGetProperties(option.GetName(), out properties);
    
    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out StringConfigProperties properties) => this.UiConfig.TryGetProperties(option.GetName(), out properties);

    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out bool value) => this.UiControl.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out uint value) => this.UiControl.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out float value) => this.UiControl.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out string value) => this.UiControl.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out UIntConfigProperties properties) => this.UiControl.TryGetProperties(option.GetName(), out properties);
    
    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out FloatConfigProperties properties) => this.UiControl.TryGetProperties(option.GetName(), out properties);
    
    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out StringConfigProperties properties) => this.UiControl.TryGetProperties(option.GetName(), out properties);

    /// <inheritdoc/>
    public void Set(SystemConfigOption option, bool value) => this.System.Set(option.GetName(), value);

    /// <inheritdoc/>
    public void Set(SystemConfigOption option, uint value) => this.System.Set(option.GetName(), value);

    /// <inheritdoc/>
    public void Set(SystemConfigOption option, float value) => this.System.Set(option.GetName(), value);

    /// <inheritdoc/>
    public void Set(SystemConfigOption option, string value) => this.System.Set(option.GetName(), value);

    /// <inheritdoc/>
    public void Set(UiConfigOption option, bool value) => this.UiConfig.Set(option.GetName(), value);
    
    /// <inheritdoc/>
    public void Set(UiConfigOption option, uint value) => this.UiConfig.Set(option.GetName(), value);

    /// <inheritdoc/>
    public void Set(UiConfigOption option, float value) => this.UiConfig.Set(option.GetName(), value);

    /// <inheritdoc/>
    public void Set(UiConfigOption option, string value) => this.UiConfig.Set(option.GetName(), value);
    
    /// <inheritdoc/>
    public void Set(UiControlOption option, bool value) => this.UiControl.Set(option.GetName(), value);
    
    /// <inheritdoc/>
    public void Set(UiControlOption option, uint value) => this.UiControl.Set(option.GetName(), value);
    
    /// <inheritdoc/>
    public void Set(UiControlOption option, float value) => this.UiControl.Set(option.GetName(), value);
    
    /// <inheritdoc/>
    public void Set(UiControlOption option, string value) => this.UiControl.Set(option.GetName(), value);
    
    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        var ode = new ObjectDisposedException(nameof(GameConfig));
        this.tcsInitialization.SetExceptionIfIncomplete(ode);
        this.tcsSystem.SetExceptionIfIncomplete(ode);
        this.tcsUiConfig.SetExceptionIfIncomplete(ode);
        this.tcsUiControl.SetExceptionIfIncomplete(ode);
        this.configChangeHook?.Disable();
        this.configChangeHook?.Dispose();
    }
    
    private unsafe nint OnConfigChanged(ConfigBase* configBase, ConfigEntry* configEntry)
    {
        var returnValue = this.configChangeHook!.Original(configBase, configEntry);
        try
        {
            ConfigChangeEvent? eventArgs = null;
            
            if (configBase == this.System.GetConfigBase())
            {
                eventArgs = this.System.InvokeChange<SystemConfigOption>(configEntry);
            }
            else if (configBase == this.UiConfig.GetConfigBase())
            {
                eventArgs = this.UiConfig.InvokeChange<UiConfigOption>(configEntry);
            }
            else if (configBase == this.UiControl.GetConfigBase())
            {
                eventArgs = this.UiControl.InvokeChange<UiControlOption>(configEntry);
            }

            if (eventArgs == null) return returnValue;

            this.Changed?.InvokeSafely(this, eventArgs);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Exception thrown handing {nameof(this.OnConfigChanged)} events.");
        }
        
        return returnValue;
    }
}

/// <summary>
/// Plugin-scoped version of a GameConfig service.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IGameConfig>]
#pragma warning restore SA1015
internal class GameConfigPluginScoped : IInternalDisposableService, IGameConfig
{
    [ServiceManager.ServiceDependency]
    private readonly GameConfig gameConfigService = Service<GameConfig>.Get();

    private readonly Task initializationTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameConfigPluginScoped"/> class.
    /// </summary>
    internal GameConfigPluginScoped()
    {
        this.gameConfigService.Changed += this.ConfigChangedForward;
        this.initializationTask = this.gameConfigService.InitializationTask.ContinueWith(
            r =>
            {
                if (!r.IsCompletedSuccessfully)
                    return r;
                this.gameConfigService.System.Changed += this.SystemConfigChangedForward;
                this.gameConfigService.UiConfig.Changed += this.UiConfigConfigChangedForward;
                this.gameConfigService.UiControl.Changed += this.UiControlConfigChangedForward;
                return Task.CompletedTask;
            }).Unwrap();
    }
    
    /// <inheritdoc/>
    public event EventHandler<ConfigChangeEvent>? Changed;
    
    /// <inheritdoc/>
    public event EventHandler<ConfigChangeEvent>? SystemChanged;
    
    /// <inheritdoc/>
    public event EventHandler<ConfigChangeEvent>? UiConfigChanged;
    
    /// <inheritdoc/>
    public event EventHandler<ConfigChangeEvent>? UiControlChanged;
    
    /// <inheritdoc/>
    public GameConfigSection System => this.gameConfigService.System;

    /// <inheritdoc/>
    public GameConfigSection UiConfig => this.gameConfigService.UiConfig;

    /// <inheritdoc/>
    public GameConfigSection UiControl => this.gameConfigService.UiControl;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.gameConfigService.Changed -= this.ConfigChangedForward;
        this.initializationTask.ContinueWith(
            r =>
            {
                if (!r.IsCompletedSuccessfully)
                    return;
                this.gameConfigService.System.Changed -= this.SystemConfigChangedForward;
                this.gameConfigService.UiConfig.Changed -= this.UiConfigConfigChangedForward;
                this.gameConfigService.UiControl.Changed -= this.UiControlConfigChangedForward;
            });

        this.Changed = null;
        this.SystemChanged = null;
        this.UiConfigChanged = null;
        this.UiControlChanged = null;
    }

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out bool value)
        => this.gameConfigService.TryGet(option, out value);
    
    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out uint value)
        => this.gameConfigService.TryGet(option, out value);

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out float value)
        => this.gameConfigService.TryGet(option, out value);

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out string value)
        => this.gameConfigService.TryGet(option, out value);

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out UIntConfigProperties? properties)
        => this.gameConfigService.TryGet(option, out properties);

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out FloatConfigProperties? properties)
        => this.gameConfigService.TryGet(option, out properties);
    
    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out StringConfigProperties? properties)
        => this.gameConfigService.TryGet(option, out properties);
    
    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out bool value)
        => this.gameConfigService.TryGet(option, out value);
    
    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out uint value)
        => this.gameConfigService.TryGet(option, out value);
    
    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out float value)
        => this.gameConfigService.TryGet(option, out value);
    
    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out string value)
        => this.gameConfigService.TryGet(option, out value);
    
    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out UIntConfigProperties? properties)
        => this.gameConfigService.TryGet(option, out properties);
    
    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out FloatConfigProperties? properties)
        => this.gameConfigService.TryGet(option, out properties);
    
    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out StringConfigProperties? properties)
        => this.gameConfigService.TryGet(option, out properties);
    
    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out bool value)
        => this.gameConfigService.TryGet(option, out value);
    
    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out uint value)
        => this.gameConfigService.TryGet(option, out value);
    
    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out float value)
        => this.gameConfigService.TryGet(option, out value);
    
    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out string value)
        => this.gameConfigService.TryGet(option, out value);
    
    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out UIntConfigProperties? properties)
        => this.gameConfigService.TryGet(option, out properties);
    
    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out FloatConfigProperties? properties)
        => this.gameConfigService.TryGet(option, out properties);
    
    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out StringConfigProperties? properties)
        => this.gameConfigService.TryGet(option, out properties);
    
    /// <inheritdoc/>
    public void Set(SystemConfigOption option, bool value)
        => this.gameConfigService.Set(option, value);
    
    /// <inheritdoc/>
    public void Set(SystemConfigOption option, uint value)
        => this.gameConfigService.Set(option, value);
    
    /// <inheritdoc/>
    public void Set(SystemConfigOption option, float value)
        => this.gameConfigService.Set(option, value);
    
    /// <inheritdoc/>
    public void Set(SystemConfigOption option, string value)
        => this.gameConfigService.Set(option, value);
    
    /// <inheritdoc/>
    public void Set(UiConfigOption option, bool value)
        => this.gameConfigService.Set(option, value);
    
    /// <inheritdoc/>
    public void Set(UiConfigOption option, uint value)
        => this.gameConfigService.Set(option, value);
    
    /// <inheritdoc/>
    public void Set(UiConfigOption option, float value)
        => this.gameConfigService.Set(option, value);
    
    /// <inheritdoc/>
    public void Set(UiConfigOption option, string value)
        => this.gameConfigService.Set(option, value);
    
    /// <inheritdoc/>
    public void Set(UiControlOption option, bool value)
        => this.gameConfigService.Set(option, value);
    
    /// <inheritdoc/>
    public void Set(UiControlOption option, uint value)
        => this.gameConfigService.Set(option, value);
    
    /// <inheritdoc/>
    public void Set(UiControlOption option, float value)
        => this.gameConfigService.Set(option, value);

    /// <inheritdoc/>
    public void Set(UiControlOption option, string value)
        => this.gameConfigService.Set(option, value);
    
    private void ConfigChangedForward(object sender, ConfigChangeEvent data) => this.Changed?.Invoke(sender, data);
    
    private void SystemConfigChangedForward(object sender, ConfigChangeEvent data) => this.SystemChanged?.Invoke(sender, data);
    
    private void UiConfigConfigChangedForward(object sender, ConfigChangeEvent data) => this.UiConfigChanged?.Invoke(sender, data);
    
    private void UiControlConfigChangedForward(object sender, ConfigChangeEvent data) => this.UiControlChanged?.Invoke(sender, data);
}

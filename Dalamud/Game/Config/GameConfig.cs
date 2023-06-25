using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Serilog;

namespace Dalamud.Game.Config;

/// <summary>
/// This class represents the game's configuration.
/// </summary>
[InterfaceVersion("1.0")]
[PluginInterface]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IGameConfig>]
#pragma warning restore SA1015
public sealed class GameConfig : IServiceType, IGameConfig
{
    [ServiceManager.ServiceConstructor]
    private unsafe GameConfig(Framework framework)
    {
        framework.RunOnTick(() =>
        {
            Log.Verbose("[GameConfig] Initializing");
            var csFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
            var commonConfig = &csFramework->SystemConfig.CommonSystemConfig;
            this.System = new GameConfigSection("System", framework, &commonConfig->ConfigBase);
            this.UiConfig = new GameConfigSection("UiConfig", framework, &commonConfig->UiConfig);
            this.UiControl = new GameConfigSection("UiControl", framework, () => this.UiConfig.TryGetBool("PadMode", out var padMode) && padMode ? &commonConfig->UiControlGamepadConfig : &commonConfig->UiControlConfig);
        });
    }

    /// <inheritdoc/>
    public GameConfigSection System { get; private set; }

    /// <inheritdoc/>
    public GameConfigSection UiConfig { get; private set; }

    /// <inheritdoc/>
    public GameConfigSection UiControl { get; private set; }

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out bool value) => this.System.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out uint value) => this.System.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out float value) => this.System.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(SystemConfigOption option, out string value) => this.System.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out bool value) => this.UiConfig.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out uint value) => this.UiConfig.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out float value) => this.UiConfig.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiConfigOption option, out string value) => this.UiControl.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out bool value) => this.UiControl.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out uint value) => this.UiControl.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out float value) => this.UiControl.TryGet(option.GetName(), out value);

    /// <inheritdoc/>
    public bool TryGet(UiControlOption option, out string value) => this.System.TryGet(option.GetName(), out value);
    
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
}

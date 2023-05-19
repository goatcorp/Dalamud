using System.Diagnostics;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.Config;

/// <summary>
/// This class represents the game's configuration.
/// </summary>
[InterfaceVersion("1.0")]
[PluginInterface]
[ServiceManager.EarlyLoadedService]
public sealed class GameConfig : IServiceType
{
    [ServiceManager.ServiceConstructor]
    private unsafe GameConfig(Framework framework)
    {
        framework.RunOnTick(() =>
        {
            Log.Verbose("[GameConfig] Initalizing");
            var csFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
            var commonConfig = &csFramework->SystemConfig.CommonSystemConfig;
            this.System = new GameConfigSection("System", framework, &commonConfig->ConfigBase);
            this.UiConfig = new GameConfigSection("UiConfig", framework, &commonConfig->UiConfig);
            this.UiControl = new GameConfigSection("UiControl", framework, () => this.UiConfig.TryGetBool("PadMode", out var padMode) && padMode ? &commonConfig->UiControlGamepadConfig : &commonConfig->UiControlConfig);
        });
    }

    /// <summary>
    /// Gets the collection of config options that persist between characters.
    /// </summary>
    public GameConfigSection System { get; private set; }

    /// <summary>
    /// Gets the collection of config options that are character specific.
    /// </summary>
    public GameConfigSection UiConfig { get; private set; }

    /// <summary>
    /// Gets the collection of config options that are control mode specific. (Mouse and Keyboard / Gamepad).
    /// </summary>
    public GameConfigSection UiControl { get; private set; }

    /// <summary>
    /// Attempts to get a boolean config value from the System section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(SystemConfigOption option, out bool value) => this.System.TryGet(option.GetName(), out value);

    /// <summary>
    /// Attempts to get a uint config value from the System section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(SystemConfigOption option, out uint value) => this.System.TryGet(option.GetName(), out value);

    /// <summary>
    /// Attempts to get a float config value from the System section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(SystemConfigOption option, out float value) => this.System.TryGet(option.GetName(), out value);

    /// <summary>
    /// Attempts to get a string config value from the System section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(SystemConfigOption option, out string value) => this.System.TryGet(option.GetName(), out value);

    /// <summary>
    /// Attempts to get a boolean config value from the UiConfig section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiConfigOption option, out bool value) => this.UiConfig.TryGet(option.GetName(), out value);

    /// <summary>
    /// Attempts to get a uint config value from the UiConfig section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiConfigOption option, out uint value) => this.UiConfig.TryGet(option.GetName(), out value);

    /// <summary>
    /// Attempts to get a float config value from the UiConfig section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiConfigOption option, out float value) => this.UiConfig.TryGet(option.GetName(), out value);

    /// <summary>
    /// Attempts to get a string config value from the UiConfig section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiConfigOption option, out string value) => this.UiControl.TryGet(option.GetName(), out value);

    /// <summary>
    /// Attempts to get a boolean config value from the UiControl section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiControlOption option, out bool value) => this.UiControl.TryGet(option.GetName(), out value);

    /// <summary>
    /// Attempts to get a uint config value from the UiControl section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiControlOption option, out uint value) => this.UiControl.TryGet(option.GetName(), out value);

    /// <summary>
    /// Attempts to get a float config value from the UiControl section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiControlOption option, out float value) => this.UiControl.TryGet(option.GetName(), out value);

    /// <summary>
    /// Attempts to get a string config value from the UiControl section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiControlOption option, out string value) => this.System.TryGet(option.GetName(), out value);

    /// <summary>
    /// Set a boolean config option in the System config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(SystemConfigOption option, bool value) => this.System.Set(option.GetName(), value);

    /// <summary>
    /// Set a unsigned integer config option in the System config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(SystemConfigOption option, uint value) => this.System.Set(option.GetName(), value);

    /// <summary>
    /// Set a float config option in the System config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(SystemConfigOption option, float value) => this.System.Set(option.GetName(), value);

    /// <summary>
    /// Set a string config option in the System config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(SystemConfigOption option, string value) => this.System.Set(option.GetName(), value);

    /// <summary>
    /// Set a boolean config option in the UiConfig section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiConfigOption option, bool value) => this.UiConfig.Set(option.GetName(), value);

    /// <summary>
    /// Set a unsigned integer config option in the UiConfig section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiConfigOption option, uint value) => this.UiConfig.Set(option.GetName(), value);

    /// <summary>
    /// Set a float config option in the UiConfig section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiConfigOption option, float value) => this.UiConfig.Set(option.GetName(), value);

    /// <summary>
    /// Set a string config option in the UiConfig section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiConfigOption option, string value) => this.UiConfig.Set(option.GetName(), value);

    /// <summary>
    /// Set a boolean config option in the UiControl config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiControlOption option, bool value) => this.UiControl.Set(option.GetName(), value);

    /// <summary>
    /// Set a uint config option in the UiControl config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiControlOption option, uint value) => this.UiControl.Set(option.GetName(), value);

    /// <summary>
    /// Set a float config option in the UiControl config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiControlOption option, float value) => this.UiControl.Set(option.GetName(), value);

    /// <summary>
    /// Set a string config option in the UiControl config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiControlOption option, string value) => this.UiControl.Set(option.GetName(), value);
}

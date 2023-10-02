using System;
using System.Diagnostics;

using Dalamud.Game.Config;
using FFXIVClientStructs.FFXIV.Common.Configuration;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class represents the game's configuration.
/// </summary>
public interface IGameConfig
{
    /// <summary>
    /// Event which is fired when any game config option is changed.
    /// </summary>
    public event EventHandler<ConfigChangeEvent> Changed;

    /// <summary>
    /// Event which is fired when a system config option is changed.
    /// </summary>
    public event EventHandler<ConfigChangeEvent> SystemChanged; 
    
    /// <summary>
    /// Event which is fired when a UiConfig option is changed.
    /// </summary>
    public event EventHandler<ConfigChangeEvent> UiConfigChanged; 
    
    /// <summary>
    /// Event which is fired when a UiControl config option is changed.
    /// </summary>
    public event EventHandler<ConfigChangeEvent> UiControlChanged; 

    /// <summary>
    /// Gets the collection of config options that persist between characters.
    /// </summary>
    public GameConfigSection System { get; }
    
    /// <summary>
    /// Gets the collection of config options that are character specific.
    /// </summary>
    public GameConfigSection UiConfig { get; }
    
    /// <summary>
    /// Gets the collection of config options that are control mode specific. (Mouse and Keyboard / Gamepad).
    /// </summary>
    public GameConfigSection UiControl { get; }

    /// <summary>
    /// Attempts to get a boolean config value from the System section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(SystemConfigOption option, out bool value);
    
    /// <summary>
    /// Attempts to get a uint config value from the System section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(SystemConfigOption option, out uint value);

    /// <summary>
    /// Attempts to get a float config value from the System section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(SystemConfigOption option, out float value);

    /// <summary>
    /// Attempts to get a string config value from the System section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(SystemConfigOption option, out string value);
    
    /// <summary>
    /// Attempts to get the properties of a UInt option from the System section.
    /// </summary>
    /// <param name="option">Option to get the properties of.</param>
    /// <param name="properties">Details of the option: Minimum, Maximum, and Default values.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(SystemConfigOption option, out UIntConfigProperties? properties);
    
    /// <summary>
    /// Attempts to get the properties of a Float option from the System section.
    /// </summary>
    /// <param name="option">Option to get the properties of.</param>
    /// <param name="properties">Details of the option: Minimum, Maximum, and Default values.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(SystemConfigOption option, out FloatConfigProperties? properties);
    
    /// <summary>
    /// Attempts to get the properties of a String option from the System section.
    /// </summary>
    /// <param name="option">Option to get the properties of.</param>
    /// <param name="properties">Details of the option: Default Value.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(SystemConfigOption option, out StringConfigProperties? properties);

    /// <summary>
    /// Attempts to get a boolean config value from the UiConfig section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiConfigOption option, out bool value);

    /// <summary>
    /// Attempts to get a uint config value from the UiConfig section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiConfigOption option, out uint value);

    /// <summary>
    /// Attempts to get a float config value from the UiConfig section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiConfigOption option, out float value);

    /// <summary>
    /// Attempts to get a string config value from the UiConfig section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiConfigOption option, out string value);

    /// <summary>
    /// Attempts to get the properties of a UInt option from the UiConfig section.
    /// </summary>
    /// <param name="option">Option to get the properties of.</param>
    /// <param name="properties">Details of the option: Minimum, Maximum, and Default values.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiConfigOption option, out UIntConfigProperties? properties);
    
    /// <summary>
    /// Attempts to get the properties of a Float option from the UiConfig section.
    /// </summary>
    /// <param name="option">Option to get the properties of.</param>
    /// <param name="properties">Details of the option: Minimum, Maximum, and Default values.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiConfigOption option, out FloatConfigProperties? properties);
    
    /// <summary>
    /// Attempts to get the properties of a String option from the UiConfig section.
    /// </summary>
    /// <param name="option">Option to get the properties of.</param>
    /// <param name="properties">Details of the option: Default Value.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiConfigOption option, out StringConfigProperties? properties);
    
    /// <summary>
    /// Attempts to get a boolean config value from the UiControl section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiControlOption option, out bool value);

    /// <summary>
    /// Attempts to get a uint config value from the UiControl section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiControlOption option, out uint value);

    /// <summary>
    /// Attempts to get a float config value from the UiControl section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiControlOption option, out float value);
    
    /// <summary>
    /// Attempts to get a string config value from the UiControl section.
    /// </summary>
    /// <param name="option">Option to get the value of.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiControlOption option, out string value);

    /// <summary>
    /// Attempts to get the properties of a UInt option from the UiControl section.
    /// </summary>
    /// <param name="option">Option to get the properties of.</param>
    /// <param name="properties">Details of the option: Minimum, Maximum, and Default values.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiControlOption option, out UIntConfigProperties? properties);
    
    /// <summary>
    /// Attempts to get the properties of a Float option from the UiControl section.
    /// </summary>
    /// <param name="option">Option to get the properties of.</param>
    /// <param name="properties">Details of the option: Minimum, Maximum, and Default values.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiControlOption option, out FloatConfigProperties? properties);
    
    /// <summary>
    /// Attempts to get the properties of a String option from the UiControl section.
    /// </summary>
    /// <param name="option">Option to get the properties of.</param>
    /// <param name="properties">Details of the option: Default Value.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(UiControlOption option, out StringConfigProperties? properties);

    /// <summary>
    /// Set a boolean config option in the System config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(SystemConfigOption option, bool value);

    /// <summary>
    /// Set a unsigned integer config option in the System config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(SystemConfigOption option, uint value);

    /// <summary>
    /// Set a float config option in the System config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(SystemConfigOption option, float value);

    /// <summary>
    /// Set a string config option in the System config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(SystemConfigOption option, string value);

    /// <summary>
    /// Set a boolean config option in the UiConfig section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiConfigOption option, bool value);

    /// <summary>
    /// Set a unsigned integer config option in the UiConfig section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiConfigOption option, uint value);

    /// <summary>
    /// Set a float config option in the UiConfig section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiConfigOption option, float value);

    /// <summary>
    /// Set a string config option in the UiConfig section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiConfigOption option, string value);

    /// <summary>
    /// Set a boolean config option in the UiControl config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiControlOption option, bool value);

    /// <summary>
    /// Set a uint config option in the UiControl config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiControlOption option, uint value);

    /// <summary>
    /// Set a float config option in the UiControl config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiControlOption option, float value);

    /// <summary>
    /// Set a string config option in the UiControl config section.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="option">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public void Set(UiControlOption option, string value);
}

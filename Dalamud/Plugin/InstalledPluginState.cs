using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Plugin;

/// <summary>
/// Interface representing an installed plugin, to be exposed to other plugins.
/// </summary>
public interface IExposedPlugin
{
    /// <summary>
    /// Gets the name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the internal name of the plugin.
    /// </summary>
    string InternalName { get; }

    /// <summary>
    /// Gets a value indicating whether the plugin is loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the version of the plugin.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets a value indicating whether the plugin has a main UI.
    /// </summary>
    public bool HasMainUi { get; }

    /// <summary>
    /// Gets a value indicating whether the plugin has a config UI.
    /// </summary>
    public bool HasConfigUi { get; }

    /// <summary>
    /// Opens the main UI of the plugin.
    /// Throws <see cref="InvalidOperationException"/> if <see cref="HasMainUi"/> is false.
    /// </summary>
    public void OpenMainUi();

    /// <summary>
    /// Opens the config UI of the plugin.
    /// Throws <see cref="InvalidOperationException"/> if <see cref="HasConfigUi"/> is false.
    /// </summary>
    public void OpenConfigUi();
}

/// <summary>
/// Internal representation of an installed plugin, to be exposed to other plugins.
/// </summary>
/// <param name="plugin">The plugin.</param>
internal sealed class ExposedPlugin(LocalPlugin plugin) : IExposedPlugin
{
    /// <inheritdoc/>
    public string Name => plugin.Name;

    /// <inheritdoc/>
    public string InternalName => plugin.InternalName;

    /// <inheritdoc/>
    public bool IsLoaded => plugin.IsLoaded;

    /// <inheritdoc/>
    public Version Version => plugin.EffectiveVersion;

    /// <inheritdoc/>
    public bool HasMainUi => plugin.DalamudInterface?.LocalUiBuilder.HasMainUi ?? false;

    /// <inheritdoc/>
    public bool HasConfigUi => plugin.DalamudInterface?.LocalUiBuilder.HasMainUi ?? false;

    /// <inheritdoc/>
    public void OpenMainUi()
    {
        if (plugin.DalamudInterface?.LocalUiBuilder.HasMainUi == true)
            plugin.DalamudInterface.LocalUiBuilder.OpenMain();
        else
            throw new InvalidOperationException("Plugin does not have a main UI.");
    }

    /// <inheritdoc/>
    public void OpenConfigUi()
    {
        if (plugin.DalamudInterface?.LocalUiBuilder.HasConfigUi == true)
            plugin.DalamudInterface.LocalUiBuilder.OpenConfig();
        else
            throw new InvalidOperationException("Plugin does not have a config UI.");
    }
}

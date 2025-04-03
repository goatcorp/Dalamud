using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;

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
    /// Gets a value indicating whether this plugin's API level is out of date.
    /// </summary>
    bool IsOutdated { get; }

    /// <summary>
    /// Gets a value indicating whether the plugin is for testing use only.
    /// </summary>
    bool IsTesting { get; }

    /// <summary>
    /// Gets a value indicating whether or not this plugin is orphaned(belongs to a repo) or not.
    /// </summary>
    bool IsOrphaned { get; }

    /// <summary>
    /// Gets a value indicating whether or not this plugin is serviced(repo still exists, but plugin no longer does).
    /// </summary>
    bool IsDecommissioned { get; }

    /// <summary>
    /// Gets a value indicating whether this plugin has been banned.
    /// </summary>
    bool IsBanned { get; }

    /// <summary>
    /// Gets a value indicating whether this plugin is dev plugin.
    /// </summary>
    bool IsDev { get; }

    /// <summary>
    /// Gets a value indicating whether this manifest is associated with a plugin that was installed from a third party
    /// repo.
    /// </summary>
    bool IsThirdParty { get; }

    /// <summary>
    /// Gets the plugin manifest.
    /// </summary>
    ILocalPluginManifest Manifest { get; }

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
    public bool HasConfigUi => plugin.DalamudInterface?.LocalUiBuilder.HasConfigUi ?? false;

    /// <inheritdoc/>
    public bool IsOutdated => plugin.IsOutdated;

    /// <inheritdoc/>
    public bool IsTesting => plugin.IsTesting;

    /// <inheritdoc/>
    public bool IsOrphaned => plugin.IsOrphaned;

    /// <inheritdoc/>
    public bool IsDecommissioned => plugin.IsDecommissioned;

    /// <inheritdoc/>
    public bool IsBanned => plugin.IsBanned;

    /// <inheritdoc/>
    public bool IsDev => plugin.IsDev;

    /// <inheritdoc/>
    public bool IsThirdParty => plugin.IsThirdParty;

    /// <inheritdoc/>
    public ILocalPluginManifest Manifest => plugin.Manifest;

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

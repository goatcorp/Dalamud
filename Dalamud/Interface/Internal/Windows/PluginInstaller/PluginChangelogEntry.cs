using CheapLoc;
using Dalamud.Plugin.Internal.Types;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller;

/// <summary>
/// Class representing a plugin changelog.
/// </summary>
internal class PluginChangelogEntry : IChangelogEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginChangelogEntry"/> class.
    /// </summary>
    /// <param name="plugin">The plugin manifest.</param>
    /// <param name="history">The changelog history entry.</param>
    public PluginChangelogEntry(LocalPlugin plugin, DalamudChangelogManager.PluginHistory.PluginVersion history)
    {
        this.Plugin = plugin;

        this.Version = history.Version;
        this.Text = history.Changelog ?? Loc.Localize("ChangelogNoText", "No changelog for this version.");
        this.Author = history.PublishedBy;
        this.Date = history.PublishedAt;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginChangelogEntry"/> class.
    /// </summary>
    /// <param name="plugin">The plugin manifest.</param>
    public PluginChangelogEntry(LocalPlugin plugin)
    {
        this.Plugin = plugin;

        this.Version = plugin.EffectiveVersion.ToString();
        this.Text = plugin.Manifest.Changelog ?? Loc.Localize("ChangelogNoText", "No changelog for this version.");
        this.Author = plugin.Manifest.Author;

        try
        {
            this.Date = DateTimeOffset.FromUnixTimeSeconds(this.Plugin.Manifest.LastUpdate).DateTime;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Log.Warning(ex, "Manifest included improper timestamp, e.g. wrong unit: {PluginName}",
                        plugin.Manifest.Name);
            // Create a Date from 0 as with a manifest that does not include a LastUpdate field
            this.Date = DateTimeOffset.FromUnixTimeSeconds(0).DateTime;
        }
    }

    /// <summary>
    /// Gets the respective plugin.
    /// </summary>
    public LocalPlugin Plugin { get; private set; }

    /// <inheritdoc/>
    public string Title => this.Plugin.Manifest.Name;

    /// <inheritdoc/>
    public string Version { get; private set; }

    /// <inheritdoc/>
    public string Text { get; private set; }

    /// <inheritdoc/>
    public string? Author { get; private set; }

    /// <inheritdoc/>
    public DateTime Date { get; private set; }
}

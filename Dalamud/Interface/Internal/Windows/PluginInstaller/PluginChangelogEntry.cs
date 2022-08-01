using System;

using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller
{
    /// <summary>
    /// Class representing a plugin changelog.
    /// </summary>
    internal class PluginChangelogEntry : IChangelogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginChangelogEntry"/> class.
        /// </summary>
        /// <param name="plugin">The plugin manifest.</param>
        public PluginChangelogEntry(LocalPlugin plugin)
        {
            this.Plugin = plugin;

            if (plugin.Manifest.Changelog.IsNullOrEmpty())
                throw new ArgumentException("Manifest has no changelog.");

            var version = plugin.Manifest.EffectiveVersion;

            this.Version = version!.ToString();
        }

        /// <summary>
        /// Gets the respective plugin.
        /// </summary>
        public LocalPlugin Plugin { get; private set; }

        /// <inheritdoc/>
        public string Title => this.Plugin.Manifest.Name;

        /// <inheritdoc/>
        public string Version { get; init; }

        /// <inheritdoc/>
        public string Text => this.Plugin.Manifest.Changelog!;

        /// <inheritdoc/>
        public DateTime Date => DateTimeOffset.FromUnixTimeSeconds(this.Plugin.Manifest.LastUpdate).DateTime;
    }
}

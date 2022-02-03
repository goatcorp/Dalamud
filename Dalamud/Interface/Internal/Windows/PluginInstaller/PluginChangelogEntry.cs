using System;

using Dalamud.Plugin.Internal;
using Dalamud.Utility;
using ImGuiScene;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller
{
    /// <summary>
    /// Class representing a plugin changelog.
    /// </summary>
    internal class PluginChangelogEntry : IChangelogEntry
    {
        private readonly LocalPlugin plugin;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginChangelogEntry"/> class.
        /// </summary>
        /// <param name="plugin">The plugin manifest.</param>
        /// <param name="icon">The icon.</param>
        public PluginChangelogEntry(LocalPlugin plugin, TextureWrap icon)
        {
            this.plugin = plugin;
            this.Icon = icon;

            if (plugin.Manifest.Changelog.IsNullOrEmpty())
                throw new ArgumentException("Manifest has no changelog.");

            var version = plugin.AssemblyName?.Version;
            version ??= plugin.Manifest.Testing
                            ? plugin.Manifest.TestingAssemblyVersion
                            : plugin.Manifest.AssemblyVersion;

            this.Version = version!.ToString();
        }

        /// <inheritdoc/>
        public string Title => this.plugin.Manifest.Name;

        /// <inheritdoc/>
        public string Version { get; init; }

        /// <inheritdoc/>
        public string Text => this.plugin.Manifest.Changelog!;

        /// <inheritdoc/>
        public TextureWrap Icon { get; init; }

        /// <inheritdoc/>
        public DateTime Date => DateTimeOffset.FromUnixTimeSeconds(this.plugin.Manifest.LastUpdate).DateTime;
    }
}

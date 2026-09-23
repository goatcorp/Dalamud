using System.Collections.Generic;
using System.Linq;

using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Plugin;

/// <inheritdoc cref="IActivePluginsChangedEventArgs" />
public class ActivePluginsChangedEventArgs : EventArgs, IActivePluginsChangedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivePluginsChangedEventArgs"/> class
    /// with the specified parameters.
    /// </summary>
    /// <param name="kind">The kind of change that triggered the event.</param>
    /// <param name="affectedPlugins">The plugins affected by the change.</param>
    internal ActivePluginsChangedEventArgs(PluginListInvalidationKind kind, IEnumerable<IActivePluginsChangedEventArgs.IAffectedPlugin> affectedPlugins)
    {
        this.Kind = kind;
        this.AffectedPlugins = affectedPlugins;
    }

    /// <inheritdoc/>
    public PluginListInvalidationKind Kind { get; }

    /// <inheritdoc/>
    public IEnumerable<string> AffectedInternalNames
        => this.AffectedPlugins.Select(t => t.InternalName);

    /// <inheritdoc/>
    public IEnumerable<IActivePluginsChangedEventArgs.IAffectedPlugin> AffectedPlugins { get; }

    /// <inheritdoc/>
    internal sealed class AffectedPlugin(LocalPlugin plugin, Version? version) : IActivePluginsChangedEventArgs.IAffectedPlugin
    {
        /// <inheritdoc/>
        public string Name { get; } = plugin.Name;

        /// <inheritdoc/>
        public string InternalName { get; } = plugin.InternalName;

        /// <inheritdoc/>
        public Version Version { get; } = version ?? plugin.EffectiveVersion;

        /// <inheritdoc/>
        public Guid WorkingPluginId { get; } = plugin.Manifest.WorkingPluginId;

        /// <inheritdoc/>
        public bool IsBanned { get; } = plugin.IsBanned;

        /// <inheritdoc/>
        public bool IsDev { get; } = plugin.IsDev;

        /// <inheritdoc/>
        public bool IsThirdParty { get; } = plugin.IsThirdParty;

        /// <inheritdoc/>
        public bool IsTesting { get; } = plugin.IsTesting;
    }
}

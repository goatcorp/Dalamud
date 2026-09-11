using System.Collections.Generic;

using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.Plugin;

/// <summary>
/// Contains data about changes to the list of active plugins.
/// </summary>
public interface IActivePluginsChangedEventArgs
{
    /// <summary> Contains a subset of the data of <see cref="IExposedPlugin"/> that is available even if the plugin has been unloaded. </summary>
    public interface IAffectedPlugin
    {
        /// <inheritdoc cref="IExposedPlugin.Name"/>
        string Name { get; }

        /// <inheritdoc cref="IExposedPlugin.InternalName"/>
        string InternalName { get; }

        /// <inheritdoc cref="IExposedPlugin.Version"/>
        Version Version { get; }

        /// <inheritdoc cref="ILocalPluginManifest.WorkingPluginId"/>
        Guid WorkingPluginId { get; }

        /// <inheritdoc cref="IExposedPlugin.IsBanned"/>
        bool IsBanned { get; }

        /// <inheritdoc cref="IExposedPlugin.IsDev"/>
        bool IsDev { get; }

        /// <inheritdoc cref="IExposedPlugin.IsThirdParty"/>
        bool IsThirdParty { get; }

        /// <inheritdoc cref="IExposedPlugin.IsTesting"/>
        bool IsTesting { get; }
    }

    /// <summary>
    /// Gets the invalidation kind that caused this event to be fired.
    /// </summary>
    PluginListInvalidationKind Kind { get; }

    /// <summary>
    /// Gets the InternalNames of affected plugins.
    /// </summary>
    IEnumerable<string> AffectedInternalNames { get; }

    /// <summary>
    /// Gets the available information about affected plugins.
    /// </summary>
    IEnumerable<IAffectedPlugin> AffectedPlugins { get; }
}

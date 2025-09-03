using System.Collections.Generic;

namespace Dalamud.Plugin;

/// <summary>
/// Contains data about changes to the list of active plugins.
/// </summary>
public interface IActivePluginsChangedEventArgs
{
    /// <summary>
    /// Gets the invalidation kind that caused this event to be fired.
    /// </summary>
    PluginListInvalidationKind Kind { get; }

    /// <summary>
    /// Gets the InternalNames of affected plugins.
    /// </summary>
    IEnumerable<string> AffectedInternalNames { get; }
}

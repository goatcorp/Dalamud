using System.Collections.Generic;

namespace Dalamud.Plugin;

/// <summary>
/// Contains data about changes to the list of active plugins.
/// </summary>
public class ActivePluginsChangedEventArgs(PluginListInvalidationKind kind, IEnumerable<string> affectedInternalNames) : EventArgs
{
    /// <summary>
    /// Gets the invalidation kind that caused this event to be fired.
    /// </summary>
    public PluginListInvalidationKind Kind { get; } = kind;

    /// <summary>
    /// Gets the InternalNames of affected plugins.
    /// </summary>
    public IEnumerable<string> AffectedInternalNames { get; } = affectedInternalNames;
}

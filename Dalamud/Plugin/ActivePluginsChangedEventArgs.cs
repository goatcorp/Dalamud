using System.Collections.Generic;

namespace Dalamud.Plugin;

/// <summary>
/// Contains data about changes to the list of active plugins.
/// </summary>
public class ActivePluginsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivePluginsChangedEventArgs"/> class
    /// with the specified parameters.
    /// </summary>
    /// <param name="kind">The kind of change that triggered the event.</param>
    /// <param name="affectedInternalNames">The internal names of the plugins affected by the change.</param>
    internal ActivePluginsChangedEventArgs(PluginListInvalidationKind kind, IEnumerable<string> affectedInternalNames)
    {
        this.Kind = kind;
        this.AffectedInternalNames = affectedInternalNames;
    }

    /// <summary>
    /// Gets the invalidation kind that caused this event to be fired.
    /// </summary>
    public PluginListInvalidationKind Kind { get; }

    /// <summary>
    /// Gets the InternalNames of affected plugins.
    /// </summary>
    public IEnumerable<string> AffectedInternalNames { get; }
}

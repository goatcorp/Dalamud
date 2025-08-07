using System.Collections.Generic;

namespace Dalamud.Plugin;

/// <inheritdoc cref="IActivePluginsChangedEventArgs" />
public class ActivePluginsChangedEventArgs : EventArgs, IActivePluginsChangedEventArgs
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

    /// <inheritdoc/>
    public PluginListInvalidationKind Kind { get; }

    /// <inheritdoc/>
    public IEnumerable<string> AffectedInternalNames { get; }
}

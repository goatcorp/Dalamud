using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

namespace Dalamud.Console;

#pragma warning disable Dalamud001

/// <summary>
/// Plugin-scoped version of the console service.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IConsole>]
#pragma warning restore SA1015
public class ConsoleManagerPluginScoped : IConsole, IInternalDisposableService
{
    [ServiceManager.ServiceDependency]
    private readonly ConsoleManager console = Service<ConsoleManager>.Get();
    
    private readonly List<IConsoleEntry> trackedEntries = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleManagerPluginScoped"/> class.
    /// </summary>
    /// <param name="plugin">The plugin this service belongs to.</param>
    /// <param name="console">The console manager.</param>
    [ServiceManager.ServiceConstructor]
    internal ConsoleManagerPluginScoped(LocalPlugin plugin)
    {
        this.Prefix = ConsoleManagerPluginUtil.GetSanitizedNamespaceName(plugin.InternalName);
    }

    /// <inheritdoc/>
    public string Prefix { get; private set; }
    
    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        foreach (var trackedEntry in this.trackedEntries)
        {
            this.console.RemoveEntry(trackedEntry);
        }
        
        this.trackedEntries.Clear();
    }

    /// <inheritdoc/>
    public IConsoleCommand AddCommand(string name, string description, Func<bool> func)
        => this.InternalAddCommand(name, description, func);

    /// <inheritdoc/>
    public IConsoleCommand AddCommand<T1>(string name, string description, Func<bool, T1> func)
        => this.InternalAddCommand(name, description, func);

    /// <inheritdoc/>
    public IConsoleCommand AddCommand<T1, T2>(string name, string description, Func<bool, T1, T2> func)
        => this.InternalAddCommand(name, description, func);

    /// <inheritdoc/>
    public IConsoleCommand AddCommand<T1, T2, T3>(string name, string description, Func<bool, T1, T2, T3> func)
        => this.InternalAddCommand(name, description, func);

    /// <inheritdoc/>
    public IConsoleCommand AddCommand<T1, T2, T3, T4>(string name, string description, Func<bool, T1, T2, T3, T4> func)
        => this.InternalAddCommand(name, description, func);

    /// <inheritdoc/>
    public IConsoleCommand AddCommand<T1, T2, T3, T4, T5>(string name, string description, Func<bool, T1, T2, T3, T4, T5> func)
        => this.InternalAddCommand(name, description, func);

    /// <inheritdoc/>
    public IConsoleVariable<T> AddVariable<T>(string name, string description, T defaultValue)
    {
        var variable = this.console.AddVariable(this.GetPrefixedName(name), description, defaultValue);
        this.trackedEntries.Add(variable);
        return variable;
    }

    /// <inheritdoc/>
    public IConsoleEntry AddAlias(string name, string alias)
    {
        var entry = this.console.AddAlias(this.GetPrefixedName(name), alias);
        this.trackedEntries.Add(entry);
        return entry;
    }

    /// <inheritdoc/>
    public T GetVariable<T>(string name)
    {
        return this.console.GetVariable<T>(this.GetPrefixedName(name));
    }

    /// <inheritdoc/>
    public void SetVariable<T>(string name, T value)
    {
        this.console.SetVariable(this.GetPrefixedName(name), value);
    }

    /// <inheritdoc/>
    public void RemoveEntry(IConsoleEntry entry)
    {
        this.console.RemoveEntry(entry);
        this.trackedEntries.Remove(entry);
    }
    
    private string GetPrefixedName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        
        // If the name is empty, return the prefix to allow for a single command or variable to be top-level.
        if (name.Length == 0)
            return this.Prefix;
        
        if (name.Any(char.IsWhiteSpace))
            throw new ArgumentException("Name cannot contain whitespace.", nameof(name));
        
        return $"{this.Prefix}.{name}";
    }
    
    private IConsoleCommand InternalAddCommand(string name, string description, Delegate func)
    {
        var command = this.console.AddCommand(this.GetPrefixedName(name), description, func);
        this.trackedEntries.Add(command);
        return command;
    }
}

/// <summary>
/// Utility functions for the console manager.
/// </summary>
internal static partial class ConsoleManagerPluginUtil
{
    private static readonly string[] ReservedNamespaces = ["dalamud", "xl", "plugin"];
    
    /// <summary>
    /// Get a sanitized namespace name from a plugin's internal name.
    /// </summary>
    /// <param name="pluginInternalName">The plugin's internal name.</param>
    /// <returns>A sanitized namespace.</returns>
    public static string GetSanitizedNamespaceName(string pluginInternalName)
    {
        // Must be lowercase
        pluginInternalName = pluginInternalName.ToLowerInvariant();
        
        // Remove all non-alphabetic characters
        pluginInternalName = NonAlphaRegex().Replace(pluginInternalName, string.Empty);
        
        // Remove reserved namespaces from the start or end
        foreach (var reservedNamespace in ReservedNamespaces)
        {
            if (pluginInternalName.StartsWith(reservedNamespace))
            {
                pluginInternalName = pluginInternalName[reservedNamespace.Length..];
            }
            
            if (pluginInternalName.EndsWith(reservedNamespace))
            {
                pluginInternalName = pluginInternalName[..^reservedNamespace.Length];
            }
        }
        
        return pluginInternalName;
    }

    [GeneratedRegex(@"[^a-z]")]
    private static partial Regex NonAlphaRegex();
}

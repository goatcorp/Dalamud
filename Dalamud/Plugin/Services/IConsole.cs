using System.Diagnostics.CodeAnalysis;

using Dalamud.Console;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Provides functions to register console commands and variables.
/// </summary>
[Experimental("Dalamud001")]
public interface IConsole
{
    /// <summary>
    /// Gets this plugin's namespace prefix, derived off its internal name.
    /// This is the prefix that all commands and variables registered by this plugin will have.
    /// If the internal name is "SamplePlugin", the prefix will be "sampleplugin.".
    /// </summary>
    public string Prefix { get; }

    /// <summary>
    /// Add a command to the console.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">A description of the command.</param>
    /// <param name="func">Function to invoke when the command has been called. Must return a <see cref="bool"/> indicating success.</param>
    /// <returns>The added command.</returns>
    public IConsoleCommand AddCommand(string name, string description, Func<bool> func);

    /// <summary>
    /// Add a command to the console.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">A description of the command.</param>
    /// <param name="func">Function to invoke when the command has been called. Must return a <see cref="bool"/> indicating success.</param>
    /// <typeparam name="T1">The first argument to the command.</typeparam>
    /// <returns>The added command.</returns>
    public IConsoleCommand AddCommand<T1>(string name, string description, Func<bool, T1> func);

    /// <summary>
    /// Add a command to the console.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">A description of the command.</param>
    /// <param name="func">Function to invoke when the command has been called. Must return a <see cref="bool"/> indicating success.</param>
    /// <typeparam name="T1">The first argument to the command.</typeparam>
    /// <typeparam name="T2">The second argument to the command.</typeparam>
    /// <returns>The added command.</returns>
    public IConsoleCommand AddCommand<T1, T2>(string name, string description, Func<bool, T1, T2> func);

    /// <summary>
    /// Add a command to the console.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">A description of the command.</param>
    /// <param name="func">Function to invoke when the command has been called. Must return a <see cref="bool"/> indicating success.</param>
    /// <typeparam name="T1">The first argument to the command.</typeparam>
    /// <typeparam name="T2">The second argument to the command.</typeparam>
    /// <typeparam name="T3">The third argument to the command.</typeparam>
    /// <returns>The added command.</returns>
    public IConsoleCommand AddCommand<T1, T2, T3>(string name, string description, Func<bool, T1, T2, T3> func);

    /// <summary>
    /// Add a command to the console.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">A description of the command.</param>
    /// <param name="func">Function to invoke when the command has been called. Must return a <see cref="bool"/> indicating success.</param>
    /// <typeparam name="T1">The first argument to the command.</typeparam>
    /// <typeparam name="T2">The second argument to the command.</typeparam>
    /// <typeparam name="T3">The third argument to the command.</typeparam>
    /// <typeparam name="T4">The fourth argument to the command.</typeparam>
    /// <returns>The added command.</returns>
    public IConsoleCommand AddCommand<T1, T2, T3, T4>(
        string name, string description, Func<bool, T1, T2, T3, T4> func);

    /// <summary>
    /// Add a command to the console.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">A description of the command.</param>
    /// <param name="func">Function to invoke when the command has been called. Must return a <see cref="bool"/> indicating success.</param>
    /// <typeparam name="T1">The first argument to the command.</typeparam>
    /// <typeparam name="T2">The second argument to the command.</typeparam>
    /// <typeparam name="T3">The third argument to the command.</typeparam>
    /// <typeparam name="T4">The fourth argument to the command.</typeparam>
    /// <typeparam name="T5">The fifth argument to the command.</typeparam>
    /// <returns>The added command.</returns>
    public IConsoleCommand AddCommand<T1, T2, T3, T4, T5>(
        string name, string description, Func<bool, T1, T2, T3, T4, T5> func);

    /// <summary>
    /// Add a variable to the console.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <param name="description">A description of the variable.</param>
    /// <param name="defaultValue">The default value of the variable.</param>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <returns>The added variable.</returns>
    public IConsoleVariable<T> AddVariable<T>(string name, string description, T defaultValue);

    /// <summary>
    /// Add an alias to a console entry.
    /// </summary>
    /// <param name="name">The name of the entry to add an alias for.</param>
    /// <param name="alias">The alias to use.</param>
    /// <returns>The added alias.</returns>
    public IConsoleEntry AddAlias(string name, string alias);

    /// <summary>
    /// Get the value of a variable.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <returns>The value of the variable.</returns>
    public T GetVariable<T>(string name);

    /// <summary>
    /// Set the value of a variable.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">The value to set.</param>
    /// <typeparam name="T">The type of the value to set.</typeparam>
    public void SetVariable<T>(string name, T value);

    /// <summary>
    /// Remove an entry from the console.
    /// </summary>
    /// <param name="entry">The entry to remove.</param>
    public void RemoveEntry(IConsoleEntry entry);
}

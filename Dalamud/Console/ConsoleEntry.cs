using System.Collections.Generic;

namespace Dalamud.Console;

/// <summary>
/// Interface representing an entry in the console.
/// </summary>
public interface IConsoleEntry
{
    /// <summary>
    /// Gets the name of the entry.
    /// </summary>
    public string Name { get; }
        
    /// <summary>
    /// Gets the description of the entry.
    /// </summary>
    public string Description { get; }
}
    
/// <summary>
/// Interface representing a command in the console.
/// </summary>
public interface IConsoleCommand : IConsoleEntry
{
    /// <summary>
    /// Execute this command.
    /// </summary>
    /// <param name="arguments">Arguments to invoke the entry with.</param>
    /// <returns>Whether or not execution succeeded.</returns>
    public bool Invoke(IEnumerable<object> arguments);
}

/// <summary>
/// Interface representing a variable in the console.
/// </summary>
/// <typeparam name="T">The type of the variable.</typeparam>
public interface IConsoleVariable<T> : IConsoleEntry
{
    /// <summary>
    /// Gets or sets the value of this variable.
    /// </summary>
    T Value { get; set; }
}

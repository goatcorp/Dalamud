using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Dalamud.Logging.Internal;

using Serilog.Events;

namespace Dalamud.Console;

// TODO: Mayhaps overloads with Func<bool, T1, T2, ...> for commands?

/// <summary>
/// Class managing console commands and variables.
/// </summary>
[ServiceManager.BlockingEarlyLoadedService("Console is needed by other blocking early loaded services.")]
internal partial class ConsoleManager : IServiceType
{
    private static readonly ModuleLog Log = new("CON");
    
    private Dictionary<string, IConsoleEntry> entries = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleManager"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    public ConsoleManager()
    {
        this.AddCommand("toggle", "Toggle a boolean variable.", this.OnToggleVariable);
    }
    
    /// <summary>
    /// Event that is triggered when a command is processed. Return true to stop the command from being processed any further.
    /// </summary>
    public event Func<string, bool>? Invoke;
    
    /// <summary>
    /// Gets a read-only dictionary of console entries.
    /// </summary>
    public IReadOnlyDictionary<string, IConsoleEntry> Entries => this.entries;
    
    /// <summary>
    /// Add a command to the console.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">A description of the command.</param>
    /// <param name="func">Function to invoke when the command has been called. Must return a <see cref="bool"/> indicating success.</param>
    /// <returns>The added command.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the command already exists.</exception>
    public IConsoleCommand AddCommand(string name, string description, Delegate func)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(func);
        
        if (this.FindEntry(name) != null)
            throw new InvalidOperationException($"Entry '{name}' already exists.");

        var command = new ConsoleCommand(name, description, func);
        this.entries.Add(name, command);
        
        return command;
    }

    /// <summary>
    /// Add a variable to the console.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <param name="description">A description of the variable.</param>
    /// <param name="defaultValue">The default value of the variable.</param>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <returns>The added variable.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the variable already exists.</exception>
    public IConsoleVariable<T> AddVariable<T>(string name, string description, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(description);
        Traits.ThrowIfTIsNullableAndNull(defaultValue);
        
        if (this.FindEntry(name) != null)
            throw new InvalidOperationException($"Entry '{name}' already exists.");

        var variable = new ConsoleVariable<T>(name, description);
        variable.Value = defaultValue;
        this.entries.Add(name, variable);
        
        return variable;
    }

    /// <summary>
    /// Add an alias to a console entry.
    /// </summary>
    /// <param name="name">The name of the entry to add an alias for.</param>
    /// <param name="alias">The alias to use.</param>
    /// <returns>The added alias.</returns>
    public IConsoleEntry AddAlias(string name, string alias)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(alias);
        
        var target = this.FindEntry(name);
        if (target == null)
            throw new EntryNotFoundException(name);
        
        if (this.FindEntry(alias) != null)
            throw new InvalidOperationException($"Entry '{alias}' already exists.");

        var aliasEntry = new ConsoleAlias(name, target);
        this.entries.Add(alias, aliasEntry);

        return aliasEntry;
    }

    /// <summary>
    /// Remove an entry from the console.
    /// </summary>
    /// <param name="entry">The entry to remove.</param>
    public void RemoveEntry(IConsoleEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!this.entries.Remove(entry.Name))
            throw new EntryNotFoundException(entry.Name);
    }

    /// <summary>
    /// Get the value of a variable.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <returns>The value of the variable.</returns>
    /// <exception cref="EntryNotFoundException">Thrown if the variable could not be found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the found console entry is not of the expected type.</exception>
    public T GetVariable<T>(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        
        var entry = this.FindEntry(name);
        
        if (entry is ConsoleVariable<T> variable)
            return variable.Value;
        
        if (entry is ConsoleVariable)
            throw new InvalidOperationException($"Variable '{name}' is not of type {typeof(T).Name}.");
        
        if (entry is null)
            throw new EntryNotFoundException(name);
        
        throw new InvalidOperationException($"Command '{name}' is not a variable.");
    }
    
    /// <summary>
    /// Set the value of a variable.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">The value to set.</param>
    /// <typeparam name="T">The type of the value to set.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown if the found console entry is not of the expected type.</exception>
    /// <exception cref="EntryNotFoundException">Thrown if the variable could not be found.</exception>
    public void SetVariable<T>(string name, T value)
    {
        ArgumentNullException.ThrowIfNull(name);
        Traits.ThrowIfTIsNullableAndNull(value);
        
        var entry = this.FindEntry(name);
        
        if (entry is ConsoleVariable<T> variable)
            variable.Value = value;
        
        if (entry is ConsoleVariable)
            throw new InvalidOperationException($"Variable '{name}' is not of type {typeof(T).Name}.");

        if (entry is null)
            throw new EntryNotFoundException(name); 
        
        throw new InvalidOperationException($"Command '{name}' is not a variable.");
    }

    /// <summary>
    /// Process a console command.
    /// </summary>
    /// <param name="command">The command to process.</param>
    /// <returns>Whether or not the command was successfully processed.</returns>
    public bool ProcessCommand(string command)
    {
        if (this.Invoke?.Invoke(command) == true)
            return true;
        
        var matches = GetCommandParsingRegex().Matches(command);
        if (matches.Count == 0)
            return false;
        
        var entryName = matches[0].Value;
        if (string.IsNullOrEmpty(entryName) || entryName.Any(char.IsWhiteSpace))
        {
            Log.Error("No valid command specified");
            return false;
        }

        var entry = this.FindEntry(entryName);
        if (entry == null)
        {
            Log.Error("Command {CommandName} not found", entryName);
            return false;
        }
        
        var parsedArguments = new List<object>();

        if (entry.ValidArguments != null)
        {
            for (var i = 1; i < matches.Count; i++)
            {
                if (i - 1 >= entry.ValidArguments.Count)
                {
                    Log.Error("Too many arguments for command {CommandName}", entryName);
                    PrintUsage(entry);
                    return false;
                }
                
                var argumentToMatch = entry.ValidArguments[i - 1];
            
                var group = matches[i];
                if (!group.Success)
                    continue;
            
                var value = group.Value;
                if (string.IsNullOrEmpty(value))
                    continue;

                switch (argumentToMatch.Type)
                {
                    case ConsoleArgumentType.String:
                        parsedArguments.Add(value);
                        break;

                    case ConsoleArgumentType.Integer when int.TryParse(value, out var intValue):
                        parsedArguments.Add(intValue);
                        break;
                    case ConsoleArgumentType.Integer:
                        Log.Error("Argument {Argument} for command {CommandName} is not an integer", value, entryName);
                        PrintUsage(entry);
                        return false;

                    case ConsoleArgumentType.Float when float.TryParse(value, out var floatValue):
                        parsedArguments.Add(floatValue);
                        break;
                    case ConsoleArgumentType.Float:
                        Log.Error("Argument {Argument} for command {CommandName} is not a float", value, entryName);
                        PrintUsage(entry);
                        return false;

                    case ConsoleArgumentType.Bool when bool.TryParse(value, out var boolValue):
                        parsedArguments.Add(boolValue);
                        break;
                    case ConsoleArgumentType.Bool:
                        Log.Error("Argument {Argument} for command {CommandName} is not a boolean", value, entryName);
                        PrintUsage(entry);
                        return false;

                    default:
                        throw new Exception("Unhandled argument type.");
                }
            }
            
            if (parsedArguments.Count != entry.ValidArguments.Count)
            {
                // Either fill in the default values or error out
                
                for (var i = parsedArguments.Count; i < entry.ValidArguments.Count; i++)
                {
                    var argument = entry.ValidArguments[i];
                    
                    // If the default value is DBNull, we need to error out as that means it was not specified
                    if (argument.DefaultValue == DBNull.Value)
                    {
                        Log.Error("Not enough arguments for command {CommandName}", entryName);
                        PrintUsage(entry);
                        return false;
                    }

                    parsedArguments.Add(argument.DefaultValue);
                }
                
                if (parsedArguments.Count != entry.ValidArguments.Count)
                {
                    Log.Error("Too many arguments for command {CommandName}", entryName);
                    PrintUsage(entry);
                    return false;
                }
            }
        }
        else
        {
            if (matches.Count > 1)
            {
                Log.Error("Command {CommandName} does not take any arguments", entryName);
                PrintUsage(entry);
                return false;
            }
        }

        return entry.Invoke(parsedArguments);
    }
    
    [GeneratedRegex("""("[^"]+"|[^\s"]+)""", RegexOptions.Compiled)]
    private static partial Regex GetCommandParsingRegex();
    
    private static void PrintUsage(ConsoleEntry entry, bool error = true)
    {
        Log.WriteLog(
            error ? LogEventLevel.Error : LogEventLevel.Information, 
            "Usage: {CommandName} {Arguments}",
            null,
            entry.Name,
            string.Join(" ", entry.ValidArguments?.Select(x => $"<{x.Type.ToString().ToLowerInvariant()}>") ?? Enumerable.Empty<string>()));
    }
    
    private ConsoleEntry? FindEntry(string name)
    {
        return this.entries.TryGetValue(name, out var entry) ? entry as ConsoleEntry : null;
    }

    private bool OnToggleVariable(string name)
    {
        if (this.FindEntry(name) is not IConsoleVariable<bool> variable)
        {
            Log.Error("Variable {VariableName} not found or not a boolean", name);
            return false;
        }

        variable.Value = !variable.Value;

        return true;
    }
    
    private static class Traits
    {
        public static void ThrowIfTIsNullableAndNull<T>(T? argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (argument == null && !typeof(T).IsValueType)
                throw new ArgumentNullException(paramName);
        }
    }

    /// <summary>
    /// Class representing an entry in the console.
    /// </summary>
    private abstract class ConsoleEntry : IConsoleEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleEntry"/> class.
        /// </summary>
        /// <param name="name">The name of the entry.</param>
        /// <param name="description">A description of the entry.</param>
        public ConsoleEntry(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public string Description { get; }
        
        /// <summary>
        /// Gets or sets a list of valid argument types for this console entry.
        /// </summary>
        public IReadOnlyList<ArgumentInfo>? ValidArguments { get; protected set; }
        
        /// <summary>
        /// Execute this command.
        /// </summary>
        /// <param name="arguments">Arguments to invoke the entry with.</param>
        /// <returns>Whether or not execution succeeded.</returns>
        public abstract bool Invoke(IEnumerable<object> arguments);

        /// <summary>
        /// Get an instance of <see cref="ArgumentInfo"/> for a given type.
        /// </summary>
        /// <param name="type">The type of the argument.</param>
        /// <param name="defaultValue">The default value to use if none is specified.</param>
        /// <returns>An <see cref="ArgumentInfo"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown if the given type cannot be handled by the console system.</exception>
        protected static ArgumentInfo TypeToArgument(Type type, object? defaultValue)
        {
            if (type == typeof(string))
                return new ArgumentInfo(ConsoleArgumentType.String, defaultValue);
            
            if (type == typeof(int))
                return new ArgumentInfo(ConsoleArgumentType.Integer, defaultValue);
            
            if (type == typeof(float))
                return new ArgumentInfo(ConsoleArgumentType.Float, defaultValue);
            
            if (type == typeof(bool))
                return new ArgumentInfo(ConsoleArgumentType.Bool, defaultValue);
            
            throw new ArgumentException($"Invalid argument type: {type.Name}");
        }
        
        public record ArgumentInfo(ConsoleArgumentType Type, object? DefaultValue);
    }

    /// <summary>
    /// Class representing an alias to another console entry.
    /// </summary>
    private class ConsoleAlias : ConsoleEntry
    {
        private readonly ConsoleEntry target;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleAlias"/> class.
        /// </summary>
        /// <param name="name">The name of the alias.</param>
        /// <param name="target">The target entry to alias to.</param>
        public ConsoleAlias(string name, ConsoleEntry target)
            : base(name, target.Description)
        {
            this.target = target;
            this.ValidArguments = target.ValidArguments;
        }

        /// <inheritdoc/>
        public override bool Invoke(IEnumerable<object> arguments)
        {
            return this.target.Invoke(arguments);
        }
    }

    /// <summary>
    /// Class representing a console command.
    /// </summary>
    private class ConsoleCommand : ConsoleEntry, IConsoleCommand
    {
        private readonly Delegate func;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleCommand"/> class.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="description">A description of the variable.</param>
        /// <param name="func">The function to invoke.</param>
        public ConsoleCommand(string name, string description, Delegate func)
            : base(name, description)
        {
            this.func = func;
            
            if (func.Method.ReturnType != typeof(bool))
                throw new ArgumentException("Console command functions must return a boolean indicating success.");
            
            var validArguments = new List<ArgumentInfo>();
            foreach (var parameterInfo in func.Method.GetParameters())
            {
                var paraT = parameterInfo.ParameterType;
                validArguments.Add(TypeToArgument(paraT, parameterInfo.DefaultValue));
            }
            
            this.ValidArguments = validArguments;
        }

        /// <inheritdoc cref="ConsoleEntry.Invoke" />
        public override bool Invoke(IEnumerable<object> arguments)
        {
            return (bool)this.func.DynamicInvoke(arguments.ToArray())!;
        }
    }

    /// <summary>
    /// Class representing a basic console variable.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <param name="description">A description of the variable.</param>
    private abstract class ConsoleVariable(string name, string description) : ConsoleEntry(name, description);

    /// <summary>
    /// Class representing a generic console variable.
    /// </summary>
    /// <typeparam name="T">The type of the variable.</typeparam>
    private class ConsoleVariable<T> : ConsoleVariable, IConsoleVariable<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleVariable{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="description">A description of the variable.</param>
        public ConsoleVariable(string name, string description)
            : base(name, description)
        {
            this.ValidArguments = new List<ArgumentInfo> { TypeToArgument(typeof(T), null) };
        }
        
        /// <inheritdoc/>
        public T Value { get; set; }

        /// <inheritdoc/>
        public override bool Invoke(IEnumerable<object> arguments)
        {
            var first = arguments.FirstOrDefault();

            if (first == null)
            {
                // Invert the value if it's a boolean
                if (this.Value is bool boolValue)
                {
                    this.Value = (T)(object)!boolValue;
                }
                
                Log.WriteLog(LogEventLevel.Information, "{VariableName} = {VariableValue}", null, this.Name, this.Value);
                return true;
            }
            
            if (first.GetType() != typeof(T))
                throw new ArgumentException($"Console variable must be set with an argument of type {typeof(T).Name}.");

            this.Value = (T)first;
            
            return true;
        }
    }
}

/// <summary>
/// Exception thrown when a console entry is not found.
/// </summary>
internal class EntryNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntryNotFoundException"/> class.
    /// </summary>
    /// <param name="name">The name of the entry.</param>
    public EntryNotFoundException(string name)
        : base($"Console entry '{name}' does not exist.")
    {
    }
}

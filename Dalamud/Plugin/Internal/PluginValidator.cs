using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Command;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Plugin.Internal;

/// <summary>
/// Class responsible for validating a dev plugin.
/// </summary>
internal static class PluginValidator
{
    /// <summary>
    /// Represents the severity of a validation problem.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// The problem is informational.
        /// </summary>
        Information,
        
        /// <summary>
        /// The problem is a warning.
        /// </summary>
        Warning,
        
        /// <summary>
        /// The problem is fatal.
        /// </summary>
        Fatal,
    }
    
    /// <summary>
    /// Represents a validation problem.
    /// </summary>
    public interface IValidationProblem
    {
        /// <summary>
        /// Gets the severity of the validation.
        /// </summary>
        public ValidationSeverity Severity { get; }

        /// <summary>
        /// Compute the localized description of the problem.
        /// </summary>
        /// <returns>Localized string to be shown to the developer.</returns>
        public string GetLocalizedDescription();
    }
    
    /// <summary>
    /// Check for problems in a plugin.
    /// </summary>
    /// <param name="plugin">The plugin to validate.</param>
    /// <returns>An list of problems.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the plugin is not loaded. A plugin must be loaded to validate it.</exception>
    public static IReadOnlyList<IValidationProblem> CheckForProblems(LocalDevPlugin plugin)
    {
        var problems = new List<IValidationProblem>();
        
        if (!plugin.IsLoaded)
            throw new InvalidOperationException("Plugin must be loaded to validate.");
        
        if (!plugin.DalamudInterface!.UiBuilder.HasConfigUi)
            problems.Add(new NoConfigUiProblem());
        
        if (!plugin.DalamudInterface.UiBuilder.HasMainUi)
            problems.Add(new NoMainUiProblem());

        var cmdManager = Service<CommandManager>.Get();
        foreach (var cmd in cmdManager.Commands.Where(x => x.Value.LoaderAssemblyName == plugin.InternalName && x.Value.ShowInHelp))
        {
            if (string.IsNullOrEmpty(cmd.Value.HelpMessage))
                problems.Add(new CommandWithoutHelpTextProblem(cmd.Key));
        }
        
        return problems;
    }

    /// <summary>
    /// Representing a problem where the plugin does not have a config UI callback.
    /// </summary>
    public class NoConfigUiProblem : IValidationProblem
    {
        /// <inheritdoc/>
        public ValidationSeverity Severity => ValidationSeverity.Warning;
        
        /// <inheritdoc/>
        public string GetLocalizedDescription() => "The plugin does register a config UI callback. If you have a settings window or section, please consider registering UiBuilder.OpenConfigUi.";
    }
    
    /// <summary>
    /// Representing a problem where the plugin does not have a main UI callback.
    /// </summary>
    public class NoMainUiProblem : IValidationProblem
    {
        /// <inheritdoc/>
        public ValidationSeverity Severity => ValidationSeverity.Warning;

        /// <inheritdoc/>
        public string GetLocalizedDescription() => "The plugin does not register a main UI callback. If your plugin draws any kind of ImGui windows, please consider registering UiBuilder.OpenMainUi to open the plugin's main window.";
    }
    
    /// <summary>
    /// Representing a problem where a command does not have a help text.
    /// </summary>
    /// <param name="commandName">Name of the command.</param>
    public class CommandWithoutHelpTextProblem(string commandName) : IValidationProblem
    {
        /// <inheritdoc/>
        public ValidationSeverity Severity => ValidationSeverity.Fatal;

        /// <inheritdoc/>
        public string GetLocalizedDescription() => $"The plugin has a command({commandName}) without a help message. Please consider adding a help message to the command when registering it.";
    }
}

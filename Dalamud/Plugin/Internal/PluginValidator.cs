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
    private static readonly char[] LineSeparator = new[] { ' ', '\n', '\r' };
    
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
        
        if (plugin.Manifest.Tags == null || plugin.Manifest.Tags.Count == 0)
            problems.Add(new NoTagsProblem());
        
        if (string.IsNullOrEmpty(plugin.Manifest.Description) || plugin.Manifest.Description.Split(LineSeparator, StringSplitOptions.RemoveEmptyEntries).Length <= 1)
            problems.Add(new NoDescriptionProblem());
        
        if (string.IsNullOrEmpty(plugin.Manifest.Punchline))
            problems.Add(new NoPunchlineProblem());
        
        if (string.IsNullOrEmpty(plugin.Manifest.Name))
            problems.Add(new NoNameProblem());
        
        if (string.IsNullOrEmpty(plugin.Manifest.Author))
            problems.Add(new NoAuthorProblem());
        
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
        public string GetLocalizedDescription() => "The plugin does not register a config UI callback. If you have a settings window or section, please consider registering UiBuilder.OpenConfigUi to open it.";
    }
    
    /// <summary>
    /// Representing a problem where the plugin does not have a main UI callback.
    /// </summary>
    public class NoMainUiProblem : IValidationProblem
    {
        /// <inheritdoc/>
        public ValidationSeverity Severity => ValidationSeverity.Warning;

        /// <inheritdoc/>
        public string GetLocalizedDescription() => "The plugin does not register a main UI callback. If your plugin has a window that could be considered the main entrypoint to its features, please consider registering UiBuilder.OpenMainUi to open the plugin's main window.";
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
        public string GetLocalizedDescription() => $"The plugin has a command ({commandName}) without a help message. Please consider adding a help message to the command when registering it.";
    }

    /// <summary>
    /// Representing a problem where a plugin does not have any tags in its manifest.
    /// </summary>
    public class NoTagsProblem : IValidationProblem
    {
        /// <inheritdoc/>
        public ValidationSeverity Severity => ValidationSeverity.Information;

        /// <inheritdoc/>
        public string GetLocalizedDescription() => "Your plugin does not have any tags in its manifest. Please consider adding some to make it easier for users to find your plugin in the installer.";
    }
    
    /// <summary>
    /// Representing a problem where a plugin does not have a description in its manifest.
    /// </summary>
    public class NoDescriptionProblem : IValidationProblem
    {
        /// <inheritdoc/>
        public ValidationSeverity Severity => ValidationSeverity.Information;

        /// <inheritdoc/>
        public string GetLocalizedDescription() => "Your plugin does not have a description in its manifest, or it is very terse. Please consider adding one to give users more information about your plugin.";
    }
    
    /// <summary>
    /// Representing a problem where a plugin has no punchline in its manifest.
    /// </summary>
    public class NoPunchlineProblem : IValidationProblem
    {
        /// <inheritdoc/>
        public ValidationSeverity Severity => ValidationSeverity.Information;

        /// <inheritdoc/>
        public string GetLocalizedDescription() => "Your plugin does not have a punchline in its manifest. Please consider adding one to give users a quick overview of what your plugin does.";
    }
    
    /// <summary>
    /// Representing a problem where a plugin has no name in its manifest.
    /// </summary>
    public class NoNameProblem : IValidationProblem
    {
        /// <inheritdoc/>
        public ValidationSeverity Severity => ValidationSeverity.Fatal;

        /// <inheritdoc/>
        public string GetLocalizedDescription() => "Your plugin does not have a name in its manifest.";
    }
    
    /// <summary>
    /// Representing a problem where a plugin has no author in its manifest.
    /// </summary>
    public class NoAuthorProblem : IValidationProblem
    {
        /// <inheritdoc/>
        public ValidationSeverity Severity => ValidationSeverity.Fatal;

        /// <inheritdoc/>
        public string GetLocalizedDescription() => "Your plugin does not have an author in its manifest.";
    }
}

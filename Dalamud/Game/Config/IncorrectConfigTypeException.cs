namespace Dalamud.Game.Config;

/// <summary>
/// An exception thrown when attempting to assign a value to a config option with the wrong type.
/// </summary>
public class IncorrectConfigTypeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IncorrectConfigTypeException"/> class.
    /// </summary>
    /// <param name="sectionName">Name of the section being accessed.</param>
    /// <param name="configOptionName">Name of the config option that was not found.</param>
    /// <param name="correctType">The correct type for the config option.</param>
    /// <param name="incorrectType">The type that was attempted.</param>
    public IncorrectConfigTypeException(string sectionName, string configOptionName, ConfigType correctType, ConfigType incorrectType)
        : base($"The option '{configOptionName}' in {sectionName} is of the type {correctType}. Assigning {incorrectType} is invalid.")
    {
    }
}

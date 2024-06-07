namespace Dalamud.Game.Config;

/// <summary>
/// An attribute for defining GameConfig options.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class GameConfigOptionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameConfigOptionAttribute"/> class.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="type">The type of the config option.</param>
    /// <param name="settable">False if the game does not take changes to the config option.</param>
    public GameConfigOptionAttribute(string name, ConfigType type, bool settable = true)
    {
        this.Name = name;
        this.Type = type;
        this.Settable = settable;
    }

    /// <summary>
    /// Gets the Name of the config option.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of the config option.
    /// </summary>
    public ConfigType Type { get; }

    /// <summary>
    /// Gets a value indicating whether the config option will update immediately or not.
    /// </summary>
    public bool Settable { get; }
}

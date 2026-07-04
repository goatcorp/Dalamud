using Dalamud.Utility;

namespace Dalamud.Game.Config;

/// <summary>
/// Represents a change in the configuration.
/// </summary>
[Api16ToDo("Remove ctor(Enum option) and Deconstruct function, which were added to not break the API when Name was added.")]
public abstract record ConfigChangeEvent
{
    /// <summary> Initializes a new instance of the <see cref="ConfigChangeEvent"/> class. </summary>
    /// <param name="option">The option that was changed.</param>
    public ConfigChangeEvent(Enum option)
    {
        this.Option = option;
        this.Name = string.Empty;
    }

    /// <summary> Initializes a new instance of the <see cref="ConfigChangeEvent"/> class. </summary>
    /// <param name="option">The option that was changed.</param>
    /// <param name="name">The name of the option that was changed.</param>
    public ConfigChangeEvent(Enum option, string name)
    {
        this.Option = option;
        this.Name = name;
    }

    /// <summary>
    /// Gets the option that was changed.
    /// </summary>
    public Enum Option { get; init; }

    /// <summary>
    /// Gets the name of the option that was changed.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Deconstructs the <see cref="ConfigChangeEvent"/> record.
    /// </summary>
    /// <param name="option">The option that was changed.</param>
    public void Deconstruct(out Enum option)
    {
        option = this.Option;
    }
}

/// <summary>
/// Represents a generic change in the configuration.
/// </summary>
/// <typeparam name="T">The type of the option.</typeparam>
[Api16ToDo("Remove ctor(T option) and Deconstruct function, which were added to not break the API when Name was added.")]
public record ConfigChangeEvent<T> : ConfigChangeEvent where T : Enum
{
    /// <summary> Initializes a new instance of the <see cref="ConfigChangeEvent{T}"/> class. </summary>
    /// <param name="option">The option that was changed.</param>
    public ConfigChangeEvent(T option)
        : base(option)
    {
        this.ConfigOption = option;
    }

    /// <summary> Initializes a new instance of the <see cref="ConfigChangeEvent{T}"/> class. </summary>
    /// <param name="option">The option that was changed.</param>
    /// <param name="name">The name of the option that was changed.</param>
    public ConfigChangeEvent(T option, string name)
        : base(option, name)
    {
        this.ConfigOption = option;
    }

    /// <summary>
    /// Gets the option that was changed.
    /// </summary>
    public T ConfigOption { get; init; }

    /// <summary>
    /// Deconstructs the <see cref="ConfigChangeEvent{T}"/> record.
    /// </summary>
    /// <param name="configOption">The option that was changed.</param>
    public void Deconstruct(out T configOption)
    {
        configOption = this.ConfigOption;
    }
}

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Textures;

namespace Dalamud.Interface;

/// <summary>
/// A interface representing an entry in the title screen menu.
/// </summary>
public interface ITitleScreenMenuEntry : IReadOnlyTitleScreenMenuEntry, IComparable<TitleScreenMenuEntry>
{
    /// <summary>
    /// Gets or sets a value indicating whether or not this entry is internal.
    /// </summary>
    bool IsInternal { get; set; }

    /// <summary>
    /// Gets the calling assembly of this entry.
    /// </summary>
    Assembly? CallingAssembly { get; init; }

    /// <summary>
    /// Gets the internal ID of this entry.
    /// </summary>
    Guid Id { get; init; }

    /// <summary>
    /// Gets the keys that have to be pressed to show the menu.
    /// </summary>
    IReadOnlySet<VirtualKey> ShowConditionKeys { get; init; }

    /// <summary>
    /// Determines the displaying condition of this menu entry is met.
    /// </summary>
    /// <returns>True if met.</returns>
    bool IsShowConditionSatisfied();

    /// <summary>
    /// Trigger the action associated with this entry.
    /// </summary>
    void Trigger();
}

/// <summary>
/// A interface representing a read only entry in the title screen menu.
/// </summary>
public interface IReadOnlyTitleScreenMenuEntry
{
    /// <summary>
    /// Gets the priority of this entry.
    /// </summary>
    ulong Priority { get; }

    /// <summary>
    /// Gets the name of this entry.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the texture of this entry.
    /// </summary>
    ISharedImmediateTexture Texture { get; }
}

/// <summary>
/// Class representing an entry in the title screen menu.
/// </summary>
public class TitleScreenMenuEntry : ITitleScreenMenuEntry
{
    private readonly Action onTriggered;

    /// <summary>
    /// Initializes a new instance of the <see cref="TitleScreenMenuEntry"/> class.
    /// </summary>
    /// <param name="callingAssembly">The calling assembly.</param>
    /// <param name="priority">The priority of this entry.</param>
    /// <param name="text">The text to show.</param>
    /// <param name="texture">The texture to show.</param>
    /// <param name="onTriggered">The action to execute when the option is selected.</param>
    /// <param name="showConditionKeys">The keys that have to be held to display the menu.</param>
    internal TitleScreenMenuEntry(
        Assembly? callingAssembly,
        ulong priority,
        string text,
        ISharedImmediateTexture texture,
        Action onTriggered,
        IEnumerable<VirtualKey>? showConditionKeys = null)
    {
        this.CallingAssembly = callingAssembly;
        this.Priority = priority;
        this.Name = text;
        this.Texture = texture;
        this.onTriggered = onTriggered;
        this.ShowConditionKeys = (showConditionKeys ?? Array.Empty<VirtualKey>()).ToImmutableSortedSet();
    }

    /// <inheritdoc/>
    public ulong Priority { get; init; }

    /// <inheritdoc/>
    public string Name { get; set; }

    /// <inheritdoc/>
    public ISharedImmediateTexture Texture { get; set; }

    /// <inheritdoc/>
    public bool IsInternal { get; set; }

    /// <inheritdoc/>
    public Assembly? CallingAssembly { get; init; }

    /// <inheritdoc/>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    public IReadOnlySet<VirtualKey> ShowConditionKeys { get; init; }

    /// <inheritdoc/>
    public int CompareTo(TitleScreenMenuEntry? other)
    {
        if (other == null)
            return 1;
        if (this.CallingAssembly != other.CallingAssembly)
        {
            if (this.CallingAssembly == null && other.CallingAssembly == null)
                return 0;
            if (this.CallingAssembly == null && other.CallingAssembly != null)
                return -1;
            if (this.CallingAssembly != null && other.CallingAssembly == null)
                return 1;
            return string.Compare(
                this.CallingAssembly!.FullName!,
                other.CallingAssembly!.FullName!,
                StringComparison.CurrentCultureIgnoreCase);
        }

        if (this.Priority != other.Priority)
            return this.Priority.CompareTo(other.Priority);
        if (this.Name != other.Name)
            return string.Compare(this.Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
        return 0;
    }

    /// <summary>
    /// Determines the displaying condition of this menu entry is met.
    /// </summary>
    /// <returns>True if met.</returns>
    public bool IsShowConditionSatisfied() =>
        this.ShowConditionKeys.All(x => Service<KeyState>.GetNullable()?[x] is true);

    /// <summary>
    /// Trigger the action associated with this entry.
    /// </summary>
    public void Trigger()
    {
        this.onTriggered();
    }
}

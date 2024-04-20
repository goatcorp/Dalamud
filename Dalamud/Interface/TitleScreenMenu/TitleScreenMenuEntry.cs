using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Internal;

namespace Dalamud.Interface;

/// <summary>
/// Class representing an entry in the title screen menu.
/// </summary>
public class TitleScreenMenuEntry : IComparable<TitleScreenMenuEntry>
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
        IDalamudTextureWrap texture,
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

    /// <summary>
    /// Gets the priority of this entry.
    /// </summary>
    public ulong Priority { get; init; }

    /// <summary>
    /// Gets or sets the name of this entry.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the texture of this entry.
    /// </summary>
    public IDalamudTextureWrap Texture { get; set; }
        
    /// <summary>
    /// Gets or sets a value indicating whether or not this entry is internal.
    /// </summary>
    internal bool IsInternal { get; set; }

    /// <summary>
    /// Gets the calling assembly of this entry.
    /// </summary>
    internal Assembly? CallingAssembly { get; init; }

    /// <summary>
    /// Gets the internal ID of this entry.
    /// </summary>
    internal Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the keys that have to be pressed to show the menu.
    /// </summary>
    internal IReadOnlySet<VirtualKey> ShowConditionKeys { get; init; }

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
    internal bool IsShowConditionSatisfied() =>
        this.ShowConditionKeys.All(x => Service<KeyState>.GetNullable()?[x] is true);

    /// <summary>
    /// Trigger the action associated with this entry.
    /// </summary>
    internal void Trigger()
    {
        this.onTriggered();
    }
}

using System.Collections.Generic;

using Dalamud.Game.ClientState.Fates;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This collection represents the currently available Fate events.
/// </summary>
public interface IFateTable : IDalamudService, IReadOnlyCollection<IFate>
{
    /// <summary>
    /// Gets the address of the Fate table.
    /// </summary>
    public nint Address { get; }
    
    /// <summary>
    /// Gets the amount of currently active Fates.
    /// </summary>
    public int Length { get; }
    
    /// <summary>
    /// Get an actor at the specified spawn index.
    /// </summary>
    /// <param name="index">Spawn index.</param>
    /// <returns>A <see cref="Fate"/> at the specified spawn index.</returns>
    public IFate? this[int index] { get; }

    /// <summary>
    /// Gets a value indicating whether this Fate is still valid in memory.
    /// </summary>
    /// <param name="fate">The fate to check.</param>
    /// <returns>True or false.</returns>
    public bool IsValid(IFate fate);

    /// <summary>
    /// Gets the address of the Fate at the specified index of the fate table.
    /// </summary>wo
    /// <param name="index">The index of the Fate.</param>
    /// <returns>The memory address of the Fate.</returns>
    public nint GetFateAddress(int index);

    /// <summary>
    /// Create a reference to a FFXIV actor.
    /// </summary>
    /// <param name="offset">The offset of the actor in memory.</param>
    /// <returns><see cref="Fate"/> object containing requested data.</returns>
    public IFate? CreateFateReference(nint offset);
}

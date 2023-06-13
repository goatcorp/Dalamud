using System.Collections.Generic;

using Dalamud.Game.ClientState.Aetherytes;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This collection represents the list of available Aetherytes in the Teleport window.
/// </summary>
public interface IAetheryteList : IReadOnlyCollection<AetheryteEntry>
{
    /// <summary>
    /// Gets the amount of Aetherytes the local player has unlocked.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets a Aetheryte Entry at the specified index.
    /// </summary>
    /// <param name="index">Index.</param>
    /// <returns>A <see cref="AetheryteEntry"/> at the specified index.</returns>
    public AetheryteEntry? this[int index] { get; }
}

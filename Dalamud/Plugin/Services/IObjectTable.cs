﻿using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Types;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This collection represents the currently spawned FFXIV game objects.
/// </summary>
public interface IObjectTable : IEnumerable<IGameObject>
{
    /// <summary>
    /// Gets the address of the object table.
    /// </summary>
    public nint Address { get; }

    /// <summary>
    /// Gets the length of the object table.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Get an object at the specified spawn index.
    /// </summary>
    /// <param name="index">Spawn index.</param>
    /// <returns>An <see cref="GameObject"/> at the specified spawn index.</returns>
    public IGameObject? this[int index] { get; }

    /// <summary>
    /// Search for a game object by their Object ID.
    /// </summary>
    /// <param name="gameObjectId">Object ID to find.</param>
    /// <returns>A game object or null.</returns>
    public IGameObject? SearchById(ulong gameObjectId);

    /// <summary>
    /// Search for a game object by the Entity ID.
    /// </summary>
    /// <param name="entityId">Entity ID to find.</param>
    /// <returns>A game object or null.</returns>
    public IGameObject? SearchByEntityId(uint entityId);

    /// <summary>
    /// Gets the address of the game object at the specified index of the object table.
    /// </summary>
    /// <param name="index">The index of the object.</param>
    /// <returns>The memory address of the object.</returns>
    public nint GetObjectAddress(int index);

    /// <summary>
    /// Create a reference to an FFXIV game object.
    /// </summary>
    /// <param name="address">The address of the object in memory.</param>
    /// <returns><see cref="GameObject"/> object or inheritor containing the requested data.</returns>
    public IGameObject? CreateObjectReference(nint address);
}

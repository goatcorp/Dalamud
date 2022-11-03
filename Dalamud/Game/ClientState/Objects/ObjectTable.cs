using System;
using System.Collections;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.ClientState.Objects;

/// <summary>
/// This collection represents the currently spawned FFXIV game objects.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
public sealed partial class ObjectTable : IServiceType
{
    private const int ObjectTableLength = 596;

    private readonly ClientStateAddressResolver address;

    [ServiceManager.ServiceConstructor]
    private ObjectTable(ClientState clientState)
    {
        this.address = clientState.AddressResolver;

        Log.Verbose($"Object table address 0x{this.address.ObjectTable.ToInt64():X}");
    }

    /// <summary>
    /// Gets the address of the object table.
    /// </summary>
    public IntPtr Address => this.address.ObjectTable;

    /// <summary>
    /// Gets the length of the object table.
    /// </summary>
    public int Length => ObjectTableLength;

    /// <summary>
    /// Get an object at the specified spawn index.
    /// </summary>
    /// <param name="index">Spawn index.</param>
    /// <returns>An <see cref="GameObject"/> at the specified spawn index.</returns>
    public GameObject? this[int index]
    {
        get
        {
            var address = this.GetObjectAddress(index);
            return this.CreateObjectReference(address);
        }
    }

    /// <summary>
    /// Search for a game object by their Object ID.
    /// </summary>
    /// <param name="objectId">Object ID to find.</param>
    /// <returns>A game object or null.</returns>
    public GameObject? SearchById(uint objectId)
    {
        if (objectId is GameObject.InvalidGameObjectId or 0)
            return null;

        foreach (var obj in this)
        {
            if (obj == null)
                continue;

            if (obj.ObjectId == objectId)
                return obj;
        }

        return null;
    }

    /// <summary>
    /// Gets the address of the game object at the specified index of the object table.
    /// </summary>
    /// <param name="index">The index of the object.</param>
    /// <returns>The memory address of the object.</returns>
    public unsafe IntPtr GetObjectAddress(int index)
    {
        if (index < 0 || index >= ObjectTableLength)
            return IntPtr.Zero;

        return *(IntPtr*)(this.address.ObjectTable + (8 * index));
    }

    /// <summary>
    /// Create a reference to an FFXIV game object.
    /// </summary>
    /// <param name="address">The address of the object in memory.</param>
    /// <returns><see cref="GameObject"/> object or inheritor containing the requested data.</returns>
    public unsafe GameObject? CreateObjectReference(IntPtr address)
    {
        var clientState = Service<ClientState>.GetNullable();

        if (clientState == null || clientState.LocalContentId == 0)
            return null;

        if (address == IntPtr.Zero)
            return null;

        var obj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)address;
        var objKind = (ObjectKind)obj->ObjectKind;
        return objKind switch
        {
            ObjectKind.Player => new PlayerCharacter(address),
            ObjectKind.BattleNpc => new BattleNpc(address),
            ObjectKind.EventObj => new EventObj(address),
            ObjectKind.Companion => new Npc(address),
            _ => new GameObject(address),
        };
    }
}

/// <summary>
/// This collection represents the currently spawned FFXIV game objects.
/// </summary>
public sealed partial class ObjectTable : IReadOnlyCollection<GameObject>
{
    /// <inheritdoc/>
    int IReadOnlyCollection<GameObject>.Count => this.Length;

    /// <inheritdoc/>
    public IEnumerator<GameObject> GetEnumerator()
    {
        for (var i = 0; i < ObjectTableLength; i++)
        {
            var obj = this[i];

            if (obj == null)
                continue;

            yield return obj;
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}

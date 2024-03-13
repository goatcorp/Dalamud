using System;
using System.Collections;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using Serilog;

namespace Dalamud.Game.ClientState.Objects;

/// <summary>
/// This collection represents the currently spawned FFXIV game objects.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IObjectTable>]
#pragma warning restore SA1015
internal sealed partial class ObjectTable : IServiceType, IObjectTable
{
    private const int ObjectTableLength = 599;

    private readonly ClientState clientState;
    private GameObject?[] cachedObjectTable = new GameObject?[ObjectTableLength];

    [ServiceManager.ServiceConstructor]
    private ObjectTable(ClientState clientState, Framework framework)
    {
        this.clientState = clientState;

        framework.Update += this.FrameworkOnUpdate;

        Log.Verbose($"Object table address 0x{this.clientState.AddressResolver.ObjectTable.ToInt64():X}");
    }

    /// <inheritdoc/>
    public IntPtr Address => this.clientState.AddressResolver.ObjectTable;

    /// <inheritdoc/>
    public int Length => ObjectTableLength;

    /// <inheritdoc/>
    public GameObject? this[int index]
    {
        get
        {
            if (index >= ObjectTableLength || index < 0)
                return null;

            return this.cachedObjectTable[index];
        }
    }

    /// <inheritdoc/>
    public GameObject? SearchById(ulong objectId)
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

    /// <inheritdoc/>
    public unsafe IntPtr GetObjectAddress(int index)
    {
        if (index < 0 || index >= ObjectTableLength)
            return IntPtr.Zero;

        return *(IntPtr*)(this.clientState.AddressResolver.ObjectTable + (8 * index));
    }

    /// <inheritdoc/>
    public unsafe GameObject? CreateObjectReference(IntPtr address)
    {
        if (this.clientState.LocalContentId == 0)
            return null;

        if (address == IntPtr.Zero)
            return null;

        var obj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)address;
        var objKind = (ObjectKind)obj->ObjectKind;
        return objKind switch
        {
            ObjectKind.Player => new PlayerCharacter(address),
            ObjectKind.BattleNpc => new BattleNpc(address),
            ObjectKind.EventNpc => new Npc(address),
            ObjectKind.Retainer => new Npc(address),
            ObjectKind.EventObj => new EventObj(address),
            ObjectKind.Companion => new Npc(address),
            ObjectKind.MountType => new Npc(address),
            ObjectKind.Ornament => new Npc(address),
            _ => new GameObject(address),
        };
    }

    private void FrameworkOnUpdate(IFramework framework)
    {
        for (var i = 0; i < ObjectTableLength; i++)
        {
            this.cachedObjectTable[i] = this.CreateObjectReference(this.GetObjectAddress(i));
        }
    }
}

/// <summary>
/// This collection represents the currently spawned FFXIV game objects.
/// </summary>
internal sealed partial class ObjectTable
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

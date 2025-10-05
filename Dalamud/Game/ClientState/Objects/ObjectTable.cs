using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.Interop;

using Microsoft.Extensions.ObjectPool;

using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using CSGameObjectManager = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObjectManager;

namespace Dalamud.Game.ClientState.Objects;

/// <summary>
/// This collection represents the currently spawned FFXIV game objects.
/// </summary>
[PluginInterface]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IObjectTable>]
#pragma warning restore SA1015
internal sealed partial class ObjectTable : IServiceType, IObjectTable
{
    private static int objectTableLength;

    [ServiceManager.ServiceDependency]
    private readonly PlayerState.PlayerState playerState = Service<PlayerState.PlayerState>.Get();

    private readonly CachedEntry[] cachedObjectTable;

    private readonly Enumerator?[] frameworkThreadEnumerators = new Enumerator?[4];

    [ServiceManager.ServiceConstructor]
    private unsafe ObjectTable()
    {
        var nativeObjectTable = CSGameObjectManager.Instance()->Objects.IndexSorted;
        objectTableLength = nativeObjectTable.Length;

        this.cachedObjectTable = new CachedEntry[objectTableLength];
        for (var i = 0; i < this.cachedObjectTable.Length; i++)
            this.cachedObjectTable[i] = new(nativeObjectTable.GetPointer(i));

        for (var i = 0; i < this.frameworkThreadEnumerators.Length; i++)
            this.frameworkThreadEnumerators[i] = new(this, i);
    }

    /// <inheritdoc/>
    public unsafe nint Address
    {
        get
        {
            ThreadSafety.AssertMainThread();

            return (nint)(&CSGameObjectManager.Instance()->Objects);
        }
    }

    /// <inheritdoc/>
    public int Length => objectTableLength;

    /// <inheritdoc/>
    public IPlayerCharacter? LocalPlayer => this[0] as IPlayerCharacter;

    /// <inheritdoc/>
    public IEnumerable<IBattleChara> PlayerObjects => this.GetPlayerObjects();

    /// <inheritdoc/>
    public IEnumerable<IGameObject> CharacterManagerObjects => this.GetObjectsInRange(..199);

    /// <inheritdoc/>
    public IEnumerable<IGameObject> ClientObjects => this.GetObjectsInRange(200..448);

    /// <inheritdoc/>
    public IEnumerable<IGameObject> EventObjects => this.GetObjectsInRange(449..488);

    /// <inheritdoc/>
    public IEnumerable<IGameObject> StandObjects => this.GetObjectsInRange(489..628);

    /// <inheritdoc/>
    public IEnumerable<IGameObject> ReactionEventObjects => this.GetObjectsInRange(629..728);

    /// <inheritdoc/>
    public IGameObject? this[int index]
    {
        get
        {
            ThreadSafety.AssertMainThread();

            return (index >= objectTableLength || index < 0) ? null : this.cachedObjectTable[index].Update();
        }
    }

    /// <inheritdoc/>
    public IGameObject? SearchById(ulong gameObjectId)
    {
        ThreadSafety.AssertMainThread();

        if (gameObjectId is 0)
            return null;

        foreach (var e in this.cachedObjectTable)
        {
            if (e.Update() is { } o && o.GameObjectId == gameObjectId)
                return o;
        }

        return null;
    }

    /// <inheritdoc/>
    public IGameObject? SearchByEntityId(uint entityId)
    {
        ThreadSafety.AssertMainThread();

        if (entityId is 0 or 0xE0000000)
            return null;

        foreach (var e in this.cachedObjectTable)
        {
            if (e.Update() is { } o && o.EntityId == entityId)
                return o;
        }

        return null;
    }

    /// <inheritdoc/>
    public unsafe nint GetObjectAddress(int index)
    {
        ThreadSafety.AssertMainThread();

        return (index >= objectTableLength || index < 0) ? nint.Zero : (nint)this.cachedObjectTable[index].Address;
    }

    /// <inheritdoc/>
    public unsafe IGameObject? CreateObjectReference(nint address)
    {
        ThreadSafety.AssertMainThread();

        if (address == nint.Zero)
            return null;

        if (!this.playerState.IsLoaded)
            return null;

        var obj = (CSGameObject*)address;
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

    private IEnumerable<IBattleChara> GetPlayerObjects()
    {
        for (var index = 0; index < 200; index += 2)
        {
            if (this[index] is IBattleChara { ObjectKind: ObjectKind.Player } gameObject)
            {
                yield return gameObject;
            }
        }
    }

    private IEnumerable<IGameObject> GetObjectsInRange(Range range)
    {
        for (var index = range.Start.Value; index <= range.End.Value; index++)
        {
            if (this[index] is { } gameObject)
            {
                yield return gameObject;
            }
        }
    }

    /// <summary>Stores an object table entry, with preallocated concrete types.</summary>
    /// <remarks>Initializes a new instance of the <see cref="CachedEntry"/> struct.</remarks>
    /// <param name="gameObjectPtr">A pointer to the object table entry this entry should be pointing to.</param>
    internal readonly unsafe struct CachedEntry(Pointer<CSGameObject>* gameObjectPtr)
    {
        private readonly PlayerCharacter playerCharacter = new(nint.Zero);
        private readonly BattleNpc battleNpc = new(nint.Zero);
        private readonly Npc npc = new(nint.Zero);
        private readonly EventObj eventObj = new(nint.Zero);
        private readonly GameObject gameObject = new(nint.Zero);

        /// <summary>Gets the address of the underlying native object. May be null.</summary>
        public CSGameObject* Address
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => gameObjectPtr->Value;
        }

        /// <summary>Updates and gets the wrapped game object pointed by this struct.</summary>
        /// <returns>The pointed object, or <c>null</c> if no object exists at that slot.</returns>
        public GameObject? Update()
        {
            var address = this.Address;
            if (address is null)
                return null;

            var activeObject = (ObjectKind)address->ObjectKind switch
            {
                ObjectKind.Player => this.playerCharacter,
                ObjectKind.BattleNpc => this.battleNpc,
                ObjectKind.EventNpc => this.npc,
                ObjectKind.Retainer => this.npc,
                ObjectKind.EventObj => this.eventObj,
                ObjectKind.Companion => this.npc,
                ObjectKind.MountType => this.npc,
                ObjectKind.Ornament => this.npc,
                _ => this.gameObject,
            };
            activeObject.Address = (nint)address;
            return activeObject;
        }
    }
}

/// <summary>
/// This collection represents the currently spawned FFXIV game objects.
/// </summary>
internal sealed partial class ObjectTable
{
    /// <inheritdoc/>
    public IEnumerator<IGameObject> GetEnumerator()
    {
        ThreadSafety.AssertMainThread();

        // If we're on the framework thread, see if there's an already allocated enumerator available for use.
        foreach (ref var x in this.frameworkThreadEnumerators.AsSpan())
        {
            if (x is not null)
            {
                var t = x;
                x = null;
                t.Reset();
                return t;
            }
        }

        // No reusable enumerator is available; allocate a new temporary one.
        return new Enumerator(this, -1);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private sealed class Enumerator(ObjectTable owner, int slotId) : IEnumerator<IGameObject>, IResettable
    {
        private ObjectTable? owner = owner;

        private int index = -1;

        public IGameObject Current { get; private set; } = null!;

        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            if (this.index == objectTableLength)
                return false;

            var cache = this.owner!.cachedObjectTable.AsSpan();
            for (this.index++; this.index < objectTableLength; this.index++)
            {
                if (cache[this.index].Update() is { } ao)
                {
                    this.Current = ao;
                    return true;
                }
            }

            return false;
        }

        public void Reset() => this.index = -1;

        public void Dispose()
        {
            if (this.owner is not { } o)
                return;

            if (slotId != -1)
                o.frameworkThreadEnumerators[slotId] = this;
        }

        public bool TryReset()
        {
            this.Reset();
            return true;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Microsoft.Extensions.ObjectPool;

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
    private readonly CachedEntry[] cachedObjectTable = new CachedEntry[ObjectTableLength];

    private readonly ObjectPool<Enumerator> multiThreadedEnumerators =
        new DefaultObjectPoolProvider().Create<Enumerator>();

    private readonly Enumerator?[] frameworkThreadEnumerators = new Enumerator?[64];

    private long nextMultithreadedUsageWarnTime;

    [ServiceManager.ServiceConstructor]
    private ObjectTable(ClientState clientState)
    {
        this.clientState = clientState;
        foreach (ref var e in this.cachedObjectTable.AsSpan())
            e = CachedEntry.CreateNew();
        for (var i = 0; i < this.frameworkThreadEnumerators.Length; i++)
            this.frameworkThreadEnumerators[i] = new(this, i);

        Log.Verbose($"Object table address 0x{this.clientState.AddressResolver.ObjectTable.ToInt64():X}");
    }

    /// <inheritdoc/>
    public nint Address
    {
        get
        {
            _ = this.WarnMultithreadedUsage();

            return this.clientState.AddressResolver.ObjectTable;
        }
    }

    /// <inheritdoc/>
    public int Length => ObjectTableLength;

    /// <inheritdoc/>
    public GameObject? this[int index]
    {
        get
        {
            _ = this.WarnMultithreadedUsage();

            if (index is >= ObjectTableLength or < 0) return null;
            this.cachedObjectTable[index].Update(this.GetObjectAddressUnsafe(index));
            return this.cachedObjectTable[index].ActiveObject;
        }
    }

    /// <inheritdoc/>
    public GameObject? SearchById(ulong objectId)
    {
        _ = this.WarnMultithreadedUsage();

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
    public nint GetObjectAddress(int index)
    {
        _ = this.WarnMultithreadedUsage();

        return index is < 0 or >= ObjectTableLength ? nint.Zero : this.GetObjectAddressUnsafe(index);
    }

    /// <inheritdoc/>
    public unsafe GameObject? CreateObjectReference(nint address)
    {
        _ = this.WarnMultithreadedUsage();

        if (this.clientState.LocalContentId == 0)
            return null;

        if (address == nint.Zero)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool WarnMultithreadedUsage()
    {
        if (ThreadSafety.IsMainThread)
            return false;

        var n = Environment.TickCount64;
        if (this.nextMultithreadedUsageWarnTime < n)
        {
            this.nextMultithreadedUsageWarnTime = n + 30000;

            Log.Warning(
                "{plugin} is accessing {objectTable} outside the main thread. This is deprecated.",
                Service<PluginManager>.Get().FindCallingPlugin()?.Name ?? "<unknown plugin>",
                nameof(ObjectTable));
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe nint GetObjectAddressUnsafe(int index) =>
        *(nint*)(this.clientState.AddressResolver.ObjectTable + (8 * index));

    private struct CachedEntry
    {
        public GameObject? ActiveObject;
        public PlayerCharacter PlayerCharacter;
        public BattleNpc BattleNpc;
        public Npc Npc;
        public EventObj EventObj;
        public GameObject GameObject;

        public static CachedEntry CreateNew() =>
            new()
            {
                PlayerCharacter = new(nint.Zero),
                BattleNpc = new(nint.Zero),
                Npc = new(nint.Zero),
                EventObj = new(nint.Zero),
                GameObject = new(nint.Zero),
            };

        public unsafe void Update(nint address)
        {
            if (this.ActiveObject != null && address == this.ActiveObject.Address)
                return;

            if (address == nint.Zero)
            {
                this.ActiveObject = null;
                return;
            }

            var obj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)address;
            var objKind = (ObjectKind)obj->ObjectKind;
            var activeObject = objKind switch
            {
                ObjectKind.Player => this.PlayerCharacter,
                ObjectKind.BattleNpc => this.BattleNpc,
                ObjectKind.EventNpc => this.Npc,
                ObjectKind.Retainer => this.Npc,
                ObjectKind.EventObj => this.EventObj,
                ObjectKind.Companion => this.Npc,
                ObjectKind.MountType => this.Npc,
                ObjectKind.Ornament => this.Npc,
                _ => this.GameObject,
            };
            activeObject.Address = address;
            this.ActiveObject = activeObject;
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
        if (this.WarnMultithreadedUsage())
        {
            // let's not
            var e = this.multiThreadedEnumerators.Get();
            e.InitializeForPooledObjects(this);
            return e;
        }

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

        return new Enumerator(this, -1);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private sealed class Enumerator : IEnumerator<GameObject>, IResettable
    {
        private readonly int slotId;
        private ObjectTable? owner;

        private int index = -1;

        public Enumerator() => this.slotId = -1;

        public Enumerator(ObjectTable owner, int slotId)
        {
            this.owner = owner;
            this.slotId = slotId;
        }

        public GameObject Current { get; private set; } = null!;

        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            if (this.index == ObjectTableLength)
                return false;

            var cache = this.owner!.cachedObjectTable.AsSpan();
            for (this.index++; this.index < ObjectTableLength; this.index++)
            {
                this.owner!.cachedObjectTable[this.index].Update(this.owner!.GetObjectAddressUnsafe(this.index));
                
                if (cache[this.index].ActiveObject is { } ao)
                {
                    this.Current = ao;
                    return true;
                }
            }

            return false;
        }

        public void InitializeForPooledObjects(ObjectTable ot) => this.owner = ot;

        public void Reset() => this.index = -1;

        public void Dispose()
        {
            if (this.owner is not { } o)
                return;

            if (this.index == -1)
                o.multiThreadedEnumerators.Return(this);
            else
                o.frameworkThreadEnumerators[this.slotId] = this;
        }

        public bool TryReset()
        {
            this.Reset();
            return true;
        }
    }
}

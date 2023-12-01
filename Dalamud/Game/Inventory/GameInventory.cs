using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Inventory.InventoryChangeArgsTypes;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;

using Serilog.Events;

namespace Dalamud.Game.Inventory;

/// <summary>
/// This class provides events for the players in-game inventory.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
internal class GameInventory : IDisposable, IServiceType, IGameInventory
{
    private static readonly ModuleLog Log = new(nameof(GameInventory));

    private readonly List<InventoryItemAddedArgs> addedEvents = new();
    private readonly List<InventoryItemRemovedArgs> removedEvents = new();
    private readonly List<InventoryItemChangedArgs> changedEvents = new();
    private readonly List<InventoryItemMovedArgs> movedEvents = new();
    private readonly List<InventoryItemSplitArgs> splitEvents = new();
    private readonly List<InventoryItemMergedArgs> mergedEvents = new();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration dalamudConfiguration = Service<DalamudConfiguration>.Get();

    private readonly GameInventoryType[] inventoryTypes;
    private readonly GameInventoryItem[]?[] inventoryItems;

    [ServiceManager.ServiceConstructor]
    private GameInventory()
    {
        this.inventoryTypes = Enum.GetValues<GameInventoryType>();
        this.inventoryItems = new GameInventoryItem[this.inventoryTypes.Length][];

        this.framework.Update += this.OnFrameworkUpdate;

        // Separate log logic as an event handler.
        this.InventoryChanged += events =>
        {
            if (this.dalamudConfiguration.LogLevel > LogEventLevel.Verbose)
                return;

            foreach (var e in events)
            {
                if (e is InventoryComplexEventArgs icea)
                    Log.Verbose($"{icea}\n\t├ {icea.SourceEvent}\n\t└ {icea.TargetEvent}");
                else
                    Log.Verbose($"{e}");
            }
        };
    }

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangelogDelegate? InventoryChanged;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangelogDelegate? InventoryChangedRaw;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemAdded;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemRemoved;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemChanged;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemMoved;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemSplit;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemMerged;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.framework.Update -= this.OnFrameworkUpdate;
    }

    /// <summary>
    /// Gets a <see cref="Span{T}"/> view of <see cref="InventoryItem"/>s, wrapped as <see cref="GameInventoryItem"/>.
    /// </summary>
    /// <param name="type">The inventory type.</param>
    /// <returns>The span.</returns>
    private static unsafe ReadOnlySpan<GameInventoryItem> GetItemsForInventory(GameInventoryType type)
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager is null) return default;

        var inventory = inventoryManager->GetInventoryContainer((InventoryType)type);
        if (inventory is null) return default;

        return new ReadOnlySpan<GameInventoryItem>(inventory->Items, (int)inventory->Size);
    }

    private static void InvokeSafely(
        IGameInventory.InventoryChangelogDelegate? cb,
        IReadOnlyCollection<InventoryEventArgs> data)
    {
        try
        {
            cb?.Invoke(data);
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception during batch callback");
        }
    }

    private static void InvokeSafely(IGameInventory.InventoryChangedDelegate? cb, InventoryEventArgs arg)
    {
        try
        {
            cb?.Invoke(arg.Type, arg);
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception during {argType} callback", arg.Type);
        }
    }

    private void OnFrameworkUpdate(IFramework framework1)
    {
        for (var i = 0; i < this.inventoryTypes.Length; i++)
        {
            var newItems = GetItemsForInventory(this.inventoryTypes[i]);
            if (newItems.IsEmpty)
                continue;

            // Assumption: newItems is sorted by slots, and the last item has the highest slot number.
            var oldItems = this.inventoryItems[i] ??= new GameInventoryItem[newItems[^1].InternalItem.Slot + 1];

            foreach (ref readonly var newItem in newItems)
            {
                ref var oldItem = ref oldItems[newItem.InternalItem.Slot];

                if (oldItem.IsEmpty)
                {
                    if (!newItem.IsEmpty)
                    {
                        this.addedEvents.Add(new(newItem));
                        oldItem = newItem;
                    }
                }
                else
                {
                    if (newItem.IsEmpty)
                    {
                        this.removedEvents.Add(new(oldItem));
                        oldItem = newItem;
                    }
                    else if (!oldItem.Equals(newItem))
                    {
                        this.changedEvents.Add(new(oldItem, newItem));
                        oldItem = newItem;
                    }
                }
            }
        }

        // Was there any change? If not, stop further processing.
        // Note that only these three are checked; the rest will be populated after this check.
        if (this.addedEvents.Count == 0 && this.removedEvents.Count == 0 && this.changedEvents.Count == 0)
            return;

        // Broadcast InventoryChangedRaw.
        // Same reason with the above on why are there 3 lists of events involved.
        InvokeSafely(
            this.InventoryChangedRaw,
            new DeferredReadOnlyCollection<InventoryEventArgs>(
                this.addedEvents.Count +
                this.removedEvents.Count +
                this.changedEvents.Count,
                () => Array.Empty<InventoryEventArgs>()
                           .Concat(this.addedEvents)
                           .Concat(this.removedEvents)
                           .Concat(this.changedEvents)));

        // Resolve moved items, from 1 added + 1 removed event.
        for (var iAdded = this.addedEvents.Count - 1; iAdded >= 0; --iAdded)
        {
            var added = this.addedEvents[iAdded];
            for (var iRemoved = this.removedEvents.Count - 1; iRemoved >= 0; --iRemoved)
            {
                var removed = this.removedEvents[iRemoved];
                if (added.Item.ItemId != removed.Item.ItemId)
                    continue;

                this.movedEvents.Add(new(removed, added));

                // Remove the reinterpreted entries.
                this.addedEvents.RemoveAt(iAdded);
                this.removedEvents.RemoveAt(iRemoved);
                break;
            }
        }

        // Resolve moved items, from 2 changed events.
        for (var i = this.changedEvents.Count - 1; i >= 0; --i)
        {
            var e1 = this.changedEvents[i];
            for (var j = i - 1; j >= 0; --j)
            {
                var e2 = this.changedEvents[j];
                if (e1.Item.ItemId != e2.OldItemState.ItemId || e1.OldItemState.ItemId != e2.Item.ItemId)
                    continue;

                // Move happened, and e2 has an item.
                if (!e2.Item.IsEmpty)
                    this.movedEvents.Add(new(e1, e2));

                // Move happened, and e1 has an item.
                if (!e1.Item.IsEmpty)
                    this.movedEvents.Add(new(e2, e1));

                // Remove the reinterpreted entries. Note that i > j.
                this.changedEvents.RemoveAt(i);
                this.changedEvents.RemoveAt(j);

                // We've removed two. Adjust the outer counter.
                --i;
                break;
            }
        }

        // Resolve split items, from 1 added + 1 changed event.
        for (var iAdded = this.addedEvents.Count - 1; iAdded >= 0; --iAdded)
        {
            var added = this.addedEvents[iAdded];
            for (var iChanged = this.changedEvents.Count - 1; iChanged >= 0; --iChanged)
            {
                var changed = this.changedEvents[iChanged];
                if (added.Item.ItemId != changed.Item.ItemId || added.Item.ItemId != changed.OldItemState.ItemId)
                    continue;

                this.splitEvents.Add(new(changed, added));

                // Remove the reinterpreted entries.
                this.addedEvents.RemoveAt(iAdded);
                this.changedEvents.RemoveAt(iChanged);
                break;
            }
        }

        // Resolve merged items, from 1 removed + 1 changed event.
        for (var iRemoved = this.removedEvents.Count - 1; iRemoved >= 0; --iRemoved)
        {
            var removed = this.removedEvents[iRemoved];
            for (var iChanged = this.changedEvents.Count - 1; iChanged >= 0; --iChanged)
            {
                var changed = this.changedEvents[iChanged];
                if (removed.Item.ItemId != changed.Item.ItemId || removed.Item.ItemId != changed.OldItemState.ItemId)
                    continue;

                this.mergedEvents.Add(new(removed, changed));

                // Remove the reinterpreted entries.
                this.removedEvents.RemoveAt(iRemoved);
                this.changedEvents.RemoveAt(iChanged);
                break;
            }
        }

        // Broadcast the rest.
        InvokeSafely(
            this.InventoryChanged,
            new DeferredReadOnlyCollection<InventoryEventArgs>(
                this.addedEvents.Count +
                this.removedEvents.Count +
                this.changedEvents.Count +
                this.movedEvents.Count +
                this.splitEvents.Count +
                this.mergedEvents.Count,
                () => Array.Empty<InventoryEventArgs>()
                           .Concat(this.addedEvents)
                           .Concat(this.removedEvents)
                           .Concat(this.changedEvents)
                           .Concat(this.movedEvents)
                           .Concat(this.splitEvents)
                           .Concat(this.mergedEvents)));

        foreach (var x in this.addedEvents)
            InvokeSafely(this.ItemAdded, x);

        foreach (var x in this.removedEvents)
            InvokeSafely(this.ItemRemoved, x);

        foreach (var x in this.changedEvents)
            InvokeSafely(this.ItemChanged, x);

        foreach (var x in this.movedEvents)
            InvokeSafely(this.ItemMoved, x);

        foreach (var x in this.splitEvents)
            InvokeSafely(this.ItemSplit, x);

        foreach (var x in this.mergedEvents)
            InvokeSafely(this.ItemMerged, x);

        // We're done using the lists. Clean them up.
        this.addedEvents.Clear();
        this.removedEvents.Clear();
        this.changedEvents.Clear();
        this.movedEvents.Clear();
        this.splitEvents.Clear();
        this.mergedEvents.Clear();
    }

    /// <summary>
    /// A <see cref="IReadOnlyCollection{T}"/> view of <see cref="IEnumerable{T}"/>, so that the number of items
    /// contained within can be known in advance, and it can be enumerated multiple times.
    /// </summary>
    /// <typeparam name="T">The type of elements being enumerated.</typeparam>
    private class DeferredReadOnlyCollection<T> : IReadOnlyCollection<T>
    {
        private readonly Func<IEnumerable<T>> enumerableGenerator;

        public DeferredReadOnlyCollection(int count, Func<IEnumerable<T>> enumerableGenerator)
        {
            this.enumerableGenerator = enumerableGenerator;
            this.Count = count;
        }

        public int Count { get; }

        public IEnumerator<T> GetEnumerator() => this.enumerableGenerator().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.enumerableGenerator().GetEnumerator();
    }
}

/// <summary>
/// Plugin-scoped version of a GameInventory service.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IGameInventory>]
#pragma warning restore SA1015
internal class GameInventoryPluginScoped : IDisposable, IServiceType, IGameInventory
{
    [ServiceManager.ServiceDependency]
    private readonly GameInventory gameInventoryService = Service<GameInventory>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="GameInventoryPluginScoped"/> class.
    /// </summary>
    public GameInventoryPluginScoped()
    {
        this.gameInventoryService.InventoryChanged += this.OnInventoryChangedForward;
        this.gameInventoryService.InventoryChangedRaw += this.OnInventoryChangedRawForward;
        this.gameInventoryService.ItemAdded += this.OnInventoryItemAddedForward;
        this.gameInventoryService.ItemRemoved += this.OnInventoryItemRemovedForward;
        this.gameInventoryService.ItemMoved += this.OnInventoryItemMovedForward;
        this.gameInventoryService.ItemChanged += this.OnInventoryItemChangedForward;
        this.gameInventoryService.ItemSplit += this.OnInventoryItemSplitForward;
        this.gameInventoryService.ItemMerged += this.OnInventoryItemMergedForward;
    }

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangelogDelegate? InventoryChanged;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangelogDelegate? InventoryChangedRaw;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemAdded;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemRemoved;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemChanged;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemMoved;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemSplit;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemMerged;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.gameInventoryService.InventoryChanged -= this.OnInventoryChangedForward;
        this.gameInventoryService.InventoryChangedRaw -= this.OnInventoryChangedRawForward;
        this.gameInventoryService.ItemAdded -= this.OnInventoryItemAddedForward;
        this.gameInventoryService.ItemRemoved -= this.OnInventoryItemRemovedForward;
        this.gameInventoryService.ItemChanged -= this.OnInventoryItemChangedForward;
        this.gameInventoryService.ItemMoved -= this.OnInventoryItemMovedForward;
        this.gameInventoryService.ItemSplit -= this.OnInventoryItemSplitForward;
        this.gameInventoryService.ItemMerged -= this.OnInventoryItemMergedForward;

        this.InventoryChanged = null;
        this.InventoryChangedRaw = null;
        this.ItemAdded = null;
        this.ItemRemoved = null;
        this.ItemChanged = null;
        this.ItemMoved = null;
        this.ItemSplit = null;
        this.ItemMerged = null;
    }

    private void OnInventoryChangedForward(IReadOnlyCollection<InventoryEventArgs> events)
        => this.InventoryChanged?.Invoke(events);

    private void OnInventoryChangedRawForward(IReadOnlyCollection<InventoryEventArgs> events)
        => this.InventoryChangedRaw?.Invoke(events);

    private void OnInventoryItemAddedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemAdded?.Invoke(type, data);

    private void OnInventoryItemRemovedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemRemoved?.Invoke(type, data);

    private void OnInventoryItemChangedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemChanged?.Invoke(type, data);

    private void OnInventoryItemMovedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemMoved?.Invoke(type, data);

    private void OnInventoryItemSplitForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemSplit?.Invoke(type, data);

    private void OnInventoryItemMergedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemMerged?.Invoke(type, data);
}

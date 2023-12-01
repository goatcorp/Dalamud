using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;

namespace Dalamud.Game.GameInventory;

/// <summary>
/// This class provides events for the players in-game inventory.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
internal class GameInventory : IDisposable, IServiceType, IGameInventory
{
    private static readonly ModuleLog Log = new("GameInventory");

    private readonly List<InventoryEventArgs> changelog = new();
    
    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    private readonly GameInventoryType[] inventoryTypes;
    private readonly GameInventoryItem[]?[] inventoryItems;
    
    [ServiceManager.ServiceConstructor]
    private GameInventory()
    {
        this.inventoryTypes = Enum.GetValues<GameInventoryType>();
        this.inventoryItems = new GameInventoryItem[this.inventoryTypes.Length][];

        this.framework.Update += this.OnFrameworkUpdate;
    }

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangelogDelegate? InventoryChanged;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemAdded;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemRemoved;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemMoved;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemChanged;

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
                    if (newItem.IsEmpty)
                        continue;

                    this.changelog.Add(new InventoryItemAddedArgs 
                    {
                        Item = newItem,
                        Inventory = newItem.ContainerType,
                        Slot = newItem.InventorySlot,
                    });
                }
                else
                {
                    if (newItem.IsEmpty)
                    {
                        this.changelog.Add(new InventoryItemRemovedArgs
                        {
                            Item = oldItem,
                            Inventory = oldItem.ContainerType,
                            Slot = oldItem.InventorySlot,
                        });
                    }
                    else if (!oldItem.Equals(newItem))
                    {
                        this.changelog.Add(new InventoryItemChangedArgs
                        {
                            OldItemState = oldItem,
                            Item = newItem,
                            Inventory = newItem.ContainerType,
                            Slot = newItem.InventorySlot,
                        });
                    }
                    else
                    {
                        continue;
                    }
                }

                Log.Verbose($"[{this.changelog.Count - 1}] {this.changelog[^1]}");
                oldItem = newItem;
            }
        }

        // Was there any change? If not, stop further processing.
        if (this.changelog.Count == 0)
            return;

        try
        {
            // From this point, the size of changelog shall not change.
            var span = CollectionsMarshal.AsSpan(this.changelog);

            // Ensure that changelog is in order of Added, Removed, and then Changed.
            span.Sort((a, b) => a.Type.CompareTo(b.Type));
            
            var removedFrom = 0;
            while (removedFrom < span.Length && span[removedFrom].Type != GameInventoryEvent.Removed)
                removedFrom++;
            
            var changedFrom = removedFrom;
            while (changedFrom < span.Length && span[changedFrom].Type != GameInventoryEvent.Changed)
                changedFrom++;

            var addedSpan = span[..removedFrom];
            var removedSpan = span[removedFrom..changedFrom];
            var changedSpan = span[changedFrom..];

            // Resolve changelog for item moved, from 1 added + 1 removed
            foreach (ref var added in addedSpan)
            {
                foreach (ref var removed in removedSpan)
                {
                    if (added.Item.ItemId == removed.Item.ItemId)
                    {
                        Log.Verbose($"Move: reinterpreting {removed} + {added}");
                        added = new InventoryItemMovedArgs
                        {
                            Item = removed.Item,
                            SourceInventory = removed.Item.ContainerType,
                            SourceSlot = removed.Item.InventorySlot,
                            TargetInventory = added.Item.ContainerType,
                            TargetSlot = added.Item.InventorySlot,
                        };
                        removed = default;
                        break;
                    }
                }
            }

            // Resolve changelog for item moved, from 2 changes
            for (var i = 0; i < changedSpan.Length; i++)
            {
                if (span[i].Type is GameInventoryEvent.Empty)
                    continue;

                ref var e1 = ref changedSpan[i];
                for (var j = i + 1; j < changedSpan.Length; j++)
                {
                    ref var e2 = ref changedSpan[j];
                    if (e1.Item.ItemId == e2.Item.ItemId && e1.Item.ItemId == e2.Item.ItemId)
                    {
                        if (e1.Item.IsEmpty)
                        {
                            // e1 got moved to e2
                            Log.Verbose($"Move: reinterpreting {e1} + {e2}");
                            e1 = new InventoryItemMovedArgs
                            {
                                Item = e2.Item,
                                SourceInventory = e1.Item.ContainerType,
                                SourceSlot = e1.Item.InventorySlot,
                                TargetInventory = e2.Item.ContainerType,
                                TargetSlot = e2.Item.InventorySlot,
                            };
                            e2 = default;
                        }
                        else if (e2.Item.IsEmpty)
                        {
                            // e2 got moved to e1
                            Log.Verbose($"Move: reinterpreting {e2} + {e1}");
                            e1 = new InventoryItemMovedArgs
                            {
                                Item = e1.Item,
                                SourceInventory = e2.Item.ContainerType,
                                SourceSlot = e2.Item.InventorySlot,
                                TargetInventory = e1.Item.ContainerType,
                                TargetSlot = e1.Item.InventorySlot,
                            };
                            e2 = default;
                        }
                        else
                        {
                            // e1 and e2 got swapped
                            Log.Verbose($"Move(Swap): reinterpreting {e1} + {e2}");
                            var newEvent1 = new InventoryItemMovedArgs
                            {
                                Item = e2.Item,
                                SourceInventory = e1.Item.ContainerType,
                                SourceSlot = e1.Item.InventorySlot,
                                TargetInventory = e2.Item.ContainerType,
                                TargetSlot = e2.Item.InventorySlot,
                            };

                            var newEvent2 = new InventoryItemMovedArgs
                            {
                                Item = e1.Item,
                                SourceInventory = e2.Item.ContainerType,
                                SourceSlot = e2.Item.InventorySlot,
                                TargetInventory = e1.Item.ContainerType,
                                TargetSlot = e1.Item.InventorySlot,
                            };
                            
                            (e1, e2) = (newEvent1, newEvent2);
                        }
                    }
                }
            }

            // Filter out the emptied out entries.
            // We do not care about the order of items in the changelog anymore.
            for (var i = 0; i < span.Length;)
            {
                if (span[i] is null || span[i].Type is GameInventoryEvent.Empty)
                {
                    span[i] = span[^1];
                    span = span[..^1];
                }
                else
                {
                    i++;
                }
            }

            // Actually broadcast the changes to subscribers.
            if (!span.IsEmpty)
            {
                this.InventoryChanged?.Invoke(span);

                foreach (var change in span)
                {
                    switch (change)
                    {
                        case InventoryItemAddedArgs:
                            this.ItemAdded?.Invoke(GameInventoryEvent.Added, change);
                            break;
                        
                        case InventoryItemRemovedArgs:
                            this.ItemRemoved?.Invoke(GameInventoryEvent.Removed, change);
                            break;
                        
                        case InventoryItemMovedArgs:
                            this.ItemMoved?.Invoke(GameInventoryEvent.Moved, change);
                            break;
                        
                        case InventoryItemChangedArgs:
                            this.ItemChanged?.Invoke(GameInventoryEvent.Changed, change);
                            break;
                    }
                }
            }
        }
        finally
        {
            this.changelog.Clear();
        }
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
        this.gameInventoryService.ItemAdded += this.OnInventoryItemAddedForward;
        this.gameInventoryService.ItemRemoved += this.OnInventoryItemRemovedForward;
        this.gameInventoryService.ItemMoved += this.OnInventoryItemMovedForward;
        this.gameInventoryService.ItemChanged += this.OnInventoryItemChangedForward;
    }

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangelogDelegate? InventoryChanged;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemAdded;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemRemoved;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemMoved;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemChanged;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.gameInventoryService.InventoryChanged -= this.OnInventoryChangedForward;
        this.gameInventoryService.ItemAdded -= this.OnInventoryItemAddedForward;
        this.gameInventoryService.ItemRemoved -= this.OnInventoryItemRemovedForward;
        this.gameInventoryService.ItemMoved -= this.OnInventoryItemMovedForward;
        this.gameInventoryService.ItemChanged -= this.OnInventoryItemChangedForward;
        
        this.InventoryChanged = null;
        this.ItemAdded = null;
        this.ItemRemoved = null;
        this.ItemMoved = null;
        this.ItemChanged = null;
    }

    private void OnInventoryChangedForward(ReadOnlySpan<InventoryEventArgs> events)
        => this.InventoryChanged?.Invoke(events);
    
    private void OnInventoryItemAddedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemAdded?.Invoke(type, data);
    
    private void OnInventoryItemRemovedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemRemoved?.Invoke(type, data);

    private void OnInventoryItemMovedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemMoved?.Invoke(type, data);

    private void OnInventoryItemChangedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemChanged?.Invoke(type, data);
}

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace Dalamud.Game.Inventory;

/// <summary>
/// This class provides events for the players in-game inventory.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal class GameInventory : IDisposable, IServiceType, IGameInventory
{
    private static readonly ModuleLog Log = new("GameInventory");
    
    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    private readonly Dictionary<GameInventoryType, Dictionary<int, InventoryItem>> inventoryCache;
    
    [ServiceManager.ServiceConstructor]
    private GameInventory()
    {
        this.inventoryCache = new Dictionary<GameInventoryType, Dictionary<int, InventoryItem>>();
        
        foreach (var inventoryType in Enum.GetValues<GameInventoryType>())
        {
            this.inventoryCache.Add(inventoryType, new Dictionary<int, InventoryItem>());
        }
        
        this.framework.Update += this.OnFrameworkUpdate;
    }

    /// <inheritdoc/>
    public event IGameInventory.OnItemMovedDelegate? ItemMoved;
    
    /// <inheritdoc/>
    public event IGameInventory.OnItemRemovedDelegate? ItemRemoved;
    
    /// <inheritdoc/>
    public event IGameInventory.OnItemAddedDelegate? ItemAdded;
    
    /// <inheritdoc/>
    public event IGameInventory.OnItemChangedDelegate? ItemChanged;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.framework.Update -= this.OnFrameworkUpdate;
    }
    
    private void OnFrameworkUpdate(IFramework framework1)
    {
        // If no one is listening for event's then we don't need to track anything.
        if (!this.AnyListeners()) return;

        var performanceMonitor = Stopwatch.StartNew();
        
        var changelog = new List<GameInventoryItemChangelog>();
        
        foreach (var (inventoryType, cachedInventoryItems) in this.inventoryCache)
        {
            foreach (var item in this.GetItemsForInventory(inventoryType))
            {
                if (cachedInventoryItems.TryGetValue(item.Slot, out var inventoryItem))
                {
                    // Gained Item
                    //    If the item we have cached has an item id of 0, then we expect it to be an empty slot.
                    //    However, if the item we see in the game data has an item id that is not 0, then it now has an item.
                    if (inventoryItem.ItemID is 0 && item.ItemID is not 0)
                    {
                        var gameInventoryItem = new GameInventoryItem(item);
                        this.ItemAdded?.Invoke(inventoryType, (uint)item.Slot, gameInventoryItem);
                        changelog.Add(new GameInventoryItemChangelog(GameInventoryChangelogState.Added, gameInventoryItem));
                        
                        Log.Verbose($"New Item Added to {inventoryType}: {item.ItemID}");
                        this.inventoryCache[inventoryType][item.Slot] = item;
                    }
                    
                    // Removed Item
                    //    If the item we have cached has an item id of not 0, then we expect it to have an item.
                    //    However, if the item we see in the game data has an item id that is 0, then it was removed from this inventory.
                    if (inventoryItem.ItemID is not 0 && item.ItemID is 0)
                    {
                        var gameInventoryItem = new GameInventoryItem(inventoryItem);
                        this.ItemRemoved?.Invoke(inventoryType, (uint)item.Slot, gameInventoryItem);
                        changelog.Add(new GameInventoryItemChangelog(GameInventoryChangelogState.Removed, gameInventoryItem));
                        
                        Log.Verbose($"Item Removed from {inventoryType}: {inventoryItem.ItemID}");
                        this.inventoryCache[inventoryType][item.Slot] = item;
                    }
                    
                    // Changed Item
                    //    If the item we have cached, does not match the item that we see in the game data
                    //    AND if neither item is empty, then the item has been changed.
                    if (this.IsItemChanged(inventoryItem, item) && inventoryItem.ItemID is not 0 && item.ItemID is not 0)
                    {
                        var gameInventoryItem = new GameInventoryItem(inventoryItem);
                        this.ItemChanged?.Invoke(inventoryType, (uint)item.Slot, gameInventoryItem);
                        
                        Log.Verbose($"Item Changed {inventoryType}: {inventoryItem.ItemID}");
                        this.inventoryCache[inventoryType][item.Slot] = item;
                    }
                }
                else
                {
                    cachedInventoryItems.Add(item.Slot, item);
                }
            }
        }
        
        // Resolve changelog for item moved
        //    Group all changelogs that have the same itemId, and check if there was an add and a remove event for that item.
        foreach (var itemGroup in changelog.GroupBy(log => log.Item.ItemId))
        {
            var hasAdd = false;
            var hasRemove = false;
            
            foreach (var log in itemGroup)
            {
                switch (log.State)
                {
                    case GameInventoryChangelogState.Added:
                        hasAdd = true;
                        break;

                    case GameInventoryChangelogState.Removed:
                        hasRemove = true;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var itemMoved = hasAdd && hasRemove;
            if (itemMoved)
            {
                var added = itemGroup.FirstOrDefault(log => log.State == GameInventoryChangelogState.Added);
                var removed = itemGroup.FirstOrDefault(log => log.State == GameInventoryChangelogState.Removed);
                if (added is null || removed is null) continue;
                
                this.ItemMoved?.Invoke(removed.Item.ContainerType, removed.Item.InventorySlot, added.Item.ContainerType, added.Item.InventorySlot, added.Item);
                
                Log.Verbose($"Item Moved {removed.Item.ContainerType}:{removed.Item.InventorySlot} -> {added.Item.ContainerType}:{added.Item.InventorySlot}: {added.Item.ItemId}");
            }
        }

        var elapsed = performanceMonitor.Elapsed;
        
        Log.Verbose($"Processing Time: {elapsed.Ticks}ticks :: {elapsed.TotalMilliseconds}ms");
    }

    private bool AnyListeners()
    {
        if (this.ItemMoved is not null) return true;
        if (this.ItemRemoved is not null) return true;
        if (this.ItemAdded is not null) return true;
        if (this.ItemChanged is not null) return true;

        return false;
    }

    private unsafe ReadOnlySpan<InventoryItem> GetItemsForInventory(GameInventoryType type)
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager is null) return ReadOnlySpan<InventoryItem>.Empty;

        var inventory = inventoryManager->GetInventoryContainer((InventoryType)type);
        if (inventory is null) return ReadOnlySpan<InventoryItem>.Empty;

        return new ReadOnlySpan<InventoryItem>(inventory->Items, (int)inventory->Size);
    }

    private bool IsItemChanged(InventoryItem a, InventoryItem b)
    {
        if (a.Container != b.Container) return true; // Shouldn't be possible, but shouldn't hurt.
        if (a.Slot != b.Slot) return true;           // Shouldn't be possible, but shouldn't hurt.
        if (a.ItemID != b.ItemID) return true;
        if (a.Quantity != b.Quantity) return true;
        if (a.Spiritbond != b.Spiritbond) return true;
        if (a.Condition != b.Condition) return true;
        if (a.Flags != b.Flags) return true;
        if (a.CrafterContentID != b.CrafterContentID) return true;
        if (this.IsMateriaChanged(a, b)) return true;
        if (this.IsMateriaGradeChanged(a, b)) return true;
        if (a.Stain != b.Stain) return true;
        if (a.GlamourID != b.GlamourID) return true;

        return false;
    }

    private unsafe bool IsMateriaChanged(InventoryItem a, InventoryItem b)
        => new ReadOnlySpan<ushort>(a.Materia, 5) == new ReadOnlySpan<ushort>(b.Materia, 5);

    private unsafe bool IsMateriaGradeChanged(InventoryItem a, InventoryItem b) 
        => new ReadOnlySpan<byte>(a.MateriaGrade, 5) == new ReadOnlySpan<byte>(b.MateriaGrade, 5);
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
        this.gameInventoryService.ItemMoved += this.OnItemMovedForward;
        this.gameInventoryService.ItemRemoved += this.OnItemRemovedForward;
        this.gameInventoryService.ItemAdded += this.OnItemAddedForward;
        this.gameInventoryService.ItemChanged += this.OnItemChangedForward;
    }
    
    /// <inheritdoc/>
    public event IGameInventory.OnItemMovedDelegate? ItemMoved;
    
    /// <inheritdoc/>
    public event IGameInventory.OnItemRemovedDelegate? ItemRemoved;
    
    /// <inheritdoc/>
    public event IGameInventory.OnItemAddedDelegate? ItemAdded;
    
    /// <inheritdoc/>
    public event IGameInventory.OnItemChangedDelegate? ItemChanged;
    
    /// <inheritdoc/>
    public void Dispose()
    {
        this.gameInventoryService.ItemMoved -= this.OnItemMovedForward;
        this.gameInventoryService.ItemRemoved -= this.OnItemRemovedForward;
        this.gameInventoryService.ItemAdded -= this.OnItemAddedForward;
        this.gameInventoryService.ItemChanged -= this.OnItemChangedForward;

        this.ItemMoved = null;
        this.ItemRemoved = null;
        this.ItemAdded = null;
        this.ItemChanged = null;
    }

    private void OnItemMovedForward(GameInventoryType source, uint sourceSlot, GameInventoryType destination, uint destinationSlot, GameInventoryItem item)
        => this.ItemMoved?.Invoke(source, sourceSlot, destination, destinationSlot, item);

    private void OnItemRemovedForward(GameInventoryType source, uint sourceSlot, GameInventoryItem item)
        => this.ItemRemoved?.Invoke(source, sourceSlot, item);

    private void OnItemAddedForward(GameInventoryType destination, uint destinationSlot, GameInventoryItem item)
        => this.ItemAdded?.Invoke(destination, destinationSlot, item);

    private void OnItemChangedForward(GameInventoryType inventory, uint slot, GameInventoryItem item)
        => this.ItemChanged?.Invoke(inventory, slot, item);
}

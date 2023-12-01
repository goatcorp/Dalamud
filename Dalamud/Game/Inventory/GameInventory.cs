using System.Collections.Generic;

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
    private static readonly ModuleLog Log = new("GameInventory");

    private readonly List<InventoryEventArgs> allEvents = new();
    private readonly List<InventoryItemAddedArgs> addedEvents = new();
    private readonly List<InventoryItemRemovedArgs> removedEvents = new();
    private readonly List<InventoryItemChangedArgs> changedEvents = new();
    private readonly List<InventoryItemMovedArgs> movedEvents = new();
    
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
        // Note that...
        // * this.movedEvents is not checked; it will be populated after this check.
        // * this.allEvents is not checked; it is a temporary list to be used after this check.
        if (this.addedEvents.Count == 0 && this.removedEvents.Count == 0 && this.changedEvents.Count == 0)
            return;

        try
        {
            // Broadcast InventoryChangedRaw, if necessary.
            if (this.InventoryChangedRaw is not null)
            {
                this.allEvents.Clear();
                this.allEvents.EnsureCapacity(
                    this.addedEvents.Count
                    + this.removedEvents.Count
                    + this.changedEvents.Count);
                this.allEvents.AddRange(this.addedEvents);
                this.allEvents.AddRange(this.removedEvents);
                this.allEvents.AddRange(this.changedEvents);
                InvokeSafely(this.InventoryChangedRaw, this.allEvents);
            }

            // Resolve changelog for item moved, from 1 added + 1 removed event.
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

            // Resolve changelog for item moved, from 2 changed events.
            for (var i = this.changedEvents.Count - 1; i >= 0; --i)
            {
                var e1 = this.changedEvents[i];
                for (var j = i - 1; j >= 0; --j)
                {
                    var e2 = this.changedEvents[j];
                    if (e1.Item.ItemId != e2.Item.ItemId || e1.Item.ItemId != e2.Item.ItemId)
                        continue;

                    // move happened, and e2 has an item
                    if (!e2.Item.IsEmpty)
                        this.movedEvents.Add(new(e1, e2));

                    // move happened, and e1 has an item
                    if (!e1.Item.IsEmpty)
                        this.movedEvents.Add(new(e2, e1));
                        
                    // Remove the reinterpreted entries. Note that i > j.
                    this.changedEvents.RemoveAt(i);
                    this.changedEvents.RemoveAt(j);
                    break;
                }
            }

            // Log only if it matters.
            if (this.dalamudConfiguration.LogLevel >= LogEventLevel.Verbose)
            {
                foreach (var x in this.addedEvents)
                    Log.Verbose($"{x}");
            
                foreach (var x in this.removedEvents)
                    Log.Verbose($"{x}");
            
                foreach (var x in this.changedEvents)
                    Log.Verbose($"{x}");
            
                foreach (var x in this.movedEvents)
                    Log.Verbose($"{x} (({x.SourceEvent}) + ({x.TargetEvent}))");
            }

            // Broadcast InventoryChanged, if necessary.
            if (this.InventoryChanged is not null)
            {
                this.allEvents.Clear();
                this.allEvents.EnsureCapacity(
                    this.addedEvents.Count
                    + this.removedEvents.Count
                    + this.changedEvents.Count
                    + this.movedEvents.Count);
                this.allEvents.AddRange(this.addedEvents);
                this.allEvents.AddRange(this.removedEvents);
                this.allEvents.AddRange(this.changedEvents);
                this.allEvents.AddRange(this.movedEvents);
                InvokeSafely(this.InventoryChanged, this.allEvents);
            }

            // Broadcast the rest.
            foreach (var x in this.addedEvents)
                InvokeSafely(this.ItemAdded, x);
            
            foreach (var x in this.removedEvents)
                InvokeSafely(this.ItemRemoved, x);
            
            foreach (var x in this.changedEvents)
                InvokeSafely(this.ItemChanged, x);
            
            foreach (var x in this.movedEvents)
                InvokeSafely(this.ItemMoved, x);
        }
        finally
        {
            this.addedEvents.Clear();
            this.removedEvents.Clear();
            this.changedEvents.Clear();
            this.movedEvents.Clear();
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
        this.gameInventoryService.InventoryChangedRaw += this.OnInventoryChangedRawForward;
        this.gameInventoryService.ItemAdded += this.OnInventoryItemAddedForward;
        this.gameInventoryService.ItemRemoved += this.OnInventoryItemRemovedForward;
        this.gameInventoryService.ItemMoved += this.OnInventoryItemMovedForward;
        this.gameInventoryService.ItemChanged += this.OnInventoryItemChangedForward;
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
    public event IGameInventory.InventoryChangedDelegate? ItemMoved;

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangedDelegate? ItemChanged;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.gameInventoryService.InventoryChanged -= this.OnInventoryChangedForward;
        this.gameInventoryService.InventoryChangedRaw -= this.OnInventoryChangedRawForward;
        this.gameInventoryService.ItemAdded -= this.OnInventoryItemAddedForward;
        this.gameInventoryService.ItemRemoved -= this.OnInventoryItemRemovedForward;
        this.gameInventoryService.ItemMoved -= this.OnInventoryItemMovedForward;
        this.gameInventoryService.ItemChanged -= this.OnInventoryItemChangedForward;
        
        this.InventoryChanged = null;
        this.InventoryChangedRaw = null;
        this.ItemAdded = null;
        this.ItemRemoved = null;
        this.ItemMoved = null;
        this.ItemChanged = null;
    }

    private void OnInventoryChangedForward(IReadOnlyCollection<InventoryEventArgs> events)
        => this.InventoryChanged?.Invoke(events);

    private void OnInventoryChangedRawForward(IReadOnlyCollection<InventoryEventArgs> events)
        => this.InventoryChangedRaw?.Invoke(events);
    
    private void OnInventoryItemAddedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemAdded?.Invoke(type, data);
    
    private void OnInventoryItemRemovedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemRemoved?.Invoke(type, data);

    private void OnInventoryItemMovedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemMoved?.Invoke(type, data);

    private void OnInventoryItemChangedForward(GameInventoryEvent type, InventoryEventArgs data)
        => this.ItemChanged?.Invoke(type, data);
}

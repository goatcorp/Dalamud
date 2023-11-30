using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
[ServiceManager.BlockingEarlyLoadedService]
internal class GameInventory : IDisposable, IServiceType, IGameInventory
{
    private static readonly ModuleLog Log = new("GameInventory");

    private readonly List<IGameInventory.GameInventoryEventArgs> changelog = new();
    
    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    private readonly GameInventoryType[] inventoryTypes;
    private readonly GameInventoryItem[][] inventoryItems;
    private readonly unsafe GameInventoryItem*[] inventoryItemsPointers;
    
    [ServiceManager.ServiceConstructor]
    private unsafe GameInventory()
    {
        this.inventoryTypes = Enum.GetValues<GameInventoryType>();
        
        // Using GC.AllocateArray(pinned: true), so that Unsafe.AsPointer(ref array[0]) does not fail.
        this.inventoryItems = new GameInventoryItem[this.inventoryTypes.Length][];
        this.inventoryItemsPointers = new GameInventoryItem*[this.inventoryTypes.Length];
        for (var i = 0; i < this.inventoryItems.Length; i++)
        {
            this.inventoryItems[i] = GC.AllocateArray<GameInventoryItem>(1, true);
            this.inventoryItemsPointers[i] = (GameInventoryItem*)Unsafe.AsPointer(ref this.inventoryItems[i][0]);
        }

        this.framework.Update += this.OnFrameworkUpdate;
    }

    /// <inheritdoc/>
    public event IGameInventory.InventoryChangeDelegate? InventoryChanged;

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
    private static unsafe Span<GameInventoryItem> GetItemsForInventory(GameInventoryType type)
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager is null) return default;

        var inventory = inventoryManager->GetInventoryContainer((InventoryType)type);
        if (inventory is null) return default;

        return new(inventory->Items, (int)inventory->Size);
    }

    /// <summary>
    /// Looks for the first index of <paramref name="type"/>, or the supposed position one should be if none could be found.
    /// </summary>
    /// <param name="span">The span to look in.</param>
    /// <param name="type">The type.</param>
    /// <returns>The index.</returns>
    private static int FindTypeIndex(Span<IGameInventory.GameInventoryEventArgs> span, GameInventoryEvent type)
    {
        // Use linear lookup if span size is small enough
        if (span.Length < 64)
        {
            var i = 0;
            for (; i < span.Length; i++)
            {
                if (type <= span[i].Type)
                    break;
            }
        
            return i;
        }

        var lo = 0;
        var hi = span.Length - 1;
        while (lo <= hi)
        {
            var i = lo + ((hi - lo) >> 1);
            var type2 = span[i].Type;
            if (type == type2)
                return i;
            if (type < type2)
                lo = i + 1;
            else
                hi = i - 1;
        }
        
        return lo;
    }
    
    private unsafe void OnFrameworkUpdate(IFramework framework1)
    {
        // TODO: Uncomment this
        // // If no one is listening for event's then we don't need to track anything.
        // if (this.InventoryChanged is null) return;
        
        for (var i = 0; i < this.inventoryTypes.Length;)
        {
            var oldItemsArray = this.inventoryItems[i];
            var oldItemsLength = oldItemsArray.Length;
            var oldItemsPointer = this.inventoryItemsPointers[i];

            var resizeRequired = 0;
            foreach (ref var newItem in GetItemsForInventory(this.inventoryTypes[i]))
            {
                var slot = newItem.InternalItem.Slot;
                if (slot >= oldItemsLength)
                {
                    resizeRequired = Math.Max(resizeRequired, slot + 1);
                    continue;
                }

                // We already checked the range above. Go raw.
                ref var oldItem = ref oldItemsPointer[slot];

                if (oldItem.IsEmpty)
                {
                    if (newItem.IsEmpty)
                        continue;
                    this.changelog.Add(new(GameInventoryEvent.Added, default, newItem));
                }
                else
                {
                    if (newItem.IsEmpty)
                        this.changelog.Add(new(GameInventoryEvent.Removed, oldItem, default));
                    else if (!oldItem.Equals(newItem))
                        this.changelog.Add(new(GameInventoryEvent.Changed, oldItem, newItem));
                    else
                        continue;
                }

                Log.Verbose($"[{this.changelog.Count - 1}] {this.changelog[^1]}");
                oldItem = newItem;
            }

            // Did the max slot number get changed?
            if (resizeRequired != 0)
            {
                // Resize our buffer, and then try again.
                var oldItemsExpanded = GC.AllocateArray<GameInventoryItem>(resizeRequired, true);
                oldItemsArray.CopyTo(oldItemsExpanded, 0);
                this.inventoryItems[i] = oldItemsExpanded;
                this.inventoryItemsPointers[i] = (GameInventoryItem*)Unsafe.AsPointer(ref oldItemsExpanded[0]);
            }
            else
            {
                // Proceed to the next inventory.
                i++;
            }
        }

        // Was there any change? If not, stop further processing.
        if (this.changelog.Count == 0)
            return;

        try
        {
            // From this point, the size of changelog shall not change.
            var span = CollectionsMarshal.AsSpan(this.changelog);

            span.Sort((a, b) => a.Type.CompareTo(b.Type));
            var addedFrom = FindTypeIndex(span, GameInventoryEvent.Added);
            var removedFrom = FindTypeIndex(span, GameInventoryEvent.Removed);
            var changedFrom = FindTypeIndex(span, GameInventoryEvent.Changed);

            // Resolve changelog for item moved, from 1 added + 1 removed
            for (var iAdded = addedFrom; iAdded < removedFrom; iAdded++)
            {
                ref var added = ref span[iAdded];
                for (var iRemoved = removedFrom; iRemoved < changedFrom; iRemoved++)
                {
                    ref var removed = ref span[iRemoved];
                    if (added.Target.ItemId == removed.Source.ItemId)
                    {
                        span[iAdded] = new(GameInventoryEvent.Moved, span[iRemoved].Source, span[iAdded].Target);
                        span[iRemoved] = default;
                        Log.Verbose($"[{iAdded}] Interpreting instead as: {span[iAdded]}");
                        Log.Verbose($"[{iRemoved}] Discarding");
                        break;
                    }
                }
            }

            // Resolve changelog for item moved, from 2 changeds
            for (var i = changedFrom; i < this.changelog.Count; i++)
            {
                if (span[i].IsEmpty)
                    continue;

                ref var e1 = ref span[i];
                for (var j = i + 1; j < this.changelog.Count; j++)
                {
                    ref var e2 = ref span[j];
                    if (e1.Target.ItemId == e2.Source.ItemId && e1.Source.ItemId == e2.Target.ItemId)
                    {
                        if (e1.Target.IsEmpty)
                        {
                            // e1 got moved to e2
                            e1 = new(GameInventoryEvent.Moved, e1.Source, e2.Target);
                            e2 = default;
                            Log.Verbose($"[{i}] Interpreting instead as: {e1}");
                            Log.Verbose($"[{j}] Discarding");
                        }
                        else if (e2.Target.IsEmpty)
                        {
                            // e2 got moved to e1
                            e1 = new(GameInventoryEvent.Moved, e2.Source, e1.Target);
                            e2 = default;
                            Log.Verbose($"[{i}] Interpreting instead as: {e1}");
                            Log.Verbose($"[{j}] Discarding");
                        }
                        else
                        {
                            // e1 and e2 got swapped
                            (e1, e2) = (new(GameInventoryEvent.Moved, e1.Target, e2.Target),
                                           new(GameInventoryEvent.Moved, e2.Target, e1.Target));

                            Log.Verbose($"[{i}] Interpreting instead as: {e1}");
                            Log.Verbose($"[{j}] Interpreting instead as: {e2}");
                        }
                    }
                }
            }

            // Filter out the emptied out entries.
            // We do not care about the order of items in the changelog anymore.
            for (var i = 0; i < span.Length;)
            {
                if (span[i].IsEmpty)
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
                this.InventoryChanged?.Invoke(span);
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
    }
    
    /// <inheritdoc/>
    public event IGameInventory.InventoryChangeDelegate? InventoryChanged;
    
    /// <inheritdoc/>
    public void Dispose()
    {
        this.gameInventoryService.InventoryChanged -= this.OnInventoryChangedForward;
        this.InventoryChanged = null;
    }

    private void OnInventoryChangedForward(ReadOnlySpan<IGameInventory.GameInventoryEventArgs> events)
        => this.InventoryChanged?.Invoke(events);
}

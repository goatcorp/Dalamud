using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// This class handles interacting with the game's (right-click) context menu.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class ContextMenu : IInternalDisposableService, IContextMenu
{
    private static readonly ModuleLog Log = new("ContextMenu");

    private readonly Hook<RaptureAtkModuleOpenAddonByAgentDelegate> raptureAtkModuleOpenAddonByAgentHook;
    private readonly Hook<AddonContextMenuOnMenuSelectedDelegate> addonContextMenuOnMenuSelectedHook;
    private readonly RaptureAtkModuleOpenAddonDelegate raptureAtkModuleOpenAddon;

    [ServiceManager.ServiceConstructor]
    private ContextMenu()
    {
        this.raptureAtkModuleOpenAddonByAgentHook = Hook<RaptureAtkModuleOpenAddonByAgentDelegate>.FromAddress((nint)RaptureAtkModule.Addresses.OpenAddonByAgent.Value, this.RaptureAtkModuleOpenAddonByAgentDetour);
        this.addonContextMenuOnMenuSelectedHook = Hook<AddonContextMenuOnMenuSelectedDelegate>.FromAddress((nint)AddonContextMenu.StaticVTable.OnMenuSelected, this.AddonContextMenuOnMenuSelectedDetour);
        this.raptureAtkModuleOpenAddon = Marshal.GetDelegateForFunctionPointer<RaptureAtkModuleOpenAddonDelegate>((nint)RaptureAtkModule.Addresses.OpenAddon.Value);

        this.raptureAtkModuleOpenAddonByAgentHook.Enable();
        this.addonContextMenuOnMenuSelectedHook.Enable();
    }

    private delegate ushort RaptureAtkModuleOpenAddonByAgentDelegate(RaptureAtkModule* module, byte* addonName, AtkUnitBase* addon, int valueCount, AtkValue* values, AgentInterface* agent, nint a7, ushort parentAddonId);
    
    private delegate bool AddonContextMenuOnMenuSelectedDelegate(AddonContextMenu* addon, int selectedIdx, byte a3);
    
    private delegate ushort RaptureAtkModuleOpenAddonDelegate(RaptureAtkModule* a1, uint addonNameId, uint valueCount, AtkValue* values, AgentInterface* parentAgent, ulong unk, ushort parentAddonId, int unk2);

    /// <inheritdoc/>
    public event IContextMenu.OnMenuOpenedDelegate? OnMenuOpened;

    private Dictionary<ContextMenuType, List<MenuItem>> MenuItems { get; } = new();

    private object MenuItemsLock { get; } = new();

    private AgentInterface* SelectedAgent { get; set; }

    private ContextMenuType? SelectedMenuType { get; set; }

    private List<MenuItem>? SelectedItems { get; set; }

    private HashSet<nint> SelectedEventInterfaces { get; } = new();

    private AtkUnitBase* SelectedParentAddon { get; set; }

    // -1 -> -inf: native items
    // 0 -> inf: selected items
    private List<int> MenuCallbackIds { get; } = new();

    private IReadOnlyList<MenuItem>? SubmenuItems { get; set; }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        var manager = RaptureAtkUnitManager.Instance();
        var menu = manager->GetAddonByName("ContextMenu");
        var submenu = manager->GetAddonByName("AddonContextSub");
        if (menu->IsVisible)
            menu->FireCallbackInt(-1);
        if (submenu->IsVisible)
            submenu->FireCallbackInt(-1);

        this.raptureAtkModuleOpenAddonByAgentHook.Dispose();
        this.addonContextMenuOnMenuSelectedHook.Dispose();
    }

    /// <inheritdoc/>
    public void AddMenuItem(ContextMenuType menuType, MenuItem item)
    {
        lock (this.MenuItemsLock)
        {
            if (!this.MenuItems.TryGetValue(menuType, out var items))
                this.MenuItems[menuType] = items = new();
            items.Add(item);
        }
    }

    /// <inheritdoc/>
    public bool RemoveMenuItem(ContextMenuType menuType, MenuItem item)
    {
        lock (this.MenuItemsLock)
        {
            if (!this.MenuItems.TryGetValue(menuType, out var items))
                return false;
            return items.Remove(item);
        }
    }

    private AtkValue* ExpandContextMenuArray(Span<AtkValue> oldValues, int newSize)
    {
        // if the array has enough room, don't reallocate
        if (oldValues.Length >= newSize)
            return (AtkValue*)Unsafe.AsPointer(ref oldValues[0]);

        var size = (sizeof(AtkValue) * newSize) + 8;
        var newArray = (nint)IMemorySpace.GetUISpace()->Malloc((ulong)size, 0);
        if (newArray == nint.Zero)
            throw new OutOfMemoryException();
        NativeMemory.Fill((void*)newArray, (nuint)size, 0);

        *(ulong*)newArray = (ulong)newSize;

        // copy old memory if existing
        if (!oldValues.IsEmpty)
            oldValues.CopyTo(new((void*)(newArray + 8), oldValues.Length));

        return (AtkValue*)(newArray + 8);
    }

    private void FreeExpandedContextMenuArray(AtkValue* newValues, int newSize) =>
        IMemorySpace.Free((void*)((nint)newValues - 8), (ulong)((newSize * sizeof(AtkValue)) + 8));

    private AtkValue* CreateEmptySubmenuContextMenuArray(SeString name, int x, int y, out int valueCount)
    {
        // 0: UInt = ContextItemCount
        // 1: String = Name
        // 2: Int = PositionX
        // 3: Int = PositionY
        // 4: Bool = false
        // 5: UInt = ContextItemSubmenuMask
        // 6: UInt = ReturnArrowMask (_gap_0x6BC ? 1 << (ContextItemCount - 1) : 0)
        // 7: UInt = 1

        valueCount = 8;
        var values = this.ExpandContextMenuArray(Span<AtkValue>.Empty, valueCount);
        values[0].ChangeType(ValueType.UInt);
        values[0].UInt = 0;
        values[1].ChangeType(ValueType.String);
        values[1].SetString(name.Encode().NullTerminate());
        values[2].ChangeType(ValueType.Int);
        values[2].Int = x;
        values[3].ChangeType(ValueType.Int);
        values[3].Int = y;
        values[4].ChangeType(ValueType.Bool);
        values[4].Byte = 0;
        values[5].ChangeType(ValueType.UInt);
        values[5].UInt = 0;
        values[6].ChangeType(ValueType.UInt);
        values[6].UInt = 0;
        values[7].ChangeType(ValueType.UInt);
        values[7].UInt = 1;
        return values;
    }

    private void SetupGenericMenu(int headerCount, int sizeHeaderIdx, int returnHeaderIdx, int submenuHeaderIdx, IReadOnlyList<MenuItem> items, ref int valueCount, ref AtkValue* values)
    {
        var itemsWithIdx = items.Select((item, idx) => (item, idx)).OrderBy(i => i.item.Priority).ToArray();
        var prefixItems = itemsWithIdx.Where(i => i.item.Priority < 0).ToArray();
        var suffixItems = itemsWithIdx.Where(i => i.item.Priority >= 0).ToArray();

        var nativeMenuSize = (int)values[sizeHeaderIdx].UInt;
        var prefixMenuSize = prefixItems.Length;
        var suffixMenuSize = suffixItems.Length;

        var hasGameDisabled = valueCount - headerCount - nativeMenuSize > 0;

        var hasCustomDisabled = items.Any(item => !item.IsEnabled);
        var hasAnyDisabled = hasGameDisabled || hasCustomDisabled;

        values = this.ExpandContextMenuArray(
            new(values, valueCount),
            valueCount = (nativeMenuSize + items.Count) * (hasAnyDisabled ? 2 : 1) + headerCount);
        var offsetData = new Span<AtkValue>(values, headerCount);
        var nameData = new Span<AtkValue>(values + headerCount, nativeMenuSize + items.Count);
        var disabledData = hasAnyDisabled ? new Span<AtkValue>(values + headerCount + nativeMenuSize + items.Count, nativeMenuSize + items.Count) : Span<AtkValue>.Empty;

        var returnMask = offsetData[returnHeaderIdx].UInt;
        var submenuMask = offsetData[submenuHeaderIdx].UInt;

        nameData[..nativeMenuSize].CopyTo(nameData.Slice(prefixMenuSize, nativeMenuSize));
        if (hasAnyDisabled)
        {
            if (hasGameDisabled)
            {
                // copy old disabled data
                var oldDisabledData = new Span<AtkValue>(values + headerCount + nativeMenuSize, nativeMenuSize);
                oldDisabledData.CopyTo(disabledData.Slice(prefixMenuSize, nativeMenuSize));
            }
            else
            {
                // enable all
                for (var i = prefixMenuSize; i < prefixMenuSize + nativeMenuSize; ++i)
                {
                    disabledData[i].ChangeType(ValueType.Int);
                    disabledData[i].Int = 0;
                }
            }
        }

        returnMask <<= prefixMenuSize;
        submenuMask <<= prefixMenuSize;

        void FillData(Span<AtkValue> disabledData, Span<AtkValue> nameData, int i, MenuItem item, int idx)
        {
            this.MenuCallbackIds.Add(idx);

            if (hasAnyDisabled)
            {
                disabledData[i].ChangeType(ValueType.Int);
                disabledData[i].Int = item.IsEnabled ? 0 : 1;
            }

            if (item.IsReturn)
                returnMask |= 1u << i;
            if (item.IsSubmenu)
                submenuMask |= 1u << i;

            nameData[i].ChangeType(ValueType.String);
            nameData[i].SetString(item.PrefixedName.Encode().NullTerminate());
        }

        for (var i = 0; i < prefixMenuSize; ++i)
        {
            var (item, idx) = prefixItems[i];
            FillData(disabledData, nameData, i, item, idx);
        }

        this.MenuCallbackIds.AddRange(Enumerable.Range(0, nativeMenuSize).Select(i => -i - 1));

        for (var i = prefixMenuSize + nativeMenuSize; i < prefixMenuSize + nativeMenuSize + suffixMenuSize; ++i)
        {
            var (item, idx) = suffixItems[i - prefixMenuSize - nativeMenuSize];
            FillData(disabledData, nameData, i, item, idx);
        }

        offsetData[returnHeaderIdx].UInt = returnMask;
        offsetData[submenuHeaderIdx].UInt = submenuMask;

        offsetData[sizeHeaderIdx].UInt += (uint)items.Count;
    }

    private void SetupContextMenu(IReadOnlyList<MenuItem> items, ref int valueCount, ref AtkValue* values)
    {
        // 0: UInt = Item Count
        // 1: UInt = 0 (probably window name, just unused)
        // 2: UInt = Return Mask (?)
        // 3: UInt = Submenu Mask
        // 4: UInt = OpenAtCursorPosition ? 2 : 1
        // 5: UInt = 0
        // 6: UInt = 0

        foreach (var item in items)
        {
            if (!item.Prefix.HasValue)
            {
                item.Prefix = MenuItem.DalamudDefaultPrefix;
                item.PrefixColor = MenuItem.DalamudDefaultPrefixColor;

                if (!item.UseDefaultPrefix)
                {
                    Log.Warning($"Menu item \"{item.Name}\" has no prefix, defaulting to Dalamud's. Menu items outside of a submenu must have a prefix.");
                }
            }
        }

        this.SetupGenericMenu(7, 0, 2, 3, items, ref valueCount, ref values);
    }

    private void SetupContextSubMenu(IReadOnlyList<MenuItem> items, ref int valueCount, ref AtkValue* values)
    {
        // 0: UInt = ContextItemCount
        // 1: skipped?
        // 2: Int = PositionX
        // 3: Int = PositionY
        // 4: Bool = false
        // 5: UInt = ContextItemSubmenuMask
        // 6: UInt = _gap_0x6BC ? 1 << (ContextItemCount - 1) : 0
        // 7: UInt = 1

        this.SetupGenericMenu(8, 0, 6, 5, items, ref valueCount, ref values);
    }

    private ushort RaptureAtkModuleOpenAddonByAgentDetour(RaptureAtkModule* module, byte* addonName, AtkUnitBase* addon, int valueCount, AtkValue* values, AgentInterface* agent, nint a7, ushort parentAddonId)
    {
        var oldValues = values;

        if (MemoryHelper.EqualsZeroTerminatedString("ContextMenu", (nint)addonName))
        {
            this.MenuCallbackIds.Clear();
            this.SelectedAgent = agent;
            this.SelectedParentAddon = module->RaptureAtkUnitManager.GetAddonById(parentAddonId);
            this.SelectedEventInterfaces.Clear();
            if (this.SelectedAgent == AgentInventoryContext.Instance())
            {
                this.SelectedMenuType = ContextMenuType.Inventory;
            }
            else if (this.SelectedAgent == AgentContext.Instance())
            {
                this.SelectedMenuType = ContextMenuType.Default;

                var menu = AgentContext.Instance()->CurrentContextMenu;
                var handlers = new Span<Pointer<AtkEventInterface>>(menu->EventHandlerArray, 32);
                var ids = new Span<byte>(menu->EventIdArray, 32);
                var count = (int)values[0].UInt;
                handlers = handlers.Slice(7, count);
                ids = ids.Slice(7, count);
                for (var i = 0; i < count; ++i)
                {
                    if (ids[i] <= 106)
                        continue;
                    this.SelectedEventInterfaces.Add((nint)handlers[i].Value);
                }
            }
            else
            {
                this.SelectedMenuType = null;
            }

            this.SubmenuItems = null;

            if (this.SelectedMenuType is { } menuType)
            {
                lock (this.MenuItemsLock)
                {
                    if (this.MenuItems.TryGetValue(menuType, out var items))
                        this.SelectedItems = new(items);
                    else
                        this.SelectedItems = new();
                }

                var args = new MenuOpenedArgs(this.SelectedItems.Add, this.SelectedParentAddon, this.SelectedAgent, this.SelectedMenuType.Value, this.SelectedEventInterfaces);
                this.OnMenuOpened?.InvokeSafely(args);
                this.SelectedItems = this.FixupMenuList(this.SelectedItems, (int)values[0].UInt);
                this.SetupContextMenu(this.SelectedItems, ref valueCount, ref values);
                Log.Verbose($"Opening {this.SelectedMenuType} context menu with {this.SelectedItems.Count} custom items.");
            }
            else
            {
                this.SelectedItems = null;
            }
        }
        else if (MemoryHelper.EqualsZeroTerminatedString("AddonContextSub", (nint)addonName))
        {
            this.MenuCallbackIds.Clear();
            if (this.SubmenuItems != null)
            {
                this.SubmenuItems = this.FixupMenuList(this.SubmenuItems.ToList(), (int)values[0].UInt);

                this.SetupContextSubMenu(this.SubmenuItems, ref valueCount, ref values);
                Log.Verbose($"Opening {this.SelectedMenuType} submenu with {this.SubmenuItems.Count} custom items.");
            }
        }
        else if (MemoryHelper.EqualsZeroTerminatedString("AddonContextMenuTitle", (nint)addonName))
        {
            this.MenuCallbackIds.Clear();
        }

        var ret = this.raptureAtkModuleOpenAddonByAgentHook.Original(module, addonName, addon, valueCount, values, agent, a7, parentAddonId);
        if (values != oldValues)
            this.FreeExpandedContextMenuArray(values, valueCount);
        return ret;
    }

    private List<MenuItem> FixupMenuList(List<MenuItem> items, int nativeMenuSize)
    {
        // The in game menu actually supports 32 items, but the last item can't have a visible submenu arrow.
        // As such, we'll only work with 31 items.
        const int maxMenuItems = 31;
        if (items.Count + nativeMenuSize > maxMenuItems)
        {
            Log.Warning($"Menu size exceeds {maxMenuItems} items, truncating.");
            var orderedItems = items.OrderBy(i => i.Priority).ToArray();
            var newItems = orderedItems[..(maxMenuItems - nativeMenuSize - 1)];
            var submenuItems = orderedItems[(maxMenuItems - nativeMenuSize - 1)..];
            return newItems.Append(new MenuItem
            {
                Prefix = SeIconChar.BoxedLetterD,
                PrefixColor = 539,
                IsSubmenu = true,
                Priority = int.MaxValue,
                Name = $"See More ({submenuItems.Length})",
                OnClicked = a => a.OpenSubmenu(submenuItems),
            }).ToList();
        }

        return items;
    }

    private void OpenSubmenu(SeString name, IReadOnlyList<MenuItem> submenuItems, int posX, int posY)
    {
        if (submenuItems.Count == 0)
            throw new ArgumentException("Submenu must not be empty", nameof(submenuItems));

        this.SubmenuItems = submenuItems;

        var module = RaptureAtkModule.Instance();
        var values = this.CreateEmptySubmenuContextMenuArray(name, posX, posY, out var valueCount);

        switch (this.SelectedMenuType)
        {
            case ContextMenuType.Default:
                {
                    var ownerAddonId = ((AgentContext*)this.SelectedAgent)->OwnerAddon;
                    this.raptureAtkModuleOpenAddon(module, 445, (uint)valueCount, values, this.SelectedAgent, 71, checked((ushort)ownerAddonId), 4);
                    break;
                }

            case ContextMenuType.Inventory:
                {
                    var ownerAddonId = ((AgentInventoryContext*)this.SelectedAgent)->OwnerAddonId;
                    this.raptureAtkModuleOpenAddon(module, 445, (uint)valueCount, values, this.SelectedAgent, 0, checked((ushort)ownerAddonId), 4);
                    break;
                }

            default:
                Log.Warning($"Unknown context menu type (agent: {(nint)this.SelectedAgent}, cannot open submenu");
                break;
        }

        this.FreeExpandedContextMenuArray(values, valueCount);
    }

    private bool AddonContextMenuOnMenuSelectedDetour(AddonContextMenu* addon, int selectedIdx, byte a3)
    {
        var items = this.SubmenuItems ?? this.SelectedItems;
        if (items == null)
            goto original;
        if (this.MenuCallbackIds.Count == 0)
            goto original;
        if (selectedIdx < 0)
            goto original;
        if (selectedIdx >= this.MenuCallbackIds.Count)
            goto original;

        var callbackId = this.MenuCallbackIds[selectedIdx];

        if (callbackId < 0)
        {
            selectedIdx = -callbackId - 1;
        }
        else
        {
            var item = items[callbackId];
            var openedSubmenu = false;

            try
            {
                if (item.OnClicked == null)
                    throw new InvalidOperationException("Item has no OnClicked handler");
                item.OnClicked.InvokeSafely(new MenuItemClickedArgs(
                    (name, submenuItems) =>
                    {
                        short x, y;
                        addon->AtkUnitBase.GetPosition(&x, &y);
                        this.OpenSubmenu(name ?? item.Name, submenuItems, x, y);
                        openedSubmenu = true;
                    },
                    this.SelectedParentAddon,
                    this.SelectedAgent,
                    this.SelectedMenuType ?? default,
                    this.SelectedEventInterfaces));
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while handling context menu click");
            }

            // Close with click sound
            if (!openedSubmenu)
                addon->AtkUnitBase.FireCallbackInt(-2);
            return false;
        }

original:
        // Eventually handled by inventory context here: 14022BBD0 (6.51)
        return this.addonContextMenuOnMenuSelectedHook.Original(addon, selectedIdx, a3);
    }
}

/// <summary>
/// Plugin-scoped version of a <see cref="ContextMenu"/> service.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IContextMenu>]
#pragma warning restore SA1015
internal class ContextMenuPluginScoped : IInternalDisposableService, IContextMenu
{
    [ServiceManager.ServiceDependency]
    private readonly ContextMenu parentService = Service<ContextMenu>.Get();

    private ContextMenuPluginScoped()
    {
        this.parentService.OnMenuOpened += this.OnMenuOpenedForward;
    }

    /// <inheritdoc/>
    public event IContextMenu.OnMenuOpenedDelegate? OnMenuOpened;

    private Dictionary<ContextMenuType, List<MenuItem>> MenuItems { get; } = new();

    private object MenuItemsLock { get; } = new();

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.parentService.OnMenuOpened -= this.OnMenuOpenedForward;

        this.OnMenuOpened = null;

        lock (this.MenuItemsLock)
        {
            foreach (var (menuType, items) in this.MenuItems)
            {
                foreach (var item in items)
                    this.parentService.RemoveMenuItem(menuType, item);
            }
        }
    }

    /// <inheritdoc/>
    public void AddMenuItem(ContextMenuType menuType, MenuItem item)
    {
        lock (this.MenuItemsLock)
        {
            if (!this.MenuItems.TryGetValue(menuType, out var items))
                this.MenuItems[menuType] = items = new();
            items.Add(item);
        }

        this.parentService.AddMenuItem(menuType, item);
    }

    /// <inheritdoc/>
    public bool RemoveMenuItem(ContextMenuType menuType, MenuItem item)
    {
        lock (this.MenuItemsLock)
        {
            if (this.MenuItems.TryGetValue(menuType, out var items))
                items.Remove(item);
        }

        return this.parentService.RemoveMenuItem(menuType, item);
    }

    private void OnMenuOpenedForward(MenuOpenedArgs args) =>
        this.OnMenuOpened?.Invoke(args);
}

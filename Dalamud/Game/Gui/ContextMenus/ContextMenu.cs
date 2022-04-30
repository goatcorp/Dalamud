using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.Gui.ContextMenus;

using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

/// <summary>
/// Context menu functions.
/// Adopted from Anna Clemens' XivCommon Context Menu implementation.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public class ContextMenu : IDisposable
{
    private const int MaxItems = 32;

    /// <summary>
    /// Offset from addon to menu type.
    /// </summary>
    private const int ParentAddonIdOffset = 0x1D2;

    private const int AddonArraySizeOffset = 0x1CA;
    private const int AddonArrayOffset = 0x160;

    private const int ContextMenuItemOffset = 7;

    /// <summary>
    /// Offset from agent to actions byte array pointer (have to add the actions offset after).
    /// </summary>
    private const int MenuActionsPointerOffset = 0xD18;

    /// <summary>
    /// SetUpContextSubMenu checks this.
    /// </summary>
    private const int BooleanOffsetCheck = 0x690;

    /// <summary>
    /// Offset from [MenuActionsPointer] to actions byte array.
    /// </summary>
    private const int MenuActionsOffset = 0x428;

    /// <summary>
    /// Offset from inventory context agent to actions byte array.
    /// </summary>
    private const int InventoryMenuActionsOffset = 0x558;

    private const int ObjectIdOffset = 0xEF0;
    private const int ContentIdLowerOffset = 0xEE0;
    private const int TextPointerOffset = 0xE08;
    private const int WorldOffset = 0xF00;

    private const int ItemIdOffset = 0x5F8;
    private const int ItemAmountOffset = 0x5FC;
    private const int ItemHqOffset = 0x604;

    // Found in the first function in the agent's vtable
    private const byte NoopContextId = 0x67;
    private const byte InventoryNoopContextId = 0xFF;

    private readonly GetAddonByInternalIdDelegate getAddonByInternalId;
    private readonly AtkValueChangeTypeDelegate atkValueChangeType;
    private readonly AtkValueSetStringDelegate atkValueSetString;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextMenu"/> class.
    /// </summary>
    internal ContextMenu()
    {
        this.Language = Service<DalamudStartInfo>.Get().Language;
        var scanner = Service<SigScanner>.Get();
        this.UiAlloc = new UiAlloc(scanner);

        this.UiAlloc = new UiAlloc(scanner);

        this.Address = new ContextMenuAddressResolver();
        this.Address.Setup();

        unsafe
        {
            this.atkValueChangeType = Marshal.GetDelegateForFunctionPointer<AtkValueChangeTypeDelegate>(this.Address.ContextMenuChangeTypePtr);
            this.atkValueSetString = Marshal.GetDelegateForFunctionPointer<AtkValueSetStringDelegate>(this.Address.ContextMenuSetStringPtr);
            this.getAddonByInternalId = Marshal.GetDelegateForFunctionPointer<GetAddonByInternalIdDelegate>(this.Address.ContextMenuGetAddonPtr);
            this.SomeOpenAddonThingHook = new Hook<SomeOpenAddonThingDelegate>(this.Address.ContextMenuOpenAddonPtr, this.SomeOpenAddonThingDetour);
            this.ContextMenuOpenHook = new Hook<ContextMenuOpenDelegate>(this.Address.ContextMenuOpenPtr, this.OpenMenuDetour);
            this.ContextMenuItemSelectedHook = new Hook<ContextMenuItemSelectedInternalDelegate>(this.Address.ContextMenuSelectedPtr, this.ItemSelectedDetour);
            this.TitleContextMenuOpenHook = new Hook<ContextMenuOpenDelegate>(this.Address.ContextMenuTitleMenuOpenPtr, this.TitleContextMenuOpenDetour);
            this.ContextMenuEvent66Hook = new Hook<ContextMenuEvent66Delegate>(this.Address.ContextMenuEvent66Ptr, this.ContextMenuEvent66Detour);
        }
    }

    /// <summary>
    /// The delegate for context menu events.
    /// </summary>
    /// <param name="args">context menu args.</param>
    public delegate void GameObjectContextMenuOpenEventDelegate(GameObjectContextMenuOpenArgs args);

    /// <summary>
    /// The delegate for inventory context menu events.
    /// </summary>
    /// <param name="args">context menu args.</param>
    public delegate void InventoryContextMenuOpenEventDelegate(InventoryContextMenuOpenArgs args);

    /// <summary>
    /// The delegate that is run when a context menu item is selected.
    /// </summary>
    /// <param name="args">context menu args.</param>
    public delegate void GameObjectContextMenuItemSelectedDelegate(GameObjectContextMenuItemSelectedArgs args);

    /// <summary>
    /// The delegate that is run when an inventory context menu item is selected.
    /// </summary>
    /// <param name="args">context menu args.</param>
    public delegate void InventoryContextMenuItemSelectedDelegate(InventoryContextMenuItemSelectedArgs args);

    private delegate IntPtr SomeOpenAddonThingDelegate(IntPtr a1, IntPtr a2, IntPtr a3, uint a4, IntPtr a5, IntPtr a6, IntPtr a7, ushort a8);

    private unsafe delegate byte ContextMenuOpenDelegate(IntPtr addon, int menuSize, AtkValue* atkValueArgs);

    private delegate IntPtr GetAddonByInternalIdDelegate(IntPtr raptureAtkUnitManager, short id);

    private delegate byte ContextMenuItemSelectedInternalDelegate(IntPtr addon, int index, byte a3);

    private delegate byte ContextMenuEvent66Delegate(IntPtr agent);

    private unsafe delegate void AtkValueChangeTypeDelegate(AtkValue* thisPtr, ValueType type);

    private unsafe delegate void AtkValueSetStringDelegate(AtkValue* thisPtr, byte* bytes);

    /// <summary>
    /// Occurs when a context menu is opened by the game.
    /// </summary>
    [Obsolete("This is deprecated and no longer works. Use new context menu events instead.", false)]
    #pragma warning disable CS0618, CS0067
    // ReSharper disable once InconsistentNaming
    public event ContextMenuOpenedDelegate? ContextMenuOpened;
    #pragma warning restore CS0618, CS0067

    /// <summary>
    /// The event that is fired when a context menu is being prepared for opening.
    /// </summary>
    public event GameObjectContextMenuOpenEventDelegate? OnGameObjectContextMenuOpened;

    /// <summary>
    /// The event that is fired when an inventory context menu is being prepared for opening.
    /// </summary>
    public event InventoryContextMenuOpenEventDelegate? OnInventoryContextMenuOpened;

    private enum AgentType
    {
        Normal,
        Inventory,
        Unknown,
    }

    private Hook<SomeOpenAddonThingDelegate>? SomeOpenAddonThingHook { get; }

    private Hook<ContextMenuOpenDelegate>? ContextMenuOpenHook { get; }

    private Hook<ContextMenuOpenDelegate>? TitleContextMenuOpenHook { get; }

    private Hook<ContextMenuItemSelectedInternalDelegate>? ContextMenuItemSelectedHook { get; }

    private Hook<ContextMenuEvent66Delegate>? ContextMenuEvent66Hook { get; }

    private ClientLanguage Language { get; }

    private IntPtr Agent { get; set; } = IntPtr.Zero;

    private List<BaseContextMenuItem> Items { get; } = new();

    private int NormalSize { get; set; }

    private UiAlloc UiAlloc { get; set; }

    private ContextMenuAddressResolver Address { get; set; }

    private BaseContextMenuItem? SubMenuItem { get; set; }

    private IntPtr SubMenuTitle { get; set; } = IntPtr.Zero;

    /// <inheritdoc />
    public void Dispose()
    {
        this.SomeOpenAddonThingHook?.Dispose();
        this.ContextMenuOpenHook?.Dispose();
        this.TitleContextMenuOpenHook?.Dispose();
        this.ContextMenuItemSelectedHook?.Dispose();
        this.ContextMenuEvent66Hook?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Add dalamud indicator to context menu.
    /// </summary>
    /// <param name="name">context menu name.</param>
    /// <returns>updated name.</returns>
    internal static SeString AddDalamudContextMenuIndicator(SeString name)
    {
        return !Service<DalamudConfiguration>.Get().ShowCustomContextMenuIndicator ? name :
                   new SeString().Append(new UIForegroundPayload(539))
                                 .Append($"{SeIconChar.BoxedLetterD.ToIconString()} ")
                                 .Append(new UIForegroundPayload(0))
                                 .Append(name);
    }

    /// <summary>
    /// Enable this subsystem.
    /// </summary>
    internal void Enable()
    {
        this.SomeOpenAddonThingHook?.Enable();
        this.ContextMenuOpenHook?.Enable();
        this.TitleContextMenuOpenHook?.Enable();
        this.ContextMenuItemSelectedHook?.Enable();
        this.ContextMenuEvent66Hook?.Enable();
    }

    private static unsafe (uint ObjectId, uint ContentIdLower, SeString? Text, ushort ObjectWorld) GetAgentInfo(IntPtr agent)
    {
        var objectId = *(uint*)(agent + ObjectIdOffset);
        var contentIdLower = *(uint*)(agent + ContentIdLowerOffset);
        var textBytes = Marshal.ReadIntPtr(agent + TextPointerOffset).ReadTerminated();
        var text = textBytes.Length == 0 ? null : SeString.Parse(textBytes);
        var objectWorld = *(ushort*)(agent + WorldOffset);
        return (objectId, contentIdLower, text, objectWorld);
    }

    private static unsafe (uint ItemId, uint ItemAmount, bool ItemHq) GetInventoryAgentInfo(IntPtr agent)
    {
        var itemId = *(uint*)(agent + ItemIdOffset);
        var itemAmount = *(uint*)(agent + ItemAmountOffset);
        var itemHq = *(byte*)(agent + ItemHqOffset) == 1;
        return (itemId, itemAmount, itemHq);
    }

    private IntPtr SomeOpenAddonThingDetour(
        IntPtr a1, IntPtr a2, IntPtr a3, uint a4, IntPtr a5, IntPtr a6, IntPtr a7, ushort a8)
    {
        this.Agent = a6;
        return this.SomeOpenAddonThingHook!.Original(a1, a2, a3, a4, a5, a6, a7, a8);
    }

    private unsafe byte TitleContextMenuOpenDetour(IntPtr addon, int menuSize, AtkValue* atkValueArgs)
    {
        if (this.SubMenuTitle == IntPtr.Zero)
        {
            this.Items.Clear();
        }

        return this.TitleContextMenuOpenHook!.Original(addon, menuSize, atkValueArgs);
    }

    private unsafe (AgentType AgentType, IntPtr Agent) GetContextMenuAgent(IntPtr? agent = null)
    {
        agent ??= this.Agent;

        IntPtr GetAgent(AgentId id)
        {
            return (IntPtr)Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(id);
        }

        var agentType = AgentType.Unknown;
        if (agent == GetAgent(AgentId.Context))
        {
            agentType = AgentType.Normal;
        }
        else if (agent == GetAgent(AgentId.InventoryContext))
        {
            agentType = AgentType.Inventory;
        }

        return (agentType, agent.Value);
    }

    private unsafe string? GetParentAddonName(IntPtr addon)
    {
        var parentAddonId = Marshal.ReadInt16(addon + ParentAddonIdOffset);
        if (parentAddonId == 0)
        {
            return null;
        }

        var stage = AtkStage.GetSingleton();
        var parentAddon = this.getAddonByInternalId((IntPtr)stage->RaptureAtkUnitManager, parentAddonId);
        return Encoding.UTF8.GetString((parentAddon + 8).ReadTerminated());
    }

    [HandleProcessCorruptedStateExceptions]
    private unsafe byte OpenMenuDetour(IntPtr addon, int menuSize, AtkValue* atkValueArgs)
    {
        try
        {
            this.OpenMenuDetourInner(addon, ref menuSize, ref atkValueArgs);
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Exception in OpenMenuDetour");
        }

        return this.ContextMenuOpenHook!.Original(addon, menuSize, atkValueArgs);
    }

    private unsafe AtkValue* ExpandContextMenuArray(IntPtr addon)
    {
        const ulong newItemCount = (MaxItems * 2) + ContextMenuItemOffset;

        var oldArray = *(AtkValue**)(addon + AddonArrayOffset);
        var oldArrayItemCount = *(ushort*)(addon + AddonArraySizeOffset);

        // if the array has enough room, don't reallocate
        if (oldArrayItemCount >= newItemCount)
        {
            return oldArray;
        }

        // reallocate
        var size = ((ulong)sizeof(AtkValue) * newItemCount) + 8;
        var newArray = this.UiAlloc.Alloc(size);
        // zero new memory
        Marshal.Copy(new byte[size], 0, newArray, (int)size);
        // update size and pointer
        *(ulong*)newArray = newItemCount;
        *(void**)(addon + AddonArrayOffset) = (void*)(newArray + 8);
        *(ushort*)(addon + AddonArraySizeOffset) = (ushort)newItemCount;

        // copy old memory if existing
        if (oldArray != null)
        {
            Buffer.MemoryCopy(oldArray, (void*)(newArray + 8), size, (ulong)sizeof(AtkValue) * oldArrayItemCount);
            this.UiAlloc.Free((IntPtr)oldArray - 8);
        }

        return (AtkValue*)(newArray + 8);
    }

    private unsafe void OpenMenuDetourInner(IntPtr addon, ref int menuSize, ref AtkValue* atkValueArgs)
    {
        this.Items.Clear();
        this.FreeSubMenuTitle();

        var (agentType, agent) = this.GetContextMenuAgent();
        if (agent == IntPtr.Zero)
        {
            return;
        }

        if (agentType == AgentType.Unknown)
        {
            return;
        }

        atkValueArgs = this.ExpandContextMenuArray(addon);

        var inventory = agentType == AgentType.Inventory;
        var offset = ContextMenuItemOffset + (inventory ? 0 : *(long*)(agent + BooleanOffsetCheck) != 0 ? 1 : 0);

        this.NormalSize = (int)(&atkValueArgs[0])->UInt;

        // idx 3 is bitmask of indices that are submenus
        var submenuArg = &atkValueArgs[3];
        var submenus = (int)submenuArg->UInt;

        var hasGameDisabled = menuSize - offset - this.NormalSize > 0;

        var menuActions = inventory
                              ? (byte*)(agent + InventoryMenuActionsOffset)
                              : (byte*)(Marshal.ReadIntPtr(agent + MenuActionsPointerOffset) + MenuActionsOffset);

        var nativeItems = new List<NativeContextMenuItem>();
        for (var i = 0; i < this.NormalSize; i++)
        {
            var atkItem = &atkValueArgs[offset + i];

            var name = ((IntPtr)atkItem->String).ReadSeString();

            var enabled = true;
            if (hasGameDisabled)
            {
                var disabledItem = &atkValueArgs[offset + this.NormalSize + i];
                enabled = disabledItem->Int == 0;
            }

            var action = *(menuActions + offset + i);

            var isSubMenu = (submenus & (1 << i)) > 0;

            nativeItems.Add(new NativeContextMenuItem(action, name, enabled, isSubMenu));
        }

        if (this.PopulateItems(addon, agent, this.OnGameObjectContextMenuOpened, this.OnInventoryContextMenuOpened, nativeItems))
        {
            return;
        }

        var hasCustomDisabled = this.Items.Any(item => !item.Enabled);
        var hasAnyDisabled = hasGameDisabled || hasCustomDisabled;

        // clear all submenu flags
        submenuArg->UInt = 0;

        for (var i = 0; i < this.Items.Count; i++)
        {
            var item = this.Items[i];

            if (hasAnyDisabled)
            {
                var disabledArg = &atkValueArgs[offset + this.Items.Count + i];
                this.atkValueChangeType(disabledArg, ValueType.Int);
                disabledArg->Int = item.Enabled ? 0 : 1;
            }

            // set up the agent to take the appropriate action for this item
            *(menuActions + offset + i) = item switch
            {
                NativeContextMenuItem nativeItem => nativeItem.InternalAction,
                _ => inventory ? InventoryNoopContextId : NoopContextId,
            };

            // set submenu flag
            if (item.IsSubMenu)
            {
                submenuArg->UInt |= (uint)(1 << i);
            }

            // set up the menu item
            var newItem = &atkValueArgs[offset + i];
            this.atkValueChangeType(newItem, ValueType.String);

            var name = this.GetItemName(item);
            fixed (byte* nameBytesPtr = name.Encode().Terminate())
            {
                this.atkValueSetString(newItem, nameBytesPtr);
            }
        }

        (&atkValueArgs[0])->UInt = (uint)this.Items.Count;

        menuSize = (int)(&atkValueArgs[0])->UInt;
        if (hasAnyDisabled)
        {
            menuSize *= 2;
        }

        menuSize += offset;
    }

    /// <returns>true on error.</returns>
    private bool PopulateItems(
        IntPtr addon,
        IntPtr agent,
        GameObjectContextMenuOpenEventDelegate? normalAction,
        InventoryContextMenuOpenEventDelegate? inventoryAction,
        IReadOnlyCollection<NativeContextMenuItem>? nativeItems = null)
    {
        var (agentType, _) = this.GetContextMenuAgent(agent);
        if (agentType == AgentType.Unknown)
        {
            return true;
        }

        var inventory = agentType == AgentType.Inventory;
        var parentAddonName = this.GetParentAddonName(addon);

        if (inventory)
        {
            var info = GetInventoryAgentInfo(agent);

            var args = new InventoryContextMenuOpenArgs(
                addon,
                agent,
                parentAddonName,
                info.ItemId,
                info.ItemAmount,
                info.ItemHq);
            if (nativeItems != null)
            {
                args.Items.AddRange(nativeItems);
            }

            try
            {
                inventoryAction?.Invoke(args);
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, "Exception in OpenMenuDetour");
                return true;
            }

            // remove any NormalContextMenuItems that may have been added - these will crash the game
            args.Items.RemoveAll(item => item is GameObjectContextMenuItem);

            // set the agent of any remaining custom items
            foreach (var item in args.Items)
            {
                switch (item)
                {
                    case InventoryContextMenuItem custom:
                        custom.Agent = agent;
                        break;
                }
            }

            this.Items.AddRange(args.Items);
        }
        else
        {
            var info = GetAgentInfo(agent);

            var args = new GameObjectContextMenuOpenArgs(
                addon,
                agent,
                parentAddonName,
                info.ObjectId,
                info.ContentIdLower,
                info.Text,
                info.ObjectWorld);
            if (nativeItems != null)
            {
                args.Items.AddRange(nativeItems);
            }

            try
            {
                normalAction?.Invoke(args);
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, "Exception in OpenMenuDetour");
                return true;
            }

            // remove any InventoryContextMenuItems that may have been added - these will crash the game
            args.Items.RemoveAll(item => item is InventoryContextMenuItem);

            // set the agent of any remaining custom items
            foreach (var item in args.Items)
            {
                switch (item)
                {
                    case GameObjectContextMenuItem custom:
                        custom.Agent = agent;
                        break;
                }
            }

            this.Items.AddRange(args.Items);
        }

        if (this.Items.Count > MaxItems)
        {
            var toRemove = this.Items.Count - MaxItems;
            this.Items.RemoveRange(MaxItems, toRemove);
            PluginLog.LogWarning($"Context menu item limit ({MaxItems}) exceeded. Removing {toRemove} item(s).");
        }

        return false;
    }

    private SeString GetItemName(BaseContextMenuItem item)
    {
        return item switch
        {
            GameObjectContextMenuItem custom => this.Language switch
            {
                ClientLanguage.Japanese => custom.NameJapanese,
                ClientLanguage.English => custom.NameEnglish,
                ClientLanguage.German => custom.NameGerman,
                ClientLanguage.French => custom.NameFrench,
                _ => custom.NameEnglish,
            },
            InventoryContextMenuItem custom => this.Language switch
            {
                ClientLanguage.Japanese => custom.NameJapanese,
                ClientLanguage.English => custom.NameEnglish,
                ClientLanguage.German => custom.NameGerman,
                ClientLanguage.French => custom.NameFrench,
                _ => custom.NameEnglish,
            },
            NativeContextMenuItem native => native.Name,
            _ => "Invalid context menu item",
        };
    }

    private byte ItemSelectedDetour(IntPtr addon, int index, byte a3)
    {
        this.FreeSubMenuTitle();

        if (index < 0 || index >= this.Items.Count)
        {
            goto Original;
        }

        var item = this.Items[index];
        switch (item)
        {
            case GameObjectContextMenuItem custom:
            {
                var addonName = this.GetParentAddonName(addon);
                var info = GetAgentInfo(custom.Agent);

                var args = new GameObjectContextMenuItemSelectedArgs(
                    addon,
                    custom.Agent,
                    addonName,
                    info.ObjectId,
                    info.ContentIdLower,
                    info.Text,
                    info.ObjectWorld);

                try
                {
                    custom.Action(args);
                }
                catch (Exception ex)
                {
                    PluginLog.LogError(ex, "Exception in custom context menu item");
                }

                break;
            }

            case InventoryContextMenuItem custom:
            {
                var addonName = this.GetParentAddonName(addon);
                var info = GetInventoryAgentInfo(custom.Agent);

                var args = new InventoryContextMenuItemSelectedArgs(
                    addon,
                    custom.Agent,
                    addonName,
                    info.ItemId,
                    info.ItemAmount,
                    info.ItemHq);

                try
                {
                    custom.Action(args);
                }
                catch (Exception ex)
                {
                    PluginLog.LogError(ex, "Exception in custom context menu item");
                }

                break;
            }
        }

        Original:
        return this.ContextMenuItemSelectedHook!.Original(addon, index, a3);
    }

    private void FreeSubMenuTitle()
    {
        if (this.SubMenuTitle == IntPtr.Zero)
        {
            return;
        }

        this.UiAlloc.Free(this.SubMenuTitle);
        this.SubMenuTitle = IntPtr.Zero;
    }

    /// <returns>false if original should be called.</returns>
    private unsafe bool SubMenuInner(IntPtr agent)
    {
        if (this.SubMenuItem == null)
        {
            return false;
        }

        var subMenuItem = this.SubMenuItem;
        this.SubMenuItem = null;

        // free our workaround pointer
        this.FreeSubMenuTitle();

        this.Items.Clear();

        var name = this.GetItemName(subMenuItem);

        // Since the game checks the agent's AtkValue array for the submenu title, and since we
        // don't update that array, we need to work around this check.
        // First, we will convince the game to make the submenu title pointer null by telling it
        // that an invalid index was selected.
        // Second, we will replace the null pointer with our own pointer.
        // Third, we will restore the original selected index.

        // step 1
        var selectedIdx = (byte*)(agent + 0x670);
        var wasSelected = *selectedIdx;
        *selectedIdx = 0xFF;

        // step 2 (see SetUpContextSubMenu)
        var nameBytes = name.Encode().Terminate();
        this.SubMenuTitle = this.UiAlloc.Alloc((ulong)nameBytes.Length);
        Marshal.Copy(nameBytes, 0, this.SubMenuTitle, nameBytes.Length);
        var v10 = agent + (0x678 * *(byte*)(agent + 0x1740)) + 0x28;
        *(byte**)(v10 + 0x668) = (byte*)this.SubMenuTitle;

        // step 3
        *selectedIdx = wasSelected;

        var secondaryArgsPtr = Marshal.ReadIntPtr(agent + MenuActionsPointerOffset);
        var submenuArgs = (AtkValue*)(secondaryArgsPtr + 8);

        var booleanOffset = *(long*)(agent + (*(byte*)(agent + 0x1740) * 0x678) + 0x690) != 0 ? 1 : 0;

        for (var i = 0; i < this.Items.Count; i++)
        {
            var item = this.Items[i];

            *(ushort*)secondaryArgsPtr += 1;
            if (submenuArgs != null)
            {
                var arg = &submenuArgs[ContextMenuItemOffset + i + 1];
                this.atkValueChangeType(arg, ValueType.String);
                var itemName = this.GetItemName(item);
                fixed (byte* namePtr = itemName.Encode().Terminate())
                {
                    this.atkValueSetString(arg, namePtr);
                }
            }

            // set action to no-op
            *(byte*)(secondaryArgsPtr + booleanOffset + i + ContextMenuItemOffset + 0x428) = NoopContextId;
        }

        return true;
    }

    private byte ContextMenuEvent66Detour(IntPtr agent)
    {
        return this.SubMenuInner(agent) ? (byte)0 : this.ContextMenuEvent66Hook!.Original(agent);
    }
}

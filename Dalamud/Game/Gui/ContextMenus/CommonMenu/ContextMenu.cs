using Dalamud.Game.Gui.ContextMenus.CommonMenu.Helpers;
using Dalamud.Game.Gui.ContextMenus.CommonMenu.Inventory;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility;
using Serilog.Core;

namespace Dalamud.Game.Gui.ContextMenus.CommonMenu;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

/// <summary>
/// Context menu functions
/// </summary>
public class ContextMenu : IDisposable
{
    private static class Signatures
    {
        internal const string SomeOpenAddonThing = "E8 ?? ?? ?? ?? 0F B7 C0 48 83 C4 60";
        internal const string ContextMenuOpen = "48 8B C4 57 41 56 41 57 48 81 EC";

        internal const string ContextMenuSelected =
            "48 89 5C 24 ?? 55 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 80 B9";

        internal const string ContextMenuEvent66 = "E8 ?? ?? ?? ?? 44 39 A3 ?? ?? ?? ?? 0F 84";
        internal const string InventoryContextMenuEvent30 = "E8 ?? ?? ?? ?? 48 83 C4 30 5B C3 8B 83";
        internal const string SetUpContextSubMenu = "E8 ?? ?? ?? ?? 44 39 A3 ?? ?? ?? ?? 0F 86";
        internal const string SetUpInventoryContextSubMenu = "44 88 44 24 ?? 88 54 24 10 53";
        internal const string TitleContextMenuOpen = "48 8B C4 57 41 55 41 56 48 81 EC";
        internal const string AtkValueChangeType = "E8 ?? ?? ?? ?? 45 84 F6 48 8D 4C 24";
        internal const string AtkValueSetString = "E8 ?? ?? ?? ?? 41 03 ED";
        internal const string GetAddonByInternalId = "E8 ?? ?? ?? ?? 8B 6B 20";
    }

    #region Offsets and other constants

    private const int MaxItems = 32;

    /// <summary>
    /// Offset from addon to menu type
    /// </summary>
    private const int ParentAddonIdOffset = 0x1D2;

    private const int AddonArraySizeOffset = 0x1CA;
    private const int AddonArrayOffset = 0x160;

    private const int ContextMenuItemOffset = 7;

    /// <summary>
    /// Offset from agent to actions byte array pointer (have to add the actions offset after)
    /// </summary>
    private const int MenuActionsPointerOffset = 0xD18;

    /// <summary>
    /// SetUpContextSubMenu checks this
    /// </summary>
    private const int BooleanOffsetCheck = 0x690;

    /// <summary>
    /// Offset from [MenuActionsPointer] to actions byte array
    /// </summary>
    private const int MenuActionsOffset = 0x428;

    /// <summary>
    /// Offset from inventory context agent to actions byte array
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
    private const byte ContextSubId = 0x66;
    private const byte InventoryContextSubId = 0x30;

    #endregion

    // 82C570 called when you click on a title menu context item

    /// <summary>
    /// The delegate for context menu events.
    /// </summary>
    public delegate void ContextMenuOpenEventDelegate(ContextMenuOpenArgs args);

    /// <summary>
    /// <para>
    /// The event that is fired when a context menu is being prepared for opening.
    /// </para>
    /// <para>
    /// Requires the <see cref="Hooks.ContextMenu"/> hook to be enabled.
    /// </para>
    /// </summary>
    public event ContextMenuOpenEventDelegate? OpenContextMenu;

    /// <summary>
    /// The delegate for inventory context menu events.
    /// </summary>
    public delegate void InventoryContextMenuOpenEventDelegate(InventoryContextMenuOpenArgs args);

    /// <summary>
    /// <para>
    /// The event that is fired when an inventory context menu is being prepared for opening.
    /// </para>
    /// <para>
    /// Requires the <see cref="Hooks.ContextMenu"/> hook to be enabled.
    /// </para>
    /// </summary>
    public event InventoryContextMenuOpenEventDelegate? OpenInventoryContextMenu;

    /// <summary>
    /// The delegate that is run when a context menu item is selected.
    /// </summary>
    public delegate void ContextMenuItemSelectedDelegate(ContextMenuItemSelectedArgs args);

    /// <summary>
    /// The delegate that is run when an inventory context menu item is selected.
    /// </summary>
    public delegate void InventoryContextMenuItemSelectedDelegate(InventoryContextMenuItemSelectedArgs args);

    private delegate IntPtr SomeOpenAddonThingDelegate(
        IntPtr a1, IntPtr a2, IntPtr a3, uint a4, IntPtr a5, IntPtr a6, IntPtr a7, ushort a8);

    private Hook<SomeOpenAddonThingDelegate>? SomeOpenAddonThingHook { get; }

    private unsafe delegate byte ContextMenuOpenDelegate(IntPtr addon, int menuSize, AtkValue* atkValueArgs);

    private delegate IntPtr GetAddonByInternalIdDelegate(IntPtr raptureAtkUnitManager, short id);

    private readonly GetAddonByInternalIdDelegate _getAddonByInternalId = null!;

    private Hook<ContextMenuOpenDelegate>? ContextMenuOpenHook { get; }
    private Hook<ContextMenuOpenDelegate>? TitleContextMenuOpenHook { get; }

    private delegate byte ContextMenuItemSelectedInternalDelegate(IntPtr addon, int index, byte a3);

    private Hook<ContextMenuItemSelectedInternalDelegate>? ContextMenuItemSelectedHook { get; }

    private delegate byte SetUpContextSubMenuDelegate(IntPtr agent);

    private readonly SetUpContextSubMenuDelegate _setUpContextSubMenu = null!;

    private delegate IntPtr SetUpInventoryContextSubMenuDelegate(IntPtr agent, byte hasTitle, byte zero);

    private readonly SetUpInventoryContextSubMenuDelegate _setUpInventoryContextSubMenu = null!;

    private delegate byte ContextMenuEvent66Delegate(IntPtr agent);

    private Hook<ContextMenuEvent66Delegate>? ContextMenuEvent66Hook { get; }

    private delegate void InventoryContextMenuEvent30Delegate(IntPtr agent, IntPtr a2, int a3, int a4, short a5);

    private Hook<InventoryContextMenuEvent30Delegate>? InventoryContextMenuEvent30Hook { get; }

    private unsafe delegate void AtkValueChangeTypeDelegate(AtkValue* thisPtr, ValueType type);

    private readonly AtkValueChangeTypeDelegate _atkValueChangeType = null!;

    private unsafe delegate void AtkValueSetStringDelegate(AtkValue* thisPtr, byte* bytes);

    private readonly AtkValueSetStringDelegate _atkValueSetString = null!;
    
    private ClientLanguage Language { get; }
    private IntPtr Agent { get; set; } = IntPtr.Zero;
    private List<BaseContextMenuItem> Items { get; } = new();
    private int NormalSize { get; set; }
    private UiAlloc UiAlloc { get; set; }

    internal ContextMenu()
    {
        this.Language = Service<DalamudStartInfo>.Get().Language;
        var scanner = Service<SigScanner>.Get();
        this.UiAlloc = new UiAlloc(scanner);

        if (scanner.TryScanText(Signatures.AtkValueChangeType, out var changeTypePtr))
        {
            this._atkValueChangeType = Marshal.GetDelegateForFunctionPointer<AtkValueChangeTypeDelegate>(changeTypePtr);
        }
        else
        {
            return;
        }

        if (scanner.TryScanText(Signatures.AtkValueSetString, out var setStringPtr))
        {
            this._atkValueSetString = Marshal.GetDelegateForFunctionPointer<AtkValueSetStringDelegate>(setStringPtr);
        }
        else
        {
            return;
        }

        if (scanner.TryScanText(Signatures.GetAddonByInternalId, out var getAddonPtr))
        {
            this._getAddonByInternalId =
                Marshal.GetDelegateForFunctionPointer<GetAddonByInternalIdDelegate>(getAddonPtr);
        }
        else
        {
            return;
        }

        if (scanner.TryScanText(Signatures.SetUpContextSubMenu, out var setUpSubPtr))
        {
            this._setUpContextSubMenu = Marshal.GetDelegateForFunctionPointer<SetUpContextSubMenuDelegate>(setUpSubPtr);
        }
        else
        {
            return;
        }

        // TODO: uncomment when inv submenus
        // if (scanner.TryScanText(Signatures.SetUpInventoryContextSubMenu, out var setUpInvSubPtr, "Context Menu (set up inventory submenu)")) {
        //     this._setUpInventoryContextSubMenu = Marshal.GetDelegateForFunctionPointer<SetUpInventoryContextSubMenuDelegate>(setUpInvSubPtr);
        // } else {
        //     return;
        // }

        if (scanner.TryScanText(Signatures.SomeOpenAddonThing, out var thingPtr))
        {
            this.SomeOpenAddonThingHook = new Hook<SomeOpenAddonThingDelegate>(thingPtr, this.SomeOpenAddonThingDetour);
        }
        else
        {
            return;
        }

        if (scanner.TryScanText(Signatures.ContextMenuOpen, out var openPtr))
        {
            unsafe
            {
                this.ContextMenuOpenHook = new Hook<ContextMenuOpenDelegate>(openPtr, this.OpenMenuDetour);
            }
            
        }
        else
        {
            return;
        }

        if (scanner.TryScanText(Signatures.ContextMenuSelected, out var selectedPtr))
        {
            this.ContextMenuItemSelectedHook =
                new Hook<ContextMenuItemSelectedInternalDelegate>(selectedPtr, this.ItemSelectedDetour);
        }

        if (scanner.TryScanText(Signatures.TitleContextMenuOpen, out var titleOpenPtr))
        {
            unsafe
            {
                this.TitleContextMenuOpenHook =
                    new Hook<ContextMenuOpenDelegate>(titleOpenPtr, this.TitleContextMenuOpenDetour);
            }

        }

        if (scanner.TryScanText(Signatures.ContextMenuEvent66, out var event66Ptr))
        {
            this.ContextMenuEvent66Hook =
                new Hook<ContextMenuEvent66Delegate>(event66Ptr, this.ContextMenuEvent66Detour);
        }

        // TODO: uncomment when inv submenus
        // if (scanner.TryScanText(Signatures.InventoryContextMenuEvent30, out var event30Ptr, "Context Menu (inv event 30)")) {
        //     this.InventoryContextMenuEvent30Hook = new Hook<InventoryContextMenuEvent30Delegate>(event30Ptr, new InventoryContextMenuEvent30Delegate(this.InventoryContextMenuEvent30Detour));
        //     this.InventoryContextMenuEvent30Hook.Enable();
        // }
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

    /// <inheritdoc />
    public void Dispose()
    {
        this.SomeOpenAddonThingHook?.Dispose();
        this.ContextMenuOpenHook?.Dispose();
        this.TitleContextMenuOpenHook?.Dispose();
        this.ContextMenuItemSelectedHook?.Dispose();
        this.ContextMenuEvent66Hook?.Dispose();
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

    private enum AgentType
    {
        Normal,
        Inventory,
        Unknown,
    }

    private unsafe (AgentType agentType, IntPtr agent) GetContextMenuAgent(IntPtr? agent = null)
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
        var parentAddon = this._getAddonByInternalId((IntPtr)stage->RaptureAtkUnitManager, parentAddonId);
        return Encoding.UTF8.GetString(CommonUtil.ReadTerminated(parentAddon + 8));
    }

    private unsafe IntPtr GetAddonFromAgent(IntPtr agent)
    {
        var addonId = *(byte*)(agent + 0x20);
        if (addonId == 0)
        {
            return IntPtr.Zero;
        }

        var stage = AtkStage.GetSingleton();
        return this._getAddonByInternalId((IntPtr)stage->RaptureAtkUnitManager, addonId);
    }

    private unsafe (uint objectId, uint contentIdLower, SeString? text, ushort objectWorld) GetAgentInfo(IntPtr agent)
    {
        var objectId = *(uint*)(agent + ObjectIdOffset);
        var contentIdLower = *(uint*)(agent + ContentIdLowerOffset);
        var textBytes = CommonUtil.ReadTerminated(Marshal.ReadIntPtr(agent + TextPointerOffset));
        var text = textBytes.Length == 0 ? null : SeString.Parse(textBytes);
        var objectWorld = *(ushort*)(agent + WorldOffset);
        return (objectId, contentIdLower, text, objectWorld);
    }

    private static unsafe (uint itemId, uint itemAmount, bool itemHq) GetInventoryAgentInfo(IntPtr agent)
    {
        var itemId = *(uint*)(agent + ItemIdOffset);
        var itemAmount = *(uint*)(agent + ItemAmountOffset);
        var itemHq = *(byte*)(agent + ItemHqOffset) == 1;
        return (itemId, itemAmount, itemHq);
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
        const ulong newItemCount = MaxItems * 2 + ContextMenuItemOffset;

        var oldArray = *(AtkValue**)(addon + AddonArrayOffset);
        var oldArrayItemCount = *(ushort*)(addon + AddonArraySizeOffset);

        // if the array has enough room, don't reallocate
        if (oldArrayItemCount >= newItemCount)
        {
            return oldArray;
        }

        // reallocate
        var size = (ulong)sizeof(AtkValue) * newItemCount + 8;
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

            var name = CommonUtil.ReadSeString((IntPtr)atkItem->String);

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

        if (this.PopulateItems(addon, agent, this.OpenContextMenu, this.OpenInventoryContextMenu, nativeItems))
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
                this._atkValueChangeType(disabledArg, ValueType.Int);
                disabledArg->Int = item.Enabled ? 0 : 1;
            }

            // set up the agent to take the appropriate action for this item
            *(menuActions + offset + i) = item switch
            {
                NativeContextMenuItem nativeItem => nativeItem.InternalAction,
                NormalContextSubMenuItem => ContextSubId,
                // TODO: uncomment when inv submenus
                // InventoryContextSubMenuItem => InventoryContextSubId,
                _ => inventory ? InventoryNoopContextId : NoopContextId,
            };

            // set submenu flag
            if (item.IsSubMenu)
            {
                submenuArg->UInt |= (uint)(1 << i);
            }

            // set up the menu item
            var newItem = &atkValueArgs[offset + i];
            this._atkValueChangeType(newItem, ValueType.String);

            var name = this.GetItemName(item);
            fixed (byte* nameBytesPtr = name.Encode().Terminate())
            {
                this._atkValueSetString(newItem, nameBytesPtr);
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

    /// <returns>true on error</returns>
    private bool PopulateItems(
        IntPtr addon, IntPtr agent, ContextMenuOpenEventDelegate? normalAction,
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
                info.itemId,
                info.itemAmount,
                info.itemHq
            );
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
            args.Items.RemoveAll(item => item is NormalContextMenuItem);

            // set the agent of any remaining custom items
            foreach (var item in args.Items)
            {
                switch (item)
                {
                    case InventoryContextMenuItem custom:
                        custom.Agent = agent;
                        break;
                    case InventoryContextSubMenuItem custom:
                        custom.Agent = agent;
                        break;
                }
            }

            this.Items.AddRange(args.Items);
        }
        else
        {
            var info = this.GetAgentInfo(agent);

            var args = new ContextMenuOpenArgs(
                addon,
                agent,
                parentAddonName,
                info.objectId,
                info.contentIdLower,
                info.text,
                info.objectWorld
            );
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
                    case NormalContextMenuItem custom:
                        custom.Agent = agent;
                        break;
                    case NormalContextSubMenuItem custom:
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
            NormalContextMenuItem custom => this.Language switch
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
            NormalContextSubMenuItem custom => this.Language switch
            {
                ClientLanguage.Japanese => custom.NameJapanese,
                ClientLanguage.English => custom.NameEnglish,
                ClientLanguage.German => custom.NameGerman,
                ClientLanguage.French => custom.NameFrench,
                _ => custom.NameEnglish,
            },
            InventoryContextSubMenuItem custom => this.Language switch
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

    private BaseContextMenuItem? SubMenuItem { get; set; }

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
            case NormalContextSubMenuItem sub:
            {
                this.SubMenuItem = sub;
                break;
            }
            case InventoryContextSubMenuItem sub:
            {
                this.SubMenuItem = sub;
                break;
            }
            case NormalContextMenuItem custom:
            {
                var addonName = this.GetParentAddonName(addon);
                var info = this.GetAgentInfo(custom.Agent);

                var args = new ContextMenuItemSelectedArgs(
                    addon,
                    custom.Agent,
                    addonName,
                    info.objectId,
                    info.contentIdLower,
                    info.text,
                    info.objectWorld
                );

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
                    info.itemId,
                    info.itemAmount,
                    info.itemHq
                );

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

    private IntPtr SubMenuTitle { get; set; } = IntPtr.Zero;

    private void FreeSubMenuTitle()
    {
        if (this.SubMenuTitle == IntPtr.Zero)
        {
            return;
        }

        this.UiAlloc.Free(this.SubMenuTitle);
        this.SubMenuTitle = IntPtr.Zero;
    }

    /// <returns>false if original should be called</returns>
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
        this._setUpContextSubMenu(agent);

        // step 2 (see SetUpContextSubMenu)
        var nameBytes = name.Encode().Terminate();
        this.SubMenuTitle = this.UiAlloc.Alloc((ulong)nameBytes.Length);
        Marshal.Copy(nameBytes, 0, this.SubMenuTitle, nameBytes.Length);
        var v10 = agent + 0x678 * *(byte*)(agent + 0x1740) + 0x28;
        *(byte**)(v10 + 0x668) = (byte*)this.SubMenuTitle;

        // step 3
        *selectedIdx = wasSelected;

        var secondaryArgsPtr = Marshal.ReadIntPtr(agent + MenuActionsPointerOffset);
        var submenuArgs = (AtkValue*)(secondaryArgsPtr + 8);

        var addon = this.GetAddonFromAgent(agent);
        var normalAction = (subMenuItem as NormalContextSubMenuItem)?.Action;
        var inventoryAction = (subMenuItem as InventoryContextSubMenuItem)?.Action;
        if (this.PopulateItems(addon, agent, normalAction, inventoryAction))
        {
            return true;
        }

        var booleanOffset = *(long*)(agent + *(byte*)(agent + 0x1740) * 0x678 + 0x690) != 0 ? 1 : 0;

        for (var i = 0; i < this.Items.Count; i++)
        {
            var item = this.Items[i];

            *(ushort*)secondaryArgsPtr += 1;
            var arg = &submenuArgs[ContextMenuItemOffset + i + 1];
            this._atkValueChangeType(arg, ValueType.String);
            var itemName = this.GetItemName(item);
            fixed (byte* namePtr = itemName.Encode().Terminate())
            {
                this._atkValueSetString(arg, namePtr);
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

    private void InventoryContextMenuEvent30Detour(IntPtr agent, IntPtr a2, int a3, int a4, short a5)
    {
        if (!this.SubMenuInner(agent))
        {
            this.InventoryContextMenuEvent30Hook!.Original(agent, a2, a3, a4, a5);
        }
    }
}

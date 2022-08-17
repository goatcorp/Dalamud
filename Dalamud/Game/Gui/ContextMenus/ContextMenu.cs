using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Configuration.Internal;
using Dalamud.Game.Gui.ContextMenus.OldStructs;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Serilog;

using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// Provides an interface to modify context menus.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    [ServiceManager.BlockingEarlyLoadedService]
    public sealed class ContextMenu : IDisposable, IServiceType
    {
        private const int MaxContextMenuItemsPerContextMenu = 32;

        private readonly OpenSubContextMenuDelegate? openSubContextMenu;

        #region Hooks

        private readonly Hook<ContextMenuOpenedDelegate> contextMenuOpenedHook;
        private readonly Hook<ContextMenuOpenedDelegate> subContextMenuOpenedHook;
        private readonly Hook<ContextMenuItemSelectedDelegate> contextMenuItemSelectedHook;
        private readonly Hook<ContextMenuOpeningDelegate> contextMenuOpeningHook;
        private readonly Hook<SubContextMenuOpeningDelegate> subContextMenuOpeningHook;

        #endregion

        private unsafe OldAgentContextInterface* currentAgentContextInterface;

        private IntPtr currentSubContextMenuTitle;

        private OpenSubContextMenuItem? selectedOpenSubContextMenuItem;
        private ContextMenuOpenedArgs? currentContextMenuOpenedArgs;

        [ServiceManager.ServiceConstructor]
        private ContextMenu(SigScanner sigScanner)
        {
            this.Address = new ContextMenuAddressResolver();
            this.Address.Setup(sigScanner);

            unsafe
            {
                this.openSubContextMenu = Marshal.GetDelegateForFunctionPointer<OpenSubContextMenuDelegate>(this.Address.OpenSubContextMenuPtr);

                this.contextMenuOpeningHook = Hook<ContextMenuOpeningDelegate>.FromAddress(this.Address.ContextMenuOpeningPtr, this.ContextMenuOpeningDetour);
                this.contextMenuOpenedHook = Hook<ContextMenuOpenedDelegate>.FromAddress(this.Address.ContextMenuOpenedPtr, this.ContextMenuOpenedDetour);
                this.contextMenuItemSelectedHook = Hook<ContextMenuItemSelectedDelegate>.FromAddress(this.Address.ContextMenuItemSelectedPtr, this.ContextMenuItemSelectedDetour);
                this.subContextMenuOpeningHook = Hook<SubContextMenuOpeningDelegate>.FromAddress(this.Address.SubContextMenuOpeningPtr, this.SubContextMenuOpeningDetour);
                this.subContextMenuOpenedHook = Hook<ContextMenuOpenedDelegate>.FromAddress(this.Address.SubContextMenuOpenedPtr, this.SubContextMenuOpenedDetour);
            }
        }

        #region Delegates

        private unsafe delegate bool OpenSubContextMenuDelegate(OldAgentContext* agentContext);

        private unsafe delegate IntPtr ContextMenuOpeningDelegate(IntPtr a1, IntPtr a2, IntPtr a3, uint a4, IntPtr a5, OldAgentContextInterface* agentContextInterface, IntPtr a7, ushort a8);

        private unsafe delegate bool ContextMenuOpenedDelegate(AddonContextMenu* addonContextMenu, int menuSize, AtkValue* atkValueArgs);

        private unsafe delegate bool ContextMenuItemSelectedDelegate(AddonContextMenu* addonContextMenu, int selectedIndex, byte a3);

        private unsafe delegate bool SubContextMenuOpeningDelegate(OldAgentContext* agentContext);

        #endregion

        /// <summary>
        /// Occurs when a context menu is opened by the game.
        /// </summary>
        public event ContextMenus.ContextMenuOpenedDelegate? ContextMenuOpened;

        private ContextMenuAddressResolver Address { get; set; }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            this.subContextMenuOpeningHook.Disable();
            this.contextMenuItemSelectedHook.Disable();
            this.subContextMenuOpenedHook.Disable();
            this.contextMenuOpenedHook.Disable();
            this.contextMenuOpeningHook.Disable();
        }

        private static unsafe bool IsInventoryContext(OldAgentContextInterface* agentContextInterface)
        {
            return agentContextInterface == AgentInventoryContext.Instance();
        }

        private static int GetContextMenuItemsHashCode(IEnumerable<ContextMenuItem> contextMenuItems)
        {
            unchecked
            {
                return contextMenuItems.Aggregate(17, (current, item) => (current * 23) + item.GetHashCode());
            }
        }

        [ServiceManager.CallWhenServicesReady]
        private void ContinueConstruction(GameGui gameGui)
        {
            if (!EnvironmentConfiguration.DalamudDoContextMenu)
                return;

            this.contextMenuOpeningHook.Enable();
            this.contextMenuOpenedHook.Enable();
            this.contextMenuItemSelectedHook.Enable();
            this.subContextMenuOpeningHook.Enable();
            this.subContextMenuOpenedHook.Enable();
        }

        private unsafe IntPtr ContextMenuOpeningDetour(IntPtr a1, IntPtr a2, IntPtr a3, uint a4, IntPtr a5, OldAgentContextInterface* agentContextInterface, IntPtr a7, ushort a8)
        {
            this.currentAgentContextInterface = agentContextInterface;
            return this.contextMenuOpeningHook!.Original(a1, a2, a3, a4, a5, agentContextInterface, a7, a8);
        }

        private unsafe bool ContextMenuOpenedDetour(AddonContextMenu* addonContextMenu, int atkValueCount, AtkValue* atkValues)
        {
            try
            {
                this.ContextMenuOpenedImplementation(addonContextMenu, ref atkValueCount, ref atkValues);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ContextMenuOpenedDetour");
            }

            return this.contextMenuOpenedHook.Original(addonContextMenu, atkValueCount, atkValues);
        }

        private unsafe void ContextMenuOpenedImplementation(AddonContextMenu* addonContextMenu, ref int atkValueCount, ref AtkValue* atkValues)
        {
            if (this.ContextMenuOpened == null
                || this.currentAgentContextInterface == null)
            {
                return;
            }

            var contextMenuReaderWriter = new ContextMenuReaderWriter(this.currentAgentContextInterface, atkValueCount, atkValues);

            // Check for a title.
            string? title = null;
            if (this.selectedOpenSubContextMenuItem != null)
            {
                title = this.selectedOpenSubContextMenuItem.Name.TextValue;

                // Write the custom title
                var titleAtkValue = &atkValues[1];
                fixed (byte* titlePtr = this.selectedOpenSubContextMenuItem.Name.Encode().NullTerminate())
                {
                    titleAtkValue->SetString(titlePtr);
                }
            }
            else if (contextMenuReaderWriter.Title != null)
            {
                title = contextMenuReaderWriter.Title.TextValue;
            }

            // Determine which event to raise.
            var contextMenuOpenedDelegate = this.ContextMenuOpened;

            // this.selectedOpenSubContextMenuItem is OpenSubContextMenuItem openSubContextMenuItem
            if (this.selectedOpenSubContextMenuItem != null)
            {
                contextMenuOpenedDelegate = this.selectedOpenSubContextMenuItem.Opened;
            }

            // Get the existing items from the game.
            // TODO: For inventory sub context menus, we take only the last item -- the return item.
            // This is because we're doing a hack to spawn a Second Tier sub context menu and then appropriating it.
            var contextMenuItems = contextMenuReaderWriter.Read();
            if (contextMenuItems == null)
                return;

            if (IsInventoryContext(this.currentAgentContextInterface) && this.selectedOpenSubContextMenuItem != null)
            {
                contextMenuItems = contextMenuItems.TakeLast(1).ToArray();
            }

            var beforeHashCode = GetContextMenuItemsHashCode(contextMenuItems);

            // Raise the event and get the context menu changes.
            this.currentContextMenuOpenedArgs = this.NotifyContextMenuOpened(addonContextMenu, this.currentAgentContextInterface, title, contextMenuOpenedDelegate, contextMenuItems);
            if (this.currentContextMenuOpenedArgs == null)
            {
                return;
            }

            var afterHashCode = GetContextMenuItemsHashCode(this.currentContextMenuOpenedArgs.Items);

            PluginLog.Warning($"{beforeHashCode}={afterHashCode}");

            // Only write to memory if the items were actually changed.
            if (beforeHashCode != afterHashCode)
            {
                // Write the new changes.
                contextMenuReaderWriter.Write(this.currentContextMenuOpenedArgs.Items);

                // Update the addon.
                atkValueCount = *(&addonContextMenu->AtkValuesCount) = (ushort)contextMenuReaderWriter.AtkValueCount;
                atkValues = *(&addonContextMenu->AtkValues) = contextMenuReaderWriter.AtkValues;
            }
        }

        private unsafe bool SubContextMenuOpeningDetour(OldAgentContext* agentContext)
        {
            return this.SubContextMenuOpeningImplementation(agentContext) || this.subContextMenuOpeningHook.Original(agentContext);
        }

        private unsafe bool SubContextMenuOpeningImplementation(OldAgentContext* agentContext)
        {
            if (this.openSubContextMenu == null || this.selectedOpenSubContextMenuItem == null)
            {
                return false;
            }

            // The important things to make this work are:
            // 1. Allocate a temporary sub context menu title. The value doesn't matter, we'll set it later.
            // 2. Context menu item count must equal 1 to tell the game there is enough space for the "< Return" item.
            // 3. Atk value count must equal the index of the first context menu item.
            //    This is enough to keep the base data, but excludes the context menu item data.
            //    We want to exclude context menu item data in this function because the game sometimes includes garbage items which can cause problems.
            //    After this function, the game adds the "< Return" item, and THEN we add our own items after that.

            this.openSubContextMenu(agentContext);

            // Allocate a new 1 byte title. This is required for the game to render the titled context menu style.
            // The actual value doesn't matter at this point, we'll set it later.
            MemoryHelper.GameFree(ref this.currentSubContextMenuTitle, (ulong)IntPtr.Size);
            this.currentSubContextMenuTitle = MemoryHelper.GameAllocateUi(1);
            *(&(&agentContext->AgentContextInterface)->SubContextMenuTitle) = (byte*)this.currentSubContextMenuTitle;
            *(byte*)this.currentSubContextMenuTitle = 0;

            // Expect at least 1 context menu item.
            (&agentContext->Items->AtkValues)[0].UInt = 1;

            // Expect a title. This isn't needed by the game, it's needed by ContextMenuReaderWriter which uses this to check if it's a context menu
            (&agentContext->Items->AtkValues)[1].ChangeType(ValueType.String);

            (&agentContext->Items->AtkValues)[1].String = (byte*)0;

            ContextMenuReaderWriter contextMenuReaderWriter = new ContextMenuReaderWriter(&agentContext->AgentContextInterface, agentContext->Items->AtkValueCount, &agentContext->Items->AtkValues);
            *(&agentContext->Items->AtkValueCount) = (ushort)contextMenuReaderWriter.FirstContextMenuItemIndex;

            return true;
        }

        private unsafe bool SubContextMenuOpenedDetour(AddonContextMenu* addonContextMenu, int atkValueCount, AtkValue* atkValues)
        {
            try
            {
                this.SubContextMenuOpenedImplementation(addonContextMenu, ref atkValueCount, ref atkValues);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "SubContextMenuOpenedDetour");
            }

            return this.subContextMenuOpenedHook.Original(addonContextMenu, atkValueCount, atkValues);
        }

        private unsafe void SubContextMenuOpenedImplementation(AddonContextMenu* addonContextMenu, ref int atkValueCount, ref AtkValue* atkValues)
        {
            this.ContextMenuOpenedImplementation(addonContextMenu, ref atkValueCount, ref atkValues);
        }

        private unsafe ContextMenuOpenedArgs? NotifyContextMenuOpened(AddonContextMenu* addonContextMenu, OldAgentContextInterface* agentContextInterface, string? title, ContextMenus.ContextMenuOpenedDelegate contextMenuOpenedDelegate, IEnumerable<ContextMenuItem> initialContextMenuItems)
        {
            var parentAddonName = this.GetParentAddonName(&addonContextMenu->AtkUnitBase);

            Log.Warning($"AgentContextInterface at: {new IntPtr(agentContextInterface):X}");

            InventoryItemContext? inventoryItemContext = null;
            GameObjectContext? gameObjectContext = null;
            if (IsInventoryContext(agentContextInterface))
            {
                var agentInventoryContext = (AgentInventoryContext*)agentContextInterface;
                inventoryItemContext = new InventoryItemContext(agentInventoryContext->TargetDummyItem.ItemID, agentInventoryContext->TargetDummyItem.Quantity, agentInventoryContext->TargetDummyItem.Flags.HasFlag(InventoryItem.ItemFlags.HQ));
            }
            else
            {
                var agentContext = (OldAgentContext*)agentContextInterface;

                uint? id = agentContext->GameObjectId;
                if (id == 0)
                {
                    id = null;
                }

                ulong? contentId = agentContext->GameObjectContentId;
                if (contentId == 0)
                {
                    contentId = null;
                }

                var name = MemoryHelper.ReadSeStringNullTerminated((IntPtr)agentContext->GameObjectName.StringPtr).TextValue;
                if (string.IsNullOrEmpty(name))
                {
                    name = null;
                }

                ushort? worldId = agentContext->GameObjectWorldId;
                if (worldId == 0)
                {
                    worldId = null;
                }

                if (id != null
                    || contentId != null
                    || name != null
                    || worldId != null)
                {
                    gameObjectContext = new GameObjectContext(id, contentId, name, worldId);
                }
            }

            // Temporarily remove the < Return item, for UX we should enforce that it is always last in the list.
            var lastContextMenuItem = initialContextMenuItems.LastOrDefault();
            if (lastContextMenuItem is GameContextMenuItem gameContextMenuItem && gameContextMenuItem.SelectedAction == 102)
            {
                initialContextMenuItems = initialContextMenuItems.SkipLast(1);
            }

            var contextMenuOpenedArgs = new ContextMenuOpenedArgs(addonContextMenu, agentContextInterface, parentAddonName, initialContextMenuItems)
            {
                Title = title,
                InventoryItemContext = inventoryItemContext,
                GameObjectContext = gameObjectContext,
            };

            try
            {
                contextMenuOpenedDelegate.Invoke(contextMenuOpenedArgs);
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, "NotifyContextMenuOpened");
                return null;
            }

            // Readd the < Return item
            if (lastContextMenuItem is GameContextMenuItem gameContextMenuItem1 && gameContextMenuItem1.SelectedAction == 102)
            {
                contextMenuOpenedArgs.Items.Add(lastContextMenuItem);
            }

            foreach (var contextMenuItem in contextMenuOpenedArgs.Items.ToArray())
            {
                // TODO: Game doesn't support nested sub context menus, but we might be able to.
                if (contextMenuItem is OpenSubContextMenuItem && contextMenuOpenedArgs.Title != null)
                {
                    contextMenuOpenedArgs.Items.Remove(contextMenuItem);
                    PluginLog.Warning($"Context menu '{contextMenuOpenedArgs.Title}' item '{contextMenuItem}' has been removed because nested sub context menus are not supported.");
                }
            }

            if (contextMenuOpenedArgs.Items.Count > MaxContextMenuItemsPerContextMenu)
            {
                PluginLog.LogWarning($"Context menu requesting {contextMenuOpenedArgs.Items.Count} of max {MaxContextMenuItemsPerContextMenu} items. Resizing list to compensate.");
                contextMenuOpenedArgs.Items.RemoveRange(MaxContextMenuItemsPerContextMenu, contextMenuOpenedArgs.Items.Count - MaxContextMenuItemsPerContextMenu);
            }

            return contextMenuOpenedArgs;
        }

        private unsafe bool ContextMenuItemSelectedDetour(AddonContextMenu* addonContextMenu, int selectedIndex, byte a3)
        {
            try
            {
                this.ContextMenuItemSelectedImplementation(addonContextMenu, selectedIndex);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ContextMenuItemSelectedDetour");
            }

            return this.contextMenuItemSelectedHook.Original(addonContextMenu, selectedIndex, a3);
        }

        private unsafe void ContextMenuItemSelectedImplementation(AddonContextMenu* addonContextMenu, int selectedIndex)
        {
            if (this.currentContextMenuOpenedArgs == null || selectedIndex == -1)
            {
                this.currentContextMenuOpenedArgs = null;
                this.selectedOpenSubContextMenuItem = null;
                return;
            }

            // Read the selected item directly from the game
            ContextMenuReaderWriter contextMenuReaderWriter = new ContextMenuReaderWriter(this.currentAgentContextInterface, addonContextMenu->AtkValuesCount, addonContextMenu->AtkValues);
            var gameContextMenuItems = contextMenuReaderWriter.Read();
            if (gameContextMenuItems == null)
                return;

            var gameSelectedItem = gameContextMenuItems.ElementAtOrDefault(selectedIndex);

            // This should be impossible
            if (gameSelectedItem == null)
            {
                this.currentContextMenuOpenedArgs = null;
                this.selectedOpenSubContextMenuItem = null;
                return;
            }

            // Match it with the items we already know about based on its name.
            // We can get into a state where we have a game item we don't recognize when another plugin has added one.
            var selectedItem = this.currentContextMenuOpenedArgs.Items.FirstOrDefault(item => item.Name.Encode().SequenceEqual(gameSelectedItem.Name.Encode()));

            this.selectedOpenSubContextMenuItem = null;
            if (selectedItem is CustomContextMenuItem customContextMenuItem)
            {
                try
                {
                    var customContextMenuItemSelectedArgs = new CustomContextMenuItemSelectedArgs(this.currentContextMenuOpenedArgs, customContextMenuItem);
                    customContextMenuItem.ItemSelected(customContextMenuItemSelectedArgs);
                }
                catch (Exception ex)
                {
                    PluginLog.LogError(ex, "ContextMenuItemSelectedImplementation");
                }
            }
            else if (selectedItem is OpenSubContextMenuItem openSubContextMenuItem)
            {
                this.selectedOpenSubContextMenuItem = openSubContextMenuItem;
            }

            this.currentContextMenuOpenedArgs = null;
        }

        private unsafe string? GetParentAddonName(AtkUnitBase* addonInterface)
        {
            var parentAddonId = addonInterface->ContextMenuParentID;
            if (parentAddonId == 0)
            {
                return null;
            }

            var atkStage = AtkStage.GetSingleton();
            var parentAddon = atkStage->RaptureAtkUnitManager->GetAddonById(parentAddonId);
            return Marshal.PtrToStringUTF8(new IntPtr(parentAddon->Name));
        }

        private unsafe AtkUnitBase* GetAddonFromAgent(AgentInterface* agentInterface)
        {
            return agentInterface->AddonId == 0 ? null : AtkStage.GetSingleton()->RaptureAtkUnitManager->GetAddonById((ushort)agentInterface->AddonId);
        }
    }
}

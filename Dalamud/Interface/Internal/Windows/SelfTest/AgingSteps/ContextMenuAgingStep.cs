using System;
using System.Runtime.CompilerServices;
using Dalamud.Data;
using Dalamud.Game.Gui.ContextMenus;
using Dalamud.Utility;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using Serilog;
using SeString = Dalamud.Game.Text.SeStringHandling.SeString;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
    /// <summary>
    /// Tests for context menu.
    /// </summary>
    internal class ContextMenuAgingStep : IAgingStep
    {
        private SubStep currentSubStep;

        private uint clickedItemId;
        private bool clickedItemHq;
        private uint clickedItemCount;

        private string clickedPlayerName;
        private ushort? clickedPlayerWorld;
        private ulong? clickedPlayerCid;
        private uint? clickedPlayerId;

        private enum SubStep
        {
            Start,
            TestItem,
            TestGameObject,
        }

        /// <inheritdoc/>
        public string Name => "Test Context Menu";

        /// <inheritdoc/>
        public SelfTestStepResult RunStep()
        {
            var contextMenu = Service<ContextMenu>.Get();
            var dataMgr = Service<DataManager>.Get();

            switch (this.currentSubStep)
            {
                case SubStep.Start:
                    contextMenu.ContextMenuOpened += this.ContextMenuOnContextMenuOpened;
                    this.currentSubStep++;
                    break;
                case SubStep.TestItem:
                    if (this.clickedItemId != 0)
                    {
                        var item = dataMgr.GetExcelSheet<Item>()!.GetRow(this.clickedItemId);
                        ImGui.Text($"Did you click \"{item!.Name.RawString}\", hq:{this.clickedItemHq}, count:{this.clickedItemCount}?");

                        if (ImGui.Button("Yes"))
                            this.currentSubStep++;

                        ImGui.SameLine();

                        if (ImGui.Button("No"))
                            return SelfTestStepResult.Fail;
                    }
                    else
                    {
                        ImGui.Text("Right-click an item.");
                    }

                    break;

                case SubStep.TestGameObject:
                    if (!this.clickedPlayerName.IsNullOrEmpty())
                    {
                        ImGui.Text($"Did you click \"{this.clickedPlayerName}\", world:{this.clickedPlayerWorld}, cid:{this.clickedPlayerCid}, id:{this.clickedPlayerId}?");

                        if (ImGui.Button("Yes"))
                            return SelfTestStepResult.Pass;

                        ImGui.SameLine();

                        if (ImGui.Button("No"))
                            return SelfTestStepResult.Fail;
                    }
                    else
                    {
                        ImGui.Text("Right-click an item.");
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return SelfTestStepResult.Waiting;
        }

        /// <inheritdoc/>
        public void CleanUp()
        {
            var contextMenu = Service<ContextMenu>.Get();
            contextMenu.ContextMenuOpened -= this.ContextMenuOnContextMenuOpened;

            this.currentSubStep = SubStep.Start;
            this.clickedItemId = 0;
        }

        private void ContextMenuOnContextMenuOpened(ContextMenuOpenedArgs args)
        {
            Log.Information("Got context menu with parent addon: {ParentAddonName}", args.ParentAddonName);
            switch (args.ParentAddonName)
            {
                case "Inventory":
                    if (this.currentSubStep != SubStep.TestItem)
                        return;

                    args.Items.Add(new CustomContextMenuItem("Aging Item Test", selectedArgs =>
                    {
                        this.clickedItemId = args.InventoryItemContext!.Id;
                        this.clickedItemHq = args.InventoryItemContext!.IsHighQuality;
                        this.clickedItemCount = args.InventoryItemContext!.Count;
                        Log.Warning("Clicked item: {Id} hq:{Hq} count:{Count}", this.clickedItemId, this.clickedItemHq, this.clickedItemCount);
                    }));
                    break;

                case null:
                case "_PartyList":
                case "ChatLog":
                case "ContactList":
                case "ContentMemberList":
                case "CrossWorldLinkshell":
                case "FreeCompany":
                case "FriendList":
                case "LookingForGroup":
                case "LinkShell":
                case "PartyMemberList":
                case "SocialList":
                    if (this.currentSubStep != SubStep.TestGameObject || args.GameObjectContext == null || args.GameObjectContext.Name.IsNullOrEmpty())
                        return;

                    args.Items.Add(new CustomContextMenuItem("Aging Character Test", selectedArgs =>
                    {
                        this.clickedPlayerName = args.GameObjectContext.Name!;
                        this.clickedPlayerWorld = args.GameObjectContext.WorldId;
                        this.clickedPlayerCid = args.GameObjectContext.ContentId;
                        this.clickedPlayerId = args.GameObjectContext.Id;

                        Log.Warning("Clicked player: {Name} world:{World} cid:{Cid} id:{Id}", this.clickedPlayerName, this.clickedPlayerWorld, this.clickedPlayerCid, this.clickedPlayerId);
                    }));
                    break;
            }
        }
    }
}

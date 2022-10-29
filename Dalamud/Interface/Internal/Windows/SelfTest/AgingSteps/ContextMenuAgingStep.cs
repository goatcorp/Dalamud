/*using System;

using Dalamud.Data;
using Dalamud.Utility;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Serilog;*/

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Tests for context menu.
/// </summary>
internal class ContextMenuAgingStep : IAgingStep
{
    /*
    private SubStep currentSubStep;

    private uint clickedItemId;
    private bool clickedItemHq;
    private uint clickedItemCount;

    private string? clickedPlayerName;
    private ushort? clickedPlayerWorld;
    private ulong? clickedPlayerCid;
    private uint? clickedPlayerId;

    private bool multipleTriggerOne;
    private bool multipleTriggerTwo;

    private enum SubStep
    {
        Start,
        TestItem,
        TestGameObject,
        TestSubMenu,
        TestMultiple,
        Finish,
    }
    */

    /// <inheritdoc/>
    public string Name => "Test Context Menu";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        /*
        var contextMenu = Service<ContextMenu>.Get();
        var dataMgr = Service<DataManager>.Get();

        ImGui.Text(this.currentSubStep.ToString());

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

                    if (ImGui.Button("Skip"))
                        this.currentSubStep++;
                }

                break;

            case SubStep.TestGameObject:
                if (!this.clickedPlayerName.IsNullOrEmpty())
                {
                    ImGui.Text($"Did you click \"{this.clickedPlayerName}\", world:{this.clickedPlayerWorld}, cid:{this.clickedPlayerCid}, id:{this.clickedPlayerId}?");

                    if (ImGui.Button("Yes"))
                        this.currentSubStep++;

                    ImGui.SameLine();

                    if (ImGui.Button("No"))
                        return SelfTestStepResult.Fail;
                }
                else
                {
                    ImGui.Text("Right-click a character.");

                    if (ImGui.Button("Skip"))
                        this.currentSubStep++;
                }

                break;
            case SubStep.TestSubMenu:
                if (this.multipleTriggerOne && this.multipleTriggerTwo)
                {
                    this.currentSubStep++;
                    this.multipleTriggerOne = this.multipleTriggerTwo = false;
                }
                else
                {
                    ImGui.Text("Right-click a character and select both options in the submenu.");

                    if (ImGui.Button("Skip"))
                        this.currentSubStep++;
                }

                break;

            case SubStep.TestMultiple:
                if (this.multipleTriggerOne && this.multipleTriggerTwo)
                {
                    this.currentSubStep = SubStep.Finish;
                    return SelfTestStepResult.Pass;
                }

                ImGui.Text("Select both options on any context menu.");
                if (ImGui.Button("Skip"))
                    this.currentSubStep++;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return SelfTestStepResult.Waiting;
        */

        return SelfTestStepResult.Pass;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        /*
        var contextMenu = Service<ContextMenu>.Get();
        contextMenu.ContextMenuOpened -= this.ContextMenuOnContextMenuOpened;

        this.currentSubStep = SubStep.Start;
        this.clickedItemId = 0;
        this.clickedPlayerName = null;
        this.multipleTriggerOne = this.multipleTriggerTwo = false;
        */
    }

    /*
    private void ContextMenuOnContextMenuOpened(ContextMenuOpenedArgs args)
    {
        Log.Information("Got context menu with parent addon: {ParentAddonName}, title:{Title}, itemcnt:{ItemCount}", args.ParentAddonName, args.Title, args.Items.Count);
        if (args.GameObjectContext != null)
        {
            Log.Information("   => GameObject:{GameObjectName} world:{World} cid:{Cid} id:{Id}", args.GameObjectContext.Name, args.GameObjectContext.WorldId, args.GameObjectContext.ContentId, args.GameObjectContext.Id);
        }

        if (args.InventoryItemContext != null)
        {
            Log.Information("   => Inventory:{ItemId} hq:{Hq} count:{Count}", args.InventoryItemContext.Id, args.InventoryItemContext.IsHighQuality, args.InventoryItemContext.Count);
        }

        switch (this.currentSubStep)
        {
            case SubStep.TestSubMenu:
                args.AddCustomSubMenu("Aging Submenu", openedArgs =>
                {
                    openedArgs.AddCustomItem("Submenu Item 1", _ =>
                    {
                        this.multipleTriggerOne = true;
                    });

                    openedArgs.AddCustomItem("Submenu Item 2", _ =>
                    {
                        this.multipleTriggerTwo = true;
                    });
                });

                return;
            case SubStep.TestMultiple:
                args.AddCustomItem("Aging Item 1", _ =>
                {
                    this.multipleTriggerOne = true;
                });

                args.AddCustomItem("Aging Item 2", _ =>
                {
                    this.multipleTriggerTwo = true;
                });

                return;
            case SubStep.Finish:
                return;

            default:
                switch (args.ParentAddonName)
                {
                    case "Inventory":
                        if (this.currentSubStep != SubStep.TestItem)
                            return;

                        args.AddCustomItem("Aging Item", _ =>
                        {
                            this.clickedItemId = args.InventoryItemContext!.Id;
                            this.clickedItemHq = args.InventoryItemContext!.IsHighQuality;
                            this.clickedItemCount = args.InventoryItemContext!.Count;
                            Log.Warning("Clicked item: {Id} hq:{Hq} count:{Count}", this.clickedItemId, this.clickedItemHq, this.clickedItemCount);
                        });
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

                        args.AddCustomItem("Aging Character", _ =>
                        {
                            this.clickedPlayerName = args.GameObjectContext.Name!;
                            this.clickedPlayerWorld = args.GameObjectContext.WorldId;
                            this.clickedPlayerCid = args.GameObjectContext.ContentId;
                            this.clickedPlayerId = args.GameObjectContext.Id;

                            Log.Warning("Clicked player: {Name} world:{World} cid:{Cid} id:{Id}", this.clickedPlayerName, this.clickedPlayerWorld, this.clickedPlayerCid, this.clickedPlayerId);
                        });

                        break;
                }

                break;
        }
    }
    */
}

using Dalamud.Data;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Network.Structures.InfoProxy;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;

using ImGuiNET;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Tests for context menu.
/// </summary>
internal class ContextMenuAgingStep : IAgingStep
{
    private SubStep currentSubStep;

    private bool? targetInventorySubmenuOpened;
    private PlayerCharacter? targetCharacter;

    private ExcelSheet<Item> itemSheet;

    private enum SubStep
    {
        Start,
        TestInventoryAndSubmenu,
        TestDefault,
        Finish,
    }

    /// <inheritdoc/>
    public string Name => "Test Context Menu";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var contextMenu = Service<ContextMenu>.Get();
        var dataMgr = Service<DataManager>.Get();
        this.itemSheet = dataMgr.GetExcelSheet<Item>()!;

        ImGui.Text(this.currentSubStep.ToString());

        switch (this.currentSubStep)
        {
            case SubStep.Start:
                contextMenu.OnMenuOpened += this.OnMenuOpened;
                this.currentSubStep++;
                break;
            case SubStep.TestInventoryAndSubmenu:
                if (this.targetInventorySubmenuOpened == true)
                {
                    ImGui.Text($"Is the data in the submenu correct?");

                    if (ImGui.Button("Yes"))
                        this.currentSubStep++;

                    ImGui.SameLine();

                    if (ImGui.Button("No"))
                        return SelfTestStepResult.Fail;
                }
                else
                {
                    ImGui.Text("Right-click an item and select \"Self Test\".");

                    if (ImGui.Button("Skip"))
                        this.currentSubStep++;
                }

                break;

            case SubStep.TestDefault:
                if (this.targetCharacter is { } character)
                {
                    ImGui.Text($"Did you click \"{character.Name}\" ({character.ClassJob.GameData!.Abbreviation.ToDalamudString()})?");

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
            case SubStep.Finish:
                return SelfTestStepResult.Pass;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return SelfTestStepResult.Waiting;
    }
    
    /// <inheritdoc/>
    public void CleanUp()
    {
        var contextMenu = Service<ContextMenu>.Get();
        contextMenu.OnMenuOpened -= this.OnMenuOpened;

        this.currentSubStep = SubStep.Start;
        this.targetInventorySubmenuOpened = null;
        this.targetCharacter = null;
    }

    private void OnMenuOpened(MenuOpenedArgs args)
    {
        switch (this.currentSubStep)
        {
            case SubStep.TestInventoryAndSubmenu:
                if (args.MenuType == ContextMenuType.Inventory)
                {
                    args.AddMenuItem(new()
                    {
                        Name = "Aging Item",
                        Priority = -1,
                        IsSubmenu = true,
                        OnClicked = (MenuItemClickedArgs a) =>
                        {
                            SeString name;
                            uint count;
                            var targetItem = (a.Target as MenuTargetInventory).TargetItem;
                            if (targetItem is { } item)
                            {
                                name = (this.itemSheet.GetRow(item.ItemId)?.Name.ToDalamudString() ?? $"Unknown ({item.ItemId})") + (item.IsHq ? $" {SeIconChar.HighQuality.ToIconString()}" : string.Empty);
                                count = item.Quantity;
                            }
                            else
                            {
                                name = "None";
                                count = 0;
                            }

                            a.OpenSubmenu(new MenuItem[]
                            {
                            new()
                            {
                                Name = "Name: " + name,
                                IsEnabled = false,
                            },
                            new()
                            {
                                Name = $"Count: {count}",
                                IsEnabled = false,
                            },
                            });

                            this.targetInventorySubmenuOpened = true;
                        },
                    });
                }

                break;

            case SubStep.TestDefault:
                if (args.Target is MenuTargetDefault { TargetObject: PlayerCharacter { } character })
                    this.targetCharacter = character;
                break;

            case SubStep.Finish:
                return;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dalamud.Data;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Serilog;

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
    private ExcelSheet<Materia> materiaSheet;
    private ExcelSheet<Stain> stainSheet;

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
        this.materiaSheet = dataMgr.GetExcelSheet<Materia>()!;
        this.stainSheet = dataMgr.GetExcelSheet<Stain>()!;

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
        this.LogMenuOpened(args);

        switch (this.currentSubStep)
        {
            case SubStep.TestInventoryAndSubmenu:
                if (args.MenuType == ContextMenuType.Inventory)
                {
                    args.AddMenuItem(new()
                    {
                        Name = "Self Test",
                        Prefix = SeIconChar.Hyadelyn,
                        PrefixColor = 56,
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

    private void LogMenuOpened(MenuOpenedArgs args)
    {
        Log.Verbose($"Got {args.MenuType} context menu with addon 0x{args.AddonPtr:X8} ({args.AddonName}) and agent 0x{args.AgentPtr:X8}");
        if (args.Target is MenuTargetDefault targetDefault)
        {
            {
                var b = new StringBuilder();
                b.AppendLine($"Target: {targetDefault.TargetName}");
                b.AppendLine($"Home World: {targetDefault.TargetHomeWorld.GameData?.Name.ToDalamudString() ?? "Unknown"} ({targetDefault.TargetHomeWorld.Id})");
                b.AppendLine($"Content Id: 0x{targetDefault.TargetContentId:X8}");
                b.AppendLine($"Object Id: 0x{targetDefault.TargetObjectId:X8}");
                Log.Verbose(b.ToString());
            }

            if (targetDefault.TargetCharacter is { } character)
            {
                var b = new StringBuilder();
                b.AppendLine($"Character: {character.Name}");

                b.AppendLine($"Name: {character.Name}");
                b.AppendLine($"Content Id: 0x{character.ContentId:X8}");
                b.AppendLine($"FC Tag: {character.FCTag}");

                b.AppendLine($"Job: {character.ClassJob.GameData?.Abbreviation.ToDalamudString() ?? "Unknown"} ({character.ClassJob.Id})");
                b.AppendLine($"Statuses: {string.Join(", ", character.Statuses.Select(s => s.GameData?.Name.ToDalamudString() ?? s.Id.ToString()))}");
                b.AppendLine($"Home World: {character.HomeWorld.GameData?.Name.ToDalamudString() ?? "Unknown"} ({character.HomeWorld.Id})");
                b.AppendLine($"Current World: {character.CurrentWorld.GameData?.Name.ToDalamudString() ?? "Unknown"} ({character.CurrentWorld.Id})");
                b.AppendLine($"Is From Other Server: {character.IsFromOtherServer}");

                b.Append("Location: ");
                if (character.Location.GameData is { } location)
                    b.Append($"{location.PlaceNameRegion.Value?.Name.ToDalamudString() ?? "Unknown"}/{location.PlaceNameZone.Value?.Name.ToDalamudString() ?? "Unknown"}/{location.PlaceName.Value?.Name.ToDalamudString() ?? "Unknown"}");
                else
                    b.Append("Unknown");
                b.AppendLine($" ({character.Location.Id})");

                b.AppendLine($"Grand Company: {character.GrandCompany.GameData?.Name.ToDalamudString() ?? "Unknown"} ({character.GrandCompany.Id})");
                b.AppendLine($"Client Language: {character.ClientLanguage}");
                b.AppendLine($"Languages: {string.Join(", ", character.Languages)}");
                b.AppendLine($"Gender: {character.Gender}");
                b.AppendLine($"Display Group: {character.DisplayGroup}");
                b.AppendLine($"Sort: {character.Sort}");

                Log.Verbose(b.ToString());
            }
            else
            {
                Log.Verbose($"Character: null");
            }
        }
        else if (args.Target is MenuTargetInventory targetInventory)
        {
            if (targetInventory.TargetItem is { } item)
            {
                var b = new StringBuilder();
                b.AppendLine($"Item: {(item.IsEmpty ? "None" : this.itemSheet.GetRow(item.ItemId)?.Name.ToDalamudString())} ({item.ItemId})");
                b.AppendLine($"Container: {item.ContainerType}");
                b.AppendLine($"Slot: {item.InventorySlot}");
                b.AppendLine($"Quantity: {item.Quantity}");
                b.AppendLine($"{(item.IsCollectable ? "Collectability" : "Spiritbond")}: {item.Spiritbond}");
                b.AppendLine($"Condition: {item.Condition / 300f:0.00}% ({item.Condition})");
                b.AppendLine($"Is HQ: {item.IsHq}");
                b.AppendLine($"Is Company Crest Applied: {item.IsCompanyCrestApplied}");
                b.AppendLine($"Is Relic: {item.IsRelic}");
                b.AppendLine($"Is Collectable: {item.IsCollectable}");

                b.Append("Materia: ");
                var materias = new List<string>();
                foreach (var (materiaId, materiaGrade) in item.Materia.ToArray().Zip(item.MateriaGrade.ToArray()).Where(m => m.First != 0))
                {
                    Log.Verbose($"{materiaId} {materiaGrade}");
                    if (this.materiaSheet.GetRow(materiaId) is { } materia &&
                        materia.Item[materiaGrade].Value is { } materiaItem)
                        materias.Add($"{materiaItem.Name.ToDalamudString()}");
                    else
                        materias.Add($"Unknown (Id: {materiaId}, Grade: {materiaGrade})");
                }

                if (materias.Count == 0)
                    b.AppendLine("None");
                else
                    b.AppendLine(string.Join(", ", materias));

                b.Append($"Dye/Stain: ");
                if (item.Stain != 0)
                    b.AppendLine($"{this.stainSheet.GetRow(item.Stain)?.Name.ToDalamudString() ?? "Unknown"} ({item.Stain})");
                else
                    b.AppendLine("None");

                b.Append("Glamoured Item: ");
                if (item.GlamourId != 0)
                    b.AppendLine($"{this.itemSheet.GetRow(item.GlamourId)?.Name.ToDalamudString() ?? "Unknown"} ({item.GlamourId})");
                else
                    b.AppendLine("None");

                Log.Verbose(b.ToString());
            }
            else
            {
                Log.Verbose("Item: null");
            }
        }
        else
        {
            Log.Verbose($"Target: Unknown ({args.Target?.GetType().Name ?? "null"})");
        }
    }
}

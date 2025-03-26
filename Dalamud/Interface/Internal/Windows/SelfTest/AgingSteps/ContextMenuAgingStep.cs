using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dalamud.Data;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.Sheets;
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
        this.itemSheet = dataMgr.GetExcelSheet<Item>();
        this.materiaSheet = dataMgr.GetExcelSheet<Materia>();
        this.stainSheet = dataMgr.GetExcelSheet<Stain>();

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
                    ImGui.Text($"Did you click \"{character.Name}\" ({character.ClassJob.Value.Abbreviation.ExtractText()})?");

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

    private void OnMenuOpened(IMenuOpenedArgs args)
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
                        OnClicked = (IMenuItemClickedArgs a) =>
                        {
                            SeString name;
                            int count;
                            var targetItem = (a.Target as MenuTargetInventory)!.TargetItem;
                            if (targetItem is { } item)
                            {
                                name = (this.itemSheet.GetRowOrDefault(item.ItemId)?.Name.ExtractText() ?? $"Unknown ({item.ItemId})") + (item.IsHq ? $" {SeIconChar.HighQuality.ToIconString()}" : string.Empty);
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

    private void LogMenuOpened(IMenuOpenedArgs args)
    {
        Log.Verbose($"Got {args.MenuType} context menu with addon 0x{args.AddonPtr:X8} ({args.AddonName}) and agent 0x{args.AgentPtr:X8}");
        if (args.Target is MenuTargetDefault targetDefault)
        {
            {
                var b = new StringBuilder();
                b.AppendLine($"Target: {targetDefault.TargetName}");
                b.AppendLine($"Home World: {targetDefault.TargetHomeWorld.ValueNullable?.Name.ExtractText() ?? "Unknown"} ({targetDefault.TargetHomeWorld.RowId})");
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

                b.AppendLine($"Job: {character.ClassJob.ValueNullable?.Abbreviation.ExtractText() ?? "Unknown"} ({character.ClassJob.RowId})");
                b.AppendLine($"Statuses: {string.Join(", ", character.Statuses.Select(s => s.ValueNullable?.Name.ExtractText() ?? s.RowId.ToString()))}");
                b.AppendLine($"Home World: {character.HomeWorld.ValueNullable?.Name.ExtractText() ?? "Unknown"} ({character.HomeWorld.RowId})");
                b.AppendLine($"Current World: {character.CurrentWorld.ValueNullable?.Name.ExtractText() ?? "Unknown"} ({character.CurrentWorld.RowId})");
                b.AppendLine($"Is From Other Server: {character.IsFromOtherServer}");

                b.Append("Location: ");
                if (character.Location.ValueNullable is { } location)
                    b.Append($"{location.PlaceNameRegion.ValueNullable?.Name.ExtractText() ?? "Unknown"}/{location.PlaceNameZone.ValueNullable?.Name.ExtractText() ?? "Unknown"}/{location.PlaceName.ValueNullable?.Name.ExtractText() ?? "Unknown"}");
                else
                    b.Append("Unknown");
                b.AppendLine($" ({character.Location.RowId})");

                b.AppendLine($"Grand Company: {character.GrandCompany.ValueNullable?.Name.ExtractText() ?? "Unknown"} ({character.GrandCompany.RowId})");
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
                b.AppendLine($"Item: {(item.IsEmpty ? "None" : this.itemSheet.GetRowOrDefault(item.ItemId)?.Name.ExtractText())} ({item.ItemId})");
                b.AppendLine($"Container: {item.ContainerType}");
                b.AppendLine($"Slot: {item.InventorySlot}");
                b.AppendLine($"Quantity: {item.Quantity}");
                b.AppendLine($"{(item.IsCollectable ? "Collectability" : "Spiritbond")}: {item.SpiritbondOrCollectability}");
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
                        materias.Add($"{materiaItem.Name.ExtractText()}");
                    else
                        materias.Add($"Unknown (Id: {materiaId}, Grade: {materiaGrade})");
                }

                if (materias.Count == 0)
                    b.AppendLine("None");
                else
                    b.AppendLine(string.Join(", ", materias));

                b.Append($"Dye/Stain: ");
                for (var i = 0; i < item.Stains.Length; i++)
                {
                    var stainId = item.Stains[i];
                    if (stainId != 0)
                    {
                        var stainName = this.stainSheet.GetRowOrDefault(stainId)?.Name.ExtractText() ?? "Unknown";
                        b.AppendLine($"  Stain {i + 1}: {stainName} ({stainId})");
                    }
                    else
                    {
                        b.AppendLine($"  Stain {i + 1}: None");
                    }
                }

                if (item.Stains[0] != 0)
                    b.AppendLine($"{this.stainSheet.GetRowOrDefault(item.Stains[0])?.Name.ExtractText() ?? "Unknown"} ({item.Stains[0]})");
                else
                    b.AppendLine("None");

                b.Append("Glamoured Item: ");
                if (item.GlamourId != 0)
                    b.AppendLine($"{this.itemSheet.GetRowOrDefault(item.GlamourId)?.Name.ExtractText() ?? "Unknown"} ({item.GlamourId})");
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

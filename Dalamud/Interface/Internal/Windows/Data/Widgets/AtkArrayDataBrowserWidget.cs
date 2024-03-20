using System.Numerics;

using Dalamud.Memory;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying AtkArrayData.
/// </summary>
internal unsafe class AtkArrayDataBrowserWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "atkarray" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Atk Array Data"; 

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var fontWidth = ImGui.CalcTextSize("A").X;
        var fontHeight = ImGui.GetTextLineHeightWithSpacing();
        var uiModule = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule();

        if (uiModule == null)
        {
            ImGui.Text("UIModule unavailable.");
            return;
        }

        var atkArrayDataHolder = &uiModule->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;

        if (ImGui.BeginTabBar("AtkArrayDataBrowserTabBar"))
        {
            if (ImGui.BeginTabItem($"NumberArrayData [{atkArrayDataHolder->NumberArrayCount}]"))
            {
                if (ImGui.BeginTable("NumberArrayDataTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
                {
                    ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, fontWidth * 10);
                    ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, fontWidth * 10);
                    ImGui.TableSetupColumn("Pointer", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();
                    for (var numberArrayIndex = 0; numberArrayIndex < atkArrayDataHolder->NumberArrayCount; numberArrayIndex++)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text($"{numberArrayIndex} [{numberArrayIndex * 8:X}]");
                        ImGui.TableNextColumn();
                        var numberArrayData = atkArrayDataHolder->NumberArrays[numberArrayIndex];
                        if (numberArrayData != null)
                        {
                            ImGui.Text($"{numberArrayData->AtkArrayData.Size}");
                            ImGui.TableNextColumn();
                            if (ImGui.TreeNodeEx($"{(long)numberArrayData:X}###{numberArrayIndex}", ImGuiTreeNodeFlags.SpanFullWidth))
                            {
                                ImGui.NewLine();
                                var tableHeight = Math.Min(40, numberArrayData->AtkArrayData.Size + 4);
                                if (ImGui.BeginTable($"NumberArrayDataTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0.0F, fontHeight * tableHeight)))
                                {
                                    ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, fontWidth * 6);
                                    ImGui.TableSetupColumn("Hex", ImGuiTableColumnFlags.WidthFixed, fontWidth * 9);
                                    ImGui.TableSetupColumn("Integer", ImGuiTableColumnFlags.WidthFixed, fontWidth * 12);
                                    ImGui.TableSetupColumn("Float", ImGuiTableColumnFlags.WidthFixed, fontWidth * 20);
                                    ImGui.TableHeadersRow();
                                    for (var numberIndex = 0; numberIndex < numberArrayData->AtkArrayData.Size; numberIndex++)
                                    {
                                        ImGui.TableNextRow();
                                        ImGui.TableNextColumn();
                                        ImGui.Text($"{numberIndex}");
                                        ImGui.TableNextColumn();
                                        ImGui.Text($"{numberArrayData->IntArray[numberIndex]:X}");
                                        ImGui.TableNextColumn();
                                        ImGui.Text($"{numberArrayData->IntArray[numberIndex]}");
                                        ImGui.TableNextColumn();
                                        ImGui.Text($"{*(float*)&numberArrayData->IntArray[numberIndex]}");
                                    }

                                    ImGui.EndTable();
                                }

                                ImGui.TreePop();
                            }
                        }
                        else
                        {
                            ImGui.TextDisabled("--");
                            ImGui.TableNextColumn();
                            ImGui.TextDisabled("--");
                        }
                    }

                    ImGui.EndTable();
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem($"StringArrayData [{atkArrayDataHolder->StringArrayCount}]"))
            {
                if (ImGui.BeginTable("StringArrayDataTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
                {
                    ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, fontWidth * 10);
                    ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, fontWidth * 10);
                    ImGui.TableSetupColumn("Pointer", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();
                    for (var stringArrayIndex = 0; stringArrayIndex < atkArrayDataHolder->StringArrayCount; stringArrayIndex++)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text($"{stringArrayIndex} [{stringArrayIndex * 8:X}]");
                        ImGui.TableNextColumn();
                        var stringArrayData = atkArrayDataHolder->StringArrays[stringArrayIndex];
                        if (stringArrayData != null)
                        {
                            ImGui.Text($"{stringArrayData->AtkArrayData.Size}");
                            ImGui.TableNextColumn();
                            if (ImGui.TreeNodeEx($"{(long)stringArrayData:X}###{stringArrayIndex}", ImGuiTreeNodeFlags.SpanFullWidth))
                            {
                                ImGui.NewLine();
                                var tableHeight = Math.Min(40, stringArrayData->AtkArrayData.Size + 4);
                                if (ImGui.BeginTable($"StringArrayDataTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0.0F, fontHeight * tableHeight)))
                                {
                                    ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, fontWidth * 6);
                                    ImGui.TableSetupColumn("String", ImGuiTableColumnFlags.WidthStretch);
                                    ImGui.TableHeadersRow();
                                    for (var stringIndex = 0; stringIndex < stringArrayData->AtkArrayData.Size; stringIndex++)
                                    {
                                        ImGui.TableNextRow();
                                        ImGui.TableNextColumn();
                                        ImGui.Text($"{stringIndex}");
                                        ImGui.TableNextColumn();
                                        if (stringArrayData->StringArray[stringIndex] != null)
                                        {
                                            ImGui.Text($"{MemoryHelper.ReadSeStringNullTerminated(new IntPtr(stringArrayData->StringArray[stringIndex]))}");
                                        }
                                        else
                                        {
                                            ImGui.TextDisabled("--");
                                        }
                                    }

                                    ImGui.EndTable();
                                }

                                ImGui.TreePop();
                            }
                        }
                        else
                        {
                            ImGui.TextDisabled("--");
                            ImGui.TableNextColumn();
                            ImGui.TextDisabled("--");
                        }
                    }

                    ImGui.EndTable();
                }

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }
}

using Dalamud.Interface.Internal.UiDebug2.Utility;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;

using ImGuiNET;

using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <inheritdoc cref="AddonTree"/>
public unsafe partial class AddonTree
{
    /// <summary>
    /// Prints a table of AtkValues associated with a given addon.
    /// </summary>
    /// <param name="addon">The addon to look up.</param>
    internal static void PrintAtkValues(AtkUnitBase* addon)
    {
        var atkValue = addon->AtkValues;
        if (addon->AtkValuesCount > 0 && atkValue != null)
        {
            using var tree = ImRaii.TreeNode($"Atk Values [{addon->AtkValuesCount}]###atkValues_{addon->NameString}");
            if (tree.Success)
            {
                using var tbl = ImRaii.Table("atkUnitBase_atkValueTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);

                if (tbl.Success)
                {
                    ImGui.TableSetupColumn("Index");
                    ImGui.TableSetupColumn("Type");
                    ImGui.TableSetupColumn("Value");
                    ImGui.TableHeadersRow();

                    try
                    {
                        for (var i = 0; i < addon->AtkValuesCount; i++)
                        {
                            ImGui.TableNextColumn();
                            if (atkValue->Type == 0)
                            {
                                ImGuiHelpers.SafeTextDisabled($"#{i}");
                            }
                            else
                            {
                                ImGui.TextUnformatted($"#{i}");
                            }

                            ImGui.TableNextColumn();
                            if (atkValue->Type == 0)
                            {
                                ImGuiHelpers.SafeTextDisabled("Not Set");
                            }
                            else
                            {
                                ImGui.TextUnformatted($"{atkValue->Type}");
                            }

                            ImGui.TableNextColumn();

                            switch (atkValue->Type)
                            {
                                case 0:
                                    break;
                                case ValueType.Int:
                                case ValueType.UInt:
                                {
                                    ImGui.TextUnformatted($"{atkValue->Int}");
                                    break;
                                }

                                case ValueType.ManagedString:
                                case ValueType.String8:
                                case ValueType.String:
                                {
                                    if (atkValue->String == null)
                                    {
                                        ImGuiHelpers.SafeTextDisabled("null");
                                    }
                                    else
                                    {
                                        var str = MemoryHelper.ReadSeStringNullTerminated(new nint(atkValue->String));
                                        Util.ShowStruct(str, (ulong)atkValue);
                                    }

                                    break;
                                }

                                case ValueType.Bool:
                                {
                                    ImGui.TextUnformatted($"{atkValue->Byte != 0}");
                                    break;
                                }

                                case ValueType.Pointer:
                                    ImGui.TextUnformatted($"{(nint)atkValue->Pointer}");
                                    break;

                                default:
                                {
                                    ImGuiHelpers.SafeTextDisabled("Unhandled Type");
                                    ImGui.SameLine();
                                    Util.ShowStruct(atkValue);
                                    break;
                                }
                            }

                            atkValue++;
                        }
                    }
                    catch (Exception ex)
                    {
                        ImGuiHelpers.SafeTextColored(new(1, 0, 0, 1), $"{ex}");
                    }
                }
            }

            Gui.PaddedSeparator();
        }
    }
}

using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Component.GUI;

using static Dalamud.Bindings.ImGui.ImGuiTableColumnFlags;
using static Dalamud.Bindings.ImGui.ImGuiTableFlags;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <summary>
/// Class that prints the events table for a node, where applicable.
/// </summary>
public static class Events
{
    /// <summary>
    /// Prints out each <see cref="AtkEventManager.Event"/> for a given node.
    /// </summary>
    /// <param name="node">The node to print events for.</param>
    internal static unsafe void PrintEvents(AtkResNode* node)
    {
        var evt = node->AtkEventManager.Event;
        if (evt == null)
        {
            return;
        }

        using var tree = ImRaii.TreeNode($"Events##{(nint)node:X}eventTree");

        if (tree.Success)
        {
            using var tbl = ImRaii.Table($"##{(nint)node:X}eventTable", 7, Resizable | SizingFixedFit | Borders | RowBg);

            if (tbl.Success)
            {
                ImGui.TableSetupColumn("#"u8, WidthFixed);
                ImGui.TableSetupColumn("Type"u8, WidthFixed);
                ImGui.TableSetupColumn("Param"u8, WidthFixed);
                ImGui.TableSetupColumn("Flags"u8, WidthFixed);
                ImGui.TableSetupColumn("StateFlags1"u8, WidthFixed);
                ImGui.TableSetupColumn("Target"u8, WidthFixed);
                ImGui.TableSetupColumn("Listener"u8, WidthFixed);

                ImGui.TableHeadersRow();

                var i = 0;
                while (evt != null)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text($"{i++}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{evt->State.EventType}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{evt->Param}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{evt->State.StateFlags}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{evt->State.ReturnFlags}");
                    ImGui.TableNextColumn();
                    ImGuiHelpers.ClickToCopyText($"{(nint)evt->Target:X}", default, new Vector4(0.6f, 0.6f, 0.6f, 1));
                    ImGui.TableNextColumn();
                    ImGuiHelpers.ClickToCopyText($"{(nint)evt->Listener:X}", default, new Vector4(0.6f, 0.6f, 0.6f, 1));
                    evt = evt->NextEvent;
                }
            }
        }
    }
}

using System.Numerics;

using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;

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
                ImGui.TableSetupColumn("#", WidthFixed);
                ImGui.TableSetupColumn("Type", WidthFixed);
                ImGui.TableSetupColumn("Param", WidthFixed);
                ImGui.TableSetupColumn("Flags", WidthFixed);
                ImGui.TableSetupColumn("StateFlags1", WidthFixed);
                ImGui.TableSetupColumn("Target", WidthFixed);
                ImGui.TableSetupColumn("Listener", WidthFixed);

                ImGui.TableHeadersRow();

                var i = 0;
                while (evt != null)
                {
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{i++}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{evt->State.EventType}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{evt->Param}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{evt->State.StateFlags}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{evt->State.UnkFlags1}");
                    ImGui.TableNextColumn();
                    ImGuiHelpers.ClickToCopyText($"{(nint)evt->Target:X}", null, new Vector4(0.6f, 0.6f, 0.6f, 1));
                    ImGui.TableNextColumn();
                    ImGuiHelpers.ClickToCopyText($"{(nint)evt->Listener:X}", null, new Vector4(0.6f, 0.6f, 0.6f, 1));
                    evt = evt->NextEvent;
                }
            }
        }
    }
}

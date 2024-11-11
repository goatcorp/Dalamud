using Dalamud.Interface.Internal.UiDebug2.Utility;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using static ImGuiNET.ImGuiTableColumnFlags;
using static ImGuiNET.ImGuiTableFlags;

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

        if (tree)
        {
            using (ImRaii.Table($"##{(nint)node:X}eventTable", 7, Resizable | SizingFixedFit | Borders | RowBg))
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
                    Gui.ClickToCopyText($"{(nint)evt->Target:X}");
                    ImGui.TableNextColumn();
                    Gui.ClickToCopyText($"{(nint)evt->Listener:X}");
                    evt = evt->NextEvent;
                }
            }
        }
    }
}

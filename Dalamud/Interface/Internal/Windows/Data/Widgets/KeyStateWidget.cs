using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.System.Input;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying keyboard state.
/// </summary>
internal class KeyStateWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["keystate"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "KeyState";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var keyState = Service<KeyState>.Get();

        using (ImRaii.Group())
        {
            if (ImGui.CollapsingHeader("Game Keybinds"))
            {
                this.DrawKeyStateTable("##GameKeybinds", keyState, keyState.GetValidVirtualKeys);
            }

            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Extended Keybinds"))
            {
                this.DrawKeyStateTable("##ExtendedKeybinds", keyState, keyState.GetExtendedVirtualKeys, true);
            }
        }
    }

    private static void DrawFlagCell(bool isActive)
    {
        ImGui.TableNextColumn();
        var color = isActive ? ImGuiColors.SuccessForeground : ImGuiColors.DalamudOrange;
        ImGui.TextColored(color, isActive ? "True" : "False");
    }

    private void DrawKeyStateTable(string id, KeyState keyState, Func<IEnumerable<VirtualKey>> getKeys, bool isExtended = false)
    {
        if (isExtended)
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }

            ImGui.SameLine();
            ImGui.Text("Extended keybinds cannot be used by the game, but are made available for plugin use.");

            ImGui.Spacing();
        }

        using var table = ImRaii.Table(
            id,
            isExtended ? 7 : 8,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit,
            new Vector2(-1, 300));

        if (table)
        {
            ImGui.TableSetupColumn("Virtual Key", ImGuiTableColumnFlags.WidthStretch);
            if (!isExtended)
            {
                ImGui.TableSetupColumn("SeVirtualKey", ImGuiTableColumnFlags.WidthStretch);
            }

            ImGui.TableSetupColumn("Decimal", ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn("Hex", ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn("Down", ImGuiTableColumnFlags.WidthFixed, 45);
            ImGui.TableSetupColumn("Pressed", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Held", ImGuiTableColumnFlags.WidthFixed, 45);
            ImGui.TableSetupColumn("Released", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableHeadersRow();

            foreach (var vkCode in getKeys())
            {
                var code = (int)vkCode;
                var raw = keyState.GetRawValue(code);
                var flags = (KeyStateFlags)raw;

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(vkCode.ToString());

                if (!isExtended)
                {
                    ImGui.TableNextColumn();
                    if (keyState.TryGetSeVirtualKey(code, out var seVirtualKey))
                    {
                        var seKey = (SeVirtualKey)seVirtualKey;
                        ImGui.Text($"{seKey.ToString()} ({seVirtualKey})");
                    }
                    else
                    {
                        ImGui.Text("-");
                    }
                }

                ImGui.TableNextColumn();
                ImGui.Text(code.ToString());

                ImGui.TableNextColumn();
                ImGui.Text($"0x{code:X2}");

                DrawFlagCell(flags.HasFlag(KeyStateFlags.Down));
                DrawFlagCell(flags.HasFlag(KeyStateFlags.Pressed));
                DrawFlagCell(flags.HasFlag(KeyStateFlags.Held));
                DrawFlagCell(flags.HasFlag(KeyStateFlags.Released));
            }
        }
    }
}

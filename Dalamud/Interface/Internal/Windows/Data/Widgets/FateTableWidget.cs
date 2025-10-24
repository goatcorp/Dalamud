using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Utility.Raii;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying the Fate Table.
/// </summary>
internal class FateTableWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["fate", "fatetable"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Fate Table";

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
        var fateTable = Service<FateTable>.Get();
        var textureManager = Service<TextureManager>.Get();

        if (fateTable.Length == 0)
        {
            ImGui.Text("No fates or data not ready."u8);
            return;
        }

        using var table = ImRaii.Table("FateTable"u8, 13, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Address"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("FateId"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("State"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Level"u8, ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("MapIcon"u8, ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Progress"u8, ImGuiTableColumnFlags.WidthFixed, 55);
        ImGui.TableSetupColumn("Duration"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Bonus"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Position"u8, ImGuiTableColumnFlags.WidthFixed, 240);
        ImGui.TableSetupColumn("Radius"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupScrollFreeze(7, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < fateTable.Length; i++)
        {
            var fate = fateTable[i];
            if (fate == null)
                continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.Text($"#{i}");

            ImGui.TableNextColumn(); // Address
            WidgetUtil.DrawCopyableText($"0x{fate.Address:X}", "Click to copy Address");

            ImGui.TableNextColumn(); // FateId
            WidgetUtil.DrawCopyableText(fate.FateId.ToString(), "Click to copy FateId (RowId of Fate sheet)");

            ImGui.TableNextColumn(); // State
            ImGui.Text(fate.State.ToString());

            ImGui.TableNextColumn(); // Level

            if (fate.Level == fate.MaxLevel)
            {
                ImGui.Text($"{fate.Level}");
            }
            else
            {
                ImGui.Text($"{fate.Level}-{fate.MaxLevel}");
            }

            ImGui.TableNextColumn(); // Icon

            if (fate.IconId != 0)
            {
                if (textureManager.Shared.GetFromGameIcon(fate.IconId).TryGetWrap(out var texture, out _))
                {
                    ImGui.Image(texture.Handle, new(ImGui.GetTextLineHeight()));

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        ImGui.BeginTooltip();
                        ImGui.Text("Click to copy IconId"u8);
                        ImGui.Text($"ID: {fate.IconId} – Size: {texture.Width}x{texture.Height}");
                        ImGui.Image(texture.Handle, new(texture.Width, texture.Height));
                        ImGui.EndTooltip();
                    }

                    if (ImGui.IsItemClicked())
                    {
                        ImGui.SetClipboardText(fate.IconId.ToString());
                    }
                }
            }

            ImGui.TableNextColumn(); // MapIconId

            if (fate.MapIconId != 0)
            {
                if (textureManager.Shared.GetFromGameIcon(fate.MapIconId).TryGetWrap(out var texture, out _))
                {
                    ImGui.Image(texture.Handle, new(ImGui.GetTextLineHeight()));

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        ImGui.BeginTooltip();
                        ImGui.Text("Click to copy MapIconId"u8);
                        ImGui.Text($"ID: {fate.MapIconId} – Size: {texture.Width}x{texture.Height}");
                        ImGui.Image(texture.Handle, new(texture.Width, texture.Height));
                        ImGui.EndTooltip();
                    }

                    if (ImGui.IsItemClicked())
                    {
                        ImGui.SetClipboardText(fate.MapIconId.ToString());
                    }
                }
            }

            ImGui.TableNextColumn(); // Name

            WidgetUtil.DrawCopyableText(fate.Name.ToString(), "Click to copy Name");

            ImGui.TableNextColumn(); // Progress
            ImGui.Text($"{fate.Progress}%");

            ImGui.TableNextColumn(); // TimeRemaining

            if (fate.State == FateState.Running)
            {
                ImGui.Text($"{TimeSpan.FromSeconds(fate.TimeRemaining):mm\\:ss} / {TimeSpan.FromSeconds(fate.Duration):mm\\:ss}");
            }

            ImGui.TableNextColumn(); // HasExpBonus
            ImGui.Text(fate.HasBonus.ToString());

            ImGui.TableNextColumn(); // Position
            WidgetUtil.DrawCopyableText(fate.Position.ToString(), "Click to copy Position");

            ImGui.TableNextColumn(); // Radius
            WidgetUtil.DrawCopyableText(fate.Radius.ToString(), "Click to copy Radius");
        }
    }
}

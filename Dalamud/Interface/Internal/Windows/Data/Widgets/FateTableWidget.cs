using Dalamud.Game.ClientState.Fates;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying the Fate Table.
/// </summary>
internal class FateTableWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "fate", "fatetable" };
    
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
            ImGui.TextUnformatted("No fates or data not ready.");
            return;
        }

        using var table = ImRaii.Table("FateTable", 10, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("State", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Progress", ImGuiTableColumnFlags.WidthFixed, 55);
        ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Bonus", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthFixed, 240);
        ImGui.TableSetupScrollFreeze(7, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < fateTable.Length; i++)
        {
            var fate = fateTable[i];
            if (fate == null)
                continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted($"#{i}");

            ImGui.TableNextColumn(); // Address
            DrawCopyableText($"0x{fate.Address:X}", "Copy address");

            ImGui.TableNextColumn(); // RowId
            DrawCopyableText(fate.FateId.ToString(), "Copy RowId");

            ImGui.TableNextColumn(); // State
            ImGui.TextUnformatted(fate.State.ToString());

            ImGui.TableNextColumn(); // TimeRemaining

            if (fate.State == FateState.Running)
            {
                ImGui.TextUnformatted($"{TimeSpan.FromSeconds(fate.TimeRemaining):mm\\:ss} / {TimeSpan.FromSeconds(fate.Duration):mm\\:ss}");
            }

            ImGui.TableNextColumn(); // Progress
            ImGui.TextUnformatted($"{fate.Progress}%");

            ImGui.TableNextColumn(); // Level

            if (fate.Level == fate.MaxLevel)
            {
                ImGui.TextUnformatted($"{fate.Level}");
            }
            else
            {
                ImGui.TextUnformatted($"{fate.Level}-{fate.MaxLevel}");
            }

            ImGui.TableNextColumn(); // HasExpBonus
            ImGui.TextUnformatted(fate.HasExpBonus.ToString());

            ImGui.TableNextColumn(); // Name

            if (textureManager.Shared.GetFromGameIcon(fate.IconId).TryGetWrap(out var texture, out _))
            {
                ImGui.Image(texture.ImGuiHandle, new(ImGui.GetTextLineHeight()));
                ImGui.SameLine();
            }

            DrawCopyableText(fate.Name.ToString(), "Copy name");

            ImGui.TableNextColumn(); // Position
            DrawCopyableText(fate.Position.ToString(), "Copy Position");
        }
    }

    private static void DrawCopyableText(string text, string tooltipText)
    {
        ImGuiHelpers.SafeTextWrapped(text);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(tooltipText);
            ImGui.EndTooltip();
        }

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(text);
        }
    }
}

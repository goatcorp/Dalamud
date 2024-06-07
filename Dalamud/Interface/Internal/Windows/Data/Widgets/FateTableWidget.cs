using Dalamud.Game.ClientState.Fates;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying the Fate Table.
/// </summary>
internal class FateTableWidget : IDataWindowWidget
{
    private bool resolveGameData;
    
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
        ImGui.Checkbox("Resolve GameData", ref this.resolveGameData);
        
        var fateTable = Service<FateTable>.Get();

        var stateString = string.Empty;
        if (fateTable.Length == 0)
        {
            ImGui.TextUnformatted("No fates or data not ready.");
        }
        else
        {
            stateString += $"FateTableLen: {fateTable.Length}\n";

            ImGui.TextUnformatted(stateString);

            for (var i = 0; i < fateTable.Length; i++)
            {
                var fate = fateTable[i];
                if (fate == null)
                    continue;

                var fateString = $"{fate.Address.ToInt64():X}:[{i}]" +
                                 $" - Lv.{fate.Level} {fate.Name} ({fate.Progress}%)" +
                                 $" - X{fate.Position.X} Y{fate.Position.Y} Z{fate.Position.Z}" +
                                 $" - Territory {(this.resolveGameData ? (fate.TerritoryType.GameData?.Name ?? fate.TerritoryType.Id.ToString()) : fate.TerritoryType.Id.ToString())}\n";

                fateString += $"       StartTimeEpoch: {fate.StartTimeEpoch}" +
                              $" - Duration: {fate.Duration}" +
                              $" - State: {fate.State}" +
                              $" - GameData name: {(this.resolveGameData ? (fate.GameData.Name ?? fate.FateId.ToString()) : fate.FateId.ToString())}";

                ImGui.TextUnformatted(fateString);
                ImGui.SameLine();
                if (ImGui.Button($"C##{fate.Address.ToInt64():X}"))
                {
                    ImGui.SetClipboardText(fate.Address.ToString("X"));
                }
            }
        }
    }
}

using Dalamud.Game.ClientState.Party;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying information about the current party.
/// </summary>
internal class PartyListWidget : IDataWindowWidget
{
    private bool resolveGameData;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "partylist", "party" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Party List"; 

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
        var partyList = Service<PartyList>.Get();

        ImGui.Checkbox("Resolve GameData", ref this.resolveGameData);

        ImGui.Text($"GroupManager: {partyList.GroupManagerAddress.ToInt64():X}");
        ImGui.Text($"GroupList: {partyList.GroupListAddress.ToInt64():X}");
        ImGui.Text($"AllianceList: {partyList.AllianceListAddress.ToInt64():X}");

        ImGui.Text($"{partyList.Length} Members");

        for (var i = 0; i < partyList.Length; i++)
        {
            var member = partyList[i];
            if (member == null)
            {
                ImGui.Text($"[{i}] was null");
                continue;
            }

            ImGui.Text($"[{i}] {member.Address.ToInt64():X} - {member.Name} - {member.GameObject?.ObjectId}");
            if (this.resolveGameData)
            {
                var actor = member.GameObject;
                if (actor == null)
                {
                    ImGui.Text("Actor was null");
                }
                else
                {
                    Util.PrintGameObject(actor, "-", this.resolveGameData);
                }
            }
        }
    }
}

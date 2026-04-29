using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Party;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying information about the current party.
/// </summary>
internal class PartyListWidget : IDataWindowWidget
{
    private bool resolveGameData;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["partylist", "party"];

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

        ImGui.Checkbox("Resolve GameData"u8, ref this.resolveGameData);

        ImGui.Text($"GroupManager: {partyList.GroupManagerAddress:X}");
        ImGui.Text($"GroupList: {partyList.GroupListAddress:X}");
        ImGui.Text($"AllianceList: {partyList.AllianceListAddress:X}");

        ImGui.Text($"{partyList.Length} Members");

        for (var i = 0; i < partyList.Length; i++)
        {
            var member = partyList[i];
            if (member == null)
            {
                ImGui.Text($"[{i}] was null");
                continue;
            }

            ImGui.Text($"[{i}] {member.Address:X} - {member.Name} - {member.GameObject?.GameObjectId ?? 0}");
            if (this.resolveGameData)
            {
                var actor = member.GameObject;
                if (actor == null)
                {
                    ImGui.Text("Actor was null"u8);
                }
                else
                {
                    Util.PrintGameObject(actor, "-", this.resolveGameData);
                }
            }
        }
    }
}

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

        ImGui.Checkbox("Resolve GameData"u8, ref this.resolveGameData);

        ImGui.TextUnformatted($"GroupManager: {partyList.GroupManagerAddress.ToInt64():X}");
        ImGui.TextUnformatted($"GroupList: {partyList.GroupListAddress.ToInt64():X}");
        ImGui.TextUnformatted($"AllianceList: {partyList.AllianceListAddress.ToInt64():X}");

        ImGui.TextUnformatted($"{partyList.Length} Members");

        for (var i = 0; i < partyList.Length; i++)
        {
            var member = partyList[i];
            if (member == null)
            {
                ImGui.TextUnformatted($"[{i}] was null");
                continue;
            }

            ImGui.TextUnformatted($"[{i}] {member.Address.ToInt64():X} - {member.Name} - {member.GameObject?.GameObjectId}");
            if (this.resolveGameData)
            {
                var actor = member.GameObject;
                if (actor == null)
                {
                    ImGui.TextUnformatted("Actor was null"u8);
                }
                else
                {
                    Util.PrintGameObject(actor, "-", this.resolveGameData);
                }
            }
        }
    }
}

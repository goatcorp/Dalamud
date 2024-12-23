using Dalamud.Game.ClientState.Buddy;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying data about the Buddy List.
/// </summary>
internal class BuddyListWidget : IDataWindowWidget
{
    private bool resolveGameData;

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "buddy", "buddylist" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Buddy List";

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var buddyList = Service<BuddyList>.Get();

        ImGui.Checkbox("Resolve GameData", ref this.resolveGameData);
        {
            var member = buddyList.CompanionBuddy;
            if (member == null)
            {
                ImGui.TextUnformatted("[Companion] null");
            }
            else
            {
                ImGui.TextUnformatted($"[Companion] {member.Address.ToInt64():X} - {member.ObjectId} - {member.DataID}");
                if (this.resolveGameData)
                {
                    var gameObject = member.GameObject;
                    if (gameObject == null)
                    {
                        ImGui.TextUnformatted("GameObject was null");
                    }
                    else
                    {
                        Util.PrintGameObject(gameObject, "-", this.resolveGameData);
                    }
                }
            }
        }

        {
            var member = buddyList.PetBuddy;
            if (member == null)
            {
                ImGui.TextUnformatted("[Pet] null");
            }
            else
            {
                ImGui.TextUnformatted($"[Pet] {member.Address.ToInt64():X} - {member.ObjectId} - {member.DataID}");
                if (this.resolveGameData)
                {
                    var gameObject = member.GameObject;
                    if (gameObject == null)
                    {
                        ImGui.TextUnformatted("GameObject was null");
                    }
                    else
                    {
                        Util.PrintGameObject(gameObject, "-", this.resolveGameData);
                    }
                }
            }
        }

        {
            var count = buddyList.Length;
            if (count == 0)
            {
                ImGui.TextUnformatted("[BattleBuddy] None present");
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var member = buddyList[i];
                    ImGui.TextUnformatted($"[BattleBuddy] [{i}] {member?.Address.ToInt64():X} - {member?.ObjectId} - {member?.DataID}");
                    if (this.resolveGameData)
                    {
                        var gameObject = member?.GameObject;
                        if (gameObject == null)
                        {
                            ImGui.TextUnformatted("GameObject was null");
                        }
                        else
                        {
                            Util.PrintGameObject(gameObject, "-", this.resolveGameData);
                        }
                    }
                }
            }
        }
    }
}

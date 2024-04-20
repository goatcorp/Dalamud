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

        ImGui.Text($"BuddyList: {buddyList.BuddyListAddress.ToInt64():X}");
        {
            var member = buddyList.CompanionBuddy;
            if (member == null)
            {
                ImGui.Text("[Companion] null");
            }
            else
            {
                ImGui.Text($"[Companion] {member.Address.ToInt64():X} - {member.ObjectId} - {member.DataID}");
                if (this.resolveGameData)
                {
                    var gameObject = member.GameObject;
                    if (gameObject == null)
                    {
                        ImGui.Text("GameObject was null");
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
                ImGui.Text("[Pet] null");
            }
            else
            {
                ImGui.Text($"[Pet] {member.Address.ToInt64():X} - {member.ObjectId} - {member.DataID}");
                if (this.resolveGameData)
                {
                    var gameObject = member.GameObject;
                    if (gameObject == null)
                    {
                        ImGui.Text("GameObject was null");
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
                ImGui.Text("[BattleBuddy] None present");
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var member = buddyList[i];
                    ImGui.Text($"[BattleBuddy] [{i}] {member?.Address.ToInt64():X} - {member?.ObjectId} - {member?.DataID}");
                    if (this.resolveGameData)
                    {
                        var gameObject = member?.GameObject;
                        if (gameObject == null)
                        {
                            ImGui.Text("GameObject was null");
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

using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Utility;

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
    public string[]? CommandShortcuts { get; init; } = ["buddy", "buddylist"];

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

        ImGui.Checkbox("Resolve GameData"u8, ref this.resolveGameData);

        var companionBuddy = buddyList.CompanionBuddy;
        if (companionBuddy == null)
        {
            ImGui.Text("[Companion] null"u8);
        }
        else
        {
            ImGui.Text($"[Companion] {companionBuddy.Address:X} - {companionBuddy.EntityId} - {companionBuddy.DataID}");
            if (this.resolveGameData)
            {
                var gameObject = companionBuddy.GameObject;
                if (gameObject == null)
                {
                    ImGui.Text("GameObject was null"u8);
                }
                else
                {
                    Util.PrintGameObject(gameObject, "-", this.resolveGameData);
                }
            }
        }

        var petBuddy = buddyList.PetBuddy;
        if (petBuddy == null)
        {
            ImGui.Text("[Pet] null"u8);
        }
        else
        {
            ImGui.Text($"[Pet] {petBuddy.Address:X} - {petBuddy.EntityId} - {petBuddy.DataID}");
            if (this.resolveGameData)
            {
                var gameObject = petBuddy.GameObject;
                if (gameObject == null)
                {
                    ImGui.Text("GameObject was null"u8);
                }
                else
                {
                    Util.PrintGameObject(gameObject, "-", this.resolveGameData);
                }
            }
        }

        var count = buddyList.Length;
        if (count == 0)
        {
            ImGui.Text("[BattleBuddy] None present"u8);
        }
        else
        {
            for (var i = 0; i < count; i++)
            {
                var member = buddyList[i];
                ImGui.Text($"[BattleBuddy] [{i}] {member?.Address ?? 0:X} - {member?.EntityId ?? 0} - {member?.DataID ?? 0}");
                if (this.resolveGameData)
                {
                    var gameObject = member?.GameObject;
                    if (gameObject == null)
                    {
                        ImGui.Text("GameObject was null"u8);
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

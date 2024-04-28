using Dalamud.Game.ClientState.Conditions;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying current character condition flags.
/// </summary>
internal class ConditionWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "condition" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Condition"; 

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var condition = Service<Condition>.Get();

#if DEBUG
        ImGui.Text($"ptr: 0x{condition.Address.ToInt64():X}");
#endif

        ImGui.Text("Current Conditions:");
        ImGui.Separator();

        var didAny = false;

        for (var i = 0; i < Condition.MaxConditionEntries; i++)
        {
            var typedCondition = (ConditionFlag)i;
            var cond = condition[typedCondition];

            if (!cond) continue;

            didAny = true;

            ImGui.Text($"ID: {i} Enum: {typedCondition}");
        }

        if (!didAny)
            ImGui.Text("None. Talk to a shop NPC or visit a market board to find out more!");
    }
}

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility;

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
        ImGui.TextUnformatted($"ptr: {Util.DescribeAddress(condition.Address)}");
#endif

        ImGui.TextUnformatted("Current Conditions:");
        ImGui.Separator();

        var didAny = false;

        for (var i = 0; i < Condition.MaxConditionEntries; i++)
        {
            var typedCondition = (ConditionFlag)i;
            var cond = condition[typedCondition];

            if (!cond) continue;

            didAny = true;

            ImGui.TextUnformatted($"ID: {i} Enum: {typedCondition}");
        }

        if (!didAny)
            ImGui.TextUnformatted("None. Talk to a shop NPC or visit a market board to find out more!");
    }
}

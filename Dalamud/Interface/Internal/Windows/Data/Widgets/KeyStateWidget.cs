using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying keyboard state.
/// </summary>
internal class KeyStateWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["keystate"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "KeyState";

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
        var keyState = Service<KeyState>.Get();

        // TODO: Use table instead of columns
        ImGui.Columns(4);

        var i = 0;
        foreach (var vkCode in keyState.GetValidVirtualKeys())
        {
            var code = (int)vkCode;
            var value = keyState[code];

            using (ImRaii.PushColor(ImGuiCol.Text, value ? ImGuiColors.HealerGreen : ImGuiColors.DPSRed))
            {
                ImGui.Text($"{vkCode} ({code})");
            }

            i++;
            if (i % 24 == 0)
                ImGui.NextColumn();
        }

        ImGui.Columns(1);
    }
}

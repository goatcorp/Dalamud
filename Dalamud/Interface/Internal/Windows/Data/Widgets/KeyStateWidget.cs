using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying keyboard state.
/// </summary>
internal class KeyStateWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "keystate" };
    
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

        ImGui.Columns(4);

        var i = 0;
        foreach (var vkCode in keyState.GetValidVirtualKeys())
        {
            var code = (int)vkCode;
            var value = keyState[code];

            ImGui.PushStyleColor(ImGuiCol.Text, value ? ImGuiColors.HealerGreen : ImGuiColors.DPSRed);

            ImGui.Text($"{vkCode} ({code})");

            ImGui.PopStyleColor();

            i++;
            if (i % 24 == 0)
                ImGui.NextColumn();
        }

        ImGui.Columns(1);
    }
}

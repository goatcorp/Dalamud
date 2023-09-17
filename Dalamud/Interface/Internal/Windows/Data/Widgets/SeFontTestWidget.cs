using Dalamud.Game.Text;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying test data for SE Font Symbols.
/// </summary>
internal class SeFontTestWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "sefont", "sefonttest" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "SeFont Test"; 

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
        var specialChars = string.Empty;

        for (var i = 0xE020; i <= 0xE0DB; i++)
            specialChars += $"0x{i:X} - {(SeIconChar)i} - {(char)i}\n";

        ImGui.TextUnformatted(specialChars);
    }
}

using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying test data for SE Font Symbols.
/// </summary>
internal class SeFontTestWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["sefont", "sefonttest"];

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

        var min = (char)Enum.GetValues<SeIconChar>().Min();
        var max = (char)Enum.GetValues<SeIconChar>().Max();

        for (var i = min; i <= max; i++)
            specialChars += $"0x{(int)i:X} - {(SeIconChar)i} - {i}\n";

        ImGui.Text(specialChars);
    }
}

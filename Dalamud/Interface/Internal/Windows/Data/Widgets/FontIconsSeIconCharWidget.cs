using System.Linq;
using System.Text;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying test data for SE Font Symbols.
/// </summary>
internal class FontIconsSeIconCharWidget : IDataWindowWidget
{
    private string specialChars = string.Empty;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["sefont", "sefonttest"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Font: Icons(SeIconChar)";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        var values = Enum.GetValues<SeIconChar>();
        var min = (char)values.Min();
        var max = (char)values.Max();

        var sb = new StringBuilder();
        for (var i = min; i <= max; i++)
            sb.Append($"0x{(int)i:X} - {(SeIconChar)i} - {i}\n");
        this.specialChars = sb.ToString();
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw() => ImGui.Text(this.specialChars);
}

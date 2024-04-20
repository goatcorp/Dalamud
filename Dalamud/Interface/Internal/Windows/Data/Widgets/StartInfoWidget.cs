using ImGuiNET;
using Newtonsoft.Json;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying start info.
/// </summary>
internal class StartInfoWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "startinfo" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Start Info"; 

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
        var startInfo = Service<Dalamud>.Get().StartInfo;

        ImGui.Text(JsonConvert.SerializeObject(startInfo, Formatting.Indented));
    }
}

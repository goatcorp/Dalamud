using ImGuiNET;
using Newtonsoft.Json;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Widget for displaying start info.
/// </summary>
internal class StartInfoWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public DataKind DataKind { get; init; } = DataKind.StartInfo;

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
        var startInfo = Service<DalamudStartInfo>.Get();

        ImGui.Text(JsonConvert.SerializeObject(startInfo, Formatting.Indented));
    }
}

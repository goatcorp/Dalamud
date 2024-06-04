using Dalamud.Configuration.Internal;
using Dalamud.Game.Gui.Dtr;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying dtr test.
/// </summary>
internal class DtrBarWidget : IDataWindowWidget
{
    private DtrBarEntry? dtrTest1;
    private DtrBarEntry? dtrTest2;
    private DtrBarEntry? dtrTest3;
    
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "dtr", "dtrbar" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "DTR Bar"; 

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
        this.DrawDtrTestEntry(ref this.dtrTest1, "DTR Test #1");
        ImGui.Separator();
        this.DrawDtrTestEntry(ref this.dtrTest2, "DTR Test #2");
        ImGui.Separator();
        this.DrawDtrTestEntry(ref this.dtrTest3, "DTR Test #3");
        ImGui.Separator();

        var configuration = Service<DalamudConfiguration>.Get();
        if (configuration.DtrOrder != null)
        {
            ImGui.Separator();

            foreach (var order in configuration.DtrOrder)
            {
                ImGui.Text(order);
            }
        }
    }
    
    private void DrawDtrTestEntry(ref DtrBarEntry? entry, string title)
    {
        var dtrBar = Service<DtrBar>.Get();

        if (entry != null)
        {
            ImGui.Text(title);

            var text = entry.Text?.TextValue ?? string.Empty;
            if (ImGui.InputText($"Text###{entry.Title}t", ref text, 255))
                entry.Text = text;

            var shown = entry.Shown;
            if (ImGui.Checkbox($"Shown###{entry.Title}s", ref shown))
                entry.Shown = shown;

            if (ImGui.Button($"Remove###{entry.Title}r"))
            {
                entry.Remove();
                entry = null;
            }
        }
        else
        {
            if (ImGui.Button($"Add###{title}"))
            {
                entry = dtrBar.Get(title, title);
            }
        }
    }
}

using System.Diagnostics.CodeAnalysis;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public class ButtonSettingsEntry : SettingsEntry
{
    private readonly string description;
    private readonly Action runs;

    public ButtonSettingsEntry(string name, string description, Action runs)
    {
        this.description = description;
        this.runs = runs;
        this.Name = name;
    }

    public override void Load()
    {
        // ignored
    }

    public override void Save()
    {
        // ignored
    }

    public override void Draw()
    {
        if (ImGui.Button(this.Name))
        {
            this.runs.Invoke();
        }

        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, this.description);
    }
}

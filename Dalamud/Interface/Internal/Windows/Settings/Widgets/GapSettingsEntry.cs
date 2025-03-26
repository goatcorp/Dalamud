using System.Diagnostics.CodeAnalysis;

using Dalamud.Interface.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public sealed class GapSettingsEntry : SettingsEntry
{
    private readonly float size;
    private readonly bool hr;

    public GapSettingsEntry(float size, bool hr = false)
    {
        this.size = size;
        this.hr = hr;
        this.IsValid = true;
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
        ImGuiHelpers.ScaledDummy(this.size);

        if (this.hr)
        {
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(this.size);
        }
    }
}

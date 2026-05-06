using System.Diagnostics.CodeAnalysis;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal sealed class GapSettingsEntry : SettingsEntry
{
    private readonly float size;
    private readonly bool hr;
    private readonly Func<bool>? visibility;

    public GapSettingsEntry(float size, bool hr = false, Func<bool>? visibility = null)
    {
        this.size = size;
        this.hr = hr;
        this.visibility = visibility;
        this.IsValid = true;
    }

    public override bool IsVisible => this.visibility == null || this.visibility();

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

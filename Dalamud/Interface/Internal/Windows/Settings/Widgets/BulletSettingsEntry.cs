using System.Diagnostics.CodeAnalysis;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility.Internal;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal sealed class BulletSettingsEntry : SettingsEntry
{
    private readonly LazyLoc text;

    public BulletSettingsEntry(LazyLoc text)
    {
        this.text = text;
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
        using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextWrapped(this.text);
    }
}

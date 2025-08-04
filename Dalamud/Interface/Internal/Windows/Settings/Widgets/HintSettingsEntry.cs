using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public class HintSettingsEntry : SettingsEntry
{
    private readonly string text;
    private readonly Vector4 color;

    public HintSettingsEntry(string text, Vector4? color = null)
    {
        this.text = text;
        this.color = color ?? ImGuiColors.DalamudGrey;
    }

    public override void Load()
    {
        // ignore
    }

    public override void Save()
    {
        // ignore
    }

    public override void Draw()
    {
        ImGui.TextColoredWrapped(this.color, this.text);
    }
}

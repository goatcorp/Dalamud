using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Utility.Internal;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal sealed class HintSettingsEntry : SettingsEntry
{
    private readonly LazyLoc text;
    private readonly Vector4 color;

    public HintSettingsEntry(LazyLoc text, Vector4? color = null)
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

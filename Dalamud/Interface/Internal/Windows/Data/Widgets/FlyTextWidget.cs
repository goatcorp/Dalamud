using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.FlyText;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying fly text info.
/// </summary>
internal class FlyTextWidget : IDataWindowWidget
{
    private int flyActor;
    private FlyTextKind flyKind;
    private int flyVal1;
    private int flyVal2;
    private string flyText1 = string.Empty;
    private string flyText2 = string.Empty;
    private int flyIcon;
    private int flyDmgIcon;
    private Vector4 flyColor = new(1, 0, 0, 1);

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["flytext"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Fly Text";

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
        if (ImGui.BeginCombo("Kind"u8, $"{this.flyKind} ({(int)this.flyKind})"))
        {
            var values = Enum.GetValues<FlyTextKind>().Distinct();
            foreach (var value in values)
            {
                if (ImGui.Selectable($"{value} ({(int)value})"))
                {
                    this.flyKind = value;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.InputText("Text1"u8, ref this.flyText1, 200);
        ImGui.InputText("Text2"u8, ref this.flyText2, 200);

        ImGui.InputInt("Val1"u8, ref this.flyVal1);
        ImGui.InputInt("Val2"u8, ref this.flyVal2);

        ImGui.InputInt("Icon ID"u8, ref this.flyIcon);
        ImGui.InputInt("Damage Icon ID"u8, ref this.flyDmgIcon);
        ImGui.ColorEdit4("Color", ref this.flyColor);
        ImGui.InputInt("Actor Index"u8, ref this.flyActor);
        var sendColor = ImGui.ColorConvertFloat4ToU32(this.flyColor);

        if (ImGui.Button("Send"u8))
        {
            Service<FlyTextGui>.Get().AddFlyText(
                this.flyKind,
                unchecked((uint)this.flyActor),
                unchecked((uint)this.flyVal1),
                unchecked((uint)this.flyVal2),
                this.flyText1,
                this.flyText2,
                sendColor,
                unchecked((uint)this.flyIcon),
                unchecked((uint)this.flyDmgIcon));
        }
    }
}

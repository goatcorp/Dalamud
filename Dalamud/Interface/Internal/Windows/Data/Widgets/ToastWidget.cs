using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.Utility;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying toast test.
/// </summary>
internal class ToastWidget : IDataWindowWidget
{
    private string inputTextToast = string.Empty;
    private int toastPosition;
    private int toastSpeed;
    private int questToastPosition;
    private bool questToastSound;
    private int questToastIconId;
    private bool questToastCheckmark;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "toast" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Toast";

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
        var toastGui = Service<ToastGui>.Get();

        ImGui.InputText("Toast text"u8, ref this.inputTextToast, 200);

        ImGui.Combo("Toast Position", ref this.toastPosition, ["Bottom", "Top",], 2);
        ImGui.Combo("Toast Speed", ref this.toastSpeed, ["Slow", "Fast",], 2);
        ImGui.Combo("Quest Toast Position", ref this.questToastPosition, ["Centre", "Right", "Left"], 3);
        ImGui.Checkbox("Quest Checkmark"u8, ref this.questToastCheckmark);
        ImGui.Checkbox("Quest Play Sound"u8, ref this.questToastSound);
        ImGui.InputInt("Quest Icon ID"u8, ref this.questToastIconId);

        ImGuiHelpers.ScaledDummy(new Vector2(10, 10));

        if (ImGui.Button("Show toast"u8))
        {
            toastGui.ShowNormal(this.inputTextToast, new ToastOptions
            {
                Position = (ToastPosition)this.toastPosition,
                Speed = (ToastSpeed)this.toastSpeed,
            });
        }

        if (ImGui.Button("Show Quest toast"u8))
        {
            toastGui.ShowQuest(this.inputTextToast, new QuestToastOptions
            {
                Position = (QuestToastPosition)this.questToastPosition,
                DisplayCheckmark = this.questToastCheckmark,
                IconId = (uint)this.questToastIconId,
                PlaySound = this.questToastSound,
            });
        }

        if (ImGui.Button("Show Error toast"u8))
        {
            toastGui.ShowError(this.inputTextToast);
        }
    }
}

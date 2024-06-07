using System.Numerics;

using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.Utility;

using ImGuiNET;

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

        ImGui.InputText("Toast text", ref this.inputTextToast, 200);

        ImGui.Combo("Toast Position", ref this.toastPosition, new[] { "Bottom", "Top", }, 2);
        ImGui.Combo("Toast Speed", ref this.toastSpeed, new[] { "Slow", "Fast", }, 2);
        ImGui.Combo("Quest Toast Position", ref this.questToastPosition, new[] { "Centre", "Right", "Left" }, 3);
        ImGui.Checkbox("Quest Checkmark", ref this.questToastCheckmark);
        ImGui.Checkbox("Quest Play Sound", ref this.questToastSound);
        ImGui.InputInt("Quest Icon ID", ref this.questToastIconId);

        ImGuiHelpers.ScaledDummy(new Vector2(10, 10));

        if (ImGui.Button("Show toast"))
        {
            toastGui.ShowNormal(this.inputTextToast, new ToastOptions
            {
                Position = (ToastPosition)this.toastPosition,
                Speed = (ToastSpeed)this.toastSpeed,
            });
        }

        if (ImGui.Button("Show Quest toast"))
        {
            toastGui.ShowQuest(this.inputTextToast, new QuestToastOptions
            {
                Position = (QuestToastPosition)this.questToastPosition,
                DisplayCheckmark = this.questToastCheckmark,
                IconId = (uint)this.questToastIconId,
                PlaySound = this.questToastSound,
            });
        }

        if (ImGui.Button("Show Error toast"))
        {
            toastGui.ShowError(this.inputTextToast);
        }
    }
}

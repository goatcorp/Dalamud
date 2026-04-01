using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;
using Dalamud.Interface.Utility.Raii;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Modals;

/// <summary>
/// Class responsible for drawing the Delete Plugin Config's Warning Modal.
/// </summary>
internal class DeletePluginConfigWarningModal
{
    private readonly PluginInstallerWindow pluginInstaller;

    private bool isDrawing = true;
    private bool onNextFrame;
    private bool shouldExplainTesting;
    private string warningPluginName = string.Empty;
    private TaskCompletionSource<bool>? taskCompletionSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeletePluginConfigWarningModal"/> class.
    /// </summary>
    /// <param name="pluginInstaller">Reference to main Installer Class.</param>
    public DeletePluginConfigWarningModal(PluginInstallerWindow pluginInstaller)
    {
        this.pluginInstaller = pluginInstaller;
    }

    /// <summary>
    /// Show the Delete Plugin Config Warning Popup Modal.
    /// </summary>
    /// <param name="pluginName">Name of plugin to show warning for.</param>
    /// <param name="explainTesting">If this popup should include the testing config warning.</param>
    /// <returns>Task with completion status.</returns>
    public Task<bool> ShowDeletePluginConfigWarningModal(string pluginName, bool explainTesting = false)
    {
        this.onNextFrame = true;
        this.warningPluginName = pluginName;
        this.shouldExplainTesting = explainTesting;
        this.taskCompletionSource = new TaskCompletionSource<bool>();
        return this.taskCompletionSource.Task;
    }

    /// <summary>
    /// Draws Modal when needed.
    /// </summary>
    public void Draw()
    {
        var modalTitle = PluginInstallerLocs.DeletePluginConfigWarningModal_Title;

        this.DrawModal(modalTitle);

        if (this.onNextFrame)
        {
            // NOTE(goat): ImGui cannot open a modal if no window is focused, at the moment.
            // If people click out of the installer into the game while a plugin is installing, we won't be able to show a modal if we don't grab focus.
            ImGui.SetWindowFocus(this.pluginInstaller.WindowName);

            ImGui.OpenPopup(modalTitle);
            this.onNextFrame = false;
            this.isDrawing = true;
        }
    }

    private void DrawModal(string modalTitle)
    {
        using var popupModal = ImRaii.PopupModal(
            modalTitle,
            ref this.isDrawing,
            ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar);

        if (!popupModal)
        {
            return;
        }

        if (this.shouldExplainTesting)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudOrange))
            {
                ImGui.Text(PluginInstallerLocs.DeletePluginConfigWarningModal_ExplainTesting());
            }
        }

        ImGui.Text(PluginInstallerLocs.DeletePluginConfigWarningModal_Body(this.warningPluginName));
        ImGui.Spacing();

        const float buttonWidth = 120f;
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ((buttonWidth * 2) - (ImGui.GetStyle().ItemSpacing.Y * 2))) / 2);

        if (ImGui.Button(PluginInstallerLocs.DeletePluginConfirmWarningModal_Yes, new Vector2(buttonWidth, 40)))
        {
            ImGui.CloseCurrentPopup();
            this.taskCompletionSource?.SetResult(true);
        }

        ImGui.SameLine();

        if (ImGui.Button(PluginInstallerLocs.DeletePluginConfirmWarningModal_No, new Vector2(buttonWidth, 40)))
        {
            ImGui.CloseCurrentPopup();
            this.taskCompletionSource?.SetResult(false);
        }
    }
}

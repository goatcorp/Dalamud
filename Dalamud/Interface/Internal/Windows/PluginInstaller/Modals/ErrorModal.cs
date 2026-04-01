using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;
using Dalamud.Interface.Utility.Raii;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Modals;

/// <summary>
/// Class Responsible for Drawing an Error Modal.
/// </summary>
internal class ErrorModal
{
    private readonly PluginInstallerWindow pluginInstaller;

    private bool isDrawing = true;
    private bool onNextFrame;
    private string errorMessage = string.Empty;
    private TaskCompletionSource? taskCompletionSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorModal"/> class.
    /// </summary>
    /// <param name="pluginInstaller">Reference to main Installer Window.</param>
    public ErrorModal(PluginInstallerWindow pluginInstaller)
    {
        this.pluginInstaller = pluginInstaller;
    }

    /// <summary>
    /// Show an Error Popup Modal.
    /// </summary>
    /// <param name="message">Error message to display.</param>
    /// <returns>Task with completion status.</returns>
    public Task ShowErrorModal(string message)
    {
        this.errorMessage = message;
        this.isDrawing = true;
        this.onNextFrame = true;
        this.taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        return this.taskCompletionSource.Task;
    }

    /// <summary>
    /// Draws Modal when needed.
    /// </summary>
    public void Draw()
    {
        var modalTitle = PluginInstallerLocs.ErrorModal_Title;

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

        ImGui.Text(this.errorMessage);
        ImGui.Spacing();

        const float buttonWidth = 120f;
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);

        if (ImGui.Button(PluginInstallerLocs.ErrorModalButton_Ok, new Vector2(buttonWidth, 40)))
        {
            ImGui.CloseCurrentPopup();
            this.taskCompletionSource?.SetResult();
        }
    }
}

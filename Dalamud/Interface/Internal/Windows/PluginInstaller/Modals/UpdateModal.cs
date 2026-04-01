using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Modals;

/// <summary>
/// Class responsible for drawing the Update Modal.
/// </summary>
internal class UpdateModal
{
    private readonly PluginInstallerWindow pluginInstaller;

    private bool isDrawing = true;
    private bool onNextFrame;
    private LocalPlugin? updatingPlugin;
    private TaskCompletionSource<bool>? taskCompletionSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateModal"/> class.
    /// </summary>
    /// <param name="pluginInstaller">Reference to main Installer Window.</param>
    public UpdateModal(PluginInstallerWindow pluginInstaller)
    {
        this.pluginInstaller = pluginInstaller;
    }

    /// <summary>
    /// Show the Update Modal for specified plugin.
    /// </summary>
    /// <param name="plugin">Plugin to show modal for.</param>
    /// <returns>Task with completion status.</returns>
    public Task<bool> ShowUpdateModal(LocalPlugin plugin)
    {
        this.onNextFrame = true;
        this.updatingPlugin = plugin;
        this.taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return this.taskCompletionSource.Task;
    }

    /// <summary>
    /// Draws the modal if needed.
    /// </summary>
    public void Draw()
    {
        var modalTitle = PluginInstallerLocs.UpdateModal_Title;

        if (this.updatingPlugin == null)
            return;

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

        ImGui.Text(PluginInstallerLocs.UpdateModal_UpdateAvailable(this.updatingPlugin?.Name ?? "Unable to read plugin info."));
        ImGui.Spacing();

        const float buttonWidth = 120f;
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ((buttonWidth * 2) - (ImGui.GetStyle().ItemSpacing.Y * 2))) / 2);

        if (ImGui.Button(PluginInstallerLocs.UpdateModal_Yes, new Vector2(buttonWidth, 40)))
        {
            ImGui.CloseCurrentPopup();
            this.taskCompletionSource?.SetResult(true);
        }

        ImGui.SameLine();

        if (ImGui.Button(PluginInstallerLocs.UpdateModal_No, new Vector2(buttonWidth, 40)))
        {
            ImGui.CloseCurrentPopup();
            this.taskCompletionSource?.SetResult(false);
        }
    }
}

using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Modals;

/// <summary>
/// Class responsible for drawing testing warning popup modals.
/// </summary>
internal class TestingWarningModal
{
    private readonly PluginInstallerWindow pluginInstaller;

    private bool isDrawing = true;
    private bool onNextFrame;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestingWarningModal"/> class.
    /// </summary>
    /// <param name="pluginInstaller">Reference to main Installer Window.</param>
    public TestingWarningModal(PluginInstallerWindow pluginInstaller)
    {
        this.pluginInstaller = pluginInstaller;
    }

    /// <summary>
    /// Show the Testing Warning Modal.
    /// </summary>
    public void ShowTestingModal()
    {
        this.onNextFrame = true;
    }

    /// <summary>
    /// Draw this modal.
    /// </summary>
    public void Draw()
    {
        var modalTitle = PluginInstallerLocs.TestingWarningModal_Title;

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

        ImGui.Text(PluginInstallerLocs.TestingWarningModal_DowngradeBody);

        ImGuiHelpers.ScaledDummy(10);

        const float buttonWidth = 120f;
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);

        if (ImGui.Button(PluginInstallerLocs.ErrorModalButton_Ok, new Vector2(buttonWidth, 40)))
        {
            ImGui.CloseCurrentPopup();
        }
    }
}

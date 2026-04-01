using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Support;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Modals;

/// <summary>
/// Class responsible for drawing the feedback modal.
/// </summary>
internal class FeedbackModal
{
    private readonly PluginInstallerWindow pluginInstaller;
    private readonly ErrorModal errorModal;

    private bool isDrawing = true;
    private bool onNextFrame;
    private bool onNextFrameDontClear;
    private string body = string.Empty;
    private string contact = string.Empty;
    private bool includeException;
    private IPluginManifest? plugin;
    private bool isTestingPlugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedbackModal"/> class.
    /// </summary>
    /// <param name="pluginInstaller">Reference to main Installer Window.</param>
    /// <param name="errorModal">Reference to general use error modal.</param>
    public FeedbackModal(PluginInstallerWindow pluginInstaller, ErrorModal errorModal)
    {
        this.pluginInstaller = pluginInstaller;
        this.errorModal = errorModal;
    }

    /// <summary>
    /// Draws a button that when clicked will open the Feedback Modal.
    /// </summary>
    /// <param name="manifest">Plugin manifest to get feedback data from.</param>
    /// <param name="isTesting">If the plugin is in testing.</param>
    /// <param name="big">If the button should use long text or short text.</param>
    public void DrawSendFeedbackButton(IPluginManifest manifest, bool isTesting, bool big)
    {
        var clicked = big ? ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Comment, PluginInstallerLocs.FeedbackModal_Title) : ImGuiComponents.IconButton(FontAwesomeIcon.Comment);

        if (clicked)
        {
            this.plugin = manifest;
            this.onNextFrame = true;
            this.isTestingPlugin = isTesting;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(PluginInstallerLocs.FeedbackModal_Title);
        }
    }

    /// <summary>
    /// Draw this modal.
    /// </summary>
    public void Draw()
    {
        var modalTitle = PluginInstallerLocs.FeedbackModal_Title;

        this.DrawModal(modalTitle);

        if (this.onNextFrame)
        {
            ImGui.OpenPopup(modalTitle);
            this.onNextFrame = false;
            this.isDrawing = true;
            if (!this.onNextFrameDontClear)
            {
                this.body = string.Empty;
                this.contact = Service<DalamudConfiguration>.Get().LastFeedbackContactDetails;
                this.includeException = false;
            }
            else
            {
                this.onNextFrameDontClear = false;
            }
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

        ImGui.Text(PluginInstallerLocs.FeedbackModal_Text(this.plugin?.Name ?? "Unable to Read Plugin Name from Manifest"));

        if (this.plugin?.FeedbackMessage != null)
        {
            ImGui.TextWrapped(this.plugin.FeedbackMessage);
        }

        if (this.pluginInstaller.pluginListUpdatable.Any(up => up.InstalledPlugin.Manifest.InternalName == this.plugin?.InternalName))
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, PluginInstallerLocs.FeedbackModal_HasUpdate);
        }

        ImGui.Spacing();

        ImGui.InputTextMultiline("###FeedbackContent"u8, ref this.body, 1000, new Vector2(400, 200));

        ImGui.Spacing();

        ImGui.InputText(PluginInstallerLocs.FeedbackModal_ContactInformation, ref this.contact, 100);

        ImGui.SameLine();

        if (ImGui.Button(PluginInstallerLocs.FeedbackModal_ContactInformationDiscordButton))
        {
            Process.Start(
                new ProcessStartInfo(PluginInstallerLocs.FeedbackModal_ContactInformationDiscordUrl)
                {
                    UseShellExecute = true,
                });
        }

        ImGui.Text(PluginInstallerLocs.FeedbackModal_ContactInformationHelp);

        ImGui.TextColored(ImGuiColors.DalamudRed, PluginInstallerLocs.FeedbackModal_ContactInformationWarning);

        ImGui.Spacing();

        ImGui.Checkbox(PluginInstallerLocs.FeedbackModal_IncludeLastError, ref this.includeException);
        ImGui.TextColored(ImGuiColors.DalamudGrey, PluginInstallerLocs.FeedbackModal_IncludeLastErrorHint);

        ImGui.Spacing();

        ImGui.TextColored(ImGuiColors.DalamudGrey, PluginInstallerLocs.FeedbackModal_Hint);

        const float buttonWidth = 120f;
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);

        if (ImGui.Button(PluginInstallerLocs.ErrorModalButton_Ok, new Vector2(buttonWidth, 40)))
        {
            if (string.IsNullOrWhiteSpace(this.contact))
            {
                this.errorModal.ShowErrorModal(PluginInstallerLocs.FeedbackModal_ContactInformationRequired)
                    .ContinueWith(_ =>
                    {
                        this.onNextFrameDontClear = true;
                        this.onNextFrame = true;
                    });
            }
            else
            {
                if (this.plugin != null)
                {
                    Task.Run(async () => await BugBait.SendFeedback(
                                             this.plugin,
                                             this.isTestingPlugin,
                                             this.body,
                                             this.contact,
                                             this.includeException))
                        .ContinueWith(t =>
                        {
                            var notif = Service<NotificationManager>.Get();
                            if (t.IsCanceled || t.IsFaulted)
                            {
                                notif.AddNotification(
                                    PluginInstallerLocs.FeedbackModal_NotificationError,
                                    PluginInstallerLocs.FeedbackModal_Title,
                                    NotificationType.Error);
                            }
                            else
                            {
                                notif.AddNotification(
                                    PluginInstallerLocs.FeedbackModal_NotificationSuccess,
                                    PluginInstallerLocs.FeedbackModal_Title,
                                    NotificationType.Success);
                            }
                        });
                }
                else
                {
                    PluginInstallerWindow.Log.Error("FeedbackPlugin was null.");
                }

                if (!string.IsNullOrWhiteSpace(this.contact))
                {
                    Service<DalamudConfiguration>.Get().LastFeedbackContactDetails = this.contact;
                }

                ImGui.CloseCurrentPopup();
            }
        }
    }
}

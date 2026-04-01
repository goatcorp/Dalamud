using System.Linq;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Enums;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;

/// <summary>
/// Class responsible for drawing the footer.
/// </summary>
internal class PluginInstallerFooter
{
    private readonly PluginInstallerWindow installerWindow;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstallerFooter"/> class.
    /// </summary>
    /// <param name="installerWindow">Reference to main Installer Window.</param>
    public PluginInstallerFooter(PluginInstallerWindow installerWindow)
    {
        this.installerWindow = installerWindow;
    }

    /// <summary>
    /// Draw the Footer.
    /// </summary>
    public void Draw()
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var pluginManager = Service<PluginManager>.Get();

        var windowSize = ImGui.GetWindowContentRegionMax();
        var placeholderButtonSize = ImGuiHelpers.GetButtonSize("placeholder");

        ImGui.Separator();

        ImGui.SetCursorPosY(windowSize.Y - placeholderButtonSize.Y);

        this.DrawUpdatePluginsButton();

        ImGui.SameLine();
        if (ImGui.Button(PluginInstallerLocs.FooterButton_Settings))
        {
            Service<DalamudInterface>.Get().OpenSettings();
        }

        // If any dev plugin locations exist, allow a shortcut for the /xldev menu item
        var hasDevPluginLocations = configuration.DevPluginLoadLocations.Count > 0;
        if (hasDevPluginLocations)
        {
            ImGui.SameLine();
            if (ImGui.Button(PluginInstallerLocs.FooterButton_ScanDevPlugins))
            {
                _ = pluginManager.ScanDevPluginsAsync();
            }
        }

        var closeText = PluginInstallerLocs.FooterButton_Close;
        var closeButtonSize = ImGuiHelpers.GetButtonSize(closeText);

        ImGui.SameLine(windowSize.X - closeButtonSize.X - 20);
        if (ImGui.Button(closeText))
        {
            this.installerWindow.IsOpen = false;
            configuration.QueueSave();
        }
    }

    private void DrawUpdatePluginsButton()
    {
        var pluginManager = Service<PluginManager>.Get();
        var notifications = Service<NotificationManager>.Get();

        var ready = pluginManager.PluginsReady && pluginManager.ReposReady;

        if (pluginManager.SafeMode)
        {
            ImGuiComponents.DisabledButton(PluginInstallerLocs.FooterButton_UpdateSafeMode);
        }
        else if (!ready || this.installerWindow.updateStatus is OperationStatus.InProgress || this.installerWindow.installStatus is OperationStatus.InProgress)
        {
            ImGuiComponents.DisabledButton(PluginInstallerLocs.FooterButton_UpdatePlugins);
        }
        else if (this.installerWindow.updateStatus is OperationStatus.Complete)
        {
            ImGui.Button(
                this.installerWindow.updatePluginCount > 0
                    ? PluginInstallerLocs.FooterButton_UpdateComplete(this.installerWindow.updatePluginCount)
                    : PluginInstallerLocs.FooterButton_NoUpdates);
        }
        else
        {
            if (ImGui.Button(PluginInstallerLocs.FooterButton_UpdatePlugins))
            {
                this.OnUpdatePluginsButtonClicked(pluginManager, notifications);
            }
        }
    }

    private void OnUpdatePluginsButtonClicked(PluginManager pluginManager, NotificationManager notifications)
    {
        this.installerWindow.updateStatus = OperationStatus.InProgress;
        this.installerWindow.loadingIndicatorKind = LoadingIndicatorKind.UpdatingAll;

        var toUpdate = this.installerWindow.pluginListUpdatable
                           .Where(x => x.InstalledPlugin.IsWantedByAnyProfile)
                           .ToList();

        Task.Run(() => pluginManager.UpdatePluginsAsync(toUpdate, false))
            .ContinueWith(task =>
            {
                this.installerWindow.updateStatus = OperationStatus.Complete;

                if (task.IsFaulted)
                {
                    this.installerWindow.updatePluginCount = 0;
                    this.installerWindow.updatedPlugins = null;
                    this.installerWindow.DisplayErrorContinuation(task, PluginInstallerLocs.ErrorModal_UpdaterFatal);
                }
                else
                {
                    this.installerWindow.updatedPlugins = task.Result.Where(res => res.Status == PluginUpdateStatus.StatusKind.Success).ToList();
                    this.installerWindow.updatePluginCount = this.installerWindow.updatedPlugins.Count;

                    var errorPlugins = task.Result.Where(res => res.Status != PluginUpdateStatus.StatusKind.Success).ToList();
                    var errorPluginCount = errorPlugins.Count;

                    if (errorPluginCount > 0)
                    {
                        var errorMessage = this.installerWindow.updatePluginCount > 0
                                               ? PluginInstallerLocs.ErrorModal_UpdaterFailPartial(this.installerWindow.updatePluginCount, errorPluginCount)
                                               : PluginInstallerLocs.ErrorModal_UpdaterFail(errorPluginCount);

                        var hintInsert = errorPlugins
                                         .Aggregate(string.Empty, (current, pluginUpdateStatus) => $"{current}* {pluginUpdateStatus.InternalName} ({PluginUpdateStatus.LocalizeUpdateStatusKind(pluginUpdateStatus.Status)})\n")
                                         .TrimEnd();
                        errorMessage += PluginInstallerLocs.ErrorModal_HintBlame(hintInsert);

                        this.installerWindow.DisplayErrorContinuation(task, errorMessage);
                    }

                    if (this.installerWindow.updatePluginCount > 0)
                    {
                        Service<PluginManager>.Get().PrintUpdatedPlugins(this.installerWindow.updatedPlugins, PluginInstallerLocs.PluginUpdateHeader_Chatbox);
                        notifications.AddNotification(
                            new Notification
                            {
                                Title = PluginInstallerLocs.Notifications_UpdatesInstalledTitle,
                                Content = PluginInstallerLocs.Notifications_UpdatesInstalled(this.installerWindow.updatedPlugins),
                                Type = NotificationType.Success,
                                Icon = INotificationIcon.From(FontAwesomeIcon.Download),
                            });

                        this.installerWindow.categoryManager.CurrentGroupKind = PluginCategoryManager.GroupKind.Installed;
                    }
                    else if (this.installerWindow.updatePluginCount is 0)
                    {
                        notifications.AddNotification(PluginInstallerLocs.Notifications_NoUpdatesFound, PluginInstallerLocs.Notifications_NoUpdatesFoundTitle, NotificationType.Info);
                    }
                }
            });
    }
}

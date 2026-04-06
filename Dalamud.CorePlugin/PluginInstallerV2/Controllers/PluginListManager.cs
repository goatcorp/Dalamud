using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Plugin;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.CorePlugin.PluginInstallerV2.Controllers;

/// <summary>
/// Class responsible for managing the plugin lists for the Plugin Installer.
/// </summary>
internal class PluginListManager
{
    private readonly Lock listLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginListManager"/> class.
    /// </summary>
    public PluginListManager()
    {
        this.PluginListAvailable = [];
        this.PluginListInstalled = [];
        this.PluginListUpdatable = [];
        this.HiddenPlugins = [];

        this.UpdatePluginLists();
    }

    /// <summary>
    /// Gets list of available plugins.
    /// </summary>
    public List<RemotePluginManifest> PluginListAvailable { get; private set; }

    /// <summary>
    /// Gets list of Installed Plugins.
    /// </summary>
    public List<LocalPlugin> PluginListInstalled { get; private set; }

    /// <summary>
    /// Gets list of Updatable Plugins.
    /// </summary>
    public List<AvailablePluginUpdate> PluginListUpdatable { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there are any Dev Plugins Installed.
    /// </summary>
    public bool HasDevPlugins
        => this.PluginListInstalled.Any(plugin => plugin.IsDev);

    /// <summary>
    /// Gets a value indicating whether there are any hidden plugins.
    /// </summary>
    public bool HasHiddenPlugins
        => this.HiddenPlugins.Count != 0;

    private List<string> HiddenPlugins { get; set; }

    /// <summary>
    /// Updates all plugin lists.
    /// </summary>
    public void UpdatePluginLists()
    {
        var pluginManager = Service<PluginManager>.Get();
        var configuration = Service<DalamudConfiguration>.Get();

        lock (this.listLock)
        {
            this.PluginListAvailable = pluginManager.AvailablePlugins.ToList();
            this.PluginListInstalled = pluginManager.InstalledPlugins.ToList();
            this.PluginListUpdatable = pluginManager.UpdatablePlugins.ToList();
            this.HiddenPlugins = configuration.HiddenPluginInternalName.ToList();
        }
    }

    /// <summary>
    /// Updates the plugins lists to be in the specified order.
    /// </summary>
    /// <param name="searchController">Reference to Search Controller with Information on how to sort/filter the results.</param>
    public void UpdateSortOrder(SearchController searchController)
    {
    }

    /// <summary>
    /// Enables a already loaded plugin.
    /// </summary>
    /// <param name="plugin">Plugin.</param>
    public void EnablePlugin(LocalPlugin plugin)
    {
        var notifications = Service<NotificationManager>.Get();
        var profileManager = Service<ProfileManager>.Get();

        var profilesThatWantThisPlugin = profileManager.Profiles
                                                       .Where(x => x.WantsPlugin(plugin.EffectiveWorkingPluginId) != null)
                                                       .ToArray();

        var applicableProfile = profilesThatWantThisPlugin.First();

        Task.Run(async () =>
        {
            await applicableProfile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, true, false);
            await plugin.LoadAsync(PluginLoadReason.Installer);

            notifications.AddNotification(
                PluginInstallerLocs.Notifications_PluginEnabled(plugin.Manifest.Name),
                PluginInstallerLocs.Notifications_PluginEnabledTitle,
                NotificationType.Success);
        });
    }

    /// <summary>
    /// Disables an already loaded plugin.
    /// </summary>
    /// <param name="plugin">Plugin.</param>
    public void DisablePlugin(LocalPlugin plugin)
    {
        var notifications = Service<NotificationManager>.Get();
        var profileManager = Service<ProfileManager>.Get();

        var profilesThatWantThisPlugin = profileManager.Profiles
                                                       .Where(x => x.WantsPlugin(plugin.EffectiveWorkingPluginId) != null)
                                                       .ToArray();

        var applicableProfile = profilesThatWantThisPlugin.First();

        Task.Run(async () =>
        {
            await plugin.UnloadAsync();
            await applicableProfile.AddOrUpdateAsync(
                plugin.EffectiveWorkingPluginId,
                plugin.Manifest.InternalName,
                false,
                false);

            notifications.AddNotification(
                PluginInstallerLocs.Notifications_PluginDisabled(plugin.Manifest.Name),
                PluginInstallerLocs.Notifications_PluginDisabledTitle,
                NotificationType.Success);
        });
    }

    /// <summary>
    /// Updates plugins.
    /// </summary>
    /// <returns>Task representing update progress.</returns>
    public Task UpdatePlugins()
    {
        var pluginManager = Service<PluginManager>.Get();
        var notifications = Service<NotificationManager>.Get();
        int updatePluginCount;

        var toUpdate =
            this.PluginListUpdatable
                .Where(x => x.InstalledPlugin.IsWantedByAnyProfile)
                .ToList();

        return Task.Run(() => pluginManager.UpdatePluginsAsync(toUpdate, false))
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    updatePluginCount = 0;
                    // this.DisplayErrorContinuation(task, PluginInstallerLocs.ErrorModal_UpdaterFatal); // todo: this.
                }
                else
                {
                    var updatedPlugins = task.Result.Where(res => res.Status == PluginUpdateStatus.StatusKind.Success).ToList();
                    updatePluginCount = updatedPlugins.Count;

                    var errorPlugins = task.Result.Where(res => res.Status != PluginUpdateStatus.StatusKind.Success).ToList();
                    var errorPluginCount = errorPlugins.Count;

                    if (errorPluginCount > 0)
                    {
                        var errorMessage = updatePluginCount > 0
                                               ? PluginInstallerLocs.ErrorModal_UpdaterFailPartial(updatePluginCount, errorPluginCount)
                                               : PluginInstallerLocs.ErrorModal_UpdaterFail(errorPluginCount);

                        var hintInsert = errorPlugins
                                         .Aggregate(string.Empty, (current, pluginUpdateStatus) => $"{current}* {pluginUpdateStatus.InternalName} ({PluginUpdateStatus.LocalizeUpdateStatusKind(pluginUpdateStatus.Status)})\n")
                                         .TrimEnd();
                        errorMessage += PluginInstallerLocs.ErrorModal_HintBlame(hintInsert);

                        // this.DisplayErrorContinuation(task, errorMessage); // todo: this.
                    }

                    if (updatePluginCount > 0)
                    {
                        Service<PluginManager>.Get().PrintUpdatedPlugins(updatedPlugins, PluginInstallerLocs.PluginUpdateHeader_Chatbox);
                        notifications.AddNotification(new Notification
                        {
                            Title = PluginInstallerLocs.Notifications_UpdatesInstalledTitle,
                            Content = PluginInstallerLocs.Notifications_UpdatesInstalled(updatedPlugins),
                            Type = NotificationType.Success,
                            Icon = INotificationIcon.From(FontAwesomeIcon.Download),
                        });

                        // this.categoryManager.CurrentGroupKind = PluginCategoryManager.GroupKind.Installed; // todo: this.
                    }
                    else if (updatePluginCount is 0)
                    {
                        notifications.AddNotification(PluginInstallerLocs.Notifications_NoUpdatesFound, PluginInstallerLocs.Notifications_NoUpdatesFoundTitle, NotificationType.Info);
                    }
                }
            });
    }
}

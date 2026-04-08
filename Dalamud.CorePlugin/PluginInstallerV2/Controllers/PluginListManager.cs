using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.CorePlugin.PluginInstallerV2.Enums;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Plugin;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Utility;

namespace Dalamud.CorePlugin.PluginInstallerV2.Controllers;

/// <summary>
/// Class responsible for managing the plugin lists for the Plugin Installer.
/// </summary>
internal class PluginListManager
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginListManager"/> class.
    /// </summary>
    public PluginListManager()
    {
        this.PluginListAvailable = [];
        this.PluginListInstalled = [];
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
    /// Gets a value indicating whether there are any Dev Plugins Installed.
    /// </summary>
    public bool HasDevPlugins
        => this.PluginListInstalled.Any(plugin => plugin.IsDev);

    /// <summary>
    /// Gets whether the specified internal name has been seen yet.
    /// </summary>
    /// <param name="internalName">Plugin Internal Name.</param>
    /// <returns>If the plugin has been seen.</returns>
    public static bool WasPluginSeen(string internalName)
        => Service<DalamudConfiguration>.Get().SeenPluginInternalName.Contains(internalName);

    /// <summary>
    /// Enables a already loaded plugin.
    /// </summary>
    /// <param name="plugin">Plugin.</param>
    public static void EnablePlugin(LocalPlugin plugin)
    {
        var notifications = Service<NotificationManager>.Get();
        var profileManager = Service<ProfileManager>.Get();

        var profilesThatWantThisPlugin = profileManager.Profiles.Where(x => x.WantsPlugin(plugin.EffectiveWorkingPluginId) is not null).ToArray();
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
    public static void DisablePlugin(LocalPlugin plugin)
    {
        var notifications = Service<NotificationManager>.Get();
        var profileManager = Service<ProfileManager>.Get();

        var profilesThatWantThisPlugin = profileManager.Profiles.Where(x => x.WantsPlugin(plugin.EffectiveWorkingPluginId) is not null).ToArray();
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
    /// Start a plugin install and handle errors visually.
    /// </summary>
    /// <param name="manifest">The manifest to install.</param>
    /// <param name="useTesting">Install the testing version.</param>
    public void StartInstall(RemotePluginManifest manifest, bool useTesting)
    {
        var pluginManager = Service<PluginManager>.Get();
        var notifications = Service<NotificationManager>.Get();

        Task.Run(() => pluginManager.InstallPluginAsync(manifest, useTesting || manifest.IsTestingExclusive, PluginLoadReason.Installer))
            .ContinueWith(task =>
            {
                // Fine as long as we aren't in an error state
                if (task.Result.State is PluginState.Loaded or PluginState.Unloaded)
                {
                    notifications.AddNotification(PluginInstallerLocs.Notifications_PluginInstalled(manifest.Name), PluginInstallerLocs.Notifications_PluginInstalledTitle, NotificationType.Success);
                }
                else
                {
                    notifications.AddNotification(PluginInstallerLocs.Notifications_PluginNotInstalled(manifest.Name), PluginInstallerLocs.Notifications_PluginNotInstalledTitle, NotificationType.Error);
                    // this.ShowErrorModal(PluginInstallerLocs.ErrorModal_InstallFail(manifest.Name));
                }

                // // There is no need to set as Complete for an individual plugin installation
                // if (this.DisplayErrorContinuation(task, PluginInstallerLocs.ErrorModal_InstallFail(manifest.Name)))
                // {
                //     // Fine as long as we aren't in an error state
                //     if (task.Result.State is PluginState.Loaded or PluginState.Unloaded)
                //     {
                //         notifications.AddNotification(PluginInstallerLocs.Notifications_PluginInstalled(manifest.Name), PluginInstallerLocs.Notifications_PluginInstalledTitle, NotificationType.Success);
                //     }
                //     else
                //     {
                //         notifications.AddNotification(PluginInstallerLocs.Notifications_PluginNotInstalled(manifest.Name), PluginInstallerLocs.Notifications_PluginNotInstalledTitle, NotificationType.Error);
                //         this.ShowErrorModal(PluginInstallerLocs.ErrorModal_InstallFail(manifest.Name));
                //     }
                // }
            });
    }

    /// <summary>
    /// Updates the plugins lists to be in the specified order.
    /// </summary>
    /// <param name="searchController">Reference to Search Controller with Information on how to sort/filter the results.</param>
    public void UpdateSortOrder(SearchController searchController)
    {
        var pluginManager = Service<PluginManager>.Get();
        var profileManager = Service<ProfileManager>.Get();
        var configuration = Service<DalamudConfiguration>.Get();

        var availablePlugins = pluginManager.AvailablePlugins
                                            .Where(manifest => !manifest.IsTestingExclusive || configuration.DoPluginTest)
                                            .OrderByDescending(entry => GetManifestSearchScore(entry, searchController));

        var installedPlugins = pluginManager.InstalledPlugins
                                            .OrderByDescending(p => GetManifestSearchScore(p.Manifest, searchController));

        availablePlugins = searchController.SelectedSortOption switch
        {
            SortOptions.Alphabetically => availablePlugins.ThenBy(entry => entry.Name),
            SortOptions.DownloadCount => availablePlugins.ThenByDescending(entry => entry.DownloadCount),
            SortOptions.LastUpdate => availablePlugins.ThenByDescending(entry => entry.LastUpdate),
            SortOptions.New => availablePlugins.ThenBy(entry => WasPluginSeen(entry.InternalName)),
            SortOptions.NotInstalled => availablePlugins.ThenBy(entry => pluginManager.InstalledPlugins.Any(installed => installed.InternalName == entry.InternalName)),
            SortOptions.Enabled => availablePlugins.ThenByDescending(entry => pluginManager.InstalledPlugins.Any(installed => installed.InternalName == entry.InternalName)),
            _ => availablePlugins.ThenBy(entry => entry.Name), // Fallback to Alphabetically
        };

        installedPlugins = searchController.SelectedSortOption switch
        {
            SortOptions.Alphabetically => installedPlugins.ThenBy(entry => entry.Name),
            SortOptions.DownloadCount => installedPlugins.ThenBy(entry => entry.Manifest.DownloadCount),
            SortOptions.LastUpdate => installedPlugins.ThenBy(entry => pluginManager.AvailablePlugins.FirstOrDefault(x => x.InternalName == entry.InternalName)?.LastUpdate),
            SortOptions.New => installedPlugins.ThenBy(entry => WasPluginSeen(entry.InternalName)),
            SortOptions.Enabled => installedPlugins.ThenBy(entry => entry.State is PluginState.Loaded),
            SortOptions.InCollection => installedPlugins.ThenBy(entry => profileManager.IsInDefaultProfile(entry.EffectiveWorkingPluginId)),
            _ => installedPlugins.ThenBy(entry => entry.Name), // Fallback to Alphabetically
        };

        this.PluginListAvailable = availablePlugins.ToList();
        this.PluginListInstalled = installedPlugins.ToList();
    }

    /// <summary>
    /// Updates plugins.
    /// </summary>
    /// <returns>Task representing update progress.</returns>
    public Task UpdatePlugins()
    {
        var pluginManager = Service<PluginManager>.Get();

        var toUpdate =
            pluginManager.UpdatablePlugins
                .Where(x => x.InstalledPlugin.IsWantedByAnyProfile)
                .ToList();

        return Task.Run(() => pluginManager.UpdatePluginsAsync(toUpdate, false)).ContinueWith(PluginUpdateContinuation);
    }

    private static int GetManifestSearchScore(IPluginManifest manifest, SearchController searchInfo)
    {
        var searchString = searchInfo.SearchString.ToLowerInvariant();
        var matcher = new FuzzyMatcher(searchString, MatchMode.FuzzyParts);
        List<int> scores = [0];

        if (!manifest.Name.IsNullOrEmpty())
        {
            scores.Add(matcher.Matches(manifest.Name.ToLowerInvariant()) * 110);
        }

        if (!manifest.InternalName.IsNullOrEmpty())
        {
            scores.Add(matcher.Matches(manifest.InternalName.ToLowerInvariant()) * 105);
        }

        if (!manifest.Author.IsNullOrEmpty())
        {
            scores.Add(matcher.Matches(manifest.Author.ToLowerInvariant()) * 100);
        }

        if (!manifest.Punchline.IsNullOrEmpty())
        {
            scores.Add(matcher.Matches(manifest.Punchline.ToLowerInvariant()) * 100);
        }

        if (manifest.Tags != null)
        {
            scores.Add(matcher.MatchesAny(manifest.Tags.Select(tag => tag.ToLowerInvariant()).ToArray()) * 100);
        }

        return scores.Max();
    }

    /// <summary>
    /// Continuation function to report the result of a "Update All Plugins" task.
    /// </summary>
    /// <param name="task">Source task.</param>
    private static void PluginUpdateContinuation(Task<IEnumerable<PluginUpdateStatus>> task)
    {
        var notifications = Service<NotificationManager>.Get();
        var pluginManager = Service<PluginManager>.Get();

        if (task is { IsFaulted: true })
        {
        }
        else
        {
            var updatedPlugins = task.Result.Where(res => res.Status == PluginUpdateStatus.StatusKind.Success).ToList();

            if (updatedPlugins.Count is not 0)
            {
                pluginManager.PrintUpdatedPlugins(updatedPlugins, PluginInstallerLocs.PluginUpdateHeader_Chatbox);
                notifications.AddNotification(new Notification
                {
                    Title = PluginInstallerLocs.Notifications_UpdatesInstalledTitle,
                    Content = PluginInstallerLocs.Notifications_UpdatesInstalled(updatedPlugins),
                    Type = NotificationType.Success,
                    Icon = INotificationIcon.From(FontAwesomeIcon.Download),
                });
            }
            else
            {
                notifications.AddNotification(new Notification
                {
                    Title = PluginInstallerLocs.Notifications_NoUpdatesFoundTitle,
                    Content = PluginInstallerLocs.Notifications_NoUpdatesFound,
                    Type = NotificationType.Info,
                });
            }
        }

        // .ContinueWith(task =>
        // {
        //     if (task.IsFaulted)
        //     {
        //         updatePluginCount = 0;
        //         // this.DisplayErrorContinuation(task, PluginInstallerLocs.ErrorModal_UpdaterFatal);
        //     }
        //     else
        //     {
        //         var updatedPlugins = task.Result.Where(res => res.Status == PluginUpdateStatus.StatusKind.Success).ToList();
        //         updatePluginCount = updatedPlugins.Count;
        //
        //         var errorPlugins = task.Result.Where(res => res.Status != PluginUpdateStatus.StatusKind.Success).ToList();
        //         var errorPluginCount = errorPlugins.Count;
        //
        //         if (errorPluginCount > 0)
        //         {
        //             var errorMessage = updatePluginCount > 0
        //                                    ? PluginInstallerLocs.ErrorModal_UpdaterFailPartial(updatePluginCount, errorPluginCount)
        //                                    : PluginInstallerLocs.ErrorModal_UpdaterFail(errorPluginCount);
        //
        //             var hintInsert = errorPlugins
        //                              .Aggregate(string.Empty, (current, pluginUpdateStatus) => $"{current}* {pluginUpdateStatus.InternalName} ({PluginUpdateStatus.LocalizeUpdateStatusKind(pluginUpdateStatus.Status)})\n")
        //                              .TrimEnd();
        //             errorMessage += PluginInstallerLocs.ErrorModal_HintBlame(hintInsert);
        //
        //             // this.DisplayErrorContinuation(task, errorMessage);
        //         }
        //
        //         if (updatePluginCount > 0)
        //         {
        //             Service<PluginManager>.Get().PrintUpdatedPlugins(updatedPlugins, PluginInstallerLocs.PluginUpdateHeader_Chatbox);
        //             notifications.AddNotification(new Notification
        //             {
        //                 Title = PluginInstallerLocs.Notifications_UpdatesInstalledTitle,
        //                 Content = PluginInstallerLocs.Notifications_UpdatesInstalled(updatedPlugins),
        //                 Type = NotificationType.Success,
        //                 Icon = INotificationIcon.From(FontAwesomeIcon.Download),
        //             });
        //
        //             // this.categoryManager.CurrentGroupKind = PluginCategoryManager.GroupKind.Installed;
        //         }
        //         else if (updatePluginCount is 0)
        //         {
        //             notifications.AddNotification(PluginInstallerLocs.Notifications_NoUpdatesFound, PluginInstallerLocs.Notifications_NoUpdatesFoundTitle, NotificationType.Info);
        //         }
        //     }
        // });
    }
}

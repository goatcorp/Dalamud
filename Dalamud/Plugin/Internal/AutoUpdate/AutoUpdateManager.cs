using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Console;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.EventArgs;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.DesignSystem;
using Dalamud.Interface.Utility;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

namespace Dalamud.Plugin.Internal.AutoUpdate;

/// <summary>
/// Class to manage automatic updates for plugins.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class AutoUpdateManager : IServiceType
{
    private static readonly ModuleLog Log = new("AUTOUPDATE");

    /// <summary>
    /// Time we should wait after login to update.
    /// </summary>
    private static readonly TimeSpan UpdateTimeAfterLogin = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Time we should wait between scheduled update checks.
    /// </summary>
    private static readonly TimeSpan TimeBetweenUpdateChecks = TimeSpan.FromHours(2);

    /// <summary>
    /// Time we should wait between scheduled update checks if the user has dismissed the notification,
    /// instead of updating. We don't want to spam the user with notifications.
    /// </summary>
    private static readonly TimeSpan TimeBetweenUpdateChecksIfDismissed = TimeSpan.FromHours(12);

    /// <summary>
    /// Time we should wait after unblocking to nag the user.
    /// Used to prevent spamming a nag, for example, right after an user leaves a duty.
    /// </summary>
    private static readonly TimeSpan CooldownAfterUnblock = TimeSpan.FromSeconds(30);

    [ServiceManager.ServiceDependency]
    private readonly PluginManager pluginManager = Service<PluginManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration config = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceDependency]
    private readonly NotificationManager notificationManager = Service<NotificationManager>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudInterface dalamudInterface = Service<DalamudInterface>.Get();

    private readonly IConsoleVariable<bool> isDryRun;

    private readonly Task<DalamudLinkPayload> openInstallerWindowLinkTask;

    private DateTime? loginTime;
    private DateTime? nextUpdateCheckTime;
    private DateTime? unblockedSince;

    private bool hasStartedInitialUpdateThisSession;

    private IActiveNotification? updateNotification;

    private Task? autoUpdateTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoUpdateManager"/> class.
    /// </summary>
    /// <param name="console">Console service.</param>
    [ServiceManager.ServiceConstructor]
    public AutoUpdateManager(ConsoleManager console)
    {
        Service<ClientState>.GetAsync().ContinueWith(
            t =>
            {
                t.Result.Login += this.OnLogin;
                t.Result.Logout += (int type, int code) => this.OnLogout();
            });
        Service<Framework>.GetAsync().ContinueWith(t => { t.Result.Update += this.OnUpdate; });

        this.openInstallerWindowLinkTask =
            Service<ChatGui>.GetAsync().ContinueWith(
                chatGuiTask => chatGuiTask.Result.AddChatLinkHandler(
                    "Dalamud",
                    1001,
                    (_, _) =>
                     {
                         Service<DalamudInterface>.GetNullable()?.OpenPluginInstallerTo(PluginInstallerOpenKind.InstalledPlugins);
                     }));

        this.isDryRun = console.AddVariable("dalamud.autoupdate.dry_run", "Simulate updates instead", false);
        console.AddCommand("dalamud.autoupdate.trigger_login", "Trigger a login event", () =>
        {
            this.hasStartedInitialUpdateThisSession = false;
            this.OnLogin();
            return true;
        });
        console.AddCommand("dalamud.autoupdate.force_check", "Force a check for updates", () =>
        {
            this.nextUpdateCheckTime = DateTime.Now + TimeSpan.FromSeconds(5);
            return true;
        });
    }

    private enum UpdateListingRestriction
    {
        Unrestricted,
        AllowNone,
        AllowMainRepo,
    }

    /// <summary>
    /// Gets a value indicating whether or not auto-updates have already completed this session.
    /// </summary>
    public bool IsAutoUpdateComplete { get; private set; }

    /// <summary>
    /// Gets the time of the next scheduled update check.
    /// </summary>
    public DateTime? NextUpdateCheckTime => this.nextUpdateCheckTime;

    /// <summary>
    /// Gets the time the auto-update was unblocked.
    /// </summary>
    public DateTime? UnblockedSince => this.unblockedSince;

    private static UpdateListingRestriction DecideUpdateListingRestriction(AutoUpdateBehavior behavior)
    {
        return behavior switch
        {
            // We don't generally allow any updates in this mode, but specific opt-ins.
            AutoUpdateBehavior.None => UpdateListingRestriction.AllowNone,

            // If we're only notifying, I guess it's fine to list all plugins.
            AutoUpdateBehavior.OnlyNotify => UpdateListingRestriction.Unrestricted,

            AutoUpdateBehavior.UpdateMainRepo => UpdateListingRestriction.AllowMainRepo,
            AutoUpdateBehavior.UpdateAll => UpdateListingRestriction.Unrestricted,
            _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior, null),
        };
    }

    private static void DrawOpenInstallerNotificationButton(bool primary, PluginInstallerOpenKind kind, IActiveNotification notification)
    {
        if (primary ?
                DalamudComponents.PrimaryButton(Locs.NotificationButtonOpenPluginInstaller) :
                DalamudComponents.SecondaryButton(Locs.NotificationButtonOpenPluginInstaller))
        {
            Service<DalamudInterface>.Get().OpenPluginInstallerTo(kind);
            notification.DismissNow();
        }
    }

    private void OnUpdate(IFramework framework)
    {
        if (this.loginTime == null)
            return;

        var autoUpdateTaskInProgress = this.autoUpdateTask is not null && !this.autoUpdateTask.IsCompleted;
        var isUnblocked = this.CanUpdateOrNag() && !autoUpdateTaskInProgress;

        if (this.unblockedSince == null && isUnblocked)
        {
            this.unblockedSince = DateTime.Now;
        }
        else if (this.unblockedSince != null && !isUnblocked)
        {
            this.unblockedSince = null;

            // Remove all notifications if we're not actively updating. The user probably doesn't care now.
            if (this.updateNotification != null && !autoUpdateTaskInProgress)
            {
                this.updateNotification.DismissNow();
                this.updateNotification = null;
            }
        }

        // If we're blocked, we don't do anything.
        if (!isUnblocked)
            return;

        var isInUnblockedCooldown =
            this.unblockedSince != null && DateTime.Now - this.unblockedSince < CooldownAfterUnblock;

        // If we're in the unblock cooldown period, we don't nag the user. This is intended to prevent us
        // from showing update notifications right after the user leaves a duty, for example.
        if (isInUnblockedCooldown && this.hasStartedInitialUpdateThisSession)
            return;

        var behavior = this.config.AutoUpdateBehavior ?? AutoUpdateBehavior.None;

        // 1. This is the initial update after login. We only run this exactly once and this is
        //    the only time we actually install updates automatically.
        if (!this.hasStartedInitialUpdateThisSession && DateTime.Now > this.loginTime.Value.Add(UpdateTimeAfterLogin))
        {
            this.hasStartedInitialUpdateThisSession = true;

            var currentlyUpdatablePlugins = this.GetAvailablePluginUpdates(DecideUpdateListingRestriction(behavior));
            if (currentlyUpdatablePlugins.Count == 0)
            {
                this.IsAutoUpdateComplete = true;
                this.nextUpdateCheckTime = DateTime.Now + TimeBetweenUpdateChecks;

                return;
            }

            // TODO: This is not 100% what we want... Plugins that are opted-in should be updated regardless of the behavior,
            //       and we should show a notification for the others afterwards.
            if (behavior == AutoUpdateBehavior.OnlyNotify)
            {
                // List all plugins in the notification
                Log.Verbose("Running initial auto-update, notifying for {Num} plugins", currentlyUpdatablePlugins.Count);
                this.NotifyUpdatesAreAvailable(currentlyUpdatablePlugins);
                return;
            }

            Log.Verbose("Running initial auto-update, updating {Num} plugins", currentlyUpdatablePlugins.Count);
            this.KickOffAutoUpdates(currentlyUpdatablePlugins);
            return;
        }

        // 2. Continuously check for updates while the game is running. We run these every once in a while and
        //    will only show a notification here that lets people start the update or open the installer.
        if (this.config.CheckPeriodicallyForUpdates &&
            this.nextUpdateCheckTime != null &&
            DateTime.Now > this.nextUpdateCheckTime &&
            this.updateNotification == null)
        {
            Log.Verbose("Starting periodic update check");
            this.pluginManager.ReloadPluginMastersAsync()
                .ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted || t.IsCanceled)
                        {
                            Log.Error(t.Exception!, "Failed to reload plugin masters for auto-update");
                        }

                        Log.Verbose($"Available Updates: {string.Join(", ", this.pluginManager.UpdatablePlugins.Select(s => s.UpdateManifest.InternalName))}");
                        var updatable = this.GetAvailablePluginUpdates(
                            DecideUpdateListingRestriction(behavior));

                        if (updatable.Count > 0)
                        {
                            this.NotifyUpdatesAreAvailable(updatable);
                        }
                        else
                        {
                            this.nextUpdateCheckTime = DateTime.Now + TimeBetweenUpdateChecks;
                            Log.Verbose(
                                "Auto update found nothing to do, next update at {Time}",
                                this.nextUpdateCheckTime);
                        }
                    });
        }
    }

    private IActiveNotification GetBaseNotification(Notification notification)
    {
        if (this.updateNotification != null)
            throw new InvalidOperationException("Already showing a notification");

        this.updateNotification = this.notificationManager.AddNotification(notification);

        this.updateNotification.Dismiss += _ =>
        {
            this.updateNotification = null;

            // Schedule the next update opportunistically for when this closes.
            this.nextUpdateCheckTime = DateTime.Now + TimeBetweenUpdateChecks;
        };

        return this.updateNotification!;
    }

    private void KickOffAutoUpdates(ICollection<AvailablePluginUpdate> updatablePlugins, IActiveNotification? notification = null)
    {
        this.autoUpdateTask =
            Task.Run(() => this.RunAutoUpdates(updatablePlugins, notification))
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Log.Error(t.Exception!, "Failed to run auto-updates");
                    }
                    else if (t.IsCanceled)
                    {
                        Log.Warning("Auto-update task was canceled");
                    }

                    this.autoUpdateTask = null;
                    this.IsAutoUpdateComplete = true;
                });
    }

    private async Task RunAutoUpdates(ICollection<AvailablePluginUpdate> updatablePlugins, IActiveNotification? notification = null)
    {
        Log.Information("Found {UpdatablePluginsCount} plugins to update", updatablePlugins.Count);

        if (updatablePlugins.Count == 0)
            return;

        notification ??= this.GetBaseNotification(new Notification());
        notification.Title = Locs.NotificationTitleUpdatingPlugins;
        notification.Content = Locs.NotificationContentPreparingToUpdate(updatablePlugins.Count);
        notification.Type = NotificationType.Info;
        notification.InitialDuration = TimeSpan.MaxValue;
        notification.ShowIndeterminateIfNoExpiry = false;
        notification.UserDismissable = false;
        notification.Progress = 0;
        notification.Icon = INotificationIcon.From(FontAwesomeIcon.Download);
        notification.Minimized = false;

        var progress = new Progress<PluginManager.PluginUpdateProgress>();
        progress.ProgressChanged += (_, updateProgress) =>
        {
            notification.Content = Locs.NotificationContentUpdating(updateProgress.CurrentPluginManifest.Name);
            notification.Progress = (float)updateProgress.PluginsProcessed / updateProgress.TotalPlugins;
        };

        var pluginStates = (await this.pluginManager.UpdatePluginsAsync(updatablePlugins, this.isDryRun.Value, true, progress)).ToList();
        this.pluginManager.PrintUpdatedPlugins(pluginStates, Loc.Localize("DalamudPluginAutoUpdate", "The following plugins were auto-updated:"));

        notification.Progress = 1;
        notification.UserDismissable = true;
        notification.HardExpiry = DateTime.Now.AddSeconds(30);

        notification.DrawActions += _ =>
        {
            ImGuiHelpers.ScaledDummy(2);
            DrawOpenInstallerNotificationButton(true, PluginInstallerOpenKind.InstalledPlugins, notification);
        };

        // Update the notification to show the final state
        if (pluginStates.All(x => x.Status == PluginUpdateStatus.StatusKind.Success))
        {
            notification.Minimized = true;

            // Janky way to make sure the notification does not change before it's minimized...
            await Task.Delay(500);

            notification.Title = Locs.NotificationTitleUpdatesSuccessful;
            notification.MinimizedText = Locs.NotificationContentUpdatesSuccessfulMinimized;
            notification.Type = NotificationType.Success;
            notification.Content = Locs.NotificationContentUpdatesSuccessful;
        }
        else
        {
            notification.Title = Locs.NotificationTitleUpdatesFailed;
            notification.MinimizedText = Locs.NotificationContentUpdatesFailedMinimized;
            notification.Type = NotificationType.Error;
            notification.Content = Locs.NotificationContentUpdatesFailed;

            var failedPlugins = pluginStates
                                .Where(x => x.Status != PluginUpdateStatus.StatusKind.Success)
                                .Select(x => x.Name).ToList();

            notification.Content += "\n" + Locs.NotificationContentFailedPlugins(failedPlugins);
        }
    }

    private void NotifyUpdatesAreAvailable(ICollection<AvailablePluginUpdate> updatablePlugins)
    {
        if (updatablePlugins.Count == 0)
            return;

        var notification = this.GetBaseNotification(new Notification
        {
            Title = Locs.NotificationTitleUpdatesAvailable,
            Content = Locs.NotificationContentUpdatesAvailable(updatablePlugins),
            MinimizedText = Locs.NotificationContentUpdatesAvailableMinimized(updatablePlugins.Count),
            Type = NotificationType.Info,
            InitialDuration = TimeSpan.MaxValue,
            ShowIndeterminateIfNoExpiry = false,
            Icon = INotificationIcon.From(FontAwesomeIcon.Download),
        });

        void DrawNotificationContent(INotificationDrawArgs args)
        {
            ImGuiHelpers.ScaledDummy(2);

            if (DalamudComponents.PrimaryButton(Locs.NotificationButtonUpdate))
            {
                notification.DrawActions -= DrawNotificationContent;
                this.KickOffAutoUpdates(updatablePlugins, notification);
            }

            ImGui.SameLine();
            DrawOpenInstallerNotificationButton(false, PluginInstallerOpenKind.UpdateablePlugins, notification);
        }

        notification.DrawActions += DrawNotificationContent;

        // If the user dismisses the notification, we don't want to spam them with notifications. Schedule the next
        // auto update further out. Since this is registered after the initial OnDismiss, this should take precedence.
        notification.Dismiss += args =>
        {
            if (args.Reason != NotificationDismissReason.Manual) return;

            this.nextUpdateCheckTime = DateTime.Now + TimeBetweenUpdateChecksIfDismissed;
            Log.Verbose("User dismissed update notification, next check at {Time}", this.nextUpdateCheckTime);
        };

        // Send out a chat message only if the user requested so
        if (!this.config.SendUpdateNotificationToChat)
            return;

        var chatGui = Service<ChatGui>.GetNullable();
        if (chatGui == null)
        {
            Log.Verbose("Unable to get chat gui, discard notification for chat.");
            return;
        }

        chatGui.Print(new XivChatEntry
        {
            Message = new SeString(new List<Payload>
            {
                new TextPayload(Locs.NotificationContentUpdatesAvailableMinimized(updatablePlugins.Count)),
                new TextPayload("  ["),
                new UIForegroundPayload(500),
                this.openInstallerWindowLinkTask.Result,
                new TextPayload(Loc.Localize("DalamudInstallerHelp", "Open the plugin installer")),
                RawPayload.LinkTerminator,
                new UIForegroundPayload(0),
                new TextPayload("]"),
            }),

            Type = XivChatType.Urgent,
        });
    }

    private List<AvailablePluginUpdate> GetAvailablePluginUpdates(UpdateListingRestriction restriction)
    {
        var optIns = this.config.PluginAutoUpdatePreferences.ToArray();

        // Get all of our updatable plugins and do some initial filtering that must apply to all plugins.
        var updateablePlugins = this.pluginManager.UpdatablePlugins
                                    .Where(
                                        p =>
                                            !p.InstalledPlugin.IsDev && // Never update dev-plugins
                                            p.InstalledPlugin.IsWantedByAnyProfile && // Never update plugins that are not wanted by any profile(not enabled)
                                            !p.InstalledPlugin.Manifest.ScheduledForDeletion); // Never update plugins that we want to get rid of

        return updateablePlugins.Where(FilterPlugin).ToList();

        bool FilterPlugin(AvailablePluginUpdate availablePluginUpdate)
        {
            var optIn = optIns.FirstOrDefault(x => x.WorkingPluginId == availablePluginUpdate.InstalledPlugin.EffectiveWorkingPluginId);

            // If this is an opt-out, we don't update.
            if (optIn is { Kind: AutoUpdatePreference.OptKind.NeverUpdate })
                return false;

            if (restriction == UpdateListingRestriction.AllowNone && optIn is not { Kind: AutoUpdatePreference.OptKind.AlwaysUpdate })
                return false;

            if (restriction == UpdateListingRestriction.AllowMainRepo && availablePluginUpdate.InstalledPlugin.IsThirdParty)
                return false;

            return true;
        }
    }

    private void OnLogin()
    {
        this.loginTime = DateTime.Now;
    }

    private void OnLogout()
    {
        this.loginTime = null;
    }

    private bool CanUpdateOrNag()
    {
        var condition = Service<Condition>.Get();
        return this.IsPluginManagerReady() &&
            !this.dalamudInterface.IsPluginInstallerOpen &&
            condition.OnlyAny(ConditionFlag.NormalConditions,
                              ConditionFlag.Jumping,
                              ConditionFlag.Mounted,
                              ConditionFlag.UsingParasol);
    }

    private bool IsPluginManagerReady()
    {
        return this.pluginManager.ReposReady && this.pluginManager.PluginsReady && !this.pluginManager.SafeMode;
    }

    private static class Locs
    {
        public static string NotificationButtonOpenPluginInstaller => Loc.Localize("AutoUpdateOpenPluginInstaller", "Open installer");

        public static string NotificationButtonUpdate => Loc.Localize("AutoUpdateUpdate", "Update");

        public static string NotificationTitleUpdatesAvailable => Loc.Localize("AutoUpdateUpdatesAvailable", "Updates available!");

        public static string NotificationTitleUpdatesSuccessful => Loc.Localize("AutoUpdateUpdatesSuccessful", "Updates successful!");

        public static string NotificationTitleUpdatingPlugins => Loc.Localize("AutoUpdateUpdatingPlugins", "Updating plugins...");

        public static string NotificationTitleUpdatesFailed => Loc.Localize("AutoUpdateUpdatesFailed", "Updates failed!");

        public static string NotificationContentUpdatesSuccessful => Loc.Localize("AutoUpdateUpdatesSuccessfulContent", "All plugins have been updated successfully.");

        public static string NotificationContentUpdatesSuccessfulMinimized => Loc.Localize("AutoUpdateUpdatesSuccessfulContentMinimized", "Plugins updated successfully.");

        public static string NotificationContentUpdatesFailed => Loc.Localize("AutoUpdateUpdatesFailedContent", "Some plugins failed to update. Please check the plugin installer for more information.");

        public static string NotificationContentUpdatesFailedMinimized => Loc.Localize("AutoUpdateUpdatesFailedContentMinimized", "Plugins failed to update.");

        public static string NotificationContentUpdatesAvailable(ICollection<AvailablePluginUpdate> updatablePlugins)
            => (updatablePlugins.Count == 1
                    ? Loc.Localize(
                        "AutoUpdateUpdatesAvailableContentSingular",
                        "There is a plugin that can be updated:")
                    : string.Format(
                        Loc.Localize(
                            "AutoUpdateUpdatesAvailableContentPlural",
                            "There are {0} plugins that can be updated:"),
                        updatablePlugins.Count))
               + "\n\n" + string.Join(", ", updatablePlugins.Select(x => x.InstalledPlugin.Manifest.Name));

        public static string NotificationContentUpdatesAvailableMinimized(int numUpdates)
            => numUpdates == 1 ?
                   Loc.Localize("AutoUpdateUpdatesAvailableContentMinimizedSingular", "1 plugin update available") :
                   string.Format(Loc.Localize("AutoUpdateUpdatesAvailableContentMinimizedPlural", "{0} plugin updates available"), numUpdates);

        public static string NotificationContentPreparingToUpdate(int numPlugins)
            => numPlugins == 1 ?
                   Loc.Localize("AutoUpdatePreparingToUpdateSingular", "Preparing to update 1 plugin...") :
                   string.Format(Loc.Localize("AutoUpdatePreparingToUpdatePlural", "Preparing to update {0} plugins..."), numPlugins);

        public static string NotificationContentUpdating(string name)
            => string.Format(Loc.Localize("AutoUpdateUpdating", "Updating {0}..."), name);

        public static string NotificationContentFailedPlugins(IEnumerable<string> failedPlugins)
            => string.Format(Loc.Localize("AutoUpdateFailedPlugins", "Failed plugin(s): {0}"), string.Join(", ", failedPlugins));
    }
}

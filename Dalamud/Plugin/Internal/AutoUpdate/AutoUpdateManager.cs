using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CheapLoc;

using Dalamud.Configuration.Internal;
using Dalamud.Console;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.DesignSystem;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Utility;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

using ImGuiNET;

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
    private static readonly TimeSpan TimeBetweenUpdateChecks = TimeSpan.FromHours(1.5);

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
    
    private DateTime? loginTime;
    private DateTime? lastUpdateCheckTime;
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
                t.Result.Logout += this.OnLogout;
            });
        Service<Framework>.GetAsync().ContinueWith(t => { t.Result.Update += this.OnUpdate; });
        
        this.isDryRun = console.AddVariable("dalamud.autoupdate.dry_run", "Simulate updates instead", false);
        console.AddCommand("dalamud.autoupdate.trigger_login", "Trigger a login event", () =>
        {
            this.hasStartedInitialUpdateThisSession = false;
            this.OnLogin();
            return true;
        });
        console.AddCommand("dalamud.autoupdate.force_check", "Force a check for updates", () =>
        {
            this.lastUpdateCheckTime = DateTime.Now - TimeBetweenUpdateChecks;
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
            this.lastUpdateCheckTime = DateTime.Now;
            this.hasStartedInitialUpdateThisSession = true;
            
            var currentlyUpdatablePlugins = this.GetAvailablePluginUpdates(DecideUpdateListingRestriction(behavior));

            if (currentlyUpdatablePlugins.Count == 0)
            {
                this.IsAutoUpdateComplete = true;
                return;
            }
            
            // TODO: This is not 100% what we want... Plugins that are opted-in should be updated regardless of the behavior,
            //       and we should show a notification for the others afterwards.
            if (behavior == AutoUpdateBehavior.OnlyNotify)
            {
                // List all plugins in the notification
                Log.Verbose("Ran initial update, notifying for {Num} plugins", currentlyUpdatablePlugins.Count);
                this.NotifyUpdatesAreAvailable(currentlyUpdatablePlugins);
                return;
            }

            Log.Verbose("Ran initial update, updating {Num} plugins", currentlyUpdatablePlugins.Count);
            this.KickOffAutoUpdates(currentlyUpdatablePlugins);
            return;
        }

        // 2. Continuously check for updates while the game is running. We run these every once in a while and
        //    will only show a notification here that lets people start the update or open the installer.
        if (this.config.CheckPeriodicallyForUpdates &&
            this.lastUpdateCheckTime != null &&
            DateTime.Now - this.lastUpdateCheckTime > TimeBetweenUpdateChecks &&
            this.updateNotification == null)
        {
            this.pluginManager.ReloadPluginMastersAsync()
                .ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted || t.IsCanceled)
                        {
                            Log.Error(t.Exception!, "Failed to reload plugin masters for auto-update");
                        }
                        
                        this.NotifyUpdatesAreAvailable(
                            this.GetAvailablePluginUpdates(
                                DecideUpdateListingRestriction(behavior)));
                    });

            this.lastUpdateCheckTime = DateTime.Now;
        }
    }

    private IActiveNotification GetBaseNotification(Notification notification)
    {
        if (this.updateNotification != null)
            throw new InvalidOperationException("Already showing a notification");
        
        this.updateNotification = this.notificationManager.AddNotification(notification);
        this.updateNotification.Dismiss += _ => this.updateNotification = null;
        
        return this.updateNotification!;
    }

    private void KickOffAutoUpdates(ICollection<AvailablePluginUpdate> updatablePlugins)
    {
        this.autoUpdateTask =
            Task.Run(() => this.RunAutoUpdates(updatablePlugins))
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

    private async Task RunAutoUpdates(ICollection<AvailablePluginUpdate> updatablePlugins)
    {
        Log.Information("Found {UpdatablePluginsCount} plugins to update", updatablePlugins.Count);

        if (updatablePlugins.Count == 0)
            return;

        var notification = this.GetBaseNotification(new Notification
        {
            Title = Locs.NotificationTitleUpdatingPlugins,
            Content = Locs.NotificationContentPreparingToUpdate(updatablePlugins.Count),
            Type = NotificationType.Info,
            InitialDuration = TimeSpan.MaxValue,
            ShowIndeterminateIfNoExpiry = false,
            UserDismissable = false,
            Progress = 0,
            Icon = INotificationIcon.From(FontAwesomeIcon.Download),
            Minimized = false,
        });

        var progress = new Progress<PluginManager.PluginUpdateProgress>();
        progress.ProgressChanged += (_, progress) =>
        {
            notification.Content = Locs.NotificationContentUpdating(progress.CurrentPluginManifest.Name);
            notification.Progress = (float)progress.PluginsProcessed / progress.TotalPlugins;
        };
        
        var pluginStates = await this.pluginManager.UpdatePluginsAsync(updatablePlugins, this.isDryRun.Value, true, progress);

        notification.Progress = 1;
        notification.UserDismissable = true;
        notification.HardExpiry = DateTime.Now.AddSeconds(30);
        
        notification.DrawActions += _ =>
        {
            ImGuiHelpers.ScaledDummy(2);
            if (DalamudComponents.PrimaryButton(Locs.NotificationButtonOpenPluginInstaller))
            {
                Service<DalamudInterface>.Get().OpenPluginInstaller();
                notification.DismissNow();
            }
        };
        
        // Update the notification to show the final state
        var pluginUpdateStatusEnumerable = pluginStates as PluginUpdateStatus[] ?? pluginStates.ToArray();
        if (pluginUpdateStatusEnumerable.All(x => x.Status == PluginUpdateStatus.StatusKind.Success))
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
            
            var failedPlugins = pluginUpdateStatusEnumerable
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
            Content = Locs.NotificationContentUpdatesAvailable(updatablePlugins.Count),
            MinimizedText = Locs.NotificationContentUpdatesAvailableMinimized(updatablePlugins.Count),
            Type = NotificationType.Info,
            InitialDuration = TimeSpan.MaxValue,
            ShowIndeterminateIfNoExpiry = false,
            Icon = INotificationIcon.From(FontAwesomeIcon.Download),
        });

        notification.DrawActions += _ =>
        {
            ImGuiHelpers.ScaledDummy(2);

            if (DalamudComponents.PrimaryButton(Locs.NotificationButtonUpdate))
            {
                this.KickOffAutoUpdates(updatablePlugins);
                notification.DismissNow();
            }

            ImGui.SameLine();
            if (DalamudComponents.SecondaryButton(Locs.NotificationButtonOpenPluginInstaller))
            {
                Service<DalamudInterface>.Get().OpenPluginInstaller();
                notification.DismissNow();
            }
        };
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
        
        public static string NotificationContentUpdatesAvailable(int numUpdates)
            => string.Format(Loc.Localize("AutoUpdateUpdatesAvailableContent", "There are {0} plugins that can be updated."), numUpdates);
        
        public static string NotificationContentUpdatesAvailableMinimized(int numUpdates)
            => string.Format(Loc.Localize("AutoUpdateUpdatesAvailableContent", "{0} updates available."), numUpdates);
        
        public static string NotificationContentPreparingToUpdate(int numPlugins)
            => string.Format(Loc.Localize("AutoUpdatePreparingToUpdate", "Preparing to update {0} plugins..."), numPlugins);
        
        public static string NotificationContentUpdating(string name)
            => string.Format(Loc.Localize("AutoUpdateUpdating", "Updating {0}..."), name);
        
        public static string NotificationContentFailedPlugins(IEnumerable<string> failedPlugins)
            => string.Format(Loc.Localize("AutoUpdateFailedPlugins", "Failed plugins: {0}"), string.Join(", ", failedPlugins));
    }
}

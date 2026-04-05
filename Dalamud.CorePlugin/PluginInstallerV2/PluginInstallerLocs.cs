using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using CheapLoc;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;

namespace Dalamud.CorePlugin.PluginInstallerV2;

[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Disregard here")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Locs")]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Locs still.")]
internal static class PluginInstallerLocs
{
    #region Window Title

    public static string WindowTitle
        => Loc.Localize("InstallerHeader", "Plugin Installer");

    public static string WindowTitleMod_Testing
        => Loc.Localize("InstallerHeaderTesting", " (TESTING)");

    #endregion

    #region Header

    public static string Header_Hint
        => Loc.Localize("InstallerHint", "This window allows you to install and remove Dalamud plugins.\nThey are made by the community.");

    public static string Header_SearchPlaceholder
        => Loc.Localize("InstallerSearch", "Search . . .");

    public static string Header_Downloads
        => Loc.Localize("InstallerDownloads", "downloads");

    #endregion

    #region SortBy

    public static string SortBy_SearchScore
        => Loc.Localize("InstallerSearchScore", "Search score");

    public static string SortBy_Alphabetical
        => Loc.Localize("InstallerAlphabetical", "Alphabetical");

    public static string SortBy_DownloadCounts
        => Loc.Localize("InstallerDownloadCount", "Download Count");

    public static string SortBy_LastUpdate
        => Loc.Localize("InstallerLastUpdate", "Last Update");

    public static string SortBy_NewOrNot
        => Loc.Localize("InstallerNewOrNot", "New or not");

    public static string SortBy_NotInstalled
        => Loc.Localize("InstallerNotInstalled", "Not Installed");

    public static string SortBy_EnabledDisabled
        => Loc.Localize("InstallerEnabledDisabled", "Enabled/Disabled");

    public static string SortBy_ProfileOrNot
        => Loc.Localize("InstallerProfileOrNot", "In a collection");

    public static string SortBy_Label
        => Loc.Localize("InstallerSortBy", "Sort By");

    #endregion

    #region Tab body

    public static string TabBody_LoadingPlugins
        => Loc.Localize("InstallerLoading", "Loading plugins...");

    public static string TabBody_DownloadFailed
        => Loc.Localize("InstallerDownloadFailed", "Download failed.");

    public static string TabBody_SafeMode
        => Loc.Localize("InstallerSafeMode", "Dalamud is running in Plugin Safe Mode, restart to activate plugins.");

    public static string TabBody_NoPluginsTesting
        => Loc.Localize("InstallerNoPluginsTesting", "You aren't testing any plugins at the moment!\nYou can opt in to testing versions in the plugin context menu.");

    public static string TabBody_NoPluginsInstalled
        => Loc.Localize("InstallerNoPluginsInstalled", "You don't have any plugins installed yet!\nYou can install them from the \"{0}\" tab.").Format(PluginCategoryManager.Locs.Category_All);

    public static string TabBody_NoPluginsAvailable
        => Loc.Localize("InstallerNoPluginsAvailable", "No plugins are available at the moment.");

    public static string TabBody_NoPluginsUpdateable
        => Loc.Localize("InstallerNoPluginsUpdate", "No plugins have updates available at the moment.");

    public static string TabBody_NoPluginsDev
        => Loc.Localize("InstallerNoPluginsDev", "You don't have any dev plugins. Add them from the settings.");

    public static string TabBody_NoPluginsEnabled
        => Loc.Localize("InstallerNoPluginsEnabled", "You don't have any enabled plugins.");

    public static string TabBody_NoPluginsDisabled
        => Loc.Localize("InstallerNoPluginsDisabled", "You don't have any disabled plugins.");

    public static string TabBody_NoPluginsIncompatible
        => Loc.Localize("InstallerNoPluginsIncompatible", "You don't have any incompatible plugins.");

    #endregion

    #region Search text

    public static string TabBody_SearchNoMatching
        => Loc.Localize("InstallerNoMatching", "No plugins were found matching your search.");

    public static string TabBody_SearchNoCompatible
        => Loc.Localize("InstallerNoCompatible", "No compatible plugins were found :( Please restart your game and try again.");

    public static string TabBody_SearchNoInstalled
        => Loc.Localize("InstallerNoInstalled", "No plugins are currently installed. You can install them from the \"All Plugins\" tab.");

    public static string TabBody_NoMoreResultsFor(string query)
        => Loc.Localize("InstallerNoMoreResultsForQuery", "No more search results for \"{0}\".").Format(query);

    public static string TabBody_ChangelogNone
        => Loc.Localize("InstallerNoChangelog", "None of your installed plugins have a changelog.");

    public static string TabBody_ChangelogError
        => Loc.Localize("InstallerChangelogError", "Could not download changelogs.");

    #endregion

    #region Plugin title text

    public static string PluginTitleMod_Installed
        => Loc.Localize("InstallerInstalled", " (installed)");

    public static string PluginTitleMod_Disabled
        => Loc.Localize("InstallerDisabled", " (disabled)");

    public static string PluginTitleMod_NoService
        => Loc.Localize("InstallerNoService", " (decommissioned)");

    public static string PluginTitleMod_Unloaded
        => Loc.Localize("InstallerUnloaded", " (unloaded)");

    public static string PluginTitleMod_HasUpdate
        => Loc.Localize("InstallerHasUpdate", " (has update)");

    public static string PluginTitleMod_Updated
        => Loc.Localize("InstallerUpdated", " (updated)");

    public static string PluginTitleMod_TestingVersion
        => Loc.Localize("InstallerTestingVersion", " (testing version)");

    public static string PluginTitleMod_TestingExclusive
        => Loc.Localize("InstallerTestingExclusive", " (testing exclusive)");

    public static string PluginTitleMod_TestingAvailable
        => Loc.Localize("InstallerTestingAvailable", " (has testing version)");

    public static string PluginTitleMod_Incompatible
        => Loc.Localize("InstallerTitleModIncompatible", " (incompatible)");

    public static string PluginTitleMod_DevPlugin
        => Loc.Localize("InstallerDevPlugin", "Dev Plugin");

    public static string PluginTitleMod_UpdateFailed
        => Loc.Localize("InstallerUpdateFailed", " (update failed)");

    public static string PluginTitleMod_LoadError
        => Loc.Localize("InstallerLoadError", " (load error)");

    public static string PluginTitleMod_UnloadError
        => Loc.Localize("InstallerUnloadError", " (unload error)");

    public static string PluginTitleMod_OutdatedError
        => Loc.Localize("InstallerOutdatedError", " (outdated)");

    public static string PluginTitleMod_BannedError
        => Loc.Localize("InstallerBannedError", " (automatically disabled)");

    public static string PluginTitleMod_OrphanedError
        => Loc.Localize("InstallerOrphanedError", " (unknown repository)");

    public static string PluginTitleMod_ScheduledForDeletion
        => Loc.Localize("InstallerScheduledForDeletion", " (scheduled for deletion)");

    public static string PluginTitleMod_New
        => Loc.Localize("InstallerNewPlugin ", " New!");

    #endregion

    #region Plugin context menu

    public static string PluginContext_TestingOptIn
        => Loc.Localize("InstallerTestingOptIn", "Receive plugin testing versions");

    public static string PluginContext_InstallTestingVersion
        => Loc.Localize("InstallerInstallTestingVersion", "Install testing version");

    public static string PluginContext_MarkAllSeen
        => Loc.Localize("InstallerMarkAllSeen", "Mark all as seen");

    public static string PluginContext_HidePlugin
        => Loc.Localize("InstallerHidePlugin", "Hide from installer");

    public static string PluginContext_UnhidePlugin
        => Loc.Localize("InstallerUnhidePlugin", "Unhide from installer");

    public static string PluginContext_DeletePluginConfig
        => Loc.Localize("InstallerDeletePluginConfig", "Reset plugin data");

    public static string PluginContext_DeletePluginConfigReload
        => Loc.Localize("InstallerDeletePluginConfigReload", "Reset plugin data and reload");

    #endregion

    #region Plugin body

    public static string PluginBody_AuthorWithoutDownloadCount(string author)
        => Loc.Localize("InstallerAuthorWithoutDownloadCount", " by {0}").Format(author);

    public static string PluginBody_AuthorWithDownloadCount(string author, long count)
        => Loc.Localize("InstallerAuthorWithDownloadCount", " by {0} ({1} downloads)").Format(author, count.ToString("N0"));

    public static string PluginBody_AuthorWithDownloadCountUnavailable(string author)
        => Loc.Localize("InstallerAuthorWithDownloadCountUnavailable", " by {0}").Format(author);

    public static string PluginBody_CurrentChangeLog(Version version)
        => Loc.Localize("InstallerCurrentChangeLog", "Changelog (v{0})").Format(version);

    public static string PluginBody_UpdateChangeLog(Version version)
        => Loc.Localize("InstallerUpdateChangeLog", "Available update changelog (v{0})").Format(version);

    public static string PluginBody_DevPluginPath(string path)
        => Loc.Localize("InstallerDevPluginPath", "From {0}").Format(path);

    public static string PluginBody_Plugin3rdPartyRepo(string url)
        => Loc.Localize("InstallerPlugin3rdPartyRepo", "From custom plugin repository {0}").Format(url);

    public static string PluginBody_Outdated
        => Loc.Localize("InstallerOutdatedPluginBody ", "This plugin is outdated and incompatible.");

    public static string PluginBody_Incompatible
        => Loc.Localize("InstallerIncompatiblePluginBody ", "This plugin is incompatible with your version of Dalamud. Please attempt to restart your game.");

    public static string PluginBody_Outdated_WaitForUpdate
        => Loc.Localize("InstallerOutdatedWaitForUpdate", "Please wait for it to be updated by its author.");

    public static string PluginBody_Outdated_CanNowUpdate
        => Loc.Localize("InstallerOutdatedCanNowUpdate", "An update is available for installation.");

    public static string PluginBody_Orphaned
        => Loc.Localize("InstallerOrphanedPluginBody ", "This plugin's source repository is no longer available. You may need to reinstall it from its repository, or re-add the repository.");

    public static string PluginBody_NoServiceOfficial
        => Loc.Localize("InstallerNoServiceOfficialPluginBody", "This plugin is no longer being maintained. It will still work, but there will be no further updates and you can't reinstall it.");

    public static string PluginBody_NoServiceThird
        => Loc.Localize("InstallerNoServiceThirdPluginBody", "This plugin is no longer being serviced by its source repo. You may have to look for an updated version in another repo.");

    public static string PluginBody_NoServiceThirdCrossUpdate
        => Loc.Localize("InstallerNoServiceThirdCrossUpdatePluginBody", "This plugin is no longer being serviced by its source repo. An update is available and will update it to a version from the official repository.");

    public static string PluginBody_LoadFailed
        => Loc.Localize("InstallerLoadFailedPluginBody ", "This plugin failed to load. Please contact the author for more information.");

    public static string PluginBody_Banned
        => Loc.Localize("InstallerBannedPluginBody ", "This plugin was automatically disabled due to incompatibilities and is not available.");

    public static string PluginBody_Policy
        => Loc.Localize("InstallerPolicyPluginBody ", "Plugin loads for this type of plugin were manually disabled.");

    public static string PluginBody_BannedReason(string message) =>
        Loc.Localize("InstallerBannedPluginBodyReason ", "This plugin was automatically disabled: {0}").Format(message);

    #endregion

    #region Plugin buttons

    public static string PluginButton_InstallVersion(string version)
        => Loc.Localize("InstallerInstall", "Install v{0}").Format(version);

    public static string PluginButton_Working
        => Loc.Localize("InstallerWorking", "Working");

    public static string PluginButton_Disable
        => Loc.Localize("InstallerDisable", "Disable");

    public static string PluginButton_Load
        => Loc.Localize("InstallerLoad", "Load");

    public static string PluginButton_Unload
        => Loc.Localize("InstallerUnload", "Unload");

    public static string PluginButton_SafeMode
        => Loc.Localize("InstallerSafeModeButton", "Can't change in safe mode");

    public static string PluginButton_OpenUi
        => Loc.Localize("InstallerOpenPluginUi", "Open");

    public static string PluginButton_OpenSettings
        => Loc.Localize("InstallerOpenPluginSettings", "Settings");

    #endregion

    #region Plugin button tooltips

    public static string PluginButtonToolTip_OpenUi
        => Loc.Localize("InstallerTooltipOpenUi", "Open this plugin's interface");

    public static string PluginButtonToolTip_OpenConfiguration
        => Loc.Localize("InstallerTooltipOpenConfig", "Open this plugin's settings");

    public static string PluginButtonToolTip_PickProfiles
        => Loc.Localize("InstallerPickProfiles", "Pick collections for this plugin");

    public static string PluginButtonToolTip_ProfilesNotSupported
        => Loc.Localize("InstallerProfilesNotSupported", "This plugin does not support collections");

    public static string PluginButtonToolTip_StartOnBoot
        => Loc.Localize("InstallerStartOnBoot", "Start on boot");

    public static string PluginButtonToolTip_AutomaticReloading
        => Loc.Localize("InstallerAutomaticReloading", "Automatic reloading");

    public static string PluginButtonToolTip_NotifyForErrors
        => Loc.Localize("InstallerNotifyForErrors", "Show Dalamud notifications when this plugin is creating errors");

    public static string PluginButtonToolTip_DeletePlugin
        => Loc.Localize("InstallerDeletePlugin ", "Delete plugin");

    public static string PluginButtonToolTip_DeletePluginRestricted
        => Loc.Localize("InstallerDeletePluginRestricted", "Cannot delete right now - please restart the game.");

    public static string PluginButtonToolTip_DeletePluginScheduled
        => Loc.Localize("InstallerDeletePluginScheduled", "Delete plugin on next restart");

    public static string PluginButtonToolTip_DeletePluginScheduledCancel
        => Loc.Localize("InstallerDeletePluginScheduledCancel", "Cancel scheduled deletion");

    public static string PluginButtonToolTip_DeletePluginLoaded
        => Loc.Localize("InstallerDeletePluginLoaded", "Disable this plugin before deleting it.");

    public static string PluginButtonToolTip_VisitPluginUrl
        => Loc.Localize("InstallerVisitPluginUrl", "Visit plugin URL");

    public static string PluginButtonToolTip_UpdateSingle(string version)
        => Loc.Localize("InstallerUpdateSingle", "Update to {0}").Format(version);

    public static string PluginButtonToolTip_LoadUnloadFailed
        => Loc.Localize("InstallerLoadUnloadFailedTooltip", "Plugin load/unload failed, please restart your game and try again.");

    public static string PluginButtonToolTip_NeedsToBeInDefault
        => Loc.Localize(
            "InstallerUnloadNeedsToBeInDefault",
            "This plugin is in one or more collections. If you want to enable or disable it, please do so by enabling or disabling the collections it is in.\n" +
            "If you want to manage it manually, remove it from all collections.");

    public static string PluginButtonToolTip_NeedsToBeInSingleProfile
        => Loc.Localize(
            "InstallerUnloadNeedsToBeInSingleProfile",
            "This plugin is in more than one collection. If you want to enable or disable it, please do so by enabling or disabling the collections it is in.\n" +
            "If you want to manage it here, make sure it is only in a single collection.");

    public static string PluginButtonToolTip_SafeMode
        => Loc.Localize("InstallerButtonSafeModeTooltip", "Cannot enable plugins in safe mode.");

    public static string PluginButtonToolTip_SingleProfileDisabled(string name)
        => Loc.Localize("InstallerSingleProfileDisabled", "The collection '{0}' which contains this plugin is disabled.\nPlease enable it in the collections manager to toggle the plugin individually.").Format(name);

    public static string PluginButtonToolTip_SingleProfileDoesNotWantActive(string name)
        => Loc.Localize(
            "InstallerSingleProfileDoesNotWantActive",
            "The collection '{0}' which contains this plugin is active, but is not set to activate on this character.\n" +
            "Please change the collection's settings or remove the plugin from that collection to toggle the plugin individually.").Format(name);

    #endregion

    #region Notifications

    public static string Notifications_PluginInstalledTitle
        => Loc.Localize("NotificationsPluginInstalledTitle", "Plugin installed!");

    public static string Notifications_PluginInstalled(string name)
        => Loc.Localize("NotificationsPluginInstalled", "'{0}' was successfully installed.").Format(name);

    public static string Notifications_PluginNotInstalledTitle
        => Loc.Localize("NotificationsPluginNotInstalledTitle", "Plugin not installed!");

    public static string Notifications_PluginNotInstalled(string name)
        => Loc.Localize("NotificationsPluginNotInstalled", "'{0}' failed to install.").Format(name);

    public static string Notifications_NoUpdatesFoundTitle
        => Loc.Localize("NotificationsNoUpdatesFoundTitle", "No updates found!");

    public static string Notifications_NoUpdatesFound
        => Loc.Localize("NotificationsNoUpdatesFound", "No updates were found.");

    public static string Notifications_UpdatesInstalledTitle
        => Loc.Localize("NotificationsUpdatesInstalledTitle", "Updates installed!");

    public static string Notifications_UpdatesInstalled(List<PluginUpdateStatus> updates)
        => Loc.Localize("NotificationsUpdatesInstalled", "Updates for {0} of your plugins were installed.\n\n{1}")
              .Format(updates.Count, string.Join(", ", updates.Select(x => x.InternalName)));

    public static string Notifications_PluginDisabledTitle
        => Loc.Localize("NotificationsPluginDisabledTitle", "Plugin disabled!");

    public static string Notifications_PluginDisabled(string name)
        => Loc.Localize("NotificationsPluginDisabled", "'{0}' was disabled.").Format(name);

    public static string Notifications_PluginEnabledTitle
        => Loc.Localize("NotificationsPluginEnabledTitle", "Plugin enabled!");

    public static string Notifications_PluginEnabled(string name)
        => Loc.Localize("NotificationsPluginEnabled", "'{0}' was enabled.").Format(name);

    #endregion

    #region Footer

    public static string FooterButton_UpdatePlugins
        => Loc.Localize("InstallerUpdatePlugins", "Update plugins");

    public static string FooterButton_UpdateSafeMode
        => Loc.Localize("InstallerUpdateSafeMode", "Can't update in safe mode");

    public static string FooterButton_InProgress
        => Loc.Localize("InstallerInProgress", "Install in progress...");

    public static string FooterButton_NoUpdates
        => Loc.Localize("InstallerNoUpdates", "No updates found!");

    public static string FooterButton_UpdateComplete(int count)
        => Loc.Localize("InstallerUpdateComplete", "{0} plugins updated!").Format(count);

    public static string FooterButton_Settings
        => Loc.Localize("InstallerSettings", "Settings");

    public static string FooterButton_ScanDevPlugins
        => Loc.Localize("InstallerScanDevPlugins", "Scan Dev Plugins");

    public static string FooterButton_Close
        => Loc.Localize("InstallerClose", "Close");

    #endregion

    #region Update modal

    public static string UpdateModal_Title
        => Loc.Localize("UpdateQuestionModal", "Update Available");

    public static string UpdateModal_UpdateAvailable(string name)
        => Loc.Localize("UpdateModalUpdateAvailable", "An update for \"{0}\" is available.\nDo you want to update it before enabling?\nUpdates will fix bugs and incompatibilities, and may add new features.").Format(name);

    public static string UpdateModal_Yes
        => Loc.Localize("UpdateModalYes", "Update plugin");

    public static string UpdateModal_No
        => Loc.Localize("UpdateModalNo", "Just enable");

    #endregion

    #region Error modal

    public static string ErrorModal_Title
        => Loc.Localize("InstallerError", "Installer Error");

    public static string ErrorModal_InstallContactAuthor
        => Loc.Localize("InstallerContactAuthor", "Please restart your game and try again. If this error occurs again, please contact the plugin author.");

    public static string ErrorModal_InstallFail(string name)
        => Loc.Localize("InstallerInstallFail", "Failed to install plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

    public static string ErrorModal_SingleUpdateFail(string name, string why)
        => Loc.Localize("InstallerSingleUpdateFail", "Failed to update plugin {0} ({1}).\n{2}").Format(name, why, ErrorModal_InstallContactAuthor);

    public static string ErrorModal_DeleteConfigFail(string name)
        => Loc.Localize("InstallerDeleteConfigFail", "Failed to reset the plugin {0}.\n\nThe plugin may not support this action. You can try deleting the configuration manually while the game is shut down - please see the FAQ.").Format(name);

    public static string ErrorModal_EnableFail(string name)
        => Loc.Localize("InstallerEnableFail", "Failed to enable plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

    public static string ErrorModal_DisableFail(string name)
        => Loc.Localize("InstallerDisableFail", "Failed to disable plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

    public static string ErrorModal_UnloadFail(string name)
        => Loc.Localize("InstallerUnloadFail", "Failed to unload plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

    public static string ErrorModal_LoadFail(string name)
        => Loc.Localize("InstallerLoadFail", "Failed to load plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

    public static string ErrorModal_DeleteFail(string name)
        => Loc.Localize("InstallerDeleteFail", "Failed to delete plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

    public static string ErrorModal_UpdaterFatal
        => Loc.Localize("InstallerUpdaterFatal", "Failed to update plugins.\nPlease restart your game and try again. If this error occurs again, please complain.");

    public static string ErrorModal_ProfileApplyFail
        => Loc.Localize("InstallerProfileApplyFail", "Failed to process collections.\nPlease restart your game and try again. If this error occurs again, please complain.");

    public static string ErrorModal_UpdaterFail(int failCount)
        => Loc.Localize("InstallerUpdaterFail", "Failed to update {0} plugins.\nPlease restart your game and try again. If this error occurs again, please complain.").Format(failCount);

    public static string ErrorModal_UpdaterFailPartial(int successCount, int failCount)
        => Loc.Localize("InstallerUpdaterFailPartial", "Updated {0} plugins, failed to update {1}.\nPlease restart your game and try again. If this error occurs again, please complain.").Format(successCount, failCount);

    public static string ErrorModal_HintBlame(string plugins)
        => Loc.Localize("InstallerErrorPluginInfo", "\n\nThe following plugins caused these issues:\n\n{0}\nYou may try removing these plugins manually and reinstalling them.").Format(plugins);

    // public static string ErrorModal_Hint => Loc.Localize("InstallerErrorHint", "The plugin installer ran into an issue or the plugin is incompatible.\nPlease restart the game and report this error on our discord.");

    #endregion

    #region Feedback Modal

    public static string FeedbackModal_Title
        => Loc.Localize("InstallerFeedback", "Send Feedback");

    public static string FeedbackModal_Text(string pluginName)
        => Loc.Localize("InstallerFeedbackInfo", "You can send feedback to the developer of \"{0}\" here.").Format(pluginName);

    public static string FeedbackModal_HasUpdate
        => Loc.Localize("InstallerFeedbackHasUpdate", "A new version of this plugin is available, please update before reporting bugs.");

    public static string FeedbackModal_ContactAnonymous
        => Loc.Localize("InstallerFeedbackContactAnonymous", "Submit feedback anonymously");

    public static string FeedbackModal_ContactAnonymousWarning
        => Loc.Localize("InstallerFeedbackContactAnonymousWarning", "No response will be forthcoming.\nUntick \"{0}\" and provide contact information if you need help.").Format(FeedbackModal_ContactAnonymous);

    public static string FeedbackModal_ContactInformation
        => Loc.Localize("InstallerFeedbackContactInfo", "Contact information");

    public static string FeedbackModal_ContactInformationHelp
        => Loc.Localize(
            "InstallerFeedbackContactInfoHelp",
            "Discord usernames and e-mail addresses are accepted.\n" +
            "If you submit a Discord username, please join our discord server so that we can reach out to you easier.");

    public static string FeedbackModal_ContactInformationWarning
        => Loc.Localize("InstallerFeedbackContactInfoWarning", "Do not submit in-game character names.");

    public static string FeedbackModal_ContactInformationRequired
        => Loc.Localize("InstallerFeedbackContactInfoRequired", "Contact information has not been provided. We require contact information to respond to questions, or to request additional information to troubleshoot problems.");

    public static string FeedbackModal_ContactInformationDiscordButton
        => Loc.Localize("ContactInformationDiscordButton", "Join XIVLauncher & Dalamud Discord");

    public static string FeedbackModal_ContactInformationDiscordUrl
        => Loc.Localize("ContactInformationDiscordUrl", "https://goat.place/");

    public static string FeedbackModal_IncludeLastError
        => Loc.Localize("InstallerFeedbackIncludeLastError", "Include last error message");

    public static string FeedbackModal_IncludeLastErrorHint
        => Loc.Localize("InstallerFeedbackIncludeLastErrorHint", "This option can give the plugin developer useful feedback on what exactly went wrong.");

    public static string FeedbackModal_Hint
        => Loc.Localize(
            "InstallerFeedbackHint",
            "All plugin developers will be able to see your feedback.\n" +
            "Please never include any personal or revealing information.\n" +
            "If you chose to include the last error message, information like your Windows username may be included.\n\n" +
            "The collected feedback is not stored on our end and immediately relayed to Discord.");

    public static string FeedbackModal_NotificationSuccess
        => Loc.Localize("InstallerFeedbackNotificationSuccess", "Your feedback was sent successfully!");

    public static string FeedbackModal_NotificationError
        => Loc.Localize("InstallerFeedbackNotificationError", "Your feedback could not be sent.");

    #endregion

    #region Testing Warning Modal

    public static string TestingWarningModal_Title
        => Loc.Localize("InstallerTestingWarning", "Warning###InstallerTestingWarning");

    public static string TestingWarningModal_DowngradeBody
        => Loc.Localize(
            "InstallerTestingWarningDowngradeBody",
            "Take care! If you opt out of testing for a plugin, you will remain on the testing version until it is deleted and reinstalled, or the non-testing version of the plugin is updated.\n" +
            "Keep in mind that you may lose the settings for this plugin if you downgrade manually.");

    #endregion

    #region Delete Plugin Config Warning Modal

    public static string DeletePluginConfigWarningModal_Title
        => Loc.Localize("InstallerDeletePluginConfigWarning", "Warning###InstallerDeletePluginConfigWarning");

    public static string DeletePluginConfigWarningModal_ExplainTesting()
        => Loc.Localize("InstallerDeletePluginConfigWarningExplainTesting", "Do not select this option if you are only trying to disable testing!");

    public static string DeletePluginConfigWarningModal_Body(string pluginName)
        => Loc.Localize("InstallerDeletePluginConfigWarningBody", "Are you sure you want to delete all data and configuration for {0}?\nYou will lose all of your settings for this plugin.").Format(pluginName);

    public static string DeletePluginConfirmWarningModal_Yes
        => Loc.Localize("InstallerDeletePluginConfigWarningYes", "Yes");

    public static string DeletePluginConfirmWarningModal_No
        => Loc.Localize("InstallerDeletePluginConfigWarningNo", "No");

    #endregion

    #region Plugin Update chatbox

    public static string PluginUpdateHeader_Chatbox
        => Loc.Localize("DalamudPluginUpdates", "Updates:");

    #endregion

    #region Error modal buttons

    public static string ErrorModalButton_Ok
        => Loc.Localize("OK", "OK");

    #endregion

    #region Other

    public static string SafeModeDisclaimer
        => Loc.Localize("SafeModeDisclaimer", "You enabled safe mode, no plugins will be loaded.\nYou may delete plugins from the \"Installed plugins\" tab.\nSimply restart your game to disable safe mode.");

    #endregion

    #region Profiles

    public static string Profiles_CouldNotAdd
        => Loc.Localize("InstallerProfilesCouldNotAdd", "Couldn't add plugin to this collection.");

    public static string Profiles_CouldNotRemove
        => Loc.Localize("InstallerProfilesCouldNotRemove", "Couldn't remove plugin from this collection.");

    public static string Profiles_None
        => Loc.Localize("InstallerProfilesNone", "No collections! Go add some in \"Plugin Collections\"!");

    public static string Profiles_RemoveFromAll
        => Loc.Localize("InstallerProfilesRemoveFromAll", "Remove from all collections");

    #endregion

    #region VerifiedCheckmark

    public static string VerifiedCheckmark_CustomRepo
        => Loc.Localize("VerifiedCheckmark_CustomRepo", "Custom Repo");

    public static string VerifiedCheckmark_DalamudApproved
        => Loc.Localize("VerifiedCheckmark_DalamudApproved", "Main Repo");

    public static string VerifiedCheckmark_VerifiedTooltip
        => Loc.Localize(
            "VerifiedCheckmarkVerifiedTooltip",
            "This plugin has been reviewed by the Dalamud team.\n" +
            "It follows our technical and safety criteria, and adheres to our guidelines.");

    public static string VerifiedCheckmark_UnverifiedTooltip
        => Loc.Localize(
            "VerifiedCheckmarkUnverifiedTooltip",
            "This plugin has not been reviewed by the Dalamud team.\n" +
            "We cannot take any responsibility for custom plugins and repositories.\n" +
            "Please make absolutely sure that you only install plugins from developers you trust.\n\n" +
            "You will not receive support for plugins installed from custom repositories on the XIVLauncher & Dalamud server.");

    #endregion
}

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Game.Command;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Plugin;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Exceptions;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Internal.Types.Manifest;
using Dalamud.Support;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller;

/// <summary>
/// Class responsible for drawing the plugin installer.
/// </summary>
internal class PluginInstallerWindow : Window, IDisposable
{
    private static readonly ModuleLog Log = new("PLUGINW");

    private readonly Vector4 changelogBgColor = new(0.114f, 0.584f, 0.192f, 0.678f);
    private readonly Vector4 changelogTextColor = new(0.812f, 1.000f, 0.816f, 1.000f);

    private readonly PluginImageCache imageCache;
    private readonly PluginCategoryManager categoryManager = new();

    private readonly List<int> openPluginCollapsibles = new();

    private readonly DateTime timeLoaded;

    private readonly object listLock = new();

    private readonly ProfileManagerWidget profileManagerWidget;

    private readonly Stopwatch tooltipFadeInStopwatch = new();
    private readonly InOutCubic tooltipFadeEasing = new(TimeSpan.FromSeconds(0.2f))
    {
        Point1 = Vector2.Zero,
        Point2 = Vector2.One,
    };

    private DalamudChangelogManager? dalamudChangelogManager;
    private Task? dalamudChangelogRefreshTask;
    private CancellationTokenSource? dalamudChangelogRefreshTaskCts;

    #region Image Tester State

    private string[] testerImagePaths = new string[5];
    private string testerIconPath = string.Empty;

    private IDalamudTextureWrap?[]? testerImages;
    private IDalamudTextureWrap? testerIcon;

    private bool testerError = false;
    private bool testerUpdateAvailable = false;

    #endregion

    private bool errorModalDrawing = true;
    private bool errorModalOnNextFrame = false;
    private string errorModalMessage = string.Empty;
    private TaskCompletionSource? errorModalTaskCompletionSource;

    private bool updateModalDrawing = true;
    private bool updateModalOnNextFrame = false;
    private LocalPlugin? updateModalPlugin = null;
    private TaskCompletionSource<bool>? updateModalTaskCompletionSource;

    private bool testingWarningModalDrawing = true;
    private bool testingWarningModalOnNextFrame = false;

    private bool deletePluginConfigWarningModalDrawing = true;
    private bool deletePluginConfigWarningModalOnNextFrame = false;
    private string deletePluginConfigWarningModalPluginName = string.Empty;
    private TaskCompletionSource<bool>? deletePluginConfigWarningModalTaskCompletionSource;

    private bool feedbackModalDrawing = true;
    private bool feedbackModalOnNextFrame = false;
    private bool feedbackModalOnNextFrameDontClear = false;
    private string feedbackModalBody = string.Empty;
    private string feedbackModalContact = string.Empty;
    private bool feedbackModalIncludeException = false;
    private IPluginManifest? feedbackPlugin = null;
    private bool feedbackIsTesting = false;

    private int updatePluginCount = 0;
    private List<PluginUpdateStatus>? updatedPlugins;

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Makes sense like this")]
    private List<RemotePluginManifest> pluginListAvailable = new();
    private List<LocalPlugin> pluginListInstalled = new();
    private List<AvailablePluginUpdate> pluginListUpdatable = new();
    private bool hasDevPlugins = false;

    private string searchText = string.Empty;
    private bool isSearchTextPrefilled = false;

    private PluginSortKind sortKind = PluginSortKind.Alphabetical;
    private string filterText = Locs.SortBy_Alphabetical;

    private OperationStatus installStatus = OperationStatus.Idle;
    private OperationStatus updateStatus = OperationStatus.Idle;

    private OperationStatus enableDisableStatus = OperationStatus.Idle;
    private Guid enableDisableWorkingPluginId = Guid.Empty;

    private LoadingIndicatorKind loadingIndicatorKind = LoadingIndicatorKind.Unknown;

    private string verifiedCheckmarkHoveredPlugin = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstallerWindow"/> class.
    /// </summary>
    /// <param name="imageCache">An instance of <see cref="PluginImageCache"/> class.</param>
    /// <param name="configuration">An instance of <see cref="DalamudConfiguration"/>.</param>
    public PluginInstallerWindow(PluginImageCache imageCache, DalamudConfiguration configuration)
        : base(
            Locs.WindowTitle + (configuration.DoPluginTest ? Locs.WindowTitleMod_Testing : string.Empty) + "###XlPluginInstaller",
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar)
    {
        this.IsOpen = true;
        this.imageCache = imageCache;

        this.Size = new Vector2(830, 570);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = this.Size.Value,
        };

        Service<PluginManager>.GetAsync().ContinueWith(pluginManagerTask =>
        {
            var pluginManager = pluginManagerTask.Result;

            // For debugging
            if (pluginManager.PluginsReady)
                this.OnInstalledPluginsChanged();

            this.dalamudChangelogManager = new(pluginManager);

            pluginManager.OnAvailablePluginsChanged += this.OnAvailablePluginsChanged;
            pluginManager.OnInstalledPluginsChanged += this.OnInstalledPluginsChanged;

            for (var i = 0; i < this.testerImagePaths.Length; i++)
            {
                this.testerImagePaths[i] = string.Empty;
            }
        });

        this.timeLoaded = DateTime.Now;

        this.profileManagerWidget = new(this);
    }

    /// <summary>
    /// Enum describing pages the plugin installer can be opened to.
    /// </summary>
    public enum PluginInstallerOpenKind
    {
        /// <summary>
        /// Open to the "All Plugins" page.
        /// </summary>
        AllPlugins,

        /// <summary>
        /// Open to the "Installed Plugins" page.
        /// </summary>
        InstalledPlugins,
        
        /// <summary>
        /// Open to the "Changelogs" page.
        /// </summary>
        Changelogs,
    }
    
    private enum OperationStatus
    {
        Idle,
        InProgress,
        Complete,
    }

    private enum LoadingIndicatorKind
    {
        Unknown,
        EnablingSingle,
        DisablingSingle,
        UpdatingSingle,
        UpdatingAll,
        Installing,
        Manager,
        ProfilesLoading,
    }

    private enum PluginSortKind
    {
        Alphabetical,
        DownloadCount,
        LastUpdate,
        NewOrNot,
        NotInstalled,
        EnabledDisabled,
        ProfileOrNot,
    }

    private bool AnyOperationInProgress => this.installStatus == OperationStatus.InProgress ||
                                           this.updateStatus == OperationStatus.InProgress ||
                                           this.enableDisableStatus == OperationStatus.InProgress;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.dalamudChangelogRefreshTaskCts?.Cancel();

        var pluginManager = Service<PluginManager>.GetNullable();
        if (pluginManager != null)
        {
            pluginManager.OnAvailablePluginsChanged -= this.OnAvailablePluginsChanged;
            pluginManager.OnInstalledPluginsChanged -= this.OnInstalledPluginsChanged;
        }
    }

    /// <summary>
    /// Open to the installer to the page specified by <paramref name="kind"/>.
    /// </summary>
    /// <param name="kind">The page of the installer to open.</param>
    public void OpenTo(PluginInstallerOpenKind kind)
    {
        this.IsOpen = true;
        this.SetOpenPage(kind);
    }

    /// <summary>
    /// Toggle to the installer to the page specified by <paramref name="kind"/>.
    /// </summary>
    /// <param name="kind">The page of the installer to open.</param>
    public void ToggleTo(PluginInstallerOpenKind kind)
    {
        this.Toggle();

        if (this.IsOpen)
            this.SetOpenPage(kind);
    }

    /// <inheritdoc/>
    public override void OnOpen()
    {
        var pluginManager = Service<PluginManager>.Get();

        _ = pluginManager.ReloadPluginMastersAsync();

        if (!this.isSearchTextPrefilled) this.searchText = string.Empty;
        this.sortKind = PluginSortKind.Alphabetical;
        this.filterText = Locs.SortBy_Alphabetical;

        if (this.updateStatus == OperationStatus.Complete || this.updateStatus == OperationStatus.Idle)
        {
            this.updateStatus = OperationStatus.Idle;
            this.updatePluginCount = 0;
            this.updatedPlugins = null;
        }

        this.profileManagerWidget.Reset();
    }

    /// <inheritdoc/>
    public override void OnClose()
    {
        Service<DalamudConfiguration>.Get().QueueSave();

        if (this.isSearchTextPrefilled)
        {
            this.isSearchTextPrefilled = false;
            this.searchText = string.Empty;
        }
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        lock (this.listLock)
        {
            this.DrawHeader();
            this.DrawPluginCategories();
            this.DrawFooter();
            this.DrawErrorModal();
            this.DrawUpdateModal();
            this.DrawTestingWarningModal();
            this.DrawDeletePluginConfigWarningModal();
            this.DrawFeedbackModal();
            this.DrawProgressOverlay();
        }
    }

    /// <summary>
    /// Clear the icon and image caches, forcing a fresh download.
    /// </summary>
    public void ClearIconCache()
    {
        this.imageCache.ClearIconCache();
    }

    /// <summary>
    /// Sets the current search text and marks it as prefilled.
    /// </summary>
    /// <param name="text">The search term.</param>
    public void SetSearchText(string text)
    {
        this.isSearchTextPrefilled = true;
        this.searchText = text;
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

        this.installStatus = OperationStatus.InProgress;
        this.loadingIndicatorKind = LoadingIndicatorKind.Installing;

        Task.Run(() => pluginManager.InstallPluginAsync(manifest, useTesting || manifest.IsTestingExclusive, PluginLoadReason.Installer))
            .ContinueWith(task =>
            {
                // There is no need to set as Complete for an individual plugin installation
                this.installStatus = OperationStatus.Idle;
                if (this.DisplayErrorContinuation(task, Locs.ErrorModal_InstallFail(manifest.Name)))
                {
                    // Fine as long as we aren't in an error state
                    if (task.Result.State is PluginState.Loaded or PluginState.Unloaded)
                    {
                        notifications.AddNotification(Locs.Notifications_PluginInstalled(manifest.Name), Locs.Notifications_PluginInstalledTitle, NotificationType.Success);
                    }
                    else
                    {
                        notifications.AddNotification(Locs.Notifications_PluginNotInstalled(manifest.Name), Locs.Notifications_PluginNotInstalledTitle, NotificationType.Error);
                        this.ShowErrorModal(Locs.ErrorModal_InstallFail(manifest.Name));
                    }
                }
            });
    }

    /// <summary>
    /// A continuation task that displays any errors received into the error modal.
    /// </summary>
    /// <param name="task">The previous task.</param>
    /// <param name="state">An error message to be displayed.</param>
    /// <returns>A value indicating whether to continue with the next task.</returns>
    public bool DisplayErrorContinuation(Task task, object state)
    {
        if (task.IsFaulted)
        {
            var errorModalMessage = state as string;

            foreach (var ex in task.Exception.InnerExceptions)
            {
                if (ex is PluginException)
                {
                    Log.Error(ex, "Plugin installer threw an error");
#if DEBUG
                    if (!string.IsNullOrEmpty(ex.Message))
                        errorModalMessage += $"\n\n{ex.Message}";
#endif
                }
                else
                {
                    Log.Error(ex, "Plugin installer threw an unexpected error");
#if DEBUG
                    if (!string.IsNullOrEmpty(ex.Message))
                        errorModalMessage += $"\n\n{ex.Message}";
#endif
                }
            }

            this.ShowErrorModal(errorModalMessage);

            return false;
        }

        return true;
    }

    private void SetOpenPage(PluginInstallerOpenKind kind)
    {
        switch (kind)
        {
            case PluginInstallerOpenKind.AllPlugins:
                // Plugins group
                this.categoryManager.CurrentGroupIdx = 2;
                // All category
                this.categoryManager.CurrentCategoryIdx = 0;
                break;
            case PluginInstallerOpenKind.InstalledPlugins:
                // Installed group
                this.categoryManager.CurrentGroupIdx = 1;
                // All category
                this.categoryManager.CurrentCategoryIdx = 0;
                break;
            case PluginInstallerOpenKind.Changelogs:
                // Changelog group
                this.categoryManager.CurrentGroupIdx = 3;
                // Plugins category
                this.categoryManager.CurrentCategoryIdx = 2;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private void DrawProgressOverlay()
    {
        var pluginManager = Service<PluginManager>.Get();
        var profileManager = Service<ProfileManager>.Get();

        var isWaitingManager = !pluginManager.PluginsReady ||
                               !pluginManager.ReposReady;
        var isWaitingProfiles = profileManager.IsBusy;

        var isLoading = this.AnyOperationInProgress ||
                        isWaitingManager || isWaitingProfiles;

        if (isWaitingManager)
            this.loadingIndicatorKind = LoadingIndicatorKind.Manager;
        else if (isWaitingProfiles)
            this.loadingIndicatorKind = LoadingIndicatorKind.ProfilesLoading;

        if (!isLoading)
            return;

        ImGui.SetCursorPos(Vector2.Zero);

        var windowSize = ImGui.GetWindowSize();
        var titleHeight = ImGui.GetFontSize() + (ImGui.GetStyle().FramePadding.Y * 2);

        using var loadingChild = ImRaii.Child("###installerLoadingFrame", new Vector2(-1, -1), false);
        if (loadingChild)
        {
            ImGui.GetWindowDrawList().PushClipRectFullScreen();
            ImGui.GetWindowDrawList().AddRectFilled(
                ImGui.GetWindowPos() + new Vector2(0, titleHeight),
                ImGui.GetWindowPos() + windowSize,
                0xCC000000,
                ImGui.GetStyle().WindowRounding,
                ImDrawFlags.RoundCornersBottom);
            ImGui.PopClipRect();

            ImGui.SetCursorPosY(windowSize.Y / 2);

            switch (this.loadingIndicatorKind)
            {
                case LoadingIndicatorKind.Unknown:
                    ImGuiHelpers.CenteredText("Doing something, not sure what!");
                    break;
                case LoadingIndicatorKind.EnablingSingle:
                    ImGuiHelpers.CenteredText("Enabling plugin...");
                    break;
                case LoadingIndicatorKind.DisablingSingle:
                    ImGuiHelpers.CenteredText("Disabling plugin...");
                    break;
                case LoadingIndicatorKind.UpdatingSingle:
                    ImGuiHelpers.CenteredText("Updating plugin...");
                    break;
                case LoadingIndicatorKind.UpdatingAll:
                    ImGuiHelpers.CenteredText("Updating plugins...");
                    break;
                case LoadingIndicatorKind.Installing:
                    ImGuiHelpers.CenteredText("Installing plugin...");
                    break;
                case LoadingIndicatorKind.Manager:
                    {
                        if (pluginManager.PluginsReady && !pluginManager.ReposReady)
                        {
                            ImGuiHelpers.CenteredText("Loading repositories...");
                        }
                        else if (!pluginManager.PluginsReady && pluginManager.ReposReady)
                        {
                            ImGuiHelpers.CenteredText("Loading installed plugins...");
                        }
                        else
                        {
                            ImGuiHelpers.CenteredText("Loading repositories and plugins...");
                        }

                        var currentProgress = 0;
                        var total = 0;

                        var pendingRepos = pluginManager.Repos.ToArray()
                                                        .Where(x => (x.State != PluginRepositoryState.Success &&
                                                                     x.State != PluginRepositoryState.Fail) &&
                                                                    x.IsEnabled)
                                                        .ToArray();
                        var allRepoCount =
                            pluginManager.Repos.Count(x => x.State != PluginRepositoryState.Fail && x.IsEnabled);

                        foreach (var repo in pendingRepos)
                        {
                            ImGuiHelpers.CenteredText($"{repo.PluginMasterUrl}: {repo.State}");
                        }

                        currentProgress += allRepoCount - pendingRepos.Length;
                        total += allRepoCount;

                        if (currentProgress != total)
                        {
                            ImGui.SetCursorPosX(windowSize.X / 3);
                            ImGui.ProgressBar(currentProgress / (float)total, new Vector2(windowSize.X / 3, 50));
                        }
                    }

                    break;
                case LoadingIndicatorKind.ProfilesLoading:
                    ImGuiHelpers.CenteredText("Collections are being applied...");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (DateTime.Now - this.timeLoaded > TimeSpan.FromSeconds(90) && !pluginManager.PluginsReady)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                ImGuiHelpers.CenteredText("One of your plugins may be blocking the installer.");
                ImGui.PopStyleColor();
            }
        }
    }

    private void DrawHeader()
    {
        var style = ImGui.GetStyle();
        var windowSize = ImGui.GetWindowContentRegionMax();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (5 * ImGuiHelpers.GlobalScale));

        var searchInputWidth = 180 * ImGuiHelpers.GlobalScale;
        var searchClearButtonWidth = 25 * ImGuiHelpers.GlobalScale;

        var sortByText = Locs.SortBy_Label;
        var sortByTextWidth = ImGui.CalcTextSize(sortByText).X;
        var sortSelectables = new (string Localization, PluginSortKind SortKind)[]
        {
            (Locs.SortBy_Alphabetical, PluginSortKind.Alphabetical),
            (Locs.SortBy_DownloadCounts, PluginSortKind.DownloadCount),
            (Locs.SortBy_LastUpdate, PluginSortKind.LastUpdate),
            (Locs.SortBy_NewOrNot, PluginSortKind.NewOrNot),
            (Locs.SortBy_NotInstalled, PluginSortKind.NotInstalled),
            (Locs.SortBy_EnabledDisabled, PluginSortKind.EnabledDisabled),
            (Locs.SortBy_ProfileOrNot, PluginSortKind.ProfileOrNot),
        };
        var longestSelectableWidth = sortSelectables.Select(t => ImGui.CalcTextSize(t.Localization).X).Max();
        var selectableWidth = longestSelectableWidth + (style.FramePadding.X * 2);  // This does not include the label
        var sortSelectWidth = selectableWidth + sortByTextWidth + style.ItemInnerSpacing.X;  // Item spacing between the selectable and the label

        var headerText = Locs.Header_Hint;
        var headerTextSize = ImGui.CalcTextSize(headerText);
        ImGui.Text(headerText);

        ImGui.SameLine();

        // Shift down a little to align with the middle of the header text
        var downShift = ImGui.GetCursorPosY() + (headerTextSize.Y / 4) - 2;
        ImGui.SetCursorPosY(downShift);

        ImGui.SetCursorPosX(windowSize.X - sortSelectWidth - (style.ItemSpacing.X * 2) - searchInputWidth - searchClearButtonWidth);

        var isProfileManager =
            this.categoryManager.CurrentGroupIdx == 1 && this.categoryManager.CurrentCategoryIdx == 2;
        
        // Disable search if profile editor
        using (ImRaii.Disabled(isProfileManager))
        {
            var searchTextChanged = false;
            ImGui.SetNextItemWidth(searchInputWidth);
            searchTextChanged |= ImGui.InputTextWithHint(
                "###XlPluginInstaller_Search",
                Locs.Header_SearchPlaceholder,
                ref this.searchText,
                100,
                ImGuiInputTextFlags.AutoSelectAll);

            ImGui.SameLine();
            ImGui.SetCursorPosY(downShift);

            ImGui.SetNextItemWidth(searchClearButtonWidth);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Times))
            {
                this.searchText = string.Empty;
                searchTextChanged = true;
            }

            if (searchTextChanged)
                this.UpdateCategoriesOnSearchChange();
        }

        // Disable sort if changelogs or profile editor
        using (ImRaii.Disabled(this.categoryManager.CurrentGroupIdx == 3 || isProfileManager))
        {
            ImGui.SameLine();
            ImGui.SetCursorPosY(downShift);
            ImGui.SetNextItemWidth(selectableWidth);
            if (ImGui.BeginCombo(sortByText, this.filterText, ImGuiComboFlags.NoArrowButton))
            {
                foreach (var selectable in sortSelectables)
                {
                    if (ImGui.Selectable(selectable.Localization))
                    {
                        this.sortKind = selectable.SortKind;
                        this.filterText = selectable.Localization;

                        lock (this.listLock)
                        {
                            this.ResortPlugins();
                            
                            // Positions of plugins within the list is likely to change
                            this.openPluginCollapsibles.Clear();
                        }
                    }
                }

                ImGui.EndCombo();
            }
        }
    }

    private void DrawFooter()
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var pluginManager = Service<PluginManager>.Get();

        var windowSize = ImGui.GetWindowContentRegionMax();
        var placeholderButtonSize = ImGuiHelpers.GetButtonSize("placeholder");

        ImGui.Separator();

        ImGui.SetCursorPosY(windowSize.Y - placeholderButtonSize.Y);

        this.DrawUpdatePluginsButton();

        ImGui.SameLine();
        if (ImGui.Button(Locs.FooterButton_Settings))
        {
            Service<DalamudInterface>.Get().OpenSettings();
        }

        // If any dev plugins are installed, allow a shortcut for the /xldev menu item
        if (this.hasDevPlugins)
        {
            ImGui.SameLine();
            if (ImGui.Button(Locs.FooterButton_ScanDevPlugins))
            {
                pluginManager.ScanDevPlugins();
            }
        }

        var closeText = Locs.FooterButton_Close;
        var closeButtonSize = ImGuiHelpers.GetButtonSize(closeText);

        ImGui.SameLine(windowSize.X - closeButtonSize.X - 20);
        if (ImGui.Button(closeText))
        {
            this.IsOpen = false;
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
            ImGuiComponents.DisabledButton(Locs.FooterButton_UpdateSafeMode);
        }
        else if (!ready || this.updateStatus == OperationStatus.InProgress || this.installStatus == OperationStatus.InProgress)
        {
            ImGuiComponents.DisabledButton(Locs.FooterButton_UpdatePlugins);
        }
        else if (this.updateStatus == OperationStatus.Complete)
        {
            ImGui.Button(this.updatePluginCount > 0
                             ? Locs.FooterButton_UpdateComplete(this.updatePluginCount)
                             : Locs.FooterButton_NoUpdates);
        }
        else
        {
            if (ImGui.Button(Locs.FooterButton_UpdatePlugins))
            {
                this.updateStatus = OperationStatus.InProgress;
                this.loadingIndicatorKind = LoadingIndicatorKind.UpdatingAll;

                Task.Run(() => pluginManager.UpdatePluginsAsync(true, false))
                    .ContinueWith(task =>
                    {
                        this.updateStatus = OperationStatus.Complete;

                        if (task.IsFaulted)
                        {
                            this.updatePluginCount = 0;
                            this.updatedPlugins = null;
                            this.DisplayErrorContinuation(task, Locs.ErrorModal_UpdaterFatal);
                        }
                        else
                        {
                            this.updatedPlugins = task.Result.Where(res => res.Status == PluginUpdateStatus.StatusKind.Success).ToList();
                            this.updatePluginCount = this.updatedPlugins.Count;

                            var errorPlugins = task.Result.Where(res => res.Status != PluginUpdateStatus.StatusKind.Success).ToList();
                            var errorPluginCount = errorPlugins.Count;

                            if (errorPluginCount > 0)
                            {
                                var errorMessage = this.updatePluginCount > 0
                                                       ? Locs.ErrorModal_UpdaterFailPartial(this.updatePluginCount, errorPluginCount)
                                                       : Locs.ErrorModal_UpdaterFail(errorPluginCount);
                                    
                                var hintInsert = errorPlugins
                                                 .Aggregate(string.Empty, (current, pluginUpdateStatus) => $"{current}* {pluginUpdateStatus.InternalName} ({PluginUpdateStatus.LocalizeUpdateStatusKind(pluginUpdateStatus.Status)})\n")
                                                 .TrimEnd();
                                errorMessage += Locs.ErrorModal_HintBlame(hintInsert);

                                this.DisplayErrorContinuation(task, errorMessage);
                            }

                            if (this.updatePluginCount > 0)
                            {
                                Service<PluginManager>.Get().PrintUpdatedPlugins(this.updatedPlugins, Locs.PluginUpdateHeader_Chatbox);
                                notifications.AddNotification(Locs.Notifications_UpdatesInstalled(this.updatePluginCount), Locs.Notifications_UpdatesInstalledTitle, NotificationType.Success);

                                var installedGroupIdx = this.categoryManager.GroupList.TakeWhile(
                                    x => x.GroupKind != PluginCategoryManager.GroupKind.Installed).Count();
                                this.categoryManager.CurrentGroupIdx = installedGroupIdx;
                            }
                            else if (this.updatePluginCount == 0)
                            {
                                notifications.AddNotification(Locs.Notifications_NoUpdatesFound, Locs.Notifications_NoUpdatesFoundTitle, NotificationType.Info);
                            }
                        }
                    });
            }
        }
    }

    private void DrawErrorModal()
    {
        var modalTitle = Locs.ErrorModal_Title;

        if (ImGui.BeginPopupModal(modalTitle, ref this.errorModalDrawing, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Text(this.errorModalMessage);
            ImGui.Spacing();

            var buttonWidth = 120f;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);

            if (ImGui.Button(Locs.ErrorModalButton_Ok, new Vector2(buttonWidth, 40)))
            {
                ImGui.CloseCurrentPopup();
                this.errorModalTaskCompletionSource?.SetResult();
            }

            ImGui.EndPopup();
        }

        if (this.errorModalOnNextFrame)
        {
            // NOTE(goat): ImGui cannot open a modal if no window is focused, at the moment.
            // If people click out of the installer into the game while a plugin is installing, we won't be able to show a modal if we don't grab focus.
            ImGui.SetWindowFocus(this.WindowName);

            ImGui.OpenPopup(modalTitle);
            this.errorModalOnNextFrame = false;
            this.errorModalDrawing = true;
        }
    }

    private void DrawUpdateModal()
    {
        var modalTitle = Locs.UpdateModal_Title;

        if (this.updateModalPlugin == null)
            return;

        if (ImGui.BeginPopupModal(modalTitle, ref this.updateModalDrawing, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Text(Locs.UpdateModal_UpdateAvailable(this.updateModalPlugin.Name));
            ImGui.Spacing();

            var buttonWidth = 120f;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ((buttonWidth * 2) - (ImGui.GetStyle().ItemSpacing.Y * 2))) / 2);

            if (ImGui.Button(Locs.UpdateModal_Yes, new Vector2(buttonWidth, 40)))
            {
                ImGui.CloseCurrentPopup();
                this.updateModalTaskCompletionSource?.SetResult(true);
            }

            ImGui.SameLine();

            if (ImGui.Button(Locs.UpdateModal_No, new Vector2(buttonWidth, 40)))
            {
                ImGui.CloseCurrentPopup();
                this.updateModalTaskCompletionSource?.SetResult(false);
            }

            ImGui.EndPopup();
        }

        if (this.updateModalOnNextFrame)
        {
            // NOTE(goat): ImGui cannot open a modal if no window is focused, at the moment.
            // If people click out of the installer into the game while a plugin is installing, we won't be able to show a modal if we don't grab focus.
            ImGui.SetWindowFocus(this.WindowName);

            ImGui.OpenPopup(modalTitle);
            this.updateModalOnNextFrame = false;
            this.updateModalDrawing = true;
        }
    }

    private void DrawTestingWarningModal()
    {
        var modalTitle = Locs.TestingWarningModal_Title;

        if (ImGui.BeginPopupModal(modalTitle, ref this.testingWarningModalDrawing, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Text(Locs.TestingWarningModal_DowngradeBody);

            ImGuiHelpers.ScaledDummy(10);

            var buttonWidth = 120f;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);

            if (ImGui.Button(Locs.ErrorModalButton_Ok, new Vector2(buttonWidth, 40)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        if (this.testingWarningModalOnNextFrame)
        {
            // NOTE(goat): ImGui cannot open a modal if no window is focused, at the moment.
            // If people click out of the installer into the game while a plugin is installing, we won't be able to show a modal if we don't grab focus.
            ImGui.SetWindowFocus(this.WindowName);

            ImGui.OpenPopup(modalTitle);
            this.testingWarningModalOnNextFrame = false;
            this.testingWarningModalDrawing = true;
        }
    }

    private Task<bool> ShowDeletePluginConfigWarningModal(string pluginName)
    {
        this.deletePluginConfigWarningModalOnNextFrame = true;
        this.deletePluginConfigWarningModalPluginName = pluginName;
        this.deletePluginConfigWarningModalTaskCompletionSource = new TaskCompletionSource<bool>();
        return this.deletePluginConfigWarningModalTaskCompletionSource.Task;
    }
    
    private void DrawDeletePluginConfigWarningModal()
    {
        var modalTitle = Locs.DeletePluginConfigWarningModal_Title;

        if (ImGui.BeginPopupModal(modalTitle, ref this.deletePluginConfigWarningModalDrawing, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Text(Locs.DeletePluginConfigWarningModal_Body(this.deletePluginConfigWarningModalPluginName));
            ImGui.Spacing();

            var buttonWidth = 120f;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ((buttonWidth * 2) - (ImGui.GetStyle().ItemSpacing.Y * 2))) / 2);

            if (ImGui.Button(Locs.DeletePluginConfirmWarningModal_Yes, new Vector2(buttonWidth, 40)))
            {
                ImGui.CloseCurrentPopup();
                this.deletePluginConfigWarningModalTaskCompletionSource?.SetResult(true);
            }

            ImGui.SameLine();

            if (ImGui.Button(Locs.DeletePluginConfirmWarningModal_No, new Vector2(buttonWidth, 40)))
            {
                ImGui.CloseCurrentPopup();
                this.deletePluginConfigWarningModalTaskCompletionSource?.SetResult(false);
            }
            
            ImGui.EndPopup();
        }

        if (this.deletePluginConfigWarningModalOnNextFrame)
        {
            // NOTE(goat): ImGui cannot open a modal if no window is focused, at the moment.
            // If people click out of the installer into the game while a plugin is installing, we won't be able to show a modal if we don't grab focus.
            ImGui.SetWindowFocus(this.WindowName);

            ImGui.OpenPopup(modalTitle);
            this.deletePluginConfigWarningModalOnNextFrame = false;
            this.deletePluginConfigWarningModalDrawing = true;
        }
    }

    private void DrawFeedbackModal()
    {
        var modalTitle = Locs.FeedbackModal_Title;

        if (ImGui.BeginPopupModal(modalTitle, ref this.feedbackModalDrawing, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.TextUnformatted(Locs.FeedbackModal_Text(this.feedbackPlugin.Name));

            if (this.feedbackPlugin?.FeedbackMessage != null)
            {
                ImGuiHelpers.SafeTextWrapped(this.feedbackPlugin.FeedbackMessage);
            }

            if (this.pluginListUpdatable.Any(
                    up => up.InstalledPlugin.Manifest.InternalName == this.feedbackPlugin?.InternalName))
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, Locs.FeedbackModal_HasUpdate);
            }

            ImGui.Spacing();

            ImGui.InputTextMultiline("###FeedbackContent", ref this.feedbackModalBody, 1000, new Vector2(400, 200));

            ImGui.Spacing();

            ImGui.InputText(Locs.FeedbackModal_ContactInformation, ref this.feedbackModalContact, 100);

            ImGui.SameLine();

            if (ImGui.Button(Locs.FeedbackModal_ContactInformationDiscordButton))
            {
                Process.Start(new ProcessStartInfo(Locs.FeedbackModal_ContactInformationDiscordUrl)
                {
                    UseShellExecute = true,
                });
            }

            ImGui.Text(Locs.FeedbackModal_ContactInformationHelp);

            ImGui.TextColored(ImGuiColors.DalamudRed, Locs.FeedbackModal_ContactInformationWarning);

            ImGui.Spacing();

            ImGui.Checkbox(Locs.FeedbackModal_IncludeLastError, ref this.feedbackModalIncludeException);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.FeedbackModal_IncludeLastErrorHint);

            ImGui.Spacing();

            ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.FeedbackModal_Hint);

            var buttonWidth = 120f;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);

            if (ImGui.Button(Locs.ErrorModalButton_Ok, new Vector2(buttonWidth, 40)))
            {
                if (string.IsNullOrWhiteSpace(this.feedbackModalContact))
                {
                    this.ShowErrorModal(Locs.FeedbackModal_ContactInformationRequired)
                        .ContinueWith(_ =>
                        {
                            this.feedbackModalOnNextFrameDontClear = true;
                            this.feedbackModalOnNextFrame = true;
                        });
                }
                else
                {
                    if (this.feedbackPlugin != null)
                    {
                        Task.Run(async () => await BugBait.SendFeedback(
                                                 this.feedbackPlugin,
                                                 this.feedbackIsTesting,
                                                 this.feedbackModalBody,
                                                 this.feedbackModalContact,
                                                 this.feedbackModalIncludeException))
                            .ContinueWith(
                                t =>
                                {
                                    var notif = Service<NotificationManager>.Get();
                                    if (t.IsCanceled || t.IsFaulted)
                                    {
                                        notif.AddNotification(
                                            Locs.FeedbackModal_NotificationError,
                                            Locs.FeedbackModal_Title,
                                            NotificationType.Error);
                                    }
                                    else
                                    {
                                        notif.AddNotification(
                                            Locs.FeedbackModal_NotificationSuccess,
                                            Locs.FeedbackModal_Title,
                                            NotificationType.Success);
                                    }
                                });
                    }
                    else
                    {
                        Log.Error("FeedbackPlugin was null.");
                    }

                    if (!string.IsNullOrWhiteSpace(this.feedbackModalContact))
                    {
                        Service<DalamudConfiguration>.Get().LastFeedbackContactDetails = this.feedbackModalContact;
                    }

                    ImGui.CloseCurrentPopup();
                }
            }

            ImGui.EndPopup();
        }

        if (this.feedbackModalOnNextFrame)
        {
            ImGui.OpenPopup(modalTitle);
            this.feedbackModalOnNextFrame = false;
            this.feedbackModalDrawing = true;
            if (!this.feedbackModalOnNextFrameDontClear)
            {
                this.feedbackModalBody = string.Empty;
                this.feedbackModalContact = Service<DalamudConfiguration>.Get().LastFeedbackContactDetails;
                this.feedbackModalIncludeException = false;
            }
            else
            {
                this.feedbackModalOnNextFrameDontClear = false;
            }
        }
    }

    private void DrawChangelogList(bool displayDalamud, bool displayPlugins)
    {
        if (this.pluginListInstalled.Count == 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.TabBody_SearchNoInstalled);
            return;
        }

        if (this.dalamudChangelogRefreshTask?.IsFaulted == true ||
            this.dalamudChangelogRefreshTask?.IsCanceled == true)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.TabBody_ChangelogError);
            return;
        }

        if (this.dalamudChangelogManager?.Changelogs == null)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.TabBody_LoadingPlugins);

            if (this.dalamudChangelogManager != null &&
                this.dalamudChangelogRefreshTask == null)
            {
                this.dalamudChangelogRefreshTaskCts = new CancellationTokenSource();
                this.dalamudChangelogRefreshTask =
                    Task.Run(this.dalamudChangelogManager.ReloadChangelogAsync, this.dalamudChangelogRefreshTaskCts.Token)
                        .ContinueWith(t =>
                        {
                            if (!t.IsCompletedSuccessfully)
                            {
                                Log.Error(t.Exception, "Failed to load changelogs.");
                            }
                        });
            }

            return;
        }

        IEnumerable<IChangelogEntry> changelogs = null;
        if (displayDalamud && displayPlugins && this.dalamudChangelogManager.Changelogs != null)
        {
            changelogs = this.dalamudChangelogManager.Changelogs;
        }
        else if (displayDalamud && this.dalamudChangelogManager.Changelogs != null)
        {
            changelogs = this.dalamudChangelogManager.Changelogs.OfType<DalamudChangelogEntry>();
        }
        else if (displayPlugins)
        {
            changelogs = this.dalamudChangelogManager.Changelogs.OfType<PluginChangelogEntry>();
        }

        var sortedChangelogs = changelogs?.Where(x => this.searchText.IsNullOrWhitespace() || new FuzzyMatcher(this.searchText.ToLowerInvariant(), MatchMode.FuzzyParts).Matches(x.Title.ToLowerInvariant()) > 0)
                                                            .OrderByDescending(x => x.Date).ToList();

        if (sortedChangelogs == null || !sortedChangelogs.Any())
        {
            ImGui.TextColored(
                ImGuiColors.DalamudGrey2,
                this.pluginListInstalled.Any(plugin => !plugin.Manifest.Changelog.IsNullOrEmpty())
                    ? Locs.TabBody_SearchNoMatching
                    : Locs.TabBody_ChangelogNone);

            return;
        }

        foreach (var logEntry in sortedChangelogs)
        {
            this.DrawChangelog(logEntry);
        }
    }
    
    private record PluginInstallerAvailablePluginProxy(RemotePluginManifest? RemoteManifest, LocalPlugin? LocalPlugin);
    
#pragma warning disable SA1201
    private void DrawAvailablePluginList()
#pragma warning restore SA1201
    {
        var availableManifests = this.pluginListAvailable;
        var installedPlugins = this.pluginListInstalled.ToList(); // Copy intended

        if (availableManifests.Count == 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.TabBody_SearchNoCompatible);
            return;
        }

        var filteredAvailableManifests = availableManifests
                                .Where(rm => !this.IsManifestFiltered(rm))
                                .ToList();

        if (filteredAvailableManifests.Count == 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey2, Locs.TabBody_SearchNoMatching);
            return;
        }

        var proxies = new List<PluginInstallerAvailablePluginProxy>();
        
        // Go through all AVAILABLE manifests, associate them with a NON-DEV local plugin, if one is available, and remove it from the pile
        foreach (var availableManifest in this.categoryManager.GetCurrentCategoryContent(filteredAvailableManifests).Cast<RemotePluginManifest>())
        {
            var plugin = this.pluginListInstalled.FirstOrDefault(plugin => plugin.Manifest.InternalName == availableManifest.InternalName && plugin.Manifest.RepoUrl == availableManifest.RepoUrl);
            
            // We "consumed" this plugin from the pile and remove it. 
            if (plugin != null && !plugin.IsDev)
            {
                installedPlugins.Remove(plugin);
                proxies.Add(new PluginInstallerAvailablePluginProxy(null, plugin));
                
                continue;
            }
            
            proxies.Add(new PluginInstallerAvailablePluginProxy(availableManifest, null));
        }
        
        // Now, add all applicable local plugins that haven't been "used up", in most cases either dev or orphaned plugins.
        foreach (var installedPlugin in installedPlugins)
        {
            if (this.IsManifestFiltered(installedPlugin.Manifest))
                continue;
            
            // TODO: We should also check categories here, for good measure
            
            proxies.Add(new PluginInstallerAvailablePluginProxy(null, installedPlugin));
        }

        var i = 0;
        foreach (var proxy in proxies)
        {
            IPluginManifest applicableManifest = proxy.LocalPlugin != null ? proxy.LocalPlugin.Manifest : proxy.RemoteManifest;

            if (applicableManifest == null)
                throw new Exception("Could not determine manifest for available plugin");
            
            ImGui.PushID($"{applicableManifest.InternalName}{applicableManifest.AssemblyVersion}");

            if (proxy.LocalPlugin != null)
            {
                this.DrawInstalledPlugin(proxy.LocalPlugin, i++, true);
            }
            else if (proxy.RemoteManifest != null)
            {
                this.DrawAvailablePlugin(proxy.RemoteManifest, i++);
            }

            ImGui.PopID();
        }
    }

    private void DrawInstalledPluginList(bool filterTesting)
    {
        var pluginList = this.pluginListInstalled;
        var manager = Service<PluginManager>.Get();

        if (pluginList.Count == 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.TabBody_SearchNoInstalled);
            return;
        }

        var filteredList = pluginList
                           .Where(plugin => !this.IsManifestFiltered(plugin.Manifest))
                           .ToList();

        if (filteredList.Count == 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey2, Locs.TabBody_SearchNoMatching);
            return;
        }

        var i = 0;
        foreach (var plugin in filteredList)
        {
            if (filterTesting && !manager.HasTestingOptIn(plugin.Manifest))
                continue;

            this.DrawInstalledPlugin(plugin, i++);
        }
    }

    private void DrawInstalledDevPluginList()
    {
        var pluginList = this.pluginListInstalled
                             .Where(plugin => plugin.IsDev)
                             .ToList();

        if (pluginList.Count == 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.TabBody_SearchNoInstalled);
            return;
        }

        var filteredList = pluginList
                           .Where(plugin => !this.IsManifestFiltered(plugin.Manifest))
                           .ToList();

        if (filteredList.Count == 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey2, Locs.TabBody_SearchNoMatching);
            return;
        }

        var i = 0;
        foreach (var plugin in filteredList)
        {
            this.DrawInstalledPlugin(plugin, i++);
        }
    }

    private void DrawPluginCategories()
    {
        var useContentHeight = -40f; // button height + spacing
        var useMenuWidth = 180f;     // works fine as static value, table can be resized by user

        var useContentWidth = ImGui.GetContentRegionAvail().X;

        using var installerMainChild = ImRaii.Child("InstallerCategories", new Vector2(useContentWidth, useContentHeight * ImGuiHelpers.GlobalScale));
        if (installerMainChild)
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, ImGuiHelpers.ScaledVector2(5, 0));
            
            try
            {
                using (var categoriesChild = ImRaii.Child("InstallerCategoriesSelector", new Vector2(useMenuWidth * ImGuiHelpers.GlobalScale, -1), false))
                {
                    if (categoriesChild)
                    {
                        this.DrawPluginCategorySelectors();
                    }
                }

                ImGui.SameLine();

                using var scrollingChild =
                    ImRaii.Child("ScrollingPlugins", new Vector2(-1, -1), false, ImGuiWindowFlags.NoBackground);
                if (scrollingChild)
                {
                    try
                    {
                        this.DrawPluginCategoryContent();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Could not draw category content");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not draw plugin categories");
            }
        }
    }

    private void DrawPluginCategorySelectors()
    {
        var colorSearchHighlight = Vector4.One;
        unsafe
        {
            var colorPtr = ImGui.GetStyleColorVec4(ImGuiCol.NavHighlight);
            if (colorPtr != null)
            {
                colorSearchHighlight = *colorPtr;
            }
        }

        for (var groupIdx = 0; groupIdx < this.categoryManager.GroupList.Length; groupIdx++)
        {
            var groupInfo = this.categoryManager.GroupList[groupIdx];
            var canShowGroup = (groupInfo.GroupKind != PluginCategoryManager.GroupKind.DevTools) || this.hasDevPlugins;
            if (!canShowGroup)
            {
                continue;
            }

            ImGui.SetNextItemOpen(groupIdx == this.categoryManager.CurrentGroupIdx);
            if (ImGui.CollapsingHeader(groupInfo.Name, groupIdx == this.categoryManager.CurrentGroupIdx ? ImGuiTreeNodeFlags.OpenOnDoubleClick : ImGuiTreeNodeFlags.None))
            {
                if (this.categoryManager.CurrentGroupIdx != groupIdx)
                {
                    this.categoryManager.CurrentGroupIdx = groupIdx;
                }

                ImGui.Indent();
                var categoryItemSize = new Vector2(ImGui.GetContentRegionAvail().X - (5 * ImGuiHelpers.GlobalScale), ImGui.GetTextLineHeight());
                for (var categoryIdx = 0; categoryIdx < groupInfo.Categories.Count; categoryIdx++)
                {
                    var categoryInfo = Array.Find(this.categoryManager.CategoryList, x => x.CategoryId == groupInfo.Categories[categoryIdx]);

                    switch (categoryInfo.Condition)
                    {
                        case PluginCategoryManager.CategoryInfo.AppearCondition.None:
                            // Do nothing
                            break;
                        case PluginCategoryManager.CategoryInfo.AppearCondition.DoPluginTest:
                            if (!Service<DalamudConfiguration>.Get().DoPluginTest)
                                continue;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    var hasSearchHighlight = this.categoryManager.IsCategoryHighlighted(categoryInfo.CategoryId);
                    if (hasSearchHighlight)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, colorSearchHighlight);
                    }

                    if (ImGui.Selectable(categoryInfo.Name, this.categoryManager.CurrentCategoryIdx == categoryIdx, ImGuiSelectableFlags.None, categoryItemSize))
                    {
                        this.categoryManager.CurrentCategoryIdx = categoryIdx;
                    }

                    if (hasSearchHighlight)
                    {
                        ImGui.PopStyleColor();
                    }
                }

                ImGui.Unindent();

                if (groupIdx != this.categoryManager.GroupList.Length - 1)
                {
                    ImGuiHelpers.ScaledDummy(5);
                }
            }
        }
    }

    private void DrawPluginCategoryContent()
    {
        var ready = this.DrawPluginListLoading();
        if (!this.categoryManager.IsSelectionValid || !ready)
        {
            return;
        }

        var pm = Service<PluginManager>.Get();
        if (pm.SafeMode)
        {
            ImGuiHelpers.ScaledDummy(10);

            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange);
            ImGui.PushFont(InterfaceManager.IconFont);
            ImGuiHelpers.CenteredText(FontAwesomeIcon.ExclamationTriangle.ToIconString());
            ImGui.PopFont();
            ImGui.PopStyleColor();

            var lines = Locs.SafeModeDisclaimer.Split('\n');
            foreach (var line in lines)
            {
                ImGuiHelpers.CenteredText(line);
            }

            ImGuiHelpers.ScaledDummy(10);
            ImGui.Separator();
        }

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(1, 3));

        var groupInfo = this.categoryManager.GroupList[this.categoryManager.CurrentGroupIdx];
        if (this.categoryManager.IsContentDirty)
        {
            // reset opened list of collapsibles when switching between categories
            this.openPluginCollapsibles.Clear();

            // do NOT reset dirty flag when Available group is selected, it will be handled by DrawAvailablePluginList()
            if (groupInfo.GroupKind != PluginCategoryManager.GroupKind.Available)
            {
                this.categoryManager.ResetContentDirty();
            }
        }

        switch (groupInfo.GroupKind)
        {
            case PluginCategoryManager.GroupKind.DevTools:
                // this one is never sorted and remains in hardcoded order from group ctor
                switch (this.categoryManager.CurrentCategoryIdx)
                {
                    case 0:
                        this.DrawInstalledDevPluginList();
                        break;

                    case 1:
                        this.DrawImageTester();
                        break;

                    default:
                        // umm, there's nothing else, keep handled set and just skip drawing...
                        break;
                }

                break;
            case PluginCategoryManager.GroupKind.Installed:
                switch (this.categoryManager.CurrentCategoryIdx)
                {
                    case 0:
                        this.DrawInstalledPluginList(false);
                        break;

                    case 1:
                        this.DrawInstalledPluginList(true);
                        break;

                    case 2:
                        this.profileManagerWidget.Draw();
                        break;
                }

                break;
            case PluginCategoryManager.GroupKind.Changelog:
                switch (this.categoryManager.CurrentCategoryIdx)
                {
                    case 0:
                        this.DrawChangelogList(true, true);
                        break;

                    case 1:
                        this.DrawChangelogList(true, false);
                        break;

                    case 2:
                        this.DrawChangelogList(false, true);
                        break;
                }

                break;
            default:
                this.DrawAvailablePluginList();
                break;
        }

        ImGui.PopStyleVar();
    }

    private void DrawImageTester()
    {
        var sectionSize = ImGuiHelpers.GlobalScale * 66;
        var startCursor = ImGui.GetCursorPos();

        ImGui.PushStyleColor(ImGuiCol.Button, true ? new Vector4(0.5f, 0.5f, 0.5f, 0.1f) : Vector4.Zero);

        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.5f, 0.5f, 0.2f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 0.5f, 0.5f, 0.35f));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);

        ImGui.Button($"###pluginTesterCollapsibleBtn", new Vector2(ImGui.GetWindowWidth() - (ImGuiHelpers.GlobalScale * 35), sectionSize));

        ImGui.PopStyleVar();

        ImGui.PopStyleColor(3);

        ImGui.SetCursorPos(startCursor);

        var hasIcon = this.testerIcon != null;

        var iconTex = this.imageCache.DefaultIcon;
        if (hasIcon) iconTex = this.testerIcon;

        var iconSize = ImGuiHelpers.ScaledVector2(64, 64);

        var cursorBeforeImage = ImGui.GetCursorPos();
        ImGui.Image(iconTex.ImGuiHandle, iconSize);
        ImGui.SameLine();

        if (this.testerError)
        {
            ImGui.SetCursorPos(cursorBeforeImage);
            ImGui.Image(this.imageCache.TroubleIcon.ImGuiHandle, iconSize);
            ImGui.SameLine();
        }
        else if (this.testerUpdateAvailable)
        {
            ImGui.SetCursorPos(cursorBeforeImage);
            ImGui.Image(this.imageCache.UpdateIcon.ImGuiHandle, iconSize);
            ImGui.SameLine();
        }

        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        var cursor = ImGui.GetCursorPos();
        // Name
        ImGui.Text("My Cool Plugin");

        // Download count
        var downloadCountText = Locs.PluginBody_AuthorWithDownloadCount("Plugin Enjoyer", 69420);

        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudGrey3, downloadCountText);

        cursor.Y += ImGui.GetTextLineHeightWithSpacing();
        ImGui.SetCursorPos(cursor);

        // Description
        ImGui.TextWrapped("This plugin does very many great things.");

        startCursor.Y += sectionSize;
        ImGui.SetCursorPos(startCursor);

        ImGuiHelpers.ScaledDummy(5);

        ImGui.Indent();

        // Description
        ImGui.TextWrapped("This is a description.\nIt has multiple lines.\nTruly descriptive.");

        ImGuiHelpers.ScaledDummy(5);

        // Controls
        var disabled = this.updateStatus == OperationStatus.InProgress || this.installStatus == OperationStatus.InProgress;

        var versionString = "1.0.0.0";

        if (disabled)
        {
            ImGuiComponents.DisabledButton(Locs.PluginButton_InstallVersion(versionString));
        }
        else
        {
            var buttonText = Locs.PluginButton_InstallVersion(versionString);
            ImGui.Button($"{buttonText}##{buttonText}testing");
        }

        this.DrawVisitRepoUrlButton("https://google.com", true);

        if (this.testerImages != null)
        {
            ImGuiHelpers.ScaledDummy(5);

            const float thumbFactor = 2.7f;

            var scrollBarSize = 15;
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, scrollBarSize);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Vector4.Zero);

            var width = ImGui.GetWindowWidth();

            if (ImGui.BeginChild(
                    "pluginTestingImageScrolling",
                    new Vector2(width - (70 * ImGuiHelpers.GlobalScale), (PluginImageCache.PluginImageHeight / thumbFactor) + scrollBarSize),
                    false,
                    ImGuiWindowFlags.HorizontalScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse |
                    ImGuiWindowFlags.NoBackground))
            {
                if (this.testerImages != null && this.testerImages is { Length: > 0 })
                {
                    for (var i = 0; i < this.testerImages.Length; i++)
                    {
                        var popupId = $"pluginTestingImage{i}";
                        var image = this.testerImages[i];
                        if (image == null)
                            continue;

                        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 0);
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);

                        if (ImGui.BeginPopup(popupId))
                        {
                            if (ImGui.ImageButton(image.ImGuiHandle, new Vector2(image.Width, image.Height)))
                                ImGui.CloseCurrentPopup();

                            ImGui.EndPopup();
                        }

                        ImGui.PopStyleVar(3);

                        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);

                        float xAct = image.Width;
                        float yAct = image.Height;
                        float xMax = PluginImageCache.PluginImageWidth;
                        float yMax = PluginImageCache.PluginImageHeight;

                        // scale image if undersized
                        if (xAct < xMax && yAct < yMax)
                        {
                            var scale = Math.Min(xMax / xAct, yMax / yAct);
                            xAct *= scale;
                            yAct *= scale;
                        }

                        var size = ImGuiHelpers.ScaledVector2(xAct / thumbFactor, yAct / thumbFactor);
                        if (ImGui.ImageButton(image.ImGuiHandle, size))
                            ImGui.OpenPopup(popupId);

                        ImGui.PopStyleVar();

                        if (i < this.testerImages.Length - 1)
                        {
                            ImGui.SameLine();
                            ImGuiHelpers.ScaledDummy(5);
                            ImGui.SameLine();
                        }
                    }
                }
            }

            ImGui.EndChild();

            ImGui.PopStyleVar();
            ImGui.PopStyleColor();

            ImGui.Unindent();
        }

        ImGuiHelpers.ScaledDummy(20);

        static void CheckImageSize(IDalamudTextureWrap? image, int maxWidth, int maxHeight, bool requireSquare)
        {
            if (image == null)
                return;
            if (image.Width > maxWidth || image.Height > maxHeight)
                ImGui.TextColored(ImGuiColors.DalamudRed, $"Image is larger than the maximum allowed resolution ({image.Width}x{image.Height} > {maxWidth}x{maxHeight})");
            if (requireSquare && image.Width != image.Height)
                ImGui.TextColored(ImGuiColors.DalamudRed, $"Image must be square! Current size: {image.Width}x{image.Height}");
        }

        ImGui.InputText("Icon Path", ref this.testerIconPath, 1000);
        if (this.testerIcon != null)
            CheckImageSize(this.testerIcon, PluginImageCache.PluginIconWidth, PluginImageCache.PluginIconHeight, true);
        ImGui.InputText("Image 1 Path", ref this.testerImagePaths[0], 1000);
        if (this.testerImages?.Length > 0)
            CheckImageSize(this.testerImages[0], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
        ImGui.InputText("Image 2 Path", ref this.testerImagePaths[1], 1000);
        if (this.testerImages?.Length > 1)
            CheckImageSize(this.testerImages[1], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
        ImGui.InputText("Image 3 Path", ref this.testerImagePaths[2], 1000);
        if (this.testerImages?.Length > 2)
            CheckImageSize(this.testerImages[2], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
        ImGui.InputText("Image 4 Path", ref this.testerImagePaths[3], 1000);
        if (this.testerImages?.Length > 3)
            CheckImageSize(this.testerImages[3], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
        ImGui.InputText("Image 5 Path", ref this.testerImagePaths[4], 1000);
        if (this.testerImages?.Length > 4)
            CheckImageSize(this.testerImages[4], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);

        var im = Service<InterfaceManager>.Get();
        if (ImGui.Button("Load"))
        {
            try
            {
                if (this.testerIcon != null)
                {
                    this.testerIcon.Dispose();
                    this.testerIcon = null;
                }

                if (!this.testerIconPath.IsNullOrEmpty())
                {
                    this.testerIcon = im.LoadImage(this.testerIconPath);
                }

                this.testerImages = new IDalamudTextureWrap[this.testerImagePaths.Length];

                for (var i = 0; i < this.testerImagePaths.Length; i++)
                {
                    if (this.testerImagePaths[i].IsNullOrEmpty())
                        continue;

                    if (this.testerImages[i] != null)
                    {
                        this.testerImages[i].Dispose();
                        this.testerImages[i] = null;
                    }

                    this.testerImages[i] = im.LoadImage(this.testerImagePaths[i]);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load plugin images for testing.");
            }
        }

        ImGui.Checkbox("Failed", ref this.testerError);
        ImGui.Checkbox("Has Update", ref this.testerUpdateAvailable);
    }

    private bool DrawPluginListLoading()
    {
        var pluginManager = Service<PluginManager>.Get();

        var ready = pluginManager.PluginsReady && pluginManager.ReposReady;

        if (!ready)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.TabBody_LoadingPlugins);
        }

        var failedRepos = pluginManager.Repos
                                       .Where(repo => repo.State == PluginRepositoryState.Fail)
                                       .ToArray();

        if (failedRepos.Length > 0)
        {
            var failText = Locs.TabBody_DownloadFailed;
            var aggFailText = failedRepos
                              .Select(repo => $"{failText} ({repo.PluginMasterUrl})")
                              .Aggregate((s1, s2) => $"{s1}\n{s2}");

            ImGui.TextColored(ImGuiColors.DalamudRed, aggFailText);
        }

        return ready;
    }

    private bool DrawPluginCollapsingHeader(string label, LocalPlugin? plugin, IPluginManifest manifest, bool isThirdParty, bool trouble, bool updateAvailable, bool isNew, bool installableOutdated, bool isOrphan, Action drawContextMenuAction, int index)
    {
        ImGui.Separator();

        var isOpen = this.openPluginCollapsibles.Contains(index);

        var sectionSize = ImGuiHelpers.GlobalScale * 66;
        var startCursor = ImGui.GetCursorPos();

        ImGui.PushStyleColor(ImGuiCol.Button, isOpen ? new Vector4(0.5f, 0.5f, 0.5f, 0.1f) : Vector4.Zero);

        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.5f, 0.5f, 0.2f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 0.5f, 0.5f, 0.35f));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);

        if (ImGui.Button($"###plugin{index}CollapsibleBtn", new Vector2(ImGui.GetContentRegionAvail().X, sectionSize)))
        {
            if (isOpen)
            {
                this.openPluginCollapsibles.Remove(index);
            }
            else
            {
                this.openPluginCollapsibles.Add(index);
            }

            isOpen = !isOpen;
        }

        drawContextMenuAction?.Invoke();

        ImGui.PopStyleVar();

        ImGui.PopStyleColor(3);

        ImGui.SetCursorPos(startCursor);

        var pluginDisabled = plugin is { IsWantedByAnyProfile: false };

        var iconSize = ImGuiHelpers.ScaledVector2(64, 64);
        var cursorBeforeImage = ImGui.GetCursorPos();
        var rectOffset = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();
        if (ImGui.IsRectVisible(rectOffset + cursorBeforeImage, rectOffset + cursorBeforeImage + iconSize))
        {
            var iconTex = this.imageCache.DefaultIcon;
            var hasIcon = this.imageCache.TryGetIcon(plugin, manifest, isThirdParty, out var cachedIconTex);
            if (hasIcon && cachedIconTex != null)
            {
                iconTex = cachedIconTex;
            }

            if (pluginDisabled || installableOutdated)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.4f);
            }

            ImGui.Image(iconTex.ImGuiHandle, iconSize);

            if (pluginDisabled || installableOutdated)
            {
                ImGui.PopStyleVar();
            }

            ImGui.SameLine();
            ImGui.SetCursorPos(cursorBeforeImage);
        }

        var isLoaded = plugin is { IsLoaded: true };

        if (updateAvailable)
            ImGui.Image(this.imageCache.UpdateIcon.ImGuiHandle, iconSize);
        else if ((trouble && !pluginDisabled) || isOrphan)
            ImGui.Image(this.imageCache.TroubleIcon.ImGuiHandle, iconSize);
        else if (installableOutdated)
            ImGui.Image(this.imageCache.OutdatedInstallableIcon.ImGuiHandle, iconSize);
        else if (pluginDisabled)
            ImGui.Image(this.imageCache.DisabledIcon.ImGuiHandle, iconSize);
        /* NOTE: Replaced by the checkmarks for now, let's see if that is fine
        else if (isLoaded && isThirdParty)
            ImGui.Image(this.imageCache.ThirdInstalledIcon.ImGuiHandle, iconSize);
        else if (isThirdParty)
            ImGui.Image(this.imageCache.ThirdIcon.ImGuiHandle, iconSize);
        */
        else if (isLoaded)
            ImGui.Image(this.imageCache.InstalledIcon.ImGuiHandle, iconSize);
        else
            ImGui.Dummy(iconSize);
        ImGui.SameLine();

        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        var cursor = ImGui.GetCursorPos();

        // Name
        ImGui.TextUnformatted(label);
        
        // Verified Checkmark or dev plugin wrench
        {
            ImGui.SameLine();
            ImGui.Text(" ");
            ImGui.SameLine();

            var verifiedOutlineColor = KnownColor.White.Vector() with { W = 0.75f };
            var unverifiedOutlineColor = KnownColor.Black.Vector();
            var verifiedIconColor = KnownColor.RoyalBlue.Vector() with { W = 0.75f };
            var unverifiedIconColor = KnownColor.Orange.Vector();
            var devIconOutlineColor = KnownColor.White.Vector();
            var devIconColor = KnownColor.MediumOrchid.Vector();

            if (plugin is LocalDevPlugin)
            {
                this.DrawFontawesomeIconOutlined(FontAwesomeIcon.Wrench, devIconOutlineColor, devIconColor);
                this.VerifiedCheckmarkFadeTooltip(label, "This is a dev plugin. You added it.");
            }
            else if (!isThirdParty)
            {
                this.DrawFontawesomeIconOutlined(FontAwesomeIcon.CheckCircle, verifiedOutlineColor, verifiedIconColor);
                this.VerifiedCheckmarkFadeTooltip(label, Locs.VerifiedCheckmark_VerifiedTooltip);
            }
            else
            {
                this.DrawFontawesomeIconOutlined(FontAwesomeIcon.ExclamationCircle, unverifiedOutlineColor, unverifiedIconColor);
                this.VerifiedCheckmarkFadeTooltip(label, Locs.VerifiedCheckmark_UnverifiedTooltip);
            }
        }

        // Download count
        var downloadCountText = manifest.DownloadCount > 0
                                    ? Locs.PluginBody_AuthorWithDownloadCount(manifest.Author, manifest.DownloadCount)
                                    : Locs.PluginBody_AuthorWithDownloadCountUnavailable(manifest.Author);

        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudGrey3, downloadCountText);

        if (isNew)
        {
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.TankBlue, Locs.PluginTitleMod_New);
        }

        cursor.Y += ImGui.GetTextLineHeightWithSpacing();
        ImGui.SetCursorPos(cursor);

        // Outdated warning
        if (plugin is { IsOutdated: true, IsBanned: false } || installableOutdated)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);

            var bodyText = Locs.PluginBody_Outdated + " ";
            if (updateAvailable)
                bodyText += Locs.PluginBody_Outdated_CanNowUpdate;
            else
                bodyText += Locs.PluginBody_Outdated_WaitForUpdate;
            
            ImGui.TextWrapped(bodyText);
            ImGui.PopStyleColor();
        }
        else if (plugin is { IsBanned: true })
        {
            // Banned warning
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            
            var bodyText = plugin.BanReason.IsNullOrEmpty()
                               ? Locs.PluginBody_Banned
                               : Locs.PluginBody_BannedReason(plugin.BanReason);
            bodyText += " ";
            
            if (updateAvailable)
                bodyText += Locs.PluginBody_Outdated_CanNowUpdate;
            else
                bodyText += Locs.PluginBody_Outdated_WaitForUpdate;
            
            ImGuiHelpers.SafeTextWrapped(bodyText);

            ImGui.PopStyleColor();
        }
        else if (plugin is { IsOrphaned: true })
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped(Locs.PluginBody_Orphaned);
            ImGui.PopStyleColor();
        }
        else if (plugin is { IsDecommissioned: true, IsThirdParty: false })
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped(Locs.PluginBody_NoServiceOfficial);
            ImGui.PopStyleColor();
        }
        else if (plugin is { IsDecommissioned: true, IsThirdParty: true })
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped(Locs.PluginBody_NoServiceThird);
            ImGui.PopStyleColor();
        }
        else if (plugin != null && !plugin.CheckPolicy())
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped(Locs.PluginBody_Policy);
            ImGui.PopStyleColor();
        }
        else if (plugin is { State: PluginState.LoadError or PluginState.DependencyResolutionFailed })
        {
            // Load failed warning
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped(Locs.PluginBody_LoadFailed);
            ImGui.PopStyleColor();
        }

        ImGui.SetCursorPosX(cursor.X);

        // Description
        if (plugin is null or { IsOutdated: false, IsBanned: false } && !trouble)
        {
            if (!string.IsNullOrWhiteSpace(manifest.Punchline))
            {
                ImGuiHelpers.SafeTextWrapped(manifest.Punchline);
            }
            else if (!string.IsNullOrWhiteSpace(manifest.Description))
            {
                const int punchlineLen = 200;
                var firstLine = manifest.Description.Split(new[] { '\r', '\n' })[0];

                ImGuiHelpers.SafeTextWrapped(firstLine.Length < punchlineLen
                                                 ? firstLine
                                                 : firstLine[..punchlineLen]);
            }
        }

        startCursor.Y += sectionSize;
        ImGui.SetCursorPos(startCursor);

        return isOpen;
    }

    private void DrawChangelog(IChangelogEntry log)
    {
        ImGui.Separator();

        var startCursor = ImGui.GetCursorPos();

        var iconSize = ImGuiHelpers.ScaledVector2(64, 64);
        var cursorBeforeImage = ImGui.GetCursorPos();
        var rectOffset = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();
        if (ImGui.IsRectVisible(rectOffset + cursorBeforeImage, rectOffset + cursorBeforeImage + iconSize))
        {
            IDalamudTextureWrap icon;
            if (log is PluginChangelogEntry pluginLog)
            {
                icon = this.imageCache.DefaultIcon;
                var hasIcon = this.imageCache.TryGetIcon(pluginLog.Plugin, pluginLog.Plugin.Manifest, pluginLog.Plugin.IsThirdParty, out var cachedIconTex);
                if (hasIcon && cachedIconTex != null)
                {
                    icon = cachedIconTex;
                }
            }
            else
            {
                icon = this.imageCache.CorePluginIcon;
            }

            ImGui.Image(icon.ImGuiHandle, iconSize);
        }
        else
        {
            ImGui.Dummy(iconSize);
        }

        ImGui.SameLine();

        ImGuiHelpers.ScaledDummy(5);

        ImGui.SameLine();
        var cursor = ImGui.GetCursorPos();
        ImGui.TextUnformatted(log.Title);

        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudGrey3, $" v{log.Version}");
        if (log.Author != null)
        {
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudGrey3, Locs.PluginBody_AuthorWithoutDownloadCount(log.Author));
        }

        cursor.Y += ImGui.GetTextLineHeightWithSpacing();
        ImGui.SetCursorPos(cursor);

        ImGuiHelpers.SafeTextWrapped(log.Text);

        var endCursor = ImGui.GetCursorPos();

        var sectionSize = Math.Max(
            66 * ImGuiHelpers.GlobalScale, // min size due to icons
            endCursor.Y - startCursor.Y);

        startCursor.Y += sectionSize;
        ImGui.SetCursorPos(startCursor);
    }

    private void DrawAvailablePlugin(RemotePluginManifest manifest, int index)
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var pluginManager = Service<PluginManager>.Get();

        var useTesting = pluginManager.UseTesting(manifest);
        var wasSeen = this.WasPluginSeen(manifest.InternalName);

        var isOutdated = manifest.DalamudApiLevel < PluginManager.DalamudApiLevel;

        // Check for valid versions
        if ((useTesting && manifest.TestingAssemblyVersion == null) || manifest.AssemblyVersion == null)
        {
            // Without a valid version, quit
            return;
        }

        // Name
        var label = manifest.Name;

        // Testing
        if (useTesting)
        {
            label += Locs.PluginTitleMod_TestingVersion;
        }
        else if (manifest.IsTestingExclusive)
        {
            label += Locs.PluginTitleMod_TestingExclusive;
        }
        else if (configuration.DoPluginTest && PluginManager.HasTestingVersion(manifest))
        {
            label += Locs.PluginTitleMod_TestingAvailable;
        }

        ImGui.PushID($"available{index}{manifest.InternalName}");

        var isThirdParty = manifest.SourceRepo.IsThirdParty;
        if (this.DrawPluginCollapsingHeader(label, null, manifest, isThirdParty, false, false, !wasSeen, isOutdated, false, () => this.DrawAvailablePluginContextMenu(manifest), index))
        {
            if (!wasSeen)
                configuration.SeenPluginInternalName.Add(manifest.InternalName);

            ImGuiHelpers.ScaledDummy(5);

            ImGui.Indent();

            // Installable from
            if (manifest.SourceRepo.IsThirdParty)
            {
                var repoText = Locs.PluginBody_Plugin3rdPartyRepo(manifest.SourceRepo.PluginMasterUrl);
                ImGui.TextColored(ImGuiColors.DalamudGrey3, repoText);

                ImGuiHelpers.ScaledDummy(2);
            }

            // Description
            if (!string.IsNullOrWhiteSpace(manifest.Description))
            {
                ImGuiHelpers.SafeTextWrapped(manifest.Description);
            }

            ImGuiHelpers.ScaledDummy(5);

            // Controls
            var disabled = this.updateStatus == OperationStatus.InProgress || this.installStatus == OperationStatus.InProgress || isOutdated;

            var versionString = useTesting
                                    ? $"{manifest.TestingAssemblyVersion}"
                                    : $"{manifest.AssemblyVersion}";

            if (pluginManager.SafeMode)
            {
                ImGuiComponents.DisabledButton(Locs.PluginButton_SafeMode);
            }
            else if (disabled)
            {
                ImGuiComponents.DisabledButton(Locs.PluginButton_InstallVersion(versionString));
            }
            else
            {
                using var color = ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DalamudRed.Darken(0.3f).Fade(0.4f));
                var buttonText = Locs.PluginButton_InstallVersion(versionString);
                if (ImGui.Button($"{buttonText}##{buttonText}{index}"))
                {
                    this.StartInstall(manifest, useTesting);
                }
            }

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(10);
            ImGui.SameLine();

            this.DrawVisitRepoUrlButton(manifest.RepoUrl, true);
            
            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(3);
            ImGui.SameLine();

            if (!manifest.SourceRepo.IsThirdParty && manifest.AcceptsFeedback)
            {
                this.DrawSendFeedbackButton(manifest, false, true);
            }

            ImGuiHelpers.ScaledDummy(5);

            if (this.DrawPluginImages(null, manifest, isThirdParty, index))
                ImGuiHelpers.ScaledDummy(5);

            ImGui.Unindent();
        }

        ImGui.PopID();
    }

    private void DrawAvailablePluginContextMenu(PluginManifest manifest)
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var pluginManager = Service<PluginManager>.Get();

        if (ImGui.BeginPopupContextItem("ItemContextMenu"))
        {
            if (ImGui.Selectable(Locs.PluginContext_MarkAllSeen))
            {
                configuration.SeenPluginInternalName.AddRange(this.pluginListAvailable.Select(x => x.InternalName));
                configuration.QueueSave();
                pluginManager.RefilterPluginMasters();
            }

            if (ImGui.Selectable(Locs.PluginContext_HidePlugin))
            {
                Log.Debug($"Adding {manifest.InternalName} to hidden plugins");
                configuration.HiddenPluginInternalName.Add(manifest.InternalName);
                configuration.QueueSave();
                pluginManager.RefilterPluginMasters();
            }

            if (ImGui.Selectable(Locs.PluginContext_DeletePluginConfig))
            {
                this.ShowDeletePluginConfigWarningModal(manifest.Name).ContinueWith(t =>
                {
                    var shouldDelete = t.Result;

                    if (shouldDelete)
                    {
                        Log.Debug($"Deleting config for {manifest.InternalName}");

                        this.installStatus = OperationStatus.InProgress;

                        Task.Run(() =>
                            {
                                pluginManager.PluginConfigs.Delete(manifest.InternalName);
                                var dir = pluginManager.PluginConfigs.GetDirectory(manifest.InternalName);

                                if (Directory.Exists(dir))
                                    Directory.Delete(dir, true);
                            })
                            .ContinueWith(task =>
                            {
                                this.installStatus = OperationStatus.Idle;

                                this.DisplayErrorContinuation(task, Locs.ErrorModal_DeleteConfigFail(manifest.InternalName));
                            });
                    }
                });
            }

            ImGui.EndPopup();
        }
    }

    private void DrawInstalledPlugin(LocalPlugin plugin, int index, bool showInstalled = false)
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var commandManager = Service<CommandManager>.Get();

        var testingOptIn =
            configuration.PluginTestingOptIns?.FirstOrDefault(x => x.InternalName == plugin.Manifest.InternalName);
        var trouble = false;

        // Name
        var label = plugin.Manifest.Name;

        // Dev
        if (plugin.IsDev)
        {
            label += Locs.PluginTitleMod_DevPlugin;
        }

        // Testing
        if (plugin.IsTesting)
        {
            label += Locs.PluginTitleMod_TestingVersion;
        }

        var hasTestingAvailable = this.pluginListAvailable.Any(x => x.InternalName == plugin.InternalName &&
                                                                               x.IsAvailableForTesting);
        if (hasTestingAvailable && configuration.DoPluginTest && testingOptIn == null)
        {
            label += Locs.PluginTitleMod_TestingAvailable;
        }

        // Freshly installed
        if (showInstalled)
        {
            label += Locs.PluginTitleMod_Installed;
        }

        // Disabled
        if (!plugin.IsWantedByAnyProfile || !plugin.CheckPolicy())
        {
            label += Locs.PluginTitleMod_Disabled;
            trouble = true;
        }

        // Load error
        if (plugin.State is PluginState.LoadError or PluginState.DependencyResolutionFailed && plugin.CheckPolicy()
            && !plugin.IsOutdated && !plugin.IsBanned && !plugin.IsOrphaned)
        {
            label += Locs.PluginTitleMod_LoadError;
            trouble = true;
        }

        // Unload error
        if (plugin.State == PluginState.UnloadError)
        {
            label += Locs.PluginTitleMod_UnloadError;
            trouble = true;
        }

        var availablePluginUpdate = this.pluginListUpdatable.FirstOrDefault(up => up.InstalledPlugin == plugin);
        
        // Dev plugins can never update
        if (plugin.IsDev)
            availablePluginUpdate = null;
        
        // Update available
        if (availablePluginUpdate != default)
        {
            label += Locs.PluginTitleMod_HasUpdate;
        }

        // Freshly updated
        var thisWasUpdated = false;
        if (this.updatedPlugins != null && !plugin.IsDev)
        {
            var update = this.updatedPlugins.FirstOrDefault(update => update.InternalName == plugin.Manifest.InternalName);
            if (update != default)
            {
                if (update.Status == PluginUpdateStatus.StatusKind.Success)
                {
                    thisWasUpdated = true;
                    label += Locs.PluginTitleMod_Updated;
                }
                else
                {
                    label += Locs.PluginTitleMod_UpdateFailed;
                }
            }
        }

        // Outdated API level
        if (plugin.IsOutdated)
        {
            label += Locs.PluginTitleMod_OutdatedError;
            trouble = true;
        }

        // Banned
        if (plugin.IsBanned)
        {
            label += Locs.PluginTitleMod_BannedError;
            trouble = true;
        }

        // Orphaned
        if (plugin.IsOrphaned)
        {
            label += Locs.PluginTitleMod_OrphanedError;
            trouble = true;
        }

        // Out of service
        if (plugin.IsDecommissioned && !plugin.IsOrphaned)
        {
            label += Locs.PluginTitleMod_NoService;
            trouble = true;
        }

        // Scheduled for deletion
        if (plugin.Manifest.ScheduledForDeletion)
        {
            label += Locs.PluginTitleMod_ScheduledForDeletion;
        }

        ImGui.PushID($"installed{index}{plugin.Manifest.InternalName}");
        var hasChangelog = !plugin.Manifest.Changelog.IsNullOrEmpty();
        var didDrawChangelogInsideCollapsible = false;

        if (this.DrawPluginCollapsingHeader(label, plugin, plugin.Manifest, plugin.IsThirdParty, trouble, availablePluginUpdate != default, false, false, plugin.IsOrphaned, () => this.DrawInstalledPluginContextMenu(plugin, testingOptIn), index))
        {
            if (!this.WasPluginSeen(plugin.Manifest.InternalName))
                configuration.SeenPluginInternalName.Add(plugin.Manifest.InternalName);

            var manifest = plugin.Manifest;

            ImGui.Indent();

            // Name
            ImGui.TextUnformatted(manifest.Name);

            // Download count
            var downloadText = plugin.IsDev
                                   ? Locs.PluginBody_AuthorWithoutDownloadCount(manifest.Author)
                                   : manifest.DownloadCount > 0
                                       ? Locs.PluginBody_AuthorWithDownloadCount(manifest.Author, manifest.DownloadCount)
                                       : Locs.PluginBody_AuthorWithDownloadCountUnavailable(manifest.Author);

            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudGrey3, downloadText);

            var acceptsFeedback =
                this.pluginListAvailable.Any(x => x.InternalName == plugin.InternalName && x.AcceptsFeedback);

            var isThirdParty = plugin.IsThirdParty;
            var canFeedback = !isThirdParty &&
                              !plugin.IsDev &&
                              !plugin.IsOrphaned &&
                              plugin.Manifest.DalamudApiLevel == PluginManager.DalamudApiLevel &&
                              acceptsFeedback &&
                              availablePluginUpdate == default;

            // Installed from
            if (plugin.IsDev)
            {
                var fileText = Locs.PluginBody_DevPluginPath(plugin.DllFile.FullName);
                ImGui.TextColored(ImGuiColors.DalamudGrey3, fileText);
            }
            else if (isThirdParty)
            {
                var repoText = Locs.PluginBody_Plugin3rdPartyRepo(manifest.InstalledFromUrl);
                ImGui.TextColored(ImGuiColors.DalamudGrey3, repoText);
            }

            // Description
            if (!string.IsNullOrWhiteSpace(manifest.Description))
            {
                ImGuiHelpers.SafeTextWrapped(manifest.Description);
            }

            // Working Plugin ID
            if (this.hasDevPlugins)
            {
                ImGuiHelpers.ScaledDummy(3);
                ImGui.TextColored(ImGuiColors.DalamudGrey, $"WorkingPluginId: {manifest.WorkingPluginId}");
                ImGuiHelpers.ScaledDummy(3);
            }

            // Available commands (if loaded)
            if (plugin.IsLoaded)
            {
                var commands = commandManager.Commands
                                             .Where(cInfo => 
                                                        cInfo.Value is { ShowInHelp: true } &&
                                                        cInfo.Value.LoaderAssemblyName == plugin.Manifest.InternalName)
                                             .ToArray();

                if (commands.Any())
                {
                    ImGui.Dummy(ImGuiHelpers.ScaledVector2(10f, 10f));
                    foreach (var command in commands)
                    {
                        ImGuiHelpers.SafeTextWrapped($"{command.Key} → {command.Value.HelpMessage}");
                    }
                    
                    ImGuiHelpers.ScaledDummy(3);
                }
            }

            // Controls
            this.DrawPluginControlButton(plugin, availablePluginUpdate);
            this.DrawDevPluginButtons(plugin);
            this.DrawVisitRepoUrlButton(plugin.Manifest.RepoUrl, false);
            this.DrawDeletePluginButton(plugin);

            if (canFeedback)
            {
                this.DrawSendFeedbackButton(plugin.Manifest, plugin.IsTesting, false);
            }

            if (availablePluginUpdate != default && !plugin.IsDev)
                this.DrawUpdateSinglePluginButton(availablePluginUpdate);

            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudGrey3, $" v{plugin.EffectiveVersion}");

            ImGuiHelpers.ScaledDummy(5);

            if (this.DrawPluginImages(plugin, manifest, isThirdParty, index))
                ImGuiHelpers.ScaledDummy(5);

            ImGui.Unindent();

            if (hasChangelog)
            {
                if (ImGui.TreeNode(Locs.PluginBody_CurrentChangeLog(plugin.EffectiveVersion)))
                {
                    didDrawChangelogInsideCollapsible = true;
                    this.DrawInstalledPluginChangelog(plugin.Manifest);
                    ImGui.TreePop();
                }
            }

            if (availablePluginUpdate != default && !availablePluginUpdate.UpdateManifest.Changelog.IsNullOrWhitespace())
            {
                var availablePluginUpdateVersion = availablePluginUpdate.UseTesting ? availablePluginUpdate.UpdateManifest.TestingAssemblyVersion : availablePluginUpdate.UpdateManifest.AssemblyVersion;
                if (ImGui.TreeNode(Locs.PluginBody_UpdateChangeLog(availablePluginUpdateVersion)))
                {
                    this.DrawInstalledPluginChangelog(availablePluginUpdate.UpdateManifest);
                    ImGui.TreePop();
                }
            }
        }

        if (thisWasUpdated && hasChangelog && !didDrawChangelogInsideCollapsible)
        {
            this.DrawInstalledPluginChangelog(plugin.Manifest);
        }

        ImGui.PopID();
    }

    private void DrawInstalledPluginChangelog(IPluginManifest manifest)
    {
        ImGuiHelpers.ScaledDummy(5);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, this.changelogBgColor);
        ImGui.PushStyleColor(ImGuiCol.Text, this.changelogTextColor);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7, 5));

        if (ImGui.BeginChild("##changelog", new Vector2(-1, 100), true, ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoNavInputs | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Changelog:");
            ImGuiHelpers.ScaledDummy(2);
            ImGuiHelpers.SafeTextWrapped(manifest.Changelog!);
        }

        ImGui.EndChild();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(2);
    }

    private void DrawInstalledPluginContextMenu(LocalPlugin plugin, PluginTestingOptIn? optIn)
    {
        var pluginManager = Service<PluginManager>.Get();
        var configuration = Service<DalamudConfiguration>.Get();

        if (ImGui.BeginPopupContextItem("InstalledItemContextMenu"))
        {
            if (configuration.DoPluginTest)
            {
                var repoManifest = this.pluginListAvailable.FirstOrDefault(x => x.InternalName == plugin.Manifest.InternalName);
                if (repoManifest?.IsTestingExclusive == true)
                    ImGui.BeginDisabled();

                if (ImGui.MenuItem(Locs.PluginContext_TestingOptIn, string.Empty, optIn != null))
                {
                    if (optIn != null)
                    {
                        configuration.PluginTestingOptIns!.Remove(optIn);

                        if (plugin.Manifest.TestingAssemblyVersion > repoManifest?.AssemblyVersion)
                        {
                            this.testingWarningModalOnNextFrame = true;
                        }
                    }
                    else
                    {
                        configuration.PluginTestingOptIns!.Add(new PluginTestingOptIn(plugin.Manifest.InternalName));
                    }

                    configuration.QueueSave();
                }

                if (repoManifest?.IsTestingExclusive == true)
                    ImGui.EndDisabled();
            }

            if (ImGui.MenuItem(Locs.PluginContext_DeletePluginConfigReload))
            {
                this.ShowDeletePluginConfigWarningModal(plugin.Manifest.Name).ContinueWith(t =>
                {
                    var shouldDelete = t.Result;

                    if (shouldDelete)
                    {
                        Log.Debug($"Deleting config for {plugin.Manifest.InternalName}");

                        this.installStatus = OperationStatus.InProgress;

                        Task.Run(() => pluginManager.DeleteConfigurationAsync(plugin))
                            .ContinueWith(task =>
                            {
                                this.installStatus = OperationStatus.Idle;

                                this.DisplayErrorContinuation(task, Locs.ErrorModal_DeleteConfigFail(plugin.Manifest.InternalName));
                            });
                    }
                });
            }

            ImGui.EndPopup();
        }
    }

    private void DrawPluginControlButton(LocalPlugin plugin, AvailablePluginUpdate? availableUpdate)
    {
        var notifications = Service<NotificationManager>.Get();
        var pluginManager = Service<PluginManager>.Get();
        var profileManager = Service<ProfileManager>.Get();
        var config = Service<DalamudConfiguration>.Get();

        var applicableForProfiles = plugin.Manifest.SupportsProfiles /*&& !plugin.IsDev*/;
        var profilesThatWantThisPlugin = profileManager.Profiles
                                                       .Where(x => x.WantsPlugin(plugin.Manifest.WorkingPluginId) != null)
                                                       .ToArray();
        var isInSingleProfile = profilesThatWantThisPlugin.Length == 1;
        var isDefaultPlugin = profileManager.IsInDefaultProfile(plugin.Manifest.WorkingPluginId);

        // Disable everything if the updater is running or another plugin is operating
        var disabled = this.updateStatus == OperationStatus.InProgress || this.installStatus == OperationStatus.InProgress;

        // Disable everything if the plugin is outdated
        disabled = disabled || (plugin.IsOutdated && !pluginManager.LoadAllApiLevels) || plugin.IsBanned;

        // Disable everything if the plugin is orphaned
        // Control will immediately be disabled once the plugin is disabled
        disabled = disabled || (plugin.IsOrphaned && !plugin.IsLoaded);

        // Disable everything if the plugin failed to load
        // Now handled by the first case below
        // disabled = disabled || plugin.State == PluginState.LoadError || plugin.State == PluginState.DependencyResolutionFailed;

        // Disable everything if we're loading plugins
        disabled = disabled || plugin.State == PluginState.Loading || plugin.State == PluginState.Unloading;

        // Disable everything if we're applying profiles
        disabled = disabled || profileManager.IsBusy;

        var toggleId = plugin.Manifest.InternalName;
        var isLoadedAndUnloadable = plugin.State == PluginState.Loaded ||
                                    plugin.State == PluginState.DependencyResolutionFailed;

        // StyleModelV1.DalamudStandard.Push();

        var profileChooserPopupName = $"###pluginProfileChooser{plugin.Manifest.InternalName}";
        if (ImGui.BeginPopup(profileChooserPopupName))
        {
            var didAny = false;

            foreach (var profile in profileManager.Profiles.Where(x => !x.IsDefaultProfile))
            {
                var inProfile = profile.WantsPlugin(plugin.Manifest.WorkingPluginId) != null;
                if (ImGui.Checkbox($"###profilePick{profile.Guid}{plugin.Manifest.InternalName}", ref inProfile))
                {
                    if (inProfile)
                    {
                        Task.Run(() => profile.AddOrUpdateAsync(plugin.Manifest.WorkingPluginId, plugin.Manifest.InternalName, true))
                            .ContinueWith(this.DisplayErrorContinuation, Locs.Profiles_CouldNotAdd);
                    }
                    else
                    {
                        Task.Run(() => profile.RemoveAsync(plugin.Manifest.WorkingPluginId))
                            .ContinueWith(this.DisplayErrorContinuation, Locs.Profiles_CouldNotRemove);
                    }
                }

                ImGui.SameLine();

                ImGui.TextUnformatted(profile.Name);

                didAny = true;
            }

            if (!didAny)
                ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.Profiles_None);

            ImGui.Separator();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Times))
            {
                // TODO: Work this out
                Task.Run(() => profileManager.DefaultProfile.AddOrUpdateAsync(plugin.Manifest.WorkingPluginId, plugin.Manifest.InternalName, plugin.IsLoaded, false))
                    .GetAwaiter().GetResult();
                foreach (var profile in profileManager.Profiles.Where(x => !x.IsDefaultProfile && x.Plugins.Any(y => y.InternalName == plugin.Manifest.InternalName)))
                {
                    Task.Run(() => profile.RemoveAsync(plugin.Manifest.WorkingPluginId, false))
                        .GetAwaiter().GetResult();
                }

                Task.Run(profileManager.ApplyAllWantStatesAsync)
                    .ContinueWith(this.DisplayErrorContinuation, Locs.ErrorModal_ProfileApplyFail);
            }

            ImGui.SameLine();
            ImGui.Text(Locs.Profiles_RemoveFromAll);

            ImGui.EndPopup();
        }

        var inMultipleProfiles = !isDefaultPlugin && !isInSingleProfile;
        var inSingleNonDefaultProfileWhichIsDisabled =
            isInSingleProfile && !profilesThatWantThisPlugin.First().IsEnabled;

        if (plugin.State is PluginState.UnloadError or PluginState.LoadError or PluginState.DependencyResolutionFailed && !plugin.IsDev)
        {
            ImGuiComponents.DisabledButton(FontAwesomeIcon.Frown);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Locs.PluginButtonToolTip_UnloadFailed);
        }
        else if (this.enableDisableStatus == OperationStatus.InProgress && this.enableDisableWorkingPluginId == plugin.Manifest.WorkingPluginId)
        {
            ImGuiComponents.DisabledToggleButton(toggleId, this.loadingIndicatorKind == LoadingIndicatorKind.EnablingSingle);
        }
        else if (disabled || inMultipleProfiles || inSingleNonDefaultProfileWhichIsDisabled)
        {
            ImGuiComponents.DisabledToggleButton(toggleId, isLoadedAndUnloadable);

            if (inMultipleProfiles && ImGui.IsItemHovered())
                ImGui.SetTooltip(Locs.PluginButtonToolTip_NeedsToBeInSingleProfile);
            else if (inSingleNonDefaultProfileWhichIsDisabled && ImGui.IsItemHovered())
                ImGui.SetTooltip(Locs.PluginButtonToolTip_SingleProfileDisabled(profilesThatWantThisPlugin.First().Name));
        }
        else
        {
            if (ImGuiComponents.ToggleButton(toggleId, ref isLoadedAndUnloadable))
            {
                var applicableProfile = profilesThatWantThisPlugin.First();
                Log.Verbose("Switching {InternalName} in {Profile} to {State}",
                            plugin.InternalName, applicableProfile, isLoadedAndUnloadable);
                
                try
                {
                    // Reload the devPlugin manifest if it's a dev plugin
                    // The plugin might rely on changed values in the manifest
                    if (plugin.IsDev)
                    {
                        plugin.ReloadManifest();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Could not reload DevPlugin manifest");
                }
                
                // NOTE: We don't use the profile manager to actually handle loading/unloading here,
                // because that might cause us to show an error if a plugin we don't actually care about
                // fails to load/unload. Instead, we just do it ourselves and then update the profile.
                // There is probably a smarter way to handle this, but it's probably more code.
                if (!isLoadedAndUnloadable)
                {
                    this.enableDisableStatus = OperationStatus.InProgress;
                    this.loadingIndicatorKind = LoadingIndicatorKind.DisablingSingle;
                    this.enableDisableWorkingPluginId = plugin.Manifest.WorkingPluginId;

                    Task.Run(async () =>
                    {
                        await plugin.UnloadAsync();
                        await applicableProfile.AddOrUpdateAsync(
                            plugin.Manifest.WorkingPluginId, plugin.Manifest.InternalName, false, false);

                        notifications.AddNotification(Locs.Notifications_PluginDisabled(plugin.Manifest.Name), Locs.Notifications_PluginDisabledTitle, NotificationType.Success);
                    }).ContinueWith(t =>
                    {
                        this.enableDisableStatus = OperationStatus.Complete;
                        this.DisplayErrorContinuation(t, Locs.ErrorModal_UnloadFail(plugin.Name));
                    });
                }
                else
                {
                    async Task Enabler()
                    {
                        this.enableDisableStatus = OperationStatus.InProgress;
                        this.loadingIndicatorKind = LoadingIndicatorKind.EnablingSingle;
                        this.enableDisableWorkingPluginId = plugin.Manifest.WorkingPluginId;

                        await applicableProfile.AddOrUpdateAsync(plugin.Manifest.WorkingPluginId, plugin.Manifest.InternalName, true, false);
                        await plugin.LoadAsync(PluginLoadReason.Installer);

                        notifications.AddNotification(Locs.Notifications_PluginEnabled(plugin.Manifest.Name), Locs.Notifications_PluginEnabledTitle, NotificationType.Success);
                    }

                    var continuation = (Task t) =>
                    {
                        this.enableDisableStatus = OperationStatus.Complete;
                        this.DisplayErrorContinuation(t, Locs.ErrorModal_LoadFail(plugin.Name));
                    };

                    if (availableUpdate != default && !availableUpdate.InstalledPlugin.IsDev)
                    {
                        this.ShowUpdateModal(plugin).ContinueWith(async t =>
                        {
                            var shouldUpdate = t.Result;

                            if (shouldUpdate)
                            {
                                // We need to update the profile right here, because PM will not enable the plugin otherwise
                                await applicableProfile.AddOrUpdateAsync(plugin.Manifest.WorkingPluginId, plugin.Manifest.InternalName, true, false);
                                await this.UpdateSinglePlugin(availableUpdate);
                            }
                            else
                            {
                                _ = Task.Run(Enabler).ContinueWith(continuation);
                            }
                        });
                    }
                    else
                    {
                        _ = Task.Run(Enabler).ContinueWith(continuation);
                    }
                }
            }
        }

        // StyleModelV1.DalamudStandard.Pop();

        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(15, 0);

        if (plugin.State == PluginState.Loaded)
        {
            // Only if the plugin isn't broken.
            this.DrawOpenPluginSettingsButton(plugin);
            
            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(5, 0);
        }

        if (applicableForProfiles && config.ProfilesEnabled)
        {
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Toolbox))
            {
                ImGui.OpenPopup(profileChooserPopupName);
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Locs.PluginButtonToolTip_PickProfiles);
        }
        else if (!applicableForProfiles && config.ProfilesEnabled)
        {
            ImGui.SameLine();

            ImGui.BeginDisabled();
            ImGuiComponents.IconButton(FontAwesomeIcon.Toolbox);
            ImGui.EndDisabled();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Locs.PluginButtonToolTip_ProfilesNotSupported);
        }
    }

    private async Task<bool> UpdateSinglePlugin(AvailablePluginUpdate update)
    {
        var pluginManager = Service<PluginManager>.Get();

        this.installStatus = OperationStatus.InProgress;
        this.loadingIndicatorKind = LoadingIndicatorKind.UpdatingSingle;

        return await Task.Run(async () => await pluginManager.UpdateSinglePluginAsync(update, true, false))
                         .ContinueWith(task =>
                         {
                             // There is no need to set as Complete for an individual plugin installation
                             this.installStatus = OperationStatus.Idle;

                             if (task.IsCompletedSuccessfully &&
                                 task.Result.Status != PluginUpdateStatus.StatusKind.Success)
                             {
                                 this.ShowErrorModal(
                                     Locs.ErrorModal_SingleUpdateFail(update.UpdateManifest.Name, PluginUpdateStatus.LocalizeUpdateStatusKind(task.Result.Status)));
                                 return false;
                             }
                                 
                             return this.DisplayErrorContinuation(task, Locs.ErrorModal_SingleUpdateFail(update.UpdateManifest.Name, "Exception"));
                         });
    }

    private void DrawUpdateSinglePluginButton(AvailablePluginUpdate update)
    {
        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Download))
        {
            Task.Run(() => this.UpdateSinglePlugin(update));
        }

        if (ImGui.IsItemHovered())
        {
            var updateVersion = update.UseTesting
                                    ? update.UpdateManifest.TestingAssemblyVersion
                                    : update.UpdateManifest.AssemblyVersion;
            ImGui.SetTooltip(Locs.PluginButtonToolTip_UpdateSingle(updateVersion.ToString()));
        }
    }

    private void DrawOpenPluginSettingsButton(LocalPlugin plugin)
    {
        var hasMainUi = plugin.DalamudInterface?.UiBuilder.HasMainUi ?? false;
        var hasConfig = plugin.DalamudInterface?.UiBuilder.HasConfigUi ?? false;
        if (hasMainUi)
        {
            ImGui.SameLine();
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ArrowUpRightFromSquare, Locs.PluginButton_OpenUi))
            {
                try
                {
                    plugin.DalamudInterface.UiBuilder.OpenMain();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error during OpenMain(): {plugin.Name}");
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Locs.PluginButtonToolTip_OpenUi);
            }
        }

        if (hasConfig)
        {
            if (hasMainUi)
            {
                ImGui.SameLine();
                ImGuiHelpers.ScaledDummy(5, 0);
            }
            
            ImGui.SameLine();
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Cog, Locs.PluginButton_OpenSettings))
            {
                try
                {
                    plugin.DalamudInterface.UiBuilder.OpenConfig();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error during OpenConfig: {plugin.Name}");
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Locs.PluginButtonToolTip_OpenConfiguration);
            }
        }
    }

    private void DrawSendFeedbackButton(IPluginManifest manifest, bool isTesting, bool big)
    {
        ImGui.SameLine();

        var clicked = big ? 
                          ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Comment, Locs.FeedbackModal_Title) :
                          ImGuiComponents.IconButton(FontAwesomeIcon.Comment);
        
        if (clicked)
        {
            this.feedbackPlugin = manifest;
            this.feedbackModalOnNextFrame = true;
            this.feedbackIsTesting = isTesting;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Locs.FeedbackModal_Title);
        }
    }

    private void DrawDevPluginButtons(LocalPlugin localPlugin)
    {
        ImGui.SameLine();

        var configuration = Service<DalamudConfiguration>.Get();

        if (localPlugin is LocalDevPlugin plugin)
        {
            var isInDefaultProfile =
                Service<ProfileManager>.Get().IsInDefaultProfile(localPlugin.Manifest.WorkingPluginId);

            // https://colorswall.com/palette/2868/
            var greenColor = new Vector4(0x5C, 0xB8, 0x5C, 0xFF) / 0xFF;
            var redColor = new Vector4(0xD9, 0x53, 0x4F, 0xFF) / 0xFF;

            // Load on boot
            using (ImRaii.Disabled(!isInDefaultProfile))
            {
                ImGui.PushStyleColor(ImGuiCol.Button, plugin.StartOnBoot ? greenColor : redColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, plugin.StartOnBoot ? greenColor : redColor);

                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.PowerOff))
                {
                    plugin.StartOnBoot ^= true;
                    configuration.QueueSave();
                }

                ImGui.PopStyleColor(2);

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(isInDefaultProfile ? Locs.PluginButtonToolTip_StartOnBoot : Locs.PluginButtonToolTip_NeedsToBeInDefault);
                }
            }

            // Automatic reload
            ImGui.PushStyleColor(ImGuiCol.Button, plugin.AutomaticReload ? greenColor : redColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, plugin.AutomaticReload ? greenColor : redColor);

            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.SyncAlt))
            {
                plugin.AutomaticReload ^= true;
                configuration.QueueSave();
            }

            ImGui.PopStyleColor(2);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Locs.PluginButtonToolTip_AutomaticReloading);
            }
        }
    }

    private void DrawDeletePluginButton(LocalPlugin plugin)
    {
        /*var unloaded = plugin.State == PluginState.Unloaded || plugin.State == PluginState.LoadError;

        // When policy check fails, the plugin is never loaded
        var showButton = unloaded && (plugin.IsDev || plugin.IsOutdated || plugin.IsBanned || plugin.IsOrphaned || !plugin.CheckPolicy());

        if (!showButton)
            return;*/

        var pluginManager = Service<PluginManager>.Get();

        var devNotDeletable = plugin.IsDev && plugin.State != PluginState.Unloaded && plugin.State != PluginState.DependencyResolutionFailed;

        ImGui.SameLine();
        if (plugin.State == PluginState.Loaded || devNotDeletable)
        {
            ImGui.PushFont(InterfaceManager.IconFont);
            ImGuiComponents.DisabledButton(FontAwesomeIcon.TrashAlt.ToIconString());
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(plugin.State == PluginState.Loaded
                                     ? Locs.PluginButtonToolTip_DeletePluginLoaded
                                     : Locs.PluginButtonToolTip_DeletePluginRestricted);
            }
        }
        else
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.TrashAlt))
            {
                try
                {
                    if (plugin.IsDev)
                    {
                        plugin.DllFile.Delete();
                    }
                    else
                    {
                        plugin.ScheduleDeletion(!plugin.Manifest.ScheduledForDeletion);
                    }

                    if (plugin.State is PluginState.Unloaded or PluginState.DependencyResolutionFailed)
                    {
                        pluginManager.RemovePlugin(plugin);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Plugin installer threw an error during removal of {plugin.Name}");

                    this.ShowErrorModal(Locs.ErrorModal_DeleteFail(plugin.Name));
                }
            }

            if (ImGui.IsItemHovered())
            {
                string tooltipMessage;
                if (plugin.Manifest.ScheduledForDeletion)
                {
                    tooltipMessage = Locs.PluginButtonToolTip_DeletePluginScheduledCancel;
                }
                else if (plugin.State is PluginState.Unloaded or PluginState.DependencyResolutionFailed)
                {
                    tooltipMessage = Locs.PluginButtonToolTip_DeletePlugin;
                }
                else
                {
                    tooltipMessage = Locs.PluginButtonToolTip_DeletePluginScheduled;
                }

                ImGui.SetTooltip(tooltipMessage);
            }
        }
    }

    private void DrawVisitRepoUrlButton(string? repoUrl, bool big)
    {
        if (!string.IsNullOrEmpty(repoUrl) && repoUrl.StartsWith("https://"))
        {
            ImGui.SameLine();
            
            var clicked = big ?
                              ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Globe, "Open website") :
                              ImGuiComponents.IconButton(FontAwesomeIcon.Globe);
            if (clicked)
            {
                try
                {
                    _ = Process.Start(new ProcessStartInfo()
                    {
                        FileName = repoUrl,
                        UseShellExecute = true,
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Could not open repoUrl: {repoUrl}");
                }
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Locs.PluginButtonToolTip_VisitPluginUrl);
        }
    }

    private bool DrawPluginImages(LocalPlugin? plugin, IPluginManifest manifest, bool isThirdParty, int index)
    {
        var hasImages = this.imageCache.TryGetImages(plugin, manifest, isThirdParty, out var imageTextures);
        if (!hasImages || imageTextures.All(x => x == null))
            return false;

        const float thumbFactor = 2.7f;

        var scrollBarSize = 15;
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, scrollBarSize);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Vector4.Zero);

        var width = ImGui.GetWindowWidth();

        if (ImGui.BeginChild($"plugin{index}ImageScrolling", new Vector2(width - (70 * ImGuiHelpers.GlobalScale), (PluginImageCache.PluginImageHeight / thumbFactor) + scrollBarSize), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground))
        {
            for (var i = 0; i < imageTextures.Length; i++)
            {
                var image = imageTextures[i];
                if (image == null)
                    continue;

                ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 0);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);

                var popupId = $"plugin{index}image{i}";
                if (ImGui.BeginPopup(popupId))
                {
                    if (ImGui.ImageButton(image.ImGuiHandle, new Vector2(image.Width, image.Height)))
                        ImGui.CloseCurrentPopup();

                    ImGui.EndPopup();
                }

                ImGui.PopStyleVar(3);

                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);

                float xAct = image.Width;
                float yAct = image.Height;
                float xMax = PluginImageCache.PluginImageWidth;
                float yMax = PluginImageCache.PluginImageHeight;

                // scale image if undersized
                if (xAct < xMax && yAct < yMax)
                {
                    var scale = Math.Min(xMax / xAct, yMax / yAct);
                    xAct *= scale;
                    yAct *= scale;
                }

                var size = ImGuiHelpers.ScaledVector2(xAct / thumbFactor, yAct / thumbFactor);
                if (ImGui.ImageButton(image.ImGuiHandle, size))
                    ImGui.OpenPopup(popupId);

                ImGui.PopStyleVar();

                if (i < imageTextures.Length - 1)
                {
                    ImGui.SameLine();
                    ImGuiHelpers.ScaledDummy(5);
                    ImGui.SameLine();
                }
            }
        }

        ImGui.EndChild();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();

        return true;
    }

    private bool IsManifestFiltered(IPluginManifest manifest)
    {
        var searchString = this.searchText.ToLowerInvariant();
        var matcher = new FuzzyMatcher(searchString, MatchMode.FuzzyParts);
        var hasSearchString = !string.IsNullOrWhiteSpace(this.searchText);
        var oldApi = manifest.DalamudApiLevel < PluginManager.DalamudApiLevel;
        var installed = this.IsManifestInstalled(manifest).IsInstalled;

        if (oldApi && !hasSearchString && !installed)
            return true;

        return hasSearchString && !(
                                       (!manifest.Name.IsNullOrEmpty() && matcher.Matches(manifest.Name.ToLowerInvariant()) > 0) ||
                                       (!manifest.InternalName.IsNullOrEmpty() && matcher.Matches(manifest.InternalName.ToLowerInvariant()) > 0) ||
                                       (!manifest.Author.IsNullOrEmpty() && matcher.Matches(manifest.Author.ToLowerInvariant()) > 0) ||
                                       (!manifest.Punchline.IsNullOrEmpty() && manifest.Punchline.ToLowerInvariant().Contains(searchString)) ||
                                       (manifest.Tags != null && matcher.MatchesAny(manifest.Tags.Select(term => term.ToLowerInvariant()).ToArray()) > 0));
    }

    private (bool IsInstalled, LocalPlugin Plugin) IsManifestInstalled(IPluginManifest? manifest)
    {
        if (manifest == null) return (false, default);

        var plugin = this.pluginListInstalled.FirstOrDefault(plugin => plugin.Manifest.InternalName == manifest.InternalName);
        var isInstalled = plugin != default;

        return (isInstalled, plugin);
    }

    private void OnAvailablePluginsChanged()
    {
        var pluginManager = Service<PluginManager>.Get();

        lock (this.listLock)
        {
            // By removing installed plugins only when the available plugin list changes (basically when the window is
            // opened), plugins that have been newly installed remain in the available plugin list as installed.
            this.pluginListAvailable = pluginManager.AvailablePlugins.ToList();
            this.pluginListUpdatable = pluginManager.UpdatablePlugins.ToList();
            this.ResortPlugins();
        }

        this.UpdateCategoriesOnPluginsChange();
    }

    private void OnInstalledPluginsChanged()
    {
        var pluginManager = Service<PluginManager>.Get();
        using var pmLock = pluginManager.GetSyncScope();

        lock (this.listLock)
        {
            this.pluginListInstalled = pluginManager.InstalledPlugins.ToList();
            this.pluginListUpdatable = pluginManager.UpdatablePlugins.ToList();
            this.hasDevPlugins = this.pluginListInstalled.Any(plugin => plugin.IsDev);
            this.ResortPlugins();
        }

        this.UpdateCategoriesOnPluginsChange();
    }

    private void ResortPlugins()
    {
        switch (this.sortKind)
        {
            case PluginSortKind.Alphabetical:
                this.pluginListAvailable.Sort((p1, p2) => p1.Name.CompareTo(p2.Name));
                this.pluginListInstalled.Sort((p1, p2) => p1.Manifest.Name.CompareTo(p2.Manifest.Name));
                break;
            case PluginSortKind.DownloadCount:
                this.pluginListAvailable.Sort((p1, p2) => p2.DownloadCount.CompareTo(p1.DownloadCount));
                this.pluginListInstalled.Sort((p1, p2) => p2.Manifest.DownloadCount.CompareTo(p1.Manifest.DownloadCount));
                break;
            case PluginSortKind.LastUpdate:
                this.pluginListAvailable.Sort((p1, p2) => p2.LastUpdate.CompareTo(p1.LastUpdate));
                this.pluginListInstalled.Sort((p1, p2) =>
                {
                    // We need to get remote manifests here, as the local manifests will have the time when the current version is installed,
                    // not the actual time of the last update, as the plugin may be pending an update
                    IPluginManifest? p2Considered = this.pluginListAvailable.FirstOrDefault(x => x.InternalName == p2.InternalName);
                    p2Considered ??= p2.Manifest;
                    
                    IPluginManifest? p1Considered = this.pluginListAvailable.FirstOrDefault(x => x.InternalName == p1.InternalName);
                    p1Considered ??= p1.Manifest;

                    return p2Considered.LastUpdate.CompareTo(p1Considered.LastUpdate);
                });
                break;
            case PluginSortKind.NewOrNot:
                this.pluginListAvailable.Sort((p1, p2) => this.WasPluginSeen(p1.InternalName)
                                                              .CompareTo(this.WasPluginSeen(p2.InternalName)));
                this.pluginListInstalled.Sort((p1, p2) => this.WasPluginSeen(p1.Manifest.InternalName)
                                                              .CompareTo(this.WasPluginSeen(p2.Manifest.InternalName)));
                break;
            case PluginSortKind.NotInstalled:
                this.pluginListAvailable.Sort((p1, p2) => this.pluginListInstalled.Any(x => x.Manifest.InternalName == p1.InternalName)
                                                              .CompareTo(this.pluginListInstalled.Any(x => x.Manifest.InternalName == p2.InternalName)));
                this.pluginListInstalled.Sort((p1, p2) => p1.Manifest.Name.CompareTo(p2.Manifest.Name)); // Makes no sense for installed plugins
                break;
            case PluginSortKind.EnabledDisabled:
                this.pluginListAvailable.Sort((p1, p2) =>
                {
                    bool IsEnabled(PluginManifest manifest)
                    {
                        return this.pluginListInstalled.Any(x => x.Manifest.InternalName == manifest.InternalName);
                    }

                    return IsEnabled(p2).CompareTo(IsEnabled(p1));
                });
                this.pluginListInstalled.Sort((p1, p2) => (p2.State == PluginState.Loaded).CompareTo(p1.State == PluginState.Loaded));
                break;
            case PluginSortKind.ProfileOrNot:
                this.pluginListAvailable.Sort((p1, p2) => p1.Name.CompareTo(p2.Name));
                
                var profman = Service<ProfileManager>.Get();
                this.pluginListInstalled.Sort((p1, p2) => profman.IsInDefaultProfile(p1.Manifest.WorkingPluginId).CompareTo(profman.IsInDefaultProfile(p2.Manifest.WorkingPluginId)));
                break;
            default:
                throw new InvalidEnumArgumentException("Unknown plugin sort type.");
        }
    }

    private bool WasPluginSeen(string internalName) =>
        Service<DalamudConfiguration>.Get().SeenPluginInternalName.Contains(internalName);

    private Task ShowErrorModal(string message)
    {
        this.errorModalMessage = message;
        this.errorModalDrawing = true;
        this.errorModalOnNextFrame = true;
        this.errorModalTaskCompletionSource = new TaskCompletionSource();
        return this.errorModalTaskCompletionSource.Task;
    }

    private Task<bool> ShowUpdateModal(LocalPlugin plugin)
    {
        this.updateModalOnNextFrame = true;
        this.updateModalPlugin = plugin;
        this.updateModalTaskCompletionSource = new TaskCompletionSource<bool>();
        return this.updateModalTaskCompletionSource.Task;
    }

    private void UpdateCategoriesOnSearchChange()
    {
        if (string.IsNullOrEmpty(this.searchText))
        {
            this.categoryManager.SetCategoryHighlightsForPlugins(null);
        }
        else
        {
            var pluginsMatchingSearch = this.pluginListAvailable.Where(rm => !this.IsManifestFiltered(rm));
            this.categoryManager.SetCategoryHighlightsForPlugins(pluginsMatchingSearch);
        }
    }

    private void UpdateCategoriesOnPluginsChange()
    {
        this.categoryManager.BuildCategories(this.pluginListAvailable);
        this.UpdateCategoriesOnSearchChange();
    }

    private void DrawFontawesomeIconOutlined(FontAwesomeIcon icon, Vector4 outline, Vector4 iconColor)
    {
        var positionOffset = ImGuiHelpers.ScaledVector2(0.0f, 1.0f);
        var cursorStart = ImGui.GetCursorPos() + positionOffset;
        ImGui.PushFont(UiBuilder.IconFont);

        ImGui.PushStyleColor(ImGuiCol.Text, outline);
        foreach (var x in Enumerable.Range(-1, 3))
        {
            foreach (var y in Enumerable.Range(-1, 3))
            {
                if (x is 0 && y is 0) continue;
                
                ImGui.SetCursorPos(cursorStart + new Vector2(x, y));
                ImGui.Text(icon.ToIconString());
            }
        }
        
        ImGui.PopStyleColor();
        
        ImGui.PushStyleColor(ImGuiCol.Text, iconColor);
        ImGui.SetCursorPos(cursorStart);
        ImGui.Text(icon.ToIconString());
        ImGui.PopStyleColor();
        
        ImGui.PopFont();
        
        ImGui.SetCursorPos(ImGui.GetCursorPos() - positionOffset);
    }
    
    // Animates a tooltip when hovering over the ImGui Item before this call.
    private void VerifiedCheckmarkFadeTooltip(string source, string tooltip)
    {
        const float fadeInStartDelay = 250.0f;
        
        var isHoveringSameItem = this.verifiedCheckmarkHoveredPlugin == source;
        
        // If we just started a hover, start the timer
        if (ImGui.IsItemHovered() && !this.tooltipFadeInStopwatch.IsRunning)
        {
            this.verifiedCheckmarkHoveredPlugin = source;
            this.tooltipFadeInStopwatch.Restart();
        }
        
        // If we were last hovering this plugins item and are no longer hovered over that item, reset the timer
        if (!ImGui.IsItemHovered() && isHoveringSameItem)
        {
            this.verifiedCheckmarkHoveredPlugin = string.Empty;
            this.tooltipFadeInStopwatch.Stop();
            this.tooltipFadeEasing.Reset();
        }
        
        // If we have been hovering this item for > fadeInStartDelay milliseconds, fade in tooltip over fadeInTime milliseconds
        if (ImGui.IsItemHovered() && isHoveringSameItem && this.tooltipFadeInStopwatch.ElapsedMilliseconds >= fadeInStartDelay)
        {
            if (!this.tooltipFadeEasing.IsRunning)
                this.tooltipFadeEasing.Start();
            
            this.tooltipFadeEasing.Update();
            var fadePercent = this.tooltipFadeEasing.EasedPoint.X;
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] with { W = fadePercent });
            ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg] with { W = fadePercent });
            ImGui.SetTooltip(tooltip);
            ImGui.PopStyleColor(2);
        }
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Disregard here")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Locs")]
    internal static class Locs
    {
        #region Window Title

        public static string WindowTitle => Loc.Localize("InstallerHeader", "Plugin Installer");

        public static string WindowTitleMod_Testing => Loc.Localize("InstallerHeaderTesting", " (TESTING)");

        #endregion

        #region Header

        public static string Header_Hint => Loc.Localize("InstallerHint", "This window allows you to install and remove Dalamud plugins.\nThey are made by the community.");

        public static string Header_SearchPlaceholder => Loc.Localize("InstallerSearch", "Search");

        #endregion

        #region SortBy

        public static string SortBy_Alphabetical => Loc.Localize("InstallerAlphabetical", "Alphabetical");

        public static string SortBy_DownloadCounts => Loc.Localize("InstallerDownloadCount", "Download Count");

        public static string SortBy_LastUpdate => Loc.Localize("InstallerLastUpdate", "Last Update");

        public static string SortBy_NewOrNot => Loc.Localize("InstallerNewOrNot", "New or not");

        public static string SortBy_NotInstalled => Loc.Localize("InstallerNotInstalled", "Not Installed");

        public static string SortBy_EnabledDisabled => Loc.Localize("InstallerEnabledDisabled", "Enabled/Disabled");

        public static string SortBy_ProfileOrNot => Loc.Localize("InstallerProfileOrNot", "In a collection");

        public static string SortBy_Label => Loc.Localize("InstallerSortBy", "Sort By");

        #endregion

        #region Tab body

        public static string TabBody_LoadingPlugins => Loc.Localize("InstallerLoading", "Loading plugins...");

        public static string TabBody_DownloadFailed => Loc.Localize("InstallerDownloadFailed", "Download failed.");

        public static string TabBody_SafeMode => Loc.Localize("InstallerSafeMode", "Dalamud is running in Plugin Safe Mode, restart to activate plugins.");

        #endregion

        #region Search text

        public static string TabBody_SearchNoMatching => Loc.Localize("InstallerNoMatching", "No plugins were found matching your search.");

        public static string TabBody_SearchNoCompatible => Loc.Localize("InstallerNoCompatible", "No compatible plugins were found :( Please restart your game and try again.");

        public static string TabBody_SearchNoInstalled => Loc.Localize("InstallerNoInstalled", "No plugins are currently installed. You can install them from the \"All Plugins\" tab.");

        public static string TabBody_ChangelogNone => Loc.Localize("InstallerNoChangelog", "None of your installed plugins have a changelog.");

        public static string TabBody_ChangelogError => Loc.Localize("InstallerChangelogError", "Could not download changelogs.");

        #endregion

        #region Plugin title text

        public static string PluginTitleMod_Installed => Loc.Localize("InstallerInstalled", " (installed)");

        public static string PluginTitleMod_Disabled => Loc.Localize("InstallerDisabled", " (disabled)");

        public static string PluginTitleMod_NoService => Loc.Localize("InstallerNoService", " (decommissioned)");

        public static string PluginTitleMod_Unloaded => Loc.Localize("InstallerUnloaded", " (unloaded)");

        public static string PluginTitleMod_HasUpdate => Loc.Localize("InstallerHasUpdate", " (has update)");

        public static string PluginTitleMod_Updated => Loc.Localize("InstallerUpdated", " (updated)");

        public static string PluginTitleMod_TestingVersion => Loc.Localize("InstallerTestingVersion", " (testing version)");

        public static string PluginTitleMod_TestingExclusive => Loc.Localize("InstallerTestingExclusive", " (testing exclusive)");

        public static string PluginTitleMod_TestingAvailable => Loc.Localize("InstallerTestingAvailable", " (has testing version)");

        public static string PluginTitleMod_DevPlugin => Loc.Localize("InstallerDevPlugin", " (dev plugin)");

        public static string PluginTitleMod_UpdateFailed => Loc.Localize("InstallerUpdateFailed", " (update failed)");

        public static string PluginTitleMod_LoadError => Loc.Localize("InstallerLoadError", " (load error)");

        public static string PluginTitleMod_UnloadError => Loc.Localize("InstallerUnloadError", " (unload error)");

        public static string PluginTitleMod_OutdatedError => Loc.Localize("InstallerOutdatedError", " (outdated)");

        public static string PluginTitleMod_BannedError => Loc.Localize("InstallerBannedError", " (automatically disabled)");

        public static string PluginTitleMod_OrphanedError => Loc.Localize("InstallerOrphanedError", " (unknown repository)");

        public static string PluginTitleMod_ScheduledForDeletion => Loc.Localize("InstallerScheduledForDeletion", " (scheduled for deletion)");

        public static string PluginTitleMod_New => Loc.Localize("InstallerNewPlugin ", " New!");

        #endregion

        #region Plugin context menu

        public static string PluginContext_TestingOptIn => Loc.Localize("InstallerTestingOptIn", "Receive plugin testing versions");

        public static string PluginContext_MarkAllSeen => Loc.Localize("InstallerMarkAllSeen", "Mark all as seen");

        public static string PluginContext_HidePlugin => Loc.Localize("InstallerHidePlugin", "Hide from installer");

        public static string PluginContext_DeletePluginConfig => Loc.Localize("InstallerDeletePluginConfig", "Reset plugin data");

        public static string PluginContext_DeletePluginConfigReload => Loc.Localize("InstallerDeletePluginConfigReload", "Reset plugin data and reload");

        #endregion

        #region Plugin body

        public static string PluginBody_AuthorWithoutDownloadCount(string author) => Loc.Localize("InstallerAuthorWithoutDownloadCount", " by {0}").Format(author);

        public static string PluginBody_AuthorWithDownloadCount(string author, long count) => Loc.Localize("InstallerAuthorWithDownloadCount", " by {0} ({1} downloads)").Format(author, count.ToString("N0"));

        public static string PluginBody_AuthorWithDownloadCountUnavailable(string author) => Loc.Localize("InstallerAuthorWithDownloadCountUnavailable", " by {0}").Format(author);

        public static string PluginBody_CurrentChangeLog(Version version) => Loc.Localize("InstallerCurrentChangeLog", "Changelog (v{0})").Format(version);

        public static string PluginBody_UpdateChangeLog(Version version) => Loc.Localize("InstallerUpdateChangeLog", "Available update changelog (v{0})").Format(version);

        public static string PluginBody_DevPluginPath(string path) => Loc.Localize("InstallerDevPluginPath", "From {0}").Format(path);

        public static string PluginBody_Plugin3rdPartyRepo(string url) => Loc.Localize("InstallerPlugin3rdPartyRepo", "From custom plugin repository {0}").Format(url);

        public static string PluginBody_Outdated => Loc.Localize("InstallerOutdatedPluginBody ", "This plugin is outdated and incompatible.");
        
        public static string PluginBody_Outdated_WaitForUpdate => Loc.Localize("InstallerOutdatedWaitForUpdate", "Please wait for it to be updated by its author.");
        
        public static string PluginBody_Outdated_CanNowUpdate => Loc.Localize("InstallerOutdatedCanNowUpdate", "An update is available for installation.");

        public static string PluginBody_Orphaned => Loc.Localize("InstallerOrphanedPluginBody ", "This plugin's source repository is no longer available. You may need to reinstall it from its repository, or re-add the repository.");

        public static string PluginBody_NoServiceOfficial => Loc.Localize("InstallerNoServiceOfficialPluginBody", "This plugin is no longer being maintained. It will still work, but there will be no further updates and you can't reinstall it.");

        public static string PluginBody_NoServiceThird => Loc.Localize("InstallerNoServiceThirdPluginBody", "This plugin is no longer being serviced by its source repo. You may have to look for an updated version in another repo.");

        public static string PluginBody_LoadFailed => Loc.Localize("InstallerLoadFailedPluginBody ", "This plugin failed to load. Please contact the author for more information.");

        public static string PluginBody_Banned => Loc.Localize("InstallerBannedPluginBody ", "This plugin was automatically disabled due to incompatibilities and is not available.");

        public static string PluginBody_Policy => Loc.Localize("InstallerPolicyPluginBody ", "Plugin loads for this type of plugin were manually disabled.");

        public static string PluginBody_BannedReason(string message) =>
            Loc.Localize("InstallerBannedPluginBodyReason ", "This plugin was automatically disabled: {0}").Format(message);

        #endregion

        #region Plugin buttons

        public static string PluginButton_InstallVersion(string version) => Loc.Localize("InstallerInstall", "Install v{0}").Format(version);

        public static string PluginButton_Working => Loc.Localize("InstallerWorking", "Working");

        public static string PluginButton_Disable => Loc.Localize("InstallerDisable", "Disable");

        public static string PluginButton_Load => Loc.Localize("InstallerLoad", "Load");

        public static string PluginButton_Unload => Loc.Localize("InstallerUnload", "Unload");

        public static string PluginButton_SafeMode => Loc.Localize("InstallerSafeModeButton", "Can't change in safe mode");
        
        public static string PluginButton_OpenUi => Loc.Localize("InstallerOpenPluginUi", "Open");
        
        public static string PluginButton_OpenSettings => Loc.Localize("InstallerOpenPluginSettings", "Settings");

        #endregion

        #region Plugin button tooltips
        
        public static string PluginButtonToolTip_OpenUi => Loc.Localize("InstallerTooltipOpenUi", "Open this plugin's interface");

        public static string PluginButtonToolTip_OpenConfiguration => Loc.Localize("InstallerTooltipOpenConfig", "Open this plugin's settings");

        public static string PluginButtonToolTip_PickProfiles => Loc.Localize("InstallerPickProfiles", "Pick collections for this plugin");

        public static string PluginButtonToolTip_ProfilesNotSupported => Loc.Localize("InstallerProfilesNotSupported", "This plugin does not support collections");

        public static string PluginButtonToolTip_StartOnBoot => Loc.Localize("InstallerStartOnBoot", "Start on boot");

        public static string PluginButtonToolTip_AutomaticReloading => Loc.Localize("InstallerAutomaticReloading", "Automatic reloading");

        public static string PluginButtonToolTip_DeletePlugin => Loc.Localize("InstallerDeletePlugin ", "Delete plugin");

        public static string PluginButtonToolTip_DeletePluginRestricted => Loc.Localize("InstallerDeletePluginRestricted", "Cannot delete right now - please restart the game.");

        public static string PluginButtonToolTip_DeletePluginScheduled => Loc.Localize("InstallerDeletePluginScheduled", "Delete plugin on next restart");

        public static string PluginButtonToolTip_DeletePluginScheduledCancel => Loc.Localize("InstallerDeletePluginScheduledCancel", "Cancel scheduled deletion");

        public static string PluginButtonToolTip_DeletePluginLoaded => Loc.Localize("InstallerDeletePluginLoaded", "Disable this plugin before deleting it.");

        public static string PluginButtonToolTip_VisitPluginUrl => Loc.Localize("InstallerVisitPluginUrl", "Visit plugin URL");

        public static string PluginButtonToolTip_UpdateSingle(string version) => Loc.Localize("InstallerUpdateSingle", "Update to {0}").Format(version);

        public static string PluginButtonToolTip_UnloadFailed => Loc.Localize("InstallerLoadUnloadFailedTooltip", "Plugin load/unload failed, please restart your game and try again.");

        public static string PluginButtonToolTip_NeedsToBeInDefault => Loc.Localize("InstallerUnloadNeedsToBeInDefault", "This plugin is in one or more collections. If you want to enable or disable it, please do so by enabling or disabling the collections it is in.\nIf you want to manage it manually, remove it from all collections.");
        
        public static string PluginButtonToolTip_NeedsToBeInSingleProfile => Loc.Localize("InstallerUnloadNeedsToBeInSingleProfile", "This plugin is in more than one collection. If you want to enable or disable it, please do so by enabling or disabling the collections it is in.\nIf you want to manage it here, make sure it is only in a single collection.");
        
        public static string PluginButtonToolTip_SingleProfileDisabled(string name) => Loc.Localize("InstallerSingleProfileDisabled", "The collection '{0}' which contains this plugin is disabled.\nPlease enable it in the collections manager to toggle the plugin individually.").Format(name);

        #endregion

        #region Notifications

        public static string Notifications_PluginInstalledTitle => Loc.Localize("NotificationsPluginInstalledTitle", "Plugin installed!");

        public static string Notifications_PluginInstalled(string name) => Loc.Localize("NotificationsPluginInstalled", "'{0}' was successfully installed.").Format(name);

        public static string Notifications_PluginNotInstalledTitle => Loc.Localize("NotificationsPluginNotInstalledTitle", "Plugin not installed!");

        public static string Notifications_PluginNotInstalled(string name) => Loc.Localize("NotificationsPluginNotInstalled", "'{0}' failed to install.").Format(name);

        public static string Notifications_NoUpdatesFoundTitle => Loc.Localize("NotificationsNoUpdatesFoundTitle", "No updates found!");

        public static string Notifications_NoUpdatesFound => Loc.Localize("NotificationsNoUpdatesFound", "No updates were found.");

        public static string Notifications_UpdatesInstalledTitle => Loc.Localize("NotificationsUpdatesInstalledTitle", "Updates installed!");

        public static string Notifications_UpdatesInstalled(int count) => Loc.Localize("NotificationsUpdatesInstalled", "Updates for {0} of your plugins were installed.").Format(count);

        public static string Notifications_PluginDisabledTitle => Loc.Localize("NotificationsPluginDisabledTitle", "Plugin disabled!");

        public static string Notifications_PluginDisabled(string name) => Loc.Localize("NotificationsPluginDisabled", "'{0}' was disabled.").Format(name);

        public static string Notifications_PluginEnabledTitle => Loc.Localize("NotificationsPluginEnabledTitle", "Plugin enabled!");

        public static string Notifications_PluginEnabled(string name) => Loc.Localize("NotificationsPluginEnabled", "'{0}' was enabled.").Format(name);

        #endregion

        #region Footer

        public static string FooterButton_UpdatePlugins => Loc.Localize("InstallerUpdatePlugins", "Update plugins");

        public static string FooterButton_UpdateSafeMode => Loc.Localize("InstallerUpdateSafeMode", "Can't update in safe mode");

        public static string FooterButton_InProgress => Loc.Localize("InstallerInProgress", "Install in progress...");

        public static string FooterButton_NoUpdates => Loc.Localize("InstallerNoUpdates", "No updates found!");

        public static string FooterButton_UpdateComplete(int count) => Loc.Localize("InstallerUpdateComplete", "{0} plugins updated!").Format(count);

        public static string FooterButton_Settings => Loc.Localize("InstallerSettings", "Settings");

        public static string FooterButton_ScanDevPlugins => Loc.Localize("InstallerScanDevPlugins", "Scan Dev Plugins");

        public static string FooterButton_Close => Loc.Localize("InstallerClose", "Close");

        #endregion

        #region Update modal

        public static string UpdateModal_Title => Loc.Localize("UpdateQuestionModal", "Update Available");

        public static string UpdateModal_UpdateAvailable(string name) => Loc.Localize("UpdateModalUpdateAvailable", "An update for \"{0}\" is available.\nDo you want to update it before enabling?\nUpdates will fix bugs and incompatibilities, and may add new features.").Format(name);

        public static string UpdateModal_Yes => Loc.Localize("UpdateModalYes", "Update plugin");

        public static string UpdateModal_No => Loc.Localize("UpdateModalNo", "Just enable");

        #endregion

        #region Error modal

        public static string ErrorModal_Title => Loc.Localize("InstallerError", "Installer Error");

        public static string ErrorModal_InstallContactAuthor => Loc.Localize(
            "InstallerContactAuthor",
            "Please restart your game and try again. If this error occurs again, please contact the plugin author.");

        public static string ErrorModal_InstallFail(string name) => Loc.Localize("InstallerInstallFail", "Failed to install plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

        public static string ErrorModal_SingleUpdateFail(string name, string why) => Loc.Localize("InstallerSingleUpdateFail", "Failed to update plugin {0} ({1}).\n{2}").Format(name, why, ErrorModal_InstallContactAuthor);

        public static string ErrorModal_DeleteConfigFail(string name) => Loc.Localize("InstallerDeleteConfigFail", "Failed to reset the plugin {0}.\n\nThe plugin may not support this action. You can try deleting the configuration manually while the game is shut down - please see the FAQ.").Format(name);

        public static string ErrorModal_EnableFail(string name) => Loc.Localize("InstallerEnableFail", "Failed to enable plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

        public static string ErrorModal_DisableFail(string name) => Loc.Localize("InstallerDisableFail", "Failed to disable plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

        public static string ErrorModal_UnloadFail(string name) => Loc.Localize("InstallerUnloadFail", "Failed to unload plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

        public static string ErrorModal_LoadFail(string name) => Loc.Localize("InstallerLoadFail", "Failed to load plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

        public static string ErrorModal_DeleteFail(string name) => Loc.Localize("InstallerDeleteFail", "Failed to delete plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

        public static string ErrorModal_UpdaterFatal => Loc.Localize("InstallerUpdaterFatal", "Failed to update plugins.\nPlease restart your game and try again. If this error occurs again, please complain.");

        public static string ErrorModal_ProfileApplyFail => Loc.Localize("InstallerProfileApplyFail", "Failed to process collections.\nPlease restart your game and try again. If this error occurs again, please complain.");

        public static string ErrorModal_UpdaterFail(int failCount) => Loc.Localize("InstallerUpdaterFail", "Failed to update {0} plugins.\nPlease restart your game and try again. If this error occurs again, please complain.").Format(failCount);

        public static string ErrorModal_UpdaterFailPartial(int successCount, int failCount) => Loc.Localize("InstallerUpdaterFailPartial", "Updated {0} plugins, failed to update {1}.\nPlease restart your game and try again. If this error occurs again, please complain.").Format(successCount, failCount);

        public static string ErrorModal_HintBlame(string plugins) => Loc.Localize("InstallerErrorPluginInfo", "\n\nThe following plugins caused these issues:\n\n{0}\nYou may try removing these plugins manually and reinstalling them.").Format(plugins);

        // public static string ErrorModal_Hint => Loc.Localize("InstallerErrorHint", "The plugin installer ran into an issue or the plugin is incompatible.\nPlease restart the game and report this error on our discord.");

        #endregion

        #region Feedback Modal

        public static string FeedbackModal_Title => Loc.Localize("InstallerFeedback", "Send Feedback");

        public static string FeedbackModal_Text(string pluginName) => Loc.Localize("InstallerFeedbackInfo", "You can send feedback to the developer of \"{0}\" here.").Format(pluginName);

        public static string FeedbackModal_HasUpdate => Loc.Localize("InstallerFeedbackHasUpdate", "A new version of this plugin is available, please update before reporting bugs.");

        public static string FeedbackModal_ContactAnonymous => Loc.Localize("InstallerFeedbackContactAnonymous", "Submit feedback anonymously");

        public static string FeedbackModal_ContactAnonymousWarning => Loc.Localize("InstallerFeedbackContactAnonymousWarning", "No response will be forthcoming.\nUntick \"{0}\" and provide contact information if you need help.").Format(FeedbackModal_ContactAnonymous);

        public static string FeedbackModal_ContactInformation => Loc.Localize("InstallerFeedbackContactInfo", "Contact information");

        public static string FeedbackModal_ContactInformationHelp => Loc.Localize("InstallerFeedbackContactInfoHelp", "Discord usernames and e-mail addresses are accepted.\nIf you submit a Discord username, please join our discord server so that we can reach out to you easier.");

        public static string FeedbackModal_ContactInformationWarning => Loc.Localize("InstallerFeedbackContactInfoWarning", "Do not submit in-game character names.");

        public static string FeedbackModal_ContactInformationRequired => Loc.Localize("InstallerFeedbackContactInfoRequired", "Contact information has not been provided. We require contact information to respond to questions, or to request additional information to troubleshoot problems.");

        public static string FeedbackModal_ContactInformationDiscordButton => Loc.Localize("ContactInformationDiscordButton", "Join Goat Place Discord");

        public static string FeedbackModal_ContactInformationDiscordUrl => Loc.Localize("ContactInformationDiscordUrl", "https://goat.place/");

        public static string FeedbackModal_IncludeLastError => Loc.Localize("InstallerFeedbackIncludeLastError", "Include last error message");

        public static string FeedbackModal_IncludeLastErrorHint => Loc.Localize("InstallerFeedbackIncludeLastErrorHint", "This option can give the plugin developer useful feedback on what exactly went wrong.");

        public static string FeedbackModal_Hint => Loc.Localize("InstallerFeedbackHint", "All plugin developers will be able to see your feedback.\nPlease never include any personal or revealing information.\nIf you chose to include the last error message, information like your Windows username may be included.\n\nThe collected feedback is not stored on our end and immediately relayed to Discord.");

        public static string FeedbackModal_NotificationSuccess => Loc.Localize("InstallerFeedbackNotificationSuccess", "Your feedback was sent successfully!");

        public static string FeedbackModal_NotificationError => Loc.Localize("InstallerFeedbackNotificationError", "Your feedback could not be sent.");

        #endregion

        #region Testing Warning Modal

        public static string TestingWarningModal_Title => Loc.Localize("InstallerTestingWarning", "Warning###InstallerTestingWarning");

        public static string TestingWarningModal_DowngradeBody => Loc.Localize("InstallerTestingWarningDowngradeBody", "Take care! If you opt out of testing for a plugin, you will remain on the testing version until it is deleted and reinstalled, or the non-testing version of the plugin is updated.\nKeep in mind that you may lose the settings for this plugin if you downgrade manually.");

        #endregion

        #region Delete Plugin Config Warning Modal

        public static string DeletePluginConfigWarningModal_Title => Loc.Localize("InstallerDeletePluginConfigWarning", "Warning###InstallerDeletePluginConfigWarning");

        public static string DeletePluginConfigWarningModal_Body(string pluginName) => Loc.Localize("InstallerDeletePluginConfigWarningBody", "Are you sure you want to delete all data and configuration for {0}?").Format(pluginName);

        public static string DeletePluginConfirmWarningModal_Yes => Loc.Localize("InstallerDeletePluginConfigWarningYes", "Yes");

        public static string DeletePluginConfirmWarningModal_No => Loc.Localize("InstallerDeletePluginConfigWarningNo", "No");

        #endregion
        
        #region Plugin Update chatbox

        public static string PluginUpdateHeader_Chatbox => Loc.Localize("DalamudPluginUpdates", "Updates:");

        #endregion

        #region Error modal buttons

        public static string ErrorModalButton_Ok => Loc.Localize("OK", "OK");

        #endregion

        #region Other

        public static string SafeModeDisclaimer => Loc.Localize("SafeModeDisclaimer", "You enabled safe mode, no plugins will be loaded.\nYou may delete plugins from the \"Installed plugins\" tab.\nSimply restart your game to disable safe mode.");

        #endregion

        #region Profiles

        public static string Profiles_CouldNotAdd =>
            Loc.Localize("InstallerProfilesCouldNotAdd", "Couldn't add plugin to this collection.");

        public static string Profiles_CouldNotRemove =>
            Loc.Localize("InstallerProfilesCouldNotRemove", "Couldn't remove plugin from this collection.");

        public static string Profiles_None => Loc.Localize("InstallerProfilesNone", "No collections! Go add some in \"Plugin Collections\"!");

        public static string Profiles_RemoveFromAll =>
            Loc.Localize("InstallerProfilesRemoveFromAll", "Remove from all collections");

        #endregion
        
        #region VerifiedCheckmark

        public static string VerifiedCheckmark_VerifiedTooltip =>
            Loc.Localize("VerifiedCheckmarkVerifiedTooltip", "This plugin has been reviewed by the Dalamud team.\n" +
                                                             "It follows our technical and safety criteria, and adheres to our guidelines.");

        public static string VerifiedCheckmark_UnverifiedTooltip =>
            Loc.Localize("VerifiedCheckmarkUnverifiedTooltip", "This plugin has not been reviewed by the Dalamud team.\n" +
                                                               "We cannot take any responsibility for custom plugins and repositories.\n" +
                                                               "Please make absolutely sure that you only install plugins from developers you trust.\n\n" +
                                                               "You will not receive support for plugins installed from custom repositories on the XIVLauncher & Dalamud server.");

        #endregion
    }
}

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

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Console;
using Dalamud.Game.Command;
using Dalamud.Game.Player;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Changelog;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Enums;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Modals;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Widgets;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Textures.TextureWraps;
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

namespace Dalamud.Interface.Internal.Windows.PluginInstaller;

/// <summary>
/// Class responsible for drawing the plugin installer.
/// </summary>
internal class PluginInstallerWindow : Window, IDisposable
{
    /// <summary>
    /// Module log for Plugin Installer Window.
    /// </summary>
    public static readonly ModuleLog Log = ModuleLog.Create<PluginInstallerWindow>();



    public readonly PluginImageCache imageCache;
    public readonly PluginCategoryManager categoryManager = new();

    public readonly List<int> openPluginCollapsibles = [];

    public readonly DateTime timeLoaded;

    public readonly object listLock = new();

    public readonly ProfileManagerWidget profileManagerWidget;

    private readonly Stopwatch tooltipFadeInStopwatch = new();

    private readonly InOutCubic tooltipFadeEasing = new(TimeSpan.FromSeconds(0.2f))
    {
        Point1 = Vector2.Zero,
        Point2 = Vector2.One,
    };

    public DalamudChangelogManager? dalamudChangelogManager;
    public Task? dalamudChangelogRefreshTask;
    public CancellationTokenSource? dalamudChangelogRefreshTaskCts;

    public int updatePluginCount;
    public List<PluginUpdateStatus>? updatedPlugins;

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Makes sense like this")]
    public List<RemotePluginManifest> pluginListAvailable = [];

    public List<LocalPlugin> pluginListInstalled = [];
    public List<AvailablePluginUpdate> pluginListUpdatable = [];
    public bool hasDevPlugins;
    public bool hasHiddenPlugins;

    public string searchText = string.Empty;
    private bool isSearchTextPrefilled;

    public PluginSortKind sortKind = PluginSortKind.Alphabetical;
    public string filterText = PluginInstallerLocs.SortBy_Alphabetical;
    public bool adaptiveSort = true;

    public OperationStatus installStatus = OperationStatus.Idle;
    public OperationStatus updateStatus = OperationStatus.Idle;

    public OperationStatus enableDisableStatus = OperationStatus.Idle;
    public Guid enableDisableWorkingPluginId = Guid.Empty;

    public LoadingIndicatorKind loadingIndicatorKind = LoadingIndicatorKind.Unknown;

    private string verifiedCheckmarkHoveredPlugin = string.Empty;

    public string? staleDalamudNewVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstallerWindow"/> class.
    /// </summary>
    /// <param name="imageCache">An instance of <see cref="PluginImageCache"/> class.</param>
    /// <param name="configuration">An instance of <see cref="DalamudConfiguration"/>.</param>
    public PluginInstallerWindow(PluginImageCache imageCache, DalamudConfiguration configuration)
        : base(
            PluginInstallerLocs.WindowTitle + (configuration.DoPluginTest ? PluginInstallerLocs.WindowTitleMod_Testing : string.Empty) + "###XlPluginInstaller",
            ImGuiWindowFlags.NoScrollbar)
    {
        this.IsOpen = true;
        this.imageCache = imageCache;

        this.Size = new Vector2(830, 570);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = this.Size.Value,
        };

        this.ImageTester = new ImageTester(this);

        Service<PluginManager>.GetAsync().ContinueWith(pluginManagerTask =>
        {
            var pluginManager = pluginManagerTask.Result;

            // For debugging
            if (pluginManager.PluginsReady)
                this.OnInstalledPluginsChanged();

            this.dalamudChangelogManager = new DalamudChangelogManager(pluginManager);

            pluginManager.OnAvailablePluginsChanged += this.OnAvailablePluginsChanged;
            pluginManager.OnInstalledPluginsChanged += this.OnInstalledPluginsChanged;

            this.ImageTester.Reset();
        });

        this.timeLoaded = DateTime.Now;

        this.Proxies = new Proxies(this, this.categoryManager);
        this.profileManagerWidget = new ProfileManagerWidget(this);
        this.ErrorModal = new ErrorModal(this);
        this.FeedbackModal = new FeedbackModal(this, this.ErrorModal);
        this.PluginInstallerHeader = new PluginInstallerHeader(this, this.categoryManager);
        this.PluginInstallerPluginCategories = new PluginInstallerPluginCategories(this, this.categoryManager, this.Proxies, this.FeedbackModal);
        this.PluginInstallerFooter = new PluginInstallerFooter(this);
        this.UpdateModal = new UpdateModal(this);
        this.TestingWarningModal = new TestingWarningModal(this);
        this.DeletePluginConfigWarningModal = new DeletePluginConfigWarningModal(this);
        this.ProgressOverlay = new ProgressOverlay(this);
        this.PluginEntry = new PluginEntry(this, this.categoryManager, this.FeedbackModal);
    }

    /// <summary>
    /// Gets a value indicating whether any long-running operation is in progress.
    /// </summary>
    public bool AnyOperationInProgress => this.installStatus == OperationStatus.InProgress ||
                                          this.updateStatus == OperationStatus.InProgress ||
                                          this.enableDisableStatus == OperationStatus.InProgress;

    private PluginInstallerHeader PluginInstallerHeader { get; }

    private PluginInstallerPluginCategories PluginInstallerPluginCategories { get; }

    private PluginInstallerFooter PluginInstallerFooter { get; }

    public ErrorModal ErrorModal { get; }

    public UpdateModal UpdateModal { get; }

    public TestingWarningModal TestingWarningModal { get; }

    public DeletePluginConfigWarningModal DeletePluginConfigWarningModal { get; }

    public FeedbackModal FeedbackModal { get; }

    private ProgressOverlay ProgressOverlay { get; }

    private Proxies Proxies { get; }

    public PluginEntry PluginEntry { get; }

    public ImageTester ImageTester { get; }

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

        _ = pluginManager.ReloadAllReposAsync();
        _ = pluginManager.ScanDevPluginsAsync();

        if (!this.isSearchTextPrefilled)
        {
            this.searchText = string.Empty;
            this.sortKind = PluginSortKind.Alphabetical;
            this.filterText = PluginInstallerLocs.SortBy_Alphabetical;
        }

        this.adaptiveSort = true;

        if (this.updateStatus is OperationStatus.Complete or OperationStatus.Idle)
        {
            this.updateStatus = OperationStatus.Idle;
            this.updatePluginCount = 0;
            this.updatedPlugins = null;
        }

        this.profileManagerWidget.Reset();

        if (this.staleDalamudNewVersion == null && !Versioning.GetActiveTrack().IsNullOrEmpty())
        {
            Service<DalamudReleases>.Get().GetVersionForCurrentTrack().ContinueWith(t =>
            {
                if (!t.IsCompletedSuccessfully)
                    return;

                var versionInfo = t.Result;
                if (versionInfo.AssemblyVersion != Versioning.GetScmVersion())
                {
                    this.staleDalamudNewVersion = versionInfo.AssemblyVersion;
                }
            });
        }
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
            this.PluginInstallerHeader.Draw();
            this.PluginInstallerPluginCategories.Draw();
            this.PluginInstallerFooter.Draw();
            this.ErrorModal.Draw();
            this.UpdateModal.Draw();
            this.TestingWarningModal.Draw();
            this.DeletePluginConfigWarningModal.Draw();
            this.FeedbackModal.Draw();
            this.ProgressOverlay.Draw();
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
        if (string.IsNullOrEmpty(text))
        {
            this.isSearchTextPrefilled = false;
            this.searchText = string.Empty;
            if (this.sortKind == PluginSortKind.SearchScore)
            {
                this.sortKind = PluginSortKind.Alphabetical;
                this.filterText = PluginInstallerLocs.SortBy_Alphabetical;
                this.ResortPlugins();
            }
        }
        else
        {
            this.isSearchTextPrefilled = true;
            this.searchText = text;
            this.sortKind = PluginSortKind.SearchScore;
            this.filterText = PluginInstallerLocs.SortBy_SearchScore;
            this.ResortPlugins();
        }
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
                if (this.DisplayErrorContinuation(task, PluginInstallerLocs.ErrorModal_InstallFail(manifest.Name)))
                {
                    // Fine as long as we aren't in an error state
                    if (task.Result.State is PluginState.Loaded or PluginState.Unloaded)
                    {
                        notifications.AddNotification(PluginInstallerLocs.Notifications_PluginInstalled(manifest.Name), PluginInstallerLocs.Notifications_PluginInstalledTitle, NotificationType.Success);
                    }
                    else
                    {
                        notifications.AddNotification(PluginInstallerLocs.Notifications_PluginNotInstalled(manifest.Name), PluginInstallerLocs.Notifications_PluginNotInstalledTitle, NotificationType.Error);
                        this.ErrorModal.ShowErrorModal(PluginInstallerLocs.ErrorModal_InstallFail(manifest.Name));
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
        if (!task.IsFaulted && !task.IsCanceled)
            return true;

        var newErrorMessage = state as string;

        if (task.Exception != null)
        {
            foreach (var ex in task.Exception.InnerExceptions)
            {
                if (ex is PluginException)
                {
                    Log.Error(ex, "Plugin installer threw an error");
#if DEBUG
                    if (!string.IsNullOrEmpty(ex.Message))
                        newErrorMessage += $"\n\n{ex.Message}";
#endif
                }
                else
                {
                    Log.Error(ex, "Plugin installer threw an unexpected error");
#if DEBUG
                    if (!string.IsNullOrEmpty(ex.Message))
                        newErrorMessage += $"\n\n{ex.Message}";
#endif
                }
            }
        }

        if (task.IsCanceled) Log.Error("A task was cancelled");

        this.ErrorModal.ShowErrorModal(newErrorMessage ?? "An unknown error occurred.");

        return false;
    }

    private void SetOpenPage(PluginInstallerOpenKind kind)
    {
        switch (kind)
        {
            case PluginInstallerOpenKind.AllPlugins:
                // Plugins group
                this.categoryManager.CurrentGroupKind = PluginCategoryManager.GroupKind.Available;
                // All category
                this.categoryManager.CurrentCategoryKind = PluginCategoryManager.CategoryKind.All;
                break;

            case PluginInstallerOpenKind.InstalledPlugins:
                // Installed group
                this.categoryManager.CurrentGroupKind = PluginCategoryManager.GroupKind.Installed;
                // All category
                this.categoryManager.CurrentCategoryKind = PluginCategoryManager.CategoryKind.All;
                break;

            case PluginInstallerOpenKind.UpdateablePlugins:
                // Installed group
                this.categoryManager.CurrentGroupKind = PluginCategoryManager.GroupKind.Installed;
                // Updateable category
                this.categoryManager.CurrentCategoryKind = PluginCategoryManager.CategoryKind.UpdateablePlugins;
                break;

            case PluginInstallerOpenKind.Changelogs:
                // Changelog group
                this.categoryManager.CurrentGroupKind = PluginCategoryManager.GroupKind.Changelog;
                // Plugins category
                this.categoryManager.CurrentCategoryKind = PluginCategoryManager.CategoryKind.All;
                break;

            case PluginInstallerOpenKind.DalamudChangelogs:
                // Changelog group
                this.categoryManager.CurrentGroupKind = PluginCategoryManager.GroupKind.Changelog;
                // Dalamud category
                this.categoryManager.CurrentCategoryKind = PluginCategoryManager.CategoryKind.DalamudChangelogs;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    public bool DrawPluginCollapsingHeader(string label, LocalPlugin? plugin, IPluginManifest manifest, PluginHeaderFlags flags, Action drawContextMenuAction, int index)
    {
        var isOpen = this.openPluginCollapsibles.Contains(index);

        var sectionSize = ImGuiHelpers.GlobalScale * 66;

        ImGui.Separator();

        var childId = $"plugin_child_{label}_{plugin?.EffectiveWorkingPluginId}_{manifest.InternalName}";
        const ImGuiWindowFlags childFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        using var pluginChild = ImRaii.Child(childId, new Vector2(ImGui.GetContentRegionAvail().X, sectionSize), false, childFlags);
        if (!pluginChild)
        {
            return isOpen;
        }

        var startCursor = ImGui.GetCursorPos();

        if (flags.HasFlag(PluginHeaderFlags.IsTesting))
        {
            void DrawCautionTape(Vector2 position, Vector2 size, float stripeWidth, float skewAmount)
            {
                var wdl = ImGui.GetWindowDrawList();

                var windowPos = ImGui.GetWindowPos();
                var scroll = new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());

                var adjustedPosition = windowPos + position - scroll;

                var yellow = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.9f, 0.0f, 0.10f));
                var numStripes = (int)(size.X / stripeWidth) + (int)(size.Y / skewAmount) + 1; // +1 to cover partial stripe

                for (var i = 0; i < numStripes; i++)
                {
                    var x0 = adjustedPosition.X + i * stripeWidth;
                    var x1 = x0 + stripeWidth;
                    var y0 = adjustedPosition.Y;
                    var y1 = y0 + size.Y;

                    var p0 = new Vector2(x0, y0);
                    var p1 = new Vector2(x1, y0);
                    var p2 = new Vector2(x1 - skewAmount, y1);
                    var p3 = new Vector2(x0 - skewAmount, y1);

                    if (i % 2 != 0)
                        continue;

                    wdl.AddQuadFilled(p0, p1, p2, p3, yellow);
                }
            }

            DrawCautionTape(startCursor + new Vector2(0, 1), new Vector2(ImGui.GetWindowWidth(), sectionSize + ImGui.GetStyle().ItemSpacing.Y), ImGuiHelpers.GlobalScale * 40, 20);
        }

        ImGui.PushStyleColor(ImGuiCol.Button, isOpen ? new Vector4(0.5f, 0.5f, 0.5f, 0.1f) : Vector4.Zero);

        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.5f, 0.5f, 0.2f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 0.5f, 0.5f, 0.35f));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);

        ImGui.SetCursorPos(startCursor);

        if (ImGui.Button($"###plugin{index}CollapsibleBtn", new Vector2(ImGui.GetContentRegionAvail().X, sectionSize + ImGui.GetStyle().ItemSpacing.Y)))
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

        drawContextMenuAction.Invoke();

        ImGui.PopStyleVar();

        ImGui.PopStyleColor(3);

        ImGui.SetCursorPos(startCursor);

        var pluginDisabled = plugin is { IsWantedByAnyProfile: false };

        var iconSize = ImGuiHelpers.ScaledVector2(64, 64);
        var cursorBeforeImage = ImGui.GetCursorPos();
        var rectOffset = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();

        var overlayAlpha = 1.0f;
        if (ImGui.IsRectVisible(rectOffset + cursorBeforeImage, rectOffset + cursorBeforeImage + iconSize))
        {
            var iconTex = this.imageCache.DefaultIcon;
            var hasIcon = this.imageCache.TryGetIcon(plugin, manifest, flags.HasFlag(PluginHeaderFlags.IsThirdParty), out var cachedIconTex, out var loadedSince);
            if (hasIcon && cachedIconTex != null)
            {
                iconTex = cachedIconTex;
            }

            const float fadeTime = 0.3f;
            var iconAlpha = 1f;

            if (loadedSince.HasValue)
            {
                float EaseOutCubic(float t) => 1 - MathF.Pow(1 - t, 3);

                var secondsSinceLoad = (float)DateTime.Now.Subtract(loadedSince.Value).TotalSeconds;
                var fadeTo = pluginDisabled || flags.HasFlag(PluginHeaderFlags.IsInstallableOutdated) ? 0.4f : 1f;

                float Interp(float to) => Math.Clamp(EaseOutCubic(Math.Min(secondsSinceLoad, fadeTime) / fadeTime) * to, 0, 1);
                iconAlpha = Interp(fadeTo);
                overlayAlpha = Interp(1f);
            }

            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, iconAlpha);
            ImGui.Image(iconTex.Handle, iconSize);
            ImGui.PopStyleVar();

            ImGui.SameLine();
            ImGui.SetCursorPos(cursorBeforeImage);
        }

        var isLoaded = plugin is { IsLoaded: true };

        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, overlayAlpha);
        if (flags.HasFlag(PluginHeaderFlags.UpdateAvailable))
            ImGui.Image(this.imageCache.UpdateIcon.Handle, iconSize);
        else if ((flags.HasFlag(PluginHeaderFlags.HasTrouble) && !pluginDisabled) || flags.HasFlag(PluginHeaderFlags.IsOrphan) || flags.HasFlag(PluginHeaderFlags.IsIncompatible))
            ImGui.Image(this.imageCache.TroubleIcon.Handle, iconSize);
        else if (flags.HasFlag(PluginHeaderFlags.IsInstallableOutdated))
            ImGui.Image(this.imageCache.OutdatedInstallableIcon.Handle, iconSize);
        else if (pluginDisabled)
            ImGui.Image(this.imageCache.DisabledIcon.Handle, iconSize);
        /* NOTE: Replaced by the checkmarks for now, let's see if that is fine
        else if (isLoaded && isThirdParty)
            ImGui.Image(this.imageCache.ThirdInstalledIcon.ImGuiHandle, iconSize);
        else if (isThirdParty)
            ImGui.Image(this.imageCache.ThirdIcon.ImGuiHandle, iconSize);
        */
        else if (isLoaded)
            ImGui.Image(this.imageCache.InstalledIcon.Handle, iconSize);
        else
            ImGui.Dummy(iconSize);
        ImGui.PopStyleVar();

        ImGui.SameLine();

        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        var cursor = ImGui.GetCursorPos();

        // Name
        ImGui.Text(label);

        // Verified Checkmark or dev plugin wrench
        {
            ImGui.SameLine();
            ImGui.Text(" "u8);
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
            else if (!flags.HasFlag(PluginHeaderFlags.IsThirdParty))
            {
                this.DrawFontawesomeIconOutlined(FontAwesomeIcon.CheckCircle, verifiedOutlineColor, verifiedIconColor);
                this.VerifiedCheckmarkFadeTooltip(label, PluginInstallerLocs.VerifiedCheckmark_VerifiedTooltip);
            }
            else
            {
                this.DrawFontawesomeIconOutlined(FontAwesomeIcon.ExclamationCircle, unverifiedOutlineColor, unverifiedIconColor);
                this.VerifiedCheckmarkFadeTooltip(label, PluginInstallerLocs.VerifiedCheckmark_UnverifiedTooltip);
            }
        }

        // Download count
        var downloadCountText = manifest.DownloadCount > 0
                                    ? PluginInstallerLocs.PluginBody_AuthorWithDownloadCount(manifest.Author, manifest.DownloadCount)
                                    : PluginInstallerLocs.PluginBody_AuthorWithDownloadCountUnavailable(manifest.Author);

        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudGrey3, downloadCountText);

        if (flags.HasFlag(PluginHeaderFlags.IsNew))
        {
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.TankBlue, PluginInstallerLocs.PluginTitleMod_New);
        }

        cursor.Y += ImGui.GetTextLineHeightWithSpacing();
        ImGui.SetCursorPos(cursor);

        // Outdated warning
        if (flags.HasFlag(PluginHeaderFlags.IsIncompatible))
        {
            using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped(PluginInstallerLocs.PluginBody_Incompatible);
        }
        else if (plugin is { IsOutdated: true, IsBanned: false } || flags.HasFlag(PluginHeaderFlags.IsInstallableOutdated))
        {
            using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed);

            var bodyText = PluginInstallerLocs.PluginBody_Outdated + " ";
            if (flags.HasFlag(PluginHeaderFlags.UpdateAvailable))
                bodyText += PluginInstallerLocs.PluginBody_Outdated_CanNowUpdate;
            else
                bodyText += PluginInstallerLocs.PluginBody_Outdated_WaitForUpdate;

            ImGui.TextWrapped(bodyText);
        }
        else if (plugin is { IsBanned: true })
        {
            // Banned warning
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);

            var bodyText = plugin.BanReason.IsNullOrEmpty()
                               ? PluginInstallerLocs.PluginBody_Banned
                               : PluginInstallerLocs.PluginBody_BannedReason(plugin.BanReason);
            bodyText += " ";

            if (flags.HasFlag(PluginHeaderFlags.UpdateAvailable))
                bodyText += "\n" + PluginInstallerLocs.PluginBody_Outdated_CanNowUpdate;
            else
                bodyText += PluginInstallerLocs.PluginBody_Outdated_WaitForUpdate;

            ImGui.TextWrapped(bodyText);

            ImGui.PopStyleColor();
        }
        else if (plugin is { IsOrphaned: true })
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped(PluginInstallerLocs.PluginBody_Orphaned);
            ImGui.PopStyleColor();
        }
        else if (plugin is { IsDecommissioned: true, IsThirdParty: false })
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped(PluginInstallerLocs.PluginBody_NoServiceOfficial);
            ImGui.PopStyleColor();
        }
        else if (plugin is { IsDecommissioned: true, IsThirdParty: true })
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);

            ImGui.TextWrapped(
                flags.HasFlag(PluginHeaderFlags.MainRepoCrossUpdate)
                    ? PluginInstallerLocs.PluginBody_NoServiceThirdCrossUpdate
                    : PluginInstallerLocs.PluginBody_NoServiceThird);

            ImGui.PopStyleColor();
        }
        else if (plugin != null && !plugin.CheckPolicy())
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped(PluginInstallerLocs.PluginBody_Policy);
            ImGui.PopStyleColor();
        }
        else if (plugin is { State: PluginState.LoadError or PluginState.DependencyResolutionFailed })
        {
            // Load failed warning
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped(PluginInstallerLocs.PluginBody_LoadFailed);
            ImGui.PopStyleColor();
        }

        ImGui.SetCursorPosX(cursor.X);

        // Description
        if (plugin is null or { IsOutdated: false, IsBanned: false } && !flags.HasFlag(PluginHeaderFlags.HasTrouble))
        {
            if (!string.IsNullOrWhiteSpace(manifest.Punchline))
            {
                ImGui.TextWrapped(manifest.Punchline);
            }
            else if (!string.IsNullOrWhiteSpace(manifest.Description))
            {
                const int punchlineLen = 200;
                var firstLine = manifest.Description.Split(['\r', '\n'])[0];

                ImGui.TextWrapped(
                    firstLine.Length < punchlineLen
                        ? firstLine
                        : firstLine[..punchlineLen]);
            }
        }

        startCursor.Y += sectionSize;
        ImGui.SetCursorPos(startCursor);

        return isOpen;
    }

    public static void EnsureHaveTestingOptIn(IPluginManifest manifest)
    {
        var configuration = Service<DalamudConfiguration>.Get();

        if (configuration.PluginTestingOptIns.Any(x => x.InternalName == manifest.InternalName))
            return;

        configuration.PluginTestingOptIns.Add(new PluginTestingOptIn(manifest.InternalName));
        configuration.QueueSave();
    }

    public bool DrawPluginImages(LocalPlugin? plugin, IPluginManifest manifest, bool isThirdParty, int index)
    {
        var hasImages = this.imageCache.TryGetImages(plugin, manifest, isThirdParty, out var imageTextures);
        if (!hasImages || imageTextures.All(x => x == null))
            return false;

        const float thumbFactor = 2.7f;

        var scrollBarSize = 15;
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, scrollBarSize);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Vector4.Zero);

        var width = ImGui.GetWindowWidth();

        if (ImGui.BeginChild(
                $"plugin{index}ImageScrolling",
                new Vector2(width - (70 * ImGuiHelpers.GlobalScale), (PluginImageCache.PluginImageHeight / thumbFactor) + scrollBarSize),
                false,
                ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground))
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
                    if (ImGui.ImageButton(image.Handle, new Vector2(image.Width, image.Height)))
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
                if (ImGui.ImageButton(image.Handle, size))
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

    public bool IsManifestFiltered(IPluginManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(this.searchText))
            return false;

        return this.GetManifestSearchScore(manifest) < 1;
    }

    private int GetManifestSearchScore(IPluginManifest manifest)
    {
        var searchString = this.searchText.ToLowerInvariant();
        var matcher = new FuzzyMatcher(searchString, MatchMode.FuzzyParts);
        var scores = new List<int> { 0 };

        if (!manifest.Name.IsNullOrEmpty())
            scores.Add(matcher.Matches(manifest.Name.ToLowerInvariant()) * 110);
        if (!manifest.InternalName.IsNullOrEmpty())
            scores.Add(matcher.Matches(manifest.InternalName.ToLowerInvariant()) * 105);
        if (!manifest.Author.IsNullOrEmpty())
            scores.Add(matcher.Matches(manifest.Author.ToLowerInvariant()) * 100);
        if (!manifest.Punchline.IsNullOrEmpty())
            scores.Add(matcher.Matches(manifest.Punchline.ToLowerInvariant()) * 100);
        if (manifest.Tags != null)
            scores.Add(matcher.MatchesAny(manifest.Tags.Select(tag => tag.ToLowerInvariant()).ToArray()) * 100);

        return scores.Max();
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
        var configuration = Service<DalamudConfiguration>.Get();

        lock (this.listLock)
        {
            // By removing installed plugins only when the available plugin list changes (basically when the window is
            // opened), plugins that have been newly installed remain in the available plugin list as installed.
            this.pluginListAvailable = pluginManager.AvailablePlugins.ToList();
            this.pluginListUpdatable = pluginManager.UpdatablePlugins.ToList();
            this.ResortPlugins();
        }

        this.hasHiddenPlugins = this.pluginListAvailable.Any(x => configuration.HiddenPluginInternalName.Contains(x.InternalName));

        this.PluginInstallerHeader.UpdateCategoriesOnPluginsChange();
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

        this.PluginInstallerHeader.UpdateCategoriesOnPluginsChange();
    }

    public void ResortPlugins()
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
                this.pluginListInstalled.Sort((p1, p2) => profman.IsInDefaultProfile(p1.EffectiveWorkingPluginId).CompareTo(profman.IsInDefaultProfile(p2.EffectiveWorkingPluginId)));
                break;

            case PluginSortKind.SearchScore:
                this.pluginListAvailable = this.pluginListAvailable.OrderByDescending(this.GetManifestSearchScore).ThenBy(m => m.Name).ToList();
                this.pluginListInstalled = this.pluginListInstalled.OrderByDescending(p => this.GetManifestSearchScore(p.Manifest)).ThenBy(m => m.Name).ToList();
                break;

            default:
                throw new InvalidEnumArgumentException("Unknown plugin sort type.");
        }
    }

    public bool WasPluginSeen(string internalName) =>
        Service<DalamudConfiguration>.Get().SeenPluginInternalName.Contains(internalName);

    private void DrawFontawesomeIconOutlined(FontAwesomeIcon icon, Vector4 outline, Vector4 iconColor)
    {
        var positionOffset = ImGuiHelpers.ScaledVector2(0.0f, 1.0f);
        var cursorStart = ImGui.GetCursorPos() + positionOffset;
        ImGui.PushFont(InterfaceManager.IconFont);

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
}

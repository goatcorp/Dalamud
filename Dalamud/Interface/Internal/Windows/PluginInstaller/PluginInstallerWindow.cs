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

    public readonly Vector4 changelogBgColor = new(0.114f, 0.584f, 0.192f, 0.678f);
    public readonly Vector4 changelogTextColor = new(0.812f, 1.000f, 0.816f, 1.000f);

    private readonly PluginImageCache imageCache;
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

    #region Image Tester State

    private readonly string[] testerImagePaths = new string[5];
    private string testerIconPath = string.Empty;

    private Task<IDalamudTextureWrap>?[]? testerImages;
    private Task<IDalamudTextureWrap>? testerIcon;

    private bool testerError;
    private bool testerUpdateAvailable;

    #endregion

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

    private OperationStatus enableDisableStatus = OperationStatus.Idle;
    private Guid enableDisableWorkingPluginId = Guid.Empty;

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

    private ErrorModal ErrorModal { get; }

    private UpdateModal UpdateModal { get; }

    private TestingWarningModal TestingWarningModal { get; }

    private DeletePluginConfigWarningModal DeletePluginConfigWarningModal { get; }

    private FeedbackModal FeedbackModal { get; }

    private ProgressOverlay ProgressOverlay { get; }

    private Proxies Proxies { get; }

    public PluginEntry PluginEntry { get; }

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

        if (this.updateStatus == OperationStatus.Complete || this.updateStatus == OperationStatus.Idle)
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

    private static void EnsureHaveTestingOptIn(IPluginManifest manifest)
    {
        var configuration = Service<DalamudConfiguration>.Get();

        if (configuration.PluginTestingOptIns.Any(x => x.InternalName == manifest.InternalName))
            return;

        configuration.PluginTestingOptIns.Add(new PluginTestingOptIn(manifest.InternalName));
        configuration.QueueSave();
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

    public void DrawImageTester()
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

        var hasIcon = this.testerIcon?.IsCompletedSuccessfully is true;

        var iconTex = this.imageCache.DefaultIcon;
        if (hasIcon) iconTex = this.testerIcon.Result;

        var iconSize = ImGuiHelpers.ScaledVector2(64, 64);

        var cursorBeforeImage = ImGui.GetCursorPos();
        ImGui.Image(iconTex.Handle, iconSize);
        ImGui.SameLine();

        if (this.testerError)
        {
            ImGui.SetCursorPos(cursorBeforeImage);
            ImGui.Image(this.imageCache.TroubleIcon.Handle, iconSize);
            ImGui.SameLine();
        }
        else if (this.testerUpdateAvailable)
        {
            ImGui.SetCursorPos(cursorBeforeImage);
            ImGui.Image(this.imageCache.UpdateIcon.Handle, iconSize);
            ImGui.SameLine();
        }

        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        var cursor = ImGui.GetCursorPos();
        // Name
        ImGui.Text("My Cool Plugin"u8);

        // Download count
        var downloadCountText = PluginInstallerLocs.PluginBody_AuthorWithDownloadCount("Plugin Enjoyer", 69420);

        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudGrey3, downloadCountText);

        cursor.Y += ImGui.GetTextLineHeightWithSpacing();
        ImGui.SetCursorPos(cursor);

        // Description
        ImGui.TextWrapped("This plugin does very many great things."u8);

        startCursor.Y += sectionSize;
        ImGui.SetCursorPos(startCursor);

        ImGuiHelpers.ScaledDummy(5);

        ImGui.Indent();

        // Description
        ImGui.TextWrapped("This is a description.\nIt has multiple lines.\nTruly descriptive."u8);

        ImGuiHelpers.ScaledDummy(5);

        // Controls
        var disabled = this.updateStatus == OperationStatus.InProgress || this.installStatus == OperationStatus.InProgress;

        var versionString = "1.0.0.0";

        if (disabled)
        {
            ImGuiComponents.DisabledButton(PluginInstallerLocs.PluginButton_InstallVersion(versionString));
        }
        else
        {
            var buttonText = PluginInstallerLocs.PluginButton_InstallVersion(versionString);
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
                    "pluginTestingImageScrolling"u8,
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
                        var imageTask = this.testerImages[i];
                        if (imageTask == null)
                            continue;

                        if (!imageTask.IsCompleted)
                        {
                            ImGui.Text("Loading..."u8);
                            continue;
                        }

                        if (imageTask.Exception is not null)
                        {
                            ImGui.Text(imageTask.Exception.ToString());
                            continue;
                        }

                        var image = imageTask.Result;

                        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 0);
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);

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

        static void CheckImageSize(Task<IDalamudTextureWrap>? imageTask, int maxWidth, int maxHeight, bool requireSquare)
        {
            if (imageTask == null)
                return;

            if (!imageTask.IsCompleted)
            {
                ImGui.Text("Loading..."u8);
                return;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);

            if (imageTask.Exception is { } exc)
            {
                ImGui.Text(exc.ToString());
            }
            else
            {
                var image = imageTask.Result;
                if (image.Width > maxWidth || image.Height > maxHeight)
                {
                    ImGui.Text(
                        $"Image is larger than the maximum allowed resolution ({image.Width}x{image.Height} > {maxWidth}x{maxHeight})");
                }

                if (requireSquare && image.Width != image.Height)
                    ImGui.Text($"Image must be square! Current size: {image.Width}x{image.Height}");
            }

            ImGui.PopStyleColor();
        }

        ImGui.InputText("Icon Path"u8, ref this.testerIconPath, 1000);
        if (this.testerIcon != null)
            CheckImageSize(this.testerIcon, PluginImageCache.PluginIconWidth, PluginImageCache.PluginIconHeight, true);
        ImGui.InputText("Image 1 Path"u8, ref this.testerImagePaths[0], 1000);
        if (this.testerImages?.Length > 0)
            CheckImageSize(this.testerImages[0], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
        ImGui.InputText("Image 2 Path"u8, ref this.testerImagePaths[1], 1000);
        if (this.testerImages?.Length > 1)
            CheckImageSize(this.testerImages[1], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
        ImGui.InputText("Image 3 Path"u8, ref this.testerImagePaths[2], 1000);
        if (this.testerImages?.Length > 2)
            CheckImageSize(this.testerImages[2], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
        ImGui.InputText("Image 4 Path"u8, ref this.testerImagePaths[3], 1000);
        if (this.testerImages?.Length > 3)
            CheckImageSize(this.testerImages[3], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);
        ImGui.InputText("Image 5 Path"u8, ref this.testerImagePaths[4], 1000);
        if (this.testerImages?.Length > 4)
            CheckImageSize(this.testerImages[4], PluginImageCache.PluginImageWidth, PluginImageCache.PluginImageHeight, false);

        var tm = Service<TextureManager>.Get();
        if (ImGui.Button("Load"u8))
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
                    this.testerIcon = tm.Shared.GetFromFile(this.testerIconPath).RentAsync();
                }

                this.testerImages = new Task<IDalamudTextureWrap>?[this.testerImagePaths.Length];

                for (var i = 0; i < this.testerImagePaths.Length; i++)
                {
                    if (this.testerImagePaths[i].IsNullOrEmpty())
                        continue;

                    _ = this.testerImages[i]?.ToContentDisposedTask();
                    this.testerImages[i] = tm.Shared.GetFromFile(this.testerImagePaths[i]).RentAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load plugin images for testing.");
            }
        }

        ImGui.Checkbox("Failed"u8, ref this.testerError);
        ImGui.Checkbox("Has Update"u8, ref this.testerUpdateAvailable);
    }

    public bool DrawPluginListLoading()
    {
        var pluginManager = Service<PluginManager>.Get();

        var ready = pluginManager.PluginsReady && pluginManager.ReposReady;

        if (!ready)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, PluginInstallerLocs.TabBody_LoadingPlugins);
        }

        var failedRepos = pluginManager.Repos
                                       .Where(repo => repo.State == PluginRepositoryState.Fail)
                                       .ToArray();

        if (failedRepos.Length > 0)
        {
            var failText = PluginInstallerLocs.TabBody_DownloadFailed;
            var aggFailText = failedRepos
                              .Select(repo => $"{failText} ({repo.PluginMasterUrl})")
                              .Aggregate((s1, s2) => $"{s1}\n{s2}");

            ImGui.TextColored(ImGuiColors.DalamudRed, aggFailText);
        }

        return ready;
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

        drawContextMenuAction?.Invoke();

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

    public void DrawChangelog(IChangelogEntry log)
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
                var hasIcon = this.imageCache.TryGetIcon(pluginLog.Plugin, pluginLog.Plugin.Manifest, pluginLog.Plugin.IsThirdParty, out var cachedIconTex, out _);
                if (hasIcon && cachedIconTex != null)
                {
                    icon = cachedIconTex;
                }
            }
            else
            {
                icon = this.imageCache.CorePluginIcon;
            }

            ImGui.Image(icon.Handle, iconSize);
        }
        else
        {
            ImGui.Dummy(iconSize);
        }

        ImGui.SameLine();

        ImGuiHelpers.ScaledDummy(5);

        ImGui.SameLine();
        var cursor = ImGui.GetCursorPos();
        ImGui.Text(log.Title);

        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudGrey3, $" v{log.Version}");
        if (log.Author != null)
        {
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudGrey3, PluginInstallerLocs.PluginBody_AuthorWithoutDownloadCount(log.Author));
        }

        if (log.Date != DateTime.MinValue)
        {
            var whenText = log.Date.LocRelativePastLong();
            var whenSize = ImGui.CalcTextSize(whenText);
            ImGui.SameLine(ImGui.GetWindowWidth() - whenSize.X - (25 * ImGuiHelpers.GlobalScale));
            ImGui.TextColored(ImGuiColors.DalamudGrey3, whenText);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Published on " + log.Date.LocAbsolute());
        }

        cursor.Y += ImGui.GetTextLineHeightWithSpacing();
        ImGui.SetCursorPos(cursor);

        ImGui.TextWrapped(log.Text);

        var endCursor = ImGui.GetCursorPos();

        var sectionSize = Math.Max(
            66 * ImGuiHelpers.GlobalScale, // min size due to icons
            endCursor.Y - startCursor.Y);

        startCursor.Y += sectionSize;
        ImGui.SetCursorPos(startCursor);
    }

    public void DrawAvailablePlugin(RemotePluginManifest manifest, int index)
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var pluginManager = Service<PluginManager>.Get();

        var canUseTesting = pluginManager.CanUseTesting(manifest);
        var useTesting = pluginManager.UseTesting(manifest);
        var wasSeen = this.WasPluginSeen(manifest.InternalName);

        var effectiveApiLevel = useTesting ? manifest.TestingDalamudApiLevel.Value : manifest.DalamudApiLevel;
        var isOutdated = effectiveApiLevel < PluginManager.DalamudApiLevel;

        var isIncompatible = manifest.MinimumDalamudVersion != null &&
                             manifest.MinimumDalamudVersion > Versioning.GetAssemblyVersionParsed();

        var enableInstallButton = this.updateStatus != OperationStatus.InProgress &&
                                  this.installStatus != OperationStatus.InProgress &&
                                  !isOutdated &&
                                  !isIncompatible;

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
            label += PluginInstallerLocs.PluginTitleMod_TestingVersion;
        }
        else if (manifest.IsTestingExclusive)
        {
            label += PluginInstallerLocs.PluginTitleMod_TestingExclusive;
        }
        else if (canUseTesting)
        {
            label += PluginInstallerLocs.PluginTitleMod_TestingAvailable;
        }

        if (isIncompatible)
        {
            label += PluginInstallerLocs.PluginTitleMod_Incompatible;
        }

        var isThirdParty = manifest.SourceRepo.IsThirdParty;

        ImGui.PushID($"available{index}{manifest.InternalName}");

        var flags = PluginHeaderFlags.None;
        if (isThirdParty)
            flags |= PluginHeaderFlags.IsThirdParty;
        if (!wasSeen)
            flags |= PluginHeaderFlags.IsNew;
        if (isOutdated)
            flags |= PluginHeaderFlags.IsInstallableOutdated;
        if (useTesting || manifest.IsTestingExclusive)
            flags |= PluginHeaderFlags.IsTesting;
        if (isIncompatible)
            flags |= PluginHeaderFlags.IsIncompatible;

        if (this.DrawPluginCollapsingHeader(label, null, manifest, flags, () => this.DrawAvailablePluginContextMenu(manifest), index))
        {
            if (!wasSeen)
                configuration.SeenPluginInternalName.Add(manifest.InternalName);

            ImGuiHelpers.ScaledDummy(5);

            ImGui.Indent();

            // Installable from
            if (manifest.SourceRepo.IsThirdParty)
            {
                var repoText = PluginInstallerLocs.PluginBody_Plugin3rdPartyRepo(manifest.SourceRepo.PluginMasterUrl);
                ImGui.TextColored(ImGuiColors.DalamudGrey3, repoText);

                ImGuiHelpers.ScaledDummy(2);
            }

            // Description
            if (!string.IsNullOrWhiteSpace(manifest.Description))
            {
                ImGui.TextWrapped(manifest.Description);
            }

            ImGuiHelpers.ScaledDummy(5);

            var versionString = useTesting
                                    ? $"{manifest.TestingAssemblyVersion}"
                                    : $"{manifest.AssemblyVersion}";

            if (pluginManager.SafeMode)
            {
                ImGuiComponents.DisabledButton(PluginInstallerLocs.PluginButton_SafeMode);
            }
            else if (!enableInstallButton)
            {
                ImGuiComponents.DisabledButton(PluginInstallerLocs.PluginButton_InstallVersion(versionString));
            }
            else
            {
                using var color = ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DalamudRed.Darken(0.3f).Fade(0.4f));
                var buttonText = PluginInstallerLocs.PluginButton_InstallVersion(versionString);
                if (ImGui.Button($"{buttonText}##{buttonText}{index}"))
                {
                    this.StartInstall(manifest, useTesting);
                }
            }

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(10);
            ImGui.SameLine();

            if (this.DrawVisitRepoUrlButton(manifest.RepoUrl, true))
            {
                ImGui.SameLine();
                ImGuiHelpers.ScaledDummy(3);
            }

            if (!manifest.SourceRepo.IsThirdParty && manifest.AcceptsFeedback && !isOutdated)
            {
                ImGui.SameLine();
                this.FeedbackModal.DrawSendFeedbackButton(manifest, false, true);
            }

            ImGuiHelpers.ScaledDummy(5);

            if (this.DrawPluginImages(null, manifest, isThirdParty, index))
                ImGuiHelpers.ScaledDummy(5);

            ImGui.Unindent();
        }

        ImGui.PopID();
    }

    private void DrawAvailablePluginContextMenu(RemotePluginManifest manifest)
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var pluginManager = Service<PluginManager>.Get();

        var hasTestingVersionAvailable = configuration.DoPluginTest && manifest.IsAvailableForTesting;

        if (ImGui.BeginPopupContextItem("ItemContextMenu"u8))
        {
            if (hasTestingVersionAvailable)
            {
                if (ImGui.Selectable(PluginInstallerLocs.PluginContext_InstallTestingVersion))
                {
                    EnsureHaveTestingOptIn(manifest);
                    this.StartInstall(manifest, true);
                }

                ImGui.Separator();
            }

            if (ImGui.Selectable(PluginInstallerLocs.PluginContext_MarkAllSeen))
            {
                configuration.SeenPluginInternalName.AddRange(this.pluginListAvailable.Select(x => x.InternalName));
                configuration.QueueSave();
            }

            var isHidden = configuration.HiddenPluginInternalName.Contains(manifest.InternalName);
            switch (isHidden)
            {
                case false when ImGui.Selectable(PluginInstallerLocs.PluginContext_HidePlugin):
                    configuration.HiddenPluginInternalName.Add(manifest.InternalName);
                    configuration.QueueSave();
                    break;

                case true when ImGui.Selectable(PluginInstallerLocs.PluginContext_UnhidePlugin):
                    configuration.HiddenPluginInternalName.Remove(manifest.InternalName);
                    configuration.QueueSave();
                    break;
            }

            if (ImGui.Selectable(PluginInstallerLocs.PluginContext_DeletePluginConfig))
            {
                this.DeletePluginConfigWarningModal.ShowDeletePluginConfigWarningModal(manifest.Name).ContinueWith(t =>
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

                                this.DisplayErrorContinuation(task, PluginInstallerLocs.ErrorModal_DeleteConfigFail(manifest.InternalName));
                            });
                    }
                });
            }

            ImGui.EndPopup();
        }
    }

    public unsafe void DrawInstalledPluginContextMenu(LocalPlugin plugin, PluginTestingOptIn? optIn)
    {
        var pluginManager = Service<PluginManager>.Get();
        var configuration = Service<DalamudConfiguration>.Get();

        if (ImGui.BeginPopupContextItem("InstalledItemContextMenu"u8))
        {
            if (configuration.DoPluginTest)
            {
                var repoManifest = this.pluginListAvailable.FirstOrDefault(x => x.InternalName == plugin.Manifest.InternalName);
                if (repoManifest?.IsTestingExclusive == true)
                    ImGui.BeginDisabled();

                if (ImGui.MenuItem(PluginInstallerLocs.PluginContext_TestingOptIn, optIn != null))
                {
                    if (optIn != null)
                    {
                        configuration.PluginTestingOptIns!.Remove(optIn);

                        if (plugin.Manifest.TestingAssemblyVersion > repoManifest?.AssemblyVersion)
                        {
                            this.TestingWarningModal.ShowTestingModal();
                        }
                    }
                    else
                    {
                        EnsureHaveTestingOptIn(plugin.Manifest);
                    }

                    configuration.QueueSave();
                    _ = pluginManager.ReloadAllReposAsync();
                }

                if (repoManifest?.IsTestingExclusive == true)
                    ImGui.EndDisabled();
            }

            if (ImGui.MenuItem(PluginInstallerLocs.PluginContext_DeletePluginConfigReload))
            {
                this.DeletePluginConfigWarningModal.ShowDeletePluginConfigWarningModal(plugin.Manifest.Name, optIn != null).ContinueWith(t =>
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

                                this.DisplayErrorContinuation(task, PluginInstallerLocs.ErrorModal_DeleteConfigFail(plugin.Manifest.InternalName));
                            });
                    }
                });
            }

            ImGui.EndPopup();
        }
    }

    public void DrawPluginControlButton(LocalPlugin plugin, AvailablePluginUpdate? availableUpdate)
    {
        var notifications = Service<NotificationManager>.Get();
        var pluginManager = Service<PluginManager>.Get();
        var profileManager = Service<ProfileManager>.Get();
        var config = Service<DalamudConfiguration>.Get();

        var applicableForProfiles = plugin.Manifest.SupportsProfiles /*&& !plugin.IsDev*/;
        var profilesThatWantThisPlugin = profileManager.Profiles
                                                       .Where(x => x.WantsPlugin(plugin.EffectiveWorkingPluginId) != null)
                                                       .ToArray();
        var isInSingleProfile = profilesThatWantThisPlugin.Length == 1;
        var isDefaultPlugin = profileManager.IsInDefaultProfile(plugin.EffectiveWorkingPluginId);

        // Disable everything if the updater is running or another plugin is operating
        var disabled = this.updateStatus == OperationStatus.InProgress || this.installStatus == OperationStatus.InProgress;

        // Disable everything if the plugin is outdated
        disabled = disabled || (plugin.IsOutdated && !pluginManager.LoadAllApiLevels && !plugin.IsDev) || plugin.IsBanned;

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
                var inProfile = profile.WantsPlugin(plugin.EffectiveWorkingPluginId) != null;
                if (ImGui.Checkbox($"###profilePick{profile.Guid}{plugin.Manifest.InternalName}", ref inProfile))
                {
                    if (inProfile)
                    {
                        Task.Run(() => profile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, true))
                            .ContinueWith(this.DisplayErrorContinuation, PluginInstallerLocs.Profiles_CouldNotAdd);
                    }
                    else
                    {
                        Task.Run(() => profile.RemoveAsync(plugin.EffectiveWorkingPluginId))
                            .ContinueWith(this.DisplayErrorContinuation, PluginInstallerLocs.Profiles_CouldNotRemove);
                    }
                }

                ImGui.SameLine();

                ImGui.Text(profile.Name);

                didAny = true;
            }

            if (!didAny)
                ImGui.TextColored(ImGuiColors.DalamudGrey, PluginInstallerLocs.Profiles_None);

            ImGui.Separator();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Times))
            {
                // TODO: Work this out
                Task.Run(() => profileManager.DefaultProfile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, plugin.IsLoaded, false))
                    .GetAwaiter().GetResult();
                foreach (var profile in profileManager.Profiles.Where(x => !x.IsDefaultProfile && x.Plugins.Any(y => y.InternalName == plugin.Manifest.InternalName)))
                {
                    Task.Run(() => profile.RemoveAsync(plugin.EffectiveWorkingPluginId, false))
                        .GetAwaiter().GetResult();
                }

                Task.Run(() => profileManager.ApplyAllWantStatesAsync("Remove from profile"))
                    .ContinueWith(this.DisplayErrorContinuation, PluginInstallerLocs.ErrorModal_ProfileApplyFail);
            }

            ImGui.SameLine();
            ImGui.Text(PluginInstallerLocs.Profiles_RemoveFromAll);

            ImGui.EndPopup();
        }

        var inMultipleProfiles = !isDefaultPlugin && !isInSingleProfile;
        var inSingleNonDefaultProfileWhichIsDisabled =
            isInSingleProfile && !profilesThatWantThisPlugin.First().IsEnabled;
        var inSingleNonDefaultProfileWhichDoesNotWantActive =
            isInSingleProfile && !profilesThatWantThisPlugin.First().CheckWantsActiveFromGameState(Service<PlayerState>.Get().ContentId);

        if (plugin.State is PluginState.UnloadError or PluginState.LoadError or PluginState.DependencyResolutionFailed && !plugin.IsDev && !plugin.IsOutdated)
        {
            ImGuiComponents.DisabledToggleButton(toggleId, false);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_LoadUnloadFailed);
        }
        else if (this.enableDisableStatus == OperationStatus.InProgress && this.enableDisableWorkingPluginId == plugin.EffectiveWorkingPluginId)
        {
            ImGuiComponents.DisabledToggleButton(toggleId, this.loadingIndicatorKind == LoadingIndicatorKind.EnablingSingle);
        }
        else if (disabled || inMultipleProfiles || inSingleNonDefaultProfileWhichIsDisabled || inSingleNonDefaultProfileWhichDoesNotWantActive || pluginManager.SafeMode)
        {
            ImGuiComponents.DisabledToggleButton(toggleId, isLoadedAndUnloadable);

            if (pluginManager.SafeMode && ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_SafeMode);
            else if (inMultipleProfiles && ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_NeedsToBeInSingleProfile);
            else if (inSingleNonDefaultProfileWhichIsDisabled && ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_SingleProfileDisabled(profilesThatWantThisPlugin.First().Name));
            else if (inSingleNonDefaultProfileWhichDoesNotWantActive && ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_SingleProfileDoesNotWantActive(profilesThatWantThisPlugin.First().Name));
        }
        else
        {
            if (ImGuiComponents.ToggleButton(toggleId, ref isLoadedAndUnloadable))
            {
                var applicableProfile = profilesThatWantThisPlugin.First();
                Log.Verbose(
                    "Switching {InternalName} in {Profile} to {State}",
                    plugin.InternalName,
                    applicableProfile,
                    isLoadedAndUnloadable);

                try
                {
                    // Reload the devPlugin manifest if it's a dev plugin
                    // The plugin might rely on changed values in the manifest
                    if (plugin is LocalDevPlugin devPlugin)
                    {
                        devPlugin.ReloadManifest();
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
                    this.enableDisableWorkingPluginId = plugin.EffectiveWorkingPluginId;

                    Task.Run(async () =>
                    {
                        await plugin.UnloadAsync();
                        await applicableProfile.AddOrUpdateAsync(
                            plugin.EffectiveWorkingPluginId,
                            plugin.Manifest.InternalName,
                            false,
                            false);

                        notifications.AddNotification(PluginInstallerLocs.Notifications_PluginDisabled(plugin.Manifest.Name), PluginInstallerLocs.Notifications_PluginDisabledTitle, NotificationType.Success);
                    }).ContinueWith(t =>
                    {
                        this.enableDisableStatus = OperationStatus.Complete;
                        this.DisplayErrorContinuation(t, PluginInstallerLocs.ErrorModal_UnloadFail(plugin.Name));
                    });
                }
                else
                {
                    async Task Enabler()
                    {
                        this.enableDisableStatus = OperationStatus.InProgress;
                        this.loadingIndicatorKind = LoadingIndicatorKind.EnablingSingle;
                        this.enableDisableWorkingPluginId = plugin.EffectiveWorkingPluginId;

                        await applicableProfile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, true, false);
                        await plugin.LoadAsync(PluginLoadReason.Installer);

                        notifications.AddNotification(PluginInstallerLocs.Notifications_PluginEnabled(plugin.Manifest.Name), PluginInstallerLocs.Notifications_PluginEnabledTitle, NotificationType.Success);
                    }

                    var continuation = (Task t) =>
                    {
                        this.enableDisableStatus = OperationStatus.Complete;
                        this.DisplayErrorContinuation(t, PluginInstallerLocs.ErrorModal_LoadFail(plugin.Name));
                    };

                    if (availableUpdate != default && !availableUpdate.InstalledPlugin.IsDev)
                    {
                        this.UpdateModal.ShowUpdateModal(plugin).ContinueWith(async t =>
                        {
                            var shouldUpdate = t.Result;

                            if (shouldUpdate)
                            {
                                // We need to update the profile right here, because PM will not enable the plugin otherwise
                                await applicableProfile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, true, false);
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
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_PickProfiles);
        }
        else if (!applicableForProfiles && config.ProfilesEnabled)
        {
            ImGui.SameLine();

            ImGui.BeginDisabled();
            ImGuiComponents.IconButton(FontAwesomeIcon.Toolbox);
            ImGui.EndDisabled();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_ProfilesNotSupported);
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
                                 this.ErrorModal.ShowErrorModal(
                                     PluginInstallerLocs.ErrorModal_SingleUpdateFail(update.UpdateManifest.Name, PluginUpdateStatus.LocalizeUpdateStatusKind(task.Result.Status)));
                                 return false;
                             }

                             return this.DisplayErrorContinuation(task, PluginInstallerLocs.ErrorModal_SingleUpdateFail(update.UpdateManifest.Name, "Exception"));
                         });
    }

    public void DrawUpdateSinglePluginButton(AvailablePluginUpdate update)
    {
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Download))
        {
            Task.Run(() => this.UpdateSinglePlugin(update));
        }

        if (ImGui.IsItemHovered())
        {
            var updateVersion = update.UseTesting
                                    ? update.UpdateManifest.TestingAssemblyVersion
                                    : update.UpdateManifest.AssemblyVersion;
            ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_UpdateSingle(updateVersion.ToString()));
        }
    }

    private void DrawOpenPluginSettingsButton(LocalPlugin plugin)
    {
        var hasMainUi = plugin.DalamudInterface?.LocalUiBuilder.HasMainUi ?? false;
        var hasConfig = plugin.DalamudInterface?.LocalUiBuilder.HasConfigUi ?? false;
        if (hasMainUi)
        {
            ImGui.SameLine();
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ArrowUpRightFromSquare, PluginInstallerLocs.PluginButton_OpenUi))
            {
                try
                {
                    plugin.DalamudInterface.LocalUiBuilder.OpenMain();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error during OpenMain(): {plugin.Name}");
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_OpenUi);
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
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Cog, PluginInstallerLocs.PluginButton_OpenSettings))
            {
                try
                {
                    plugin.DalamudInterface.LocalUiBuilder.OpenConfig();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error during OpenConfig: {plugin.Name}");
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_OpenConfiguration);
            }
        }
    }

    public void DrawDevPluginValidationIssues(LocalDevPlugin devPlugin)
    {
        if (!devPlugin.IsLoaded)
        {
            ImGui.TextColoredWrapped(ImGuiColors.DalamudGrey, "You have to load this plugin to see validation issues."u8);
        }
        else
        {
            var problems = PluginValidator.CheckForProblems(devPlugin);
            if (problems.Count == 0)
            {
                ImGui.PushFont(InterfaceManager.IconFont);
                ImGui.Text(FontAwesomeIcon.Check.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.TextColoredWrapped(ImGuiColors.HealerGreen, "No validation issues found in this plugin!"u8);
            }
            else
            {
                var numValidProblems = problems.Count(problem => devPlugin.DismissedValidationProblems.All(name => name != problem.GetType().Name));
                var shouldBother = numValidProblems > 0;
                var validationIssuesText = shouldBother ? $"Found {problems.Count} validation issue{(problems.Count > 1 ? "s" : string.Empty)} in this plugin!" : $"{problems.Count} dismissed validation issue{(problems.Count > 1 ? "s" : string.Empty)} in this plugin.";

                using var col = ImRaii.PushColor(ImGuiCol.Text, shouldBother ? ImGuiColors.DalamudOrange : ImGuiColors.DalamudGrey);
                using var tree = ImRaii.TreeNode($"{validationIssuesText}###validationIssueCollapsible");
                if (tree.Success)
                {
                    foreach (var problem in problems)
                    {
                        var thisProblemIsDismissed = devPlugin.DismissedValidationProblems.Contains(problem.GetType().Name);

                        if (!thisProblemIsDismissed)
                        {
                            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudWhite))
                            {
                                if (ImGuiComponents.IconButton(
                                        $"##dismissValidationIssue{problem.GetType().Name}",
                                        FontAwesomeIcon.TimesCircle))
                                {
                                    devPlugin.DismissedValidationProblems.Add(problem.GetType().Name);
                                    Service<DalamudConfiguration>.Get().QueueSave();
                                }

                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.SetTooltip("Dismiss this issue"u8);
                                }
                            }

                            ImGui.SameLine();
                        }

                        var iconColor = problem.Severity switch
                        {
                            PluginValidator.ValidationSeverity.Fatal => ImGuiColors.DalamudRed,
                            PluginValidator.ValidationSeverity.Warning => ImGuiColors.DalamudOrange,
                            PluginValidator.ValidationSeverity.Information => ImGuiColors.TankBlue,
                            _ => ImGuiColors.DalamudGrey,
                        };

                        using (ImRaii.PushColor(ImGuiCol.Text, iconColor))
                        using (ImRaii.PushFont(InterfaceManager.IconFont))
                        {
                            switch (problem.Severity)
                            {
                                case PluginValidator.ValidationSeverity.Fatal:
                                    ImGui.Text(FontAwesomeIcon.TimesCircle.ToIconString());
                                    break;

                                case PluginValidator.ValidationSeverity.Warning:
                                    ImGui.Text(FontAwesomeIcon.ExclamationTriangle.ToIconString());
                                    break;

                                case PluginValidator.ValidationSeverity.Information:
                                    ImGui.Text(FontAwesomeIcon.InfoCircle.ToIconString());
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        ImGui.SameLine();

                        using (ImRaii.PushColor(ImGuiCol.Text, thisProblemIsDismissed ? ImGuiColors.DalamudGrey : ImGuiColors.DalamudWhite))
                        {
                            ImGui.TextWrapped(problem.GetLocalizedDescription());
                        }
                    }
                }
            }
        }
    }

    public void DrawDevPluginButtons(LocalPlugin localPlugin)
    {
        ImGui.SameLine();

        var configuration = Service<DalamudConfiguration>.Get();

        if (localPlugin is LocalDevPlugin plugin)
        {
            var isInDefaultProfile =
                Service<ProfileManager>.Get().IsInDefaultProfile(localPlugin.EffectiveWorkingPluginId);

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
                    ImGui.SetTooltip(isInDefaultProfile ? PluginInstallerLocs.PluginButtonToolTip_StartOnBoot : PluginInstallerLocs.PluginButtonToolTip_NeedsToBeInDefault);
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
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_AutomaticReloading);
            }

            // Error Notifications
            ImGui.PushStyleColor(ImGuiCol.Button, plugin.NotifyForErrors ? greenColor : redColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, plugin.NotifyForErrors ? greenColor : redColor);

            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Bolt))
            {
                plugin.NotifyForErrors ^= true;
                configuration.QueueSave();
            }

            ImGui.PopStyleColor(2);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_NotifyForErrors);
            }
        }
    }

    public void DrawDeletePluginButton(LocalPlugin plugin)
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
                ImGui.SetTooltip(
                    plugin.State == PluginState.Loaded
                        ? PluginInstallerLocs.PluginButtonToolTip_DeletePluginLoaded
                        : PluginInstallerLocs.PluginButtonToolTip_DeletePluginRestricted);
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

                    this.ErrorModal.ShowErrorModal(PluginInstallerLocs.ErrorModal_DeleteFail(plugin.Name));
                }
            }

            if (ImGui.IsItemHovered())
            {
                string tooltipMessage;
                if (plugin.Manifest.ScheduledForDeletion)
                {
                    tooltipMessage = PluginInstallerLocs.PluginButtonToolTip_DeletePluginScheduledCancel;
                }
                else if (plugin.State is PluginState.Unloaded or PluginState.DependencyResolutionFailed)
                {
                    tooltipMessage = PluginInstallerLocs.PluginButtonToolTip_DeletePlugin;
                }
                else
                {
                    tooltipMessage = PluginInstallerLocs.PluginButtonToolTip_DeletePluginScheduled;
                }

                ImGui.SetTooltip(tooltipMessage);
            }
        }
    }

    public bool DrawVisitRepoUrlButton(string? repoUrl, bool big)
    {
        if (!string.IsNullOrEmpty(repoUrl) && repoUrl.StartsWith("https://"))
        {
            ImGui.SameLine();

            var clicked = big ? ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Globe, "Open website") : ImGuiComponents.IconButton(FontAwesomeIcon.Globe);
            if (clicked)
            {
                try
                {
                    _ = Process.Start(
                        new ProcessStartInfo()
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
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_VisitPluginUrl);

            return true;
        }

        return false;
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

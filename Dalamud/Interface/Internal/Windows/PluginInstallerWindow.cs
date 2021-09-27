using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Game.Command;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Plugin;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Exceptions;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// Class responsible for drawing the plugin installer.
    /// </summary>
    internal class PluginInstallerWindow : Window, IDisposable
    {
        private const int PluginImageWidth = 730;
        private const int PluginImageHeight = 380;

        private const int PluginIconWidth = 512;
        private const int PluginIconHeight = 512;

        // TODO: Change back to master after release
        private const string MainRepoImageUrl = "https://raw.githubusercontent.com/goatcorp/DalamudPlugins/api4/{0}/{1}/images/{2}";

        private static readonly ModuleLog Log = new("PLUGINW");

        private readonly TextureWrap defaultIcon;
        private readonly TextureWrap troubleIcon;
        private readonly TextureWrap updateIcon;

        private readonly HttpClient httpClient = new();

        #region Image Tester State

        private string[] testerImagePaths = new string[5];
        private string testerIconPath = string.Empty;

        private TextureWrap?[] testerImages;
        private TextureWrap? testerIcon;

        private bool testerError = false;
        private bool testerUpdateAvailable = false;

        #endregion

        private bool errorModalDrawing = true;
        private bool errorModalOnNextFrame = false;
        private string errorModalMessage = string.Empty;

        private int updatePluginCount = 0;
        private List<PluginUpdateStatus>? updatedPlugins;

        private List<RemotePluginManifest> pluginListAvailable = new();
        private List<LocalPlugin> pluginListInstalled = new();
        private List<AvailablePluginUpdate> pluginListUpdatable = new();
        private bool hasDevPlugins = false;

        private Dictionary<string, TextureWrap?> pluginIconMap = new();
        private Dictionary<string, TextureWrap?[]> pluginImagesMap = new();

        private string searchText = string.Empty;

        private PluginSortKind sortKind = PluginSortKind.Alphabetical;
        private string filterText = Locs.SortBy_Alphabetical;

        private OperationStatus installStatus = OperationStatus.Idle;
        private OperationStatus updateStatus = OperationStatus.Idle;

        private List<int> openPluginCollapsibles = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstallerWindow"/> class.
        /// </summary>
        public PluginInstallerWindow()
            : base(
                Locs.WindowTitle + (Service<DalamudConfiguration>.Get().DoPluginTest ? Locs.WindowTitleMod_Testing : string.Empty) + "###XlPluginInstaller",
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar)
        {
            this.IsOpen = true;

            this.Size = new Vector2(830, 570);
            this.SizeCondition = ImGuiCond.FirstUseEver;

            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = this.Size.Value,
                MaximumSize = new Vector2(5000, 5000),
            };

            var dalamud = Service<Dalamud>.Get();
            var interfaceManager = Service<InterfaceManager>.Get();
            var pluginManager = Service<PluginManager>.Get();

            // For debugging
            if (pluginManager.PluginsReady)
                this.OnInstalledPluginsChanged();

            pluginManager.OnAvailablePluginsChanged += this.OnAvailablePluginsChanged;
            pluginManager.OnInstalledPluginsChanged += this.OnInstalledPluginsChanged;

            this.defaultIcon = interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "defaultIcon.png"));

            this.troubleIcon = interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "troubleIcon.png"));

            this.updateIcon = interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "updateIcon.png"));

            for (var i = 0; i < this.testerImagePaths.Length; i++)
            {
                this.testerImagePaths[i] = string.Empty;
            }
        }

        private enum OperationStatus
        {
            Idle,
            InProgress,
            Complete,
        }

        private enum PluginSortKind
        {
            Alphabetical,
            DownloadCount,
            LastUpdate,
            NewOrNot,
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            var pluginManager = Service<PluginManager>.Get();

            pluginManager.OnAvailablePluginsChanged -= this.OnAvailablePluginsChanged;
            pluginManager.OnInstalledPluginsChanged -= this.OnInstalledPluginsChanged;

            this.defaultIcon.Dispose();
            this.troubleIcon.Dispose();
            this.updateIcon.Dispose();
        }

        /// <inheritdoc/>
        public override void OnOpen()
        {
            var pluginManager = Service<PluginManager>.Get();

            _ = pluginManager.ReloadPluginMastersAsync();

            this.updatePluginCount = 0;
            this.updatedPlugins = null;

            this.searchText = string.Empty;
            this.sortKind = PluginSortKind.Alphabetical;
            this.filterText = Locs.SortBy_Alphabetical;
        }

        /// <inheritdoc/>
        public override void OnClose()
        {
            Service<DalamudConfiguration>.Get().Save();
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            this.DrawHeader();
            this.DrawPluginTabBar();
            this.DrawFooter();
            this.DrawErrorModal();
        }

        /// <summary>
        /// Clear the cache of downloaded icons.
        /// </summary>
        public void ClearIconCache()
        {
            this.pluginIconMap.Clear();
            this.pluginImagesMap.Clear();
        }

        private static string? GetPluginIconUrl(PluginManifest manifest, bool isThirdParty, bool isTesting)
        {
            if (isThirdParty)
                return manifest.IconUrl;

            return MainRepoImageUrl.Format(isTesting ? "testing" : "plugins", manifest.InternalName, "icon.png");
        }

        private static List<string?> GetPluginImageUrls(PluginManifest manifest, bool isThirdParty, bool isTesting)
        {
            if (isThirdParty)
            {
                if (manifest.ImageUrls.Count > 5)
                {
                    Log.Warning($"Plugin {manifest.InternalName} has too many images");
                    return manifest.ImageUrls.Take(5).ToList();
                }

                return manifest.ImageUrls;
            }

            var output = new List<string>();
            for (var i = 1; i <= 5; i++)
            {
                output.Add(MainRepoImageUrl.Format(isTesting ? "testing" : "plugins", manifest.InternalName, $"image{i}.png"));
            }

            return output;
        }

        private static FileInfo? GetPluginIconFileInfo(LocalPlugin? plugin)
        {
            var pluginDir = plugin.DllFile.Directory;

            var devUrl = new FileInfo(Path.Combine(pluginDir.FullName, "images", "icon.png"));
            if (devUrl.Exists)
                return devUrl;

            return null;
        }

        private static List<FileInfo?> GetPluginImageFileInfos(LocalPlugin? plugin)
        {
            var pluginDir = plugin.DllFile.Directory;
            var output = new List<FileInfo>();
            for (var i = 1; i <= 5; i++)
            {
                var devUrl = new FileInfo(Path.Combine(pluginDir.FullName, "images", $"image{i}.png"));
                if (devUrl.Exists)
                {
                    output.Add(devUrl);
                    continue;
                }

                output.Add(null);
            }

            return output;
        }

        private void DrawHeader()
        {
            var style = ImGui.GetStyle();
            var windowSize = ImGui.GetWindowContentRegionMax();

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (5 * ImGuiHelpers.GlobalScale));

            var searchInputWidth = 240 * ImGuiHelpers.GlobalScale;

            var sortByText = Locs.SortBy_Label;
            var sortByTextWidth = ImGui.CalcTextSize(sortByText).X;
            var sortSelectables = new (string Localization, PluginSortKind SortKind)[]
            {
                (Locs.SortBy_Alphabetical, PluginSortKind.Alphabetical),
                (Locs.SortBy_DownloadCounts, PluginSortKind.DownloadCount),
                (Locs.SortBy_LastUpdate, PluginSortKind.LastUpdate),
                (Locs.SortBy_NewOrNot, PluginSortKind.NewOrNot),
            };
            var longestSelectableWidth = sortSelectables.Select(t => ImGui.CalcTextSize(t.Localization).X).Max();
            var selectableWidth = longestSelectableWidth + (style.FramePadding.X * 2);  // This does not include the label
            var sortSelectWidth = selectableWidth + sortByTextWidth + style.ItemInnerSpacing.X;  // Item spacing between the selectable and the label

            var headerText = Locs.Header_Hint;
            var headerTextSize = ImGui.CalcTextSize(headerText);
            ImGui.Text(headerText);

            ImGui.SameLine();

            // Shift down a little to align with the middle of the header text
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (headerTextSize.Y / 4) - 2);

            ImGui.SetCursorPosX(windowSize.X - sortSelectWidth - style.ItemSpacing.X - searchInputWidth);
            ImGui.SetNextItemWidth(searchInputWidth);
            ImGui.InputTextWithHint("###XlPluginInstaller_Search", Locs.Header_SearchPlaceholder, ref this.searchText, 100);

            ImGui.SameLine();
            ImGui.SetCursorPosX(windowSize.X - sortSelectWidth);
            ImGui.SetNextItemWidth(selectableWidth);
            if (ImGui.BeginCombo(sortByText, this.filterText, ImGuiComboFlags.NoArrowButton))
            {
                foreach (var selectable in sortSelectables)
                {
                    if (ImGui.Selectable(selectable.Localization))
                    {
                        this.sortKind = selectable.SortKind;
                        this.filterText = selectable.Localization;

                        this.ResortPlugins();
                    }
                }

                ImGui.EndCombo();
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
                configuration.Save();
            }
        }

        private void DrawUpdatePluginsButton()
        {
            var pluginManager = Service<PluginManager>.Get();
            var notifications = Service<NotificationManager>.Get();

            var ready = pluginManager.PluginsReady && pluginManager.ReposReady;

            if (!ready || this.updateStatus == OperationStatus.InProgress || this.installStatus == OperationStatus.InProgress)
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

                    Task.Run(() => pluginManager.UpdatePluginsAsync())
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
                                this.updatedPlugins = task.Result.Where(res => res.WasUpdated).ToList();
                                this.updatePluginCount = this.updatedPlugins.Count;

                                var errorPlugins = task.Result.Where(res => !res.WasUpdated).ToList();
                                var errorPluginCount = errorPlugins.Count;

                                if (errorPluginCount > 0)
                                {
                                    var errorMessage = this.updatePluginCount > 0
                                        ? Locs.ErrorModal_UpdaterFailPartial(this.updatePluginCount, errorPluginCount)
                                        : Locs.ErrorModal_UpdaterFail(errorPluginCount);

                                    var hintInsert = errorPlugins
                                        .Aggregate(string.Empty, (current, pluginUpdateStatus) => $"{current}* {pluginUpdateStatus.InternalName}\n")
                                        .TrimEnd();
                                    errorMessage += Locs.ErrorModal_HintBlame(hintInsert);

                                    this.DisplayErrorContinuation(task, errorMessage);
                                }

                                if (this.updatePluginCount > 0)
                                {
                                    pluginManager.PrintUpdatedPlugins(this.updatedPlugins, Locs.PluginUpdateHeader_Chatbox);
                                    notifications.AddNotification(Locs.Notifications_UpdatesInstalled(this.updatePluginCount), Locs.Notifications_UpdatesInstalledTitle, NotificationType.Success);
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
                }

                ImGui.EndPopup();
            }

            if (this.errorModalOnNextFrame)
            {
                ImGui.OpenPopup(modalTitle);
                this.errorModalOnNextFrame = false;
            }
        }

        private void DrawPluginTabBar()
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (5 * ImGuiHelpers.GlobalScale));

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(1, 3));

            if (ImGui.BeginTabBar("PluginsTabBar", ImGuiTabBarFlags.NoTooltip))
            {
                this.DrawPluginTab(Locs.TabTitle_AvailablePlugins, this.DrawAvailablePluginList);
                this.DrawPluginTab(Locs.TabTitle_InstalledPlugins, this.DrawInstalledPluginList);

                if (this.hasDevPlugins)
                {
                    this.DrawPluginTab(Locs.TabTitle_InstalledDevPlugins, this.DrawInstalledDevPluginList);
                    this.DrawPluginTab("Image/Icon Tester", this.DrawImageTester);
                }
            }

            ImGui.PopStyleVar();
        }

        private void DrawPluginTab(string title, Action drawPluginList)
        {
            if (ImGui.BeginTabItem(title))
            {
                ImGui.BeginChild($"Scrolling{title}", ImGuiHelpers.ScaledVector2(0, -30), true, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoBackground);

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5);

                var ready = this.DrawPluginListLoading();

                if (ready)
                {
                    drawPluginList();
                }

                ImGui.EndChild();

                ImGui.EndTabItem();
            }
        }

        private void DrawAvailablePluginList()
        {
            var pluginList = this.pluginListAvailable;

            if (pluginList.Count == 0)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.TabBody_SearchNoCompatible);
                return;
            }

            var filteredList = pluginList
                .Where(rm => !this.IsManifestFiltered(rm))
                .ToList();

            if (filteredList.Count == 0)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey2, Locs.TabBody_SearchNoMatching);
                return;
            }

            var i = 0;
            foreach (var manifest in filteredList)
            {
                var (isInstalled, plugin) = this.IsManifestInstalled(manifest);

                ImGui.PushID($"{manifest.InternalName}{manifest.AssemblyVersion}");

                if (isInstalled)
                {
                    this.DrawInstalledPlugin(plugin, i++, true);
                }
                else
                {
                    this.DrawAvailablePlugin(manifest, i++);
                }

                ImGui.PopID();
            }
        }

        private void DrawInstalledPluginList()
        {
            var pluginList = this.pluginListInstalled;

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

            var iconTex = this.defaultIcon;
            if (hasIcon) iconTex = this.testerIcon;

            var iconSize = ImGuiHelpers.ScaledVector2(64, 64);

            var cursorBeforeImage = ImGui.GetCursorPos();
            ImGui.Image(iconTex.ImGuiHandle, iconSize);
            ImGui.SameLine();

            if (this.testerError)
            {
                ImGui.SetCursorPos(cursorBeforeImage);
                ImGui.Image(this.troubleIcon.ImGuiHandle, iconSize);
                ImGui.SameLine();
            }
            else if (this.testerUpdateAvailable)
            {
                ImGui.SetCursorPos(cursorBeforeImage);
                ImGui.Image(this.updateIcon.ImGuiHandle, iconSize);
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

            this.DrawVisitRepoUrlButton("https://google.com");

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
                    new Vector2(width - (70 * ImGuiHelpers.GlobalScale), (PluginImageHeight / thumbFactor) + scrollBarSize),
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

                            if (ImGui.ImageButton(image.ImGuiHandle, ImGuiHelpers.ScaledVector2(image.Width / thumbFactor, image.Height / thumbFactor)))
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

            ImGui.InputText("Icon Path", ref this.testerIconPath, 1000);
            ImGui.InputText("Image 1 Path", ref this.testerImagePaths[0], 1000);
            ImGui.InputText("Image 2 Path", ref this.testerImagePaths[1], 1000);
            ImGui.InputText("Image 3 Path", ref this.testerImagePaths[2], 1000);
            ImGui.InputText("Image 4 Path", ref this.testerImagePaths[3], 1000);
            ImGui.InputText("Image 5 Path", ref this.testerImagePaths[4], 1000);

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

                    this.testerImages = new TextureWrap[this.testerImagePaths.Length];

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

            if (pluginManager.SafeMode)
            {
                ImGui.Text(Locs.TabBody_SafeMode);
                return false;
            }

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

        private bool DrawPluginCollapsingHeader(string label, LocalPlugin? plugin, PluginManifest manifest, bool isThirdParty, bool trouble, bool updateAvailable, bool isNew, Action drawContextMenuAction, int index)
        {
            ImGui.Separator();

            var isOpen = this.openPluginCollapsibles.Contains(index);

            var sectionSize = ImGuiHelpers.GlobalScale * 66;
            var startCursor = ImGui.GetCursorPos();

            ImGui.PushStyleColor(ImGuiCol.Button, isOpen ? new Vector4(0.5f, 0.5f, 0.5f, 0.1f) : Vector4.Zero);

            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.5f, 0.5f, 0.2f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 0.5f, 0.5f, 0.35f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);

            if (ImGui.Button($"###plugin{index}CollapsibleBtn", new Vector2(ImGui.GetWindowWidth() - (ImGuiHelpers.GlobalScale * 35), sectionSize)))
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

            var iconTex = this.defaultIcon;
            var hasIcon = this.pluginIconMap.TryGetValue(manifest.InternalName, out var cachedIconTex);
            if (!hasIcon)
            {
                this.pluginIconMap.Add(manifest.InternalName, null);
                Task.Run(async () => await this.DownloadPluginIconAsync(plugin, manifest, isThirdParty));
            }
            else if (cachedIconTex != null)
            {
                iconTex = cachedIconTex;
            }
            else
            {
                // nothing
            }

            var iconSize = ImGuiHelpers.ScaledVector2(64, 64);

            var cursorBeforeImage = ImGui.GetCursorPos();
            ImGui.Image(iconTex.ImGuiHandle, iconSize);
            ImGui.SameLine();

            if (updateAvailable)
            {
                ImGui.SetCursorPos(cursorBeforeImage);
                ImGui.Image(this.updateIcon.ImGuiHandle, iconSize);
                ImGui.SameLine();
            }
            else if (trouble)
            {
                ImGui.SetCursorPos(cursorBeforeImage);
                ImGui.Image(this.troubleIcon.ImGuiHandle, iconSize);
                ImGui.SameLine();
            }

            ImGuiHelpers.ScaledDummy(5);
            ImGui.SameLine();

            var cursor = ImGui.GetCursorPos();

            // Name
            ImGui.Text(label);

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

            // Description
            if (!string.IsNullOrWhiteSpace(manifest.Punchline))
            {
                ImGui.TextWrapped(manifest.Punchline);
            }

            startCursor.Y += sectionSize;
            ImGui.SetCursorPos(startCursor);

            return isOpen;
        }

        private void DrawAvailablePlugin(RemotePluginManifest manifest, int index)
        {
            var configuration = Service<DalamudConfiguration>.Get();
            var notifications = Service<NotificationManager>.Get();
            var pluginManager = Service<PluginManager>.Get();

            var useTesting = pluginManager.UseTesting(manifest);
            var wasSeen = this.WasPluginSeen(manifest.InternalName);

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

            ImGui.PushID($"available{index}{manifest.InternalName}");

            var isThirdParty = manifest.SourceRepo.IsThirdParty;
            if (this.DrawPluginCollapsingHeader(label, null, manifest, isThirdParty, false, false, !wasSeen, () => this.DrawAvailablePluginContextMenu(manifest), index))
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
                    ImGui.TextWrapped(manifest.Description);
                }

                ImGuiHelpers.ScaledDummy(5);

                // Controls
                var disabled = this.updateStatus == OperationStatus.InProgress || this.installStatus == OperationStatus.InProgress;

                var versionString = useTesting
                    ? $"{manifest.TestingAssemblyVersion}"
                    : $"{manifest.AssemblyVersion}";

                if (disabled)
                {
                    ImGuiComponents.DisabledButton(Locs.PluginButton_InstallVersion(versionString));
                }
                else
                {
                    var buttonText = Locs.PluginButton_InstallVersion(versionString);
                    if (ImGui.Button($"{buttonText}##{buttonText}{index}"))
                    {
                        this.installStatus = OperationStatus.InProgress;

                        Task.Run(() => pluginManager.InstallPluginAsync(manifest, useTesting, PluginLoadReason.Installer))
                            .ContinueWith(task =>
                            {
                                // There is no need to set as Complete for an individual plugin installation
                                this.installStatus = OperationStatus.Idle;
                                if (this.DisplayErrorContinuation(task, Locs.ErrorModal_InstallFail(manifest.Name)))
                                {
                                    if (task.Result.State == PluginState.Loaded)
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
                }

                this.DrawVisitRepoUrlButton(manifest.RepoUrl);

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
            var startInfo = Service<DalamudStartInfo>.Get();

            if (ImGui.BeginPopupContextItem("ItemContextMenu"))
            {
                if (ImGui.Selectable(Locs.PluginContext_MarkAllSeen))
                {
                    configuration.SeenPluginInternalName.AddRange(this.pluginListAvailable.Select(x => x.InternalName));
                    configuration.Save();
                    pluginManager.RefilterPluginMasters();
                }

                if (ImGui.Selectable(Locs.PluginContext_HidePlugin))
                {
                    Log.Debug($"Adding {manifest.InternalName} to hidden plugins");
                    configuration.HiddenPluginInternalName.Add(manifest.InternalName);
                    configuration.Save();
                    pluginManager.RefilterPluginMasters();
                }

                if (ImGui.Selectable(Locs.PluginContext_DeletePluginConfig))
                {
                    Log.Debug($"Deleting config for {manifest.InternalName}");

                    this.installStatus = OperationStatus.InProgress;

                    Task.Run(() =>
                        {
                            pluginManager.PluginConfigs.Delete(manifest.InternalName);

                            var path = Path.Combine(startInfo.PluginDirectory, manifest.InternalName);
                            if (Directory.Exists(path))
                                Directory.Delete(path, true);
                        })
                        .ContinueWith(task =>
                        {
                            this.installStatus = OperationStatus.Idle;

                            this.DisplayErrorContinuation(task, Locs.ErrorModal_DeleteConfigFail(manifest.InternalName));
                        });
                }

                ImGui.EndPopup();
            }
        }

        private void DrawInstalledPlugin(LocalPlugin plugin, int index, bool showInstalled = false)
        {
            var configuration = Service<DalamudConfiguration>.Get();
            var commandManager = Service<CommandManager>.Get();
            var pluginManager = Service<PluginManager>.Get();
            var startInfo = Service<DalamudStartInfo>.Get();

            var trouble = false;

            // Name
            var label = plugin.Manifest.Name;

            // Testing
            if (plugin.Manifest.Testing)
            {
                label += Locs.PluginTitleMod_TestingVersion;
            }

            // Freshly installed
            if (showInstalled)
            {
                label += Locs.PluginTitleMod_Installed;
            }

            // Disabled
            if (plugin.IsDisabled)
            {
                label += Locs.PluginTitleMod_Disabled;
                trouble = true;
            }

            // Load error
            if (plugin.State == PluginState.LoadError)
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
            // Update available
            if (availablePluginUpdate != default)
            {
                label += Locs.PluginTitleMod_HasUpdate;
            }

            // Freshly updated
            if (this.updatedPlugins != null && !plugin.IsDev)
            {
                var update = this.updatedPlugins.FirstOrDefault(update => update.InternalName == plugin.Manifest.InternalName);
                if (update != default)
                {
                    if (update.WasUpdated)
                    {
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

            ImGui.PushID($"installed{index}{plugin.Manifest.InternalName}");

            if (this.DrawPluginCollapsingHeader(label, plugin, plugin.Manifest, plugin.Manifest.IsThirdParty, trouble, availablePluginUpdate != default, false, () => this.DrawInstalledPluginContextMenu(plugin), index))
            {
                if (!this.WasPluginSeen(plugin.Manifest.InternalName))
                    configuration.SeenPluginInternalName.Add(plugin.Manifest.InternalName);

                var manifest = plugin.Manifest;

                ImGui.Indent();

                // Name
                ImGui.Text(manifest.Name);

                // Download count
                var downloadText = plugin.IsDev
                    ? Locs.PluginBody_AuthorWithoutDownloadCount(manifest.Author)
                    : manifest.DownloadCount > 0
                    ? Locs.PluginBody_AuthorWithDownloadCount(manifest.Author, manifest.DownloadCount)
                    : Locs.PluginBody_AuthorWithDownloadCountUnavailable(manifest.Author);

                ImGui.SameLine();
                ImGui.TextColored(ImGuiColors.DalamudGrey3, downloadText);

                var isThirdParty = manifest.IsThirdParty;

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
                    ImGui.TextWrapped(manifest.Description);
                }

                if (plugin.IsOutdated)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                    ImGui.TextWrapped(Locs.PluginBody_Outdated);
                    ImGui.PopStyleColor();
                }

                if (plugin.IsBanned)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                    ImGui.TextWrapped(Locs.PluginBody_Banned);
                    ImGui.PopStyleColor();
                }

                // Available commands (if loaded)
                if (plugin.IsLoaded)
                {
                    var commands = commandManager.Commands
                        .Where(cInfo => cInfo.Value.ShowInHelp && cInfo.Value.LoaderAssemblyName == plugin.Manifest.InternalName)
                        .ToArray();

                    if (commands.Any())
                    {
                        ImGui.Dummy(ImGuiHelpers.ScaledVector2(10f, 10f));
                        foreach (var command in commands)
                        {
                            ImGui.TextWrapped($"{command.Key} → {command.Value.HelpMessage}");
                        }
                    }
                }

                // Controls
                this.DrawPluginControlButton(plugin);
                this.DrawDevPluginButtons(plugin);
                this.DrawDeletePluginButton(plugin);
                this.DrawVisitRepoUrlButton(plugin.Manifest.RepoUrl);

                if (availablePluginUpdate != default)
                    this.DrawUpdateSinglePluginButton(availablePluginUpdate);

                ImGui.SameLine();
                ImGui.TextColored(ImGuiColors.DalamudGrey3, $" v{plugin.Manifest.AssemblyVersion}");

                if (plugin.IsDev)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(ImGuiColors.DalamudRed, Locs.PluginBody_DeleteDevPlugin);
                }

                ImGuiHelpers.ScaledDummy(5);

                if (this.DrawPluginImages(plugin, manifest, isThirdParty, index))
                    ImGuiHelpers.ScaledDummy(5);

                ImGui.Unindent();
            }

            ImGui.PopID();
        }

        private void DrawInstalledPluginContextMenu(LocalPlugin plugin)
        {
            var pluginManager = Service<PluginManager>.Get();

            if (ImGui.BeginPopupContextItem("InstalledItemContextMenu"))
            {
                if (ImGui.Selectable(Locs.PluginContext_DeletePluginConfigReload))
                {
                    Log.Debug($"Deleting config for {plugin.Manifest.InternalName}");

                    this.installStatus = OperationStatus.InProgress;

                    Task.Run(() => pluginManager.DeleteConfiguration(plugin))
                        .ContinueWith(task =>
                        {
                            this.installStatus = OperationStatus.Idle;

                            this.DisplayErrorContinuation(task, Locs.ErrorModal_DeleteConfigFail(plugin.Name));
                        });
                }

                ImGui.EndPopup();
            }
        }

        private void DrawPluginControlButton(LocalPlugin plugin)
        {
            var configuration = Service<DalamudConfiguration>.Get();
            var notifications = Service<NotificationManager>.Get();
            var pluginManager = Service<PluginManager>.Get();
            var startInfo = Service<DalamudStartInfo>.Get();

            // Disable everything if the updater is running or another plugin is operating
            var disabled = this.updateStatus == OperationStatus.InProgress || this.installStatus == OperationStatus.InProgress;

            // Disable everything if the plugin is outdated
            disabled = disabled || (plugin.IsOutdated && !configuration.LoadAllApiLevels) || plugin.IsBanned;

            if (plugin.State == PluginState.InProgress)
            {
                ImGuiComponents.DisabledButton(Locs.PluginButton_Working);
            }
            else if (plugin.State == PluginState.Loaded || plugin.State == PluginState.LoadError)
            {
                if (disabled)
                {
                    ImGuiComponents.DisabledButton(Locs.PluginButton_Disable);
                }
                else
                {
                    if (ImGui.Button(Locs.PluginButton_Disable))
                    {
                        Task.Run(() =>
                        {
                            var unloadTask = Task.Run(() => plugin.Unload())
                                .ContinueWith(this.DisplayErrorContinuation, Locs.ErrorModal_UnloadFail(plugin.Name));

                            unloadTask.Wait();
                            if (!unloadTask.Result)
                                return;

                            var disableTask = Task.Run(() => plugin.Disable())
                                .ContinueWith(this.DisplayErrorContinuation, Locs.ErrorModal_DisableFail(plugin.Name));

                            disableTask.Wait();
                            if (!disableTask.Result)
                                return;

                            if (!plugin.IsDev)
                            {
                                pluginManager.RemovePlugin(plugin);
                            }

                            notifications.AddNotification(Locs.Notifications_PluginDisabled(plugin.Manifest.Name), Locs.Notifications_PluginDisabledTitle, NotificationType.Success);
                        });
                    }
                }

                if (plugin.State == PluginState.Loaded)
                {
                    // Only if the plugin isn't broken.
                    this.DrawOpenPluginSettingsButton(plugin);
                }
            }
            else if (plugin.State == PluginState.Unloaded)
            {
                if (disabled)
                {
                    ImGuiComponents.DisabledButton(Locs.PluginButton_Load);
                }
                else
                {
                    if (ImGui.Button(Locs.PluginButton_Load))
                    {
                        Task.Run(() =>
                        {
                            var enableTask = Task.Run(() => plugin.Enable())
                                .ContinueWith(this.DisplayErrorContinuation, Locs.ErrorModal_EnableFail(plugin.Name));

                            enableTask.Wait();
                            if (!enableTask.Result)
                                return;

                            var loadTask = Task.Run(() => plugin.Load(PluginLoadReason.Installer))
                                .ContinueWith(this.DisplayErrorContinuation, Locs.ErrorModal_LoadFail(plugin.Name));

                            loadTask.Wait();
                            if (!loadTask.Result)
                                return;
                        });
                    }
                }
            }
            else if (plugin.State == PluginState.UnloadError)
            {
                ImGuiComponents.DisabledButton(FontAwesomeIcon.Frown);

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Locs.PluginButtonToolTip_UnloadFailed);
            }
        }

        private void DrawUpdateSinglePluginButton(AvailablePluginUpdate update)
        {
            var pluginManager = Service<PluginManager>.Get();

            ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Download))
            {
                this.installStatus = OperationStatus.InProgress;

                Task.Run(async () => await pluginManager.UpdateSinglePluginAsync(update, true, false))
                    .ContinueWith(task =>
                    {
                        // There is no need to set as Complete for an individual plugin installation
                        this.installStatus = OperationStatus.Idle;

                        var errorMessage = Locs.ErrorModal_SingleUpdateFail(update.UpdateManifest.Name);
                        this.DisplayErrorContinuation(task, errorMessage);

                        if (!task.Result.WasUpdated)
                        {
                            this.ShowErrorModal(errorMessage);
                        }
                    });
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
            if (plugin.DalamudInterface?.UiBuilder?.HasConfigUi ?? false)
            {
                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
                {
                    try
                    {
                        plugin.DalamudInterface.UiBuilder.OpenConfig();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Error during OpenConfigUi: {plugin.Name}");
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Locs.PluginButtonToolTip_OpenConfiguration);
                }
            }
        }

        private void DrawDevPluginButtons(LocalPlugin localPlugin)
        {
            var configuration = Service<DalamudConfiguration>.Get();

            if (localPlugin is LocalDevPlugin plugin)
            {
                // https://colorswall.com/palette/2868/
                var greenColor = new Vector4(0x5C, 0xB8, 0x5C, 0xFF) / 0xFF;
                var redColor = new Vector4(0xD9, 0x53, 0x4F, 0xFF) / 0xFF;

                // Load on boot
                ImGui.PushStyleColor(ImGuiCol.Button, plugin.StartOnBoot ? greenColor : redColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, plugin.StartOnBoot ? greenColor : redColor);

                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.PowerOff))
                {
                    plugin.StartOnBoot ^= true;
                    configuration.Save();
                }

                ImGui.PopStyleColor(2);

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Locs.PluginButtonToolTip_StartOnBoot);
                }

                // Automatic reload
                ImGui.PushStyleColor(ImGuiCol.Button, plugin.AutomaticReload ? greenColor : redColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, plugin.AutomaticReload ? greenColor : redColor);

                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.SyncAlt))
                {
                    plugin.AutomaticReload ^= true;
                    configuration.Save();
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
            var unloaded = plugin.State == PluginState.Unloaded;
            var showButton = unloaded && (plugin.IsDev || plugin.IsOutdated || plugin.IsBanned);

            if (!showButton)
                return;

            var pluginManager = Service<PluginManager>.Get();

            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.TrashAlt))
            {
                try
                {
                    plugin.DllFile.Delete();
                    pluginManager.RemovePlugin(plugin);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Plugin installer threw an error during removal of {plugin.Name}");

                    this.errorModalMessage = Locs.ErrorModal_DeleteFail(plugin.Name);
                    this.errorModalDrawing = true;
                    this.errorModalOnNextFrame = true;
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Locs.PluginButtonToolTip_DeletePlugin);
            }
        }

        private void DrawVisitRepoUrlButton(string? repoUrl)
        {
            if (!string.IsNullOrEmpty(repoUrl) && repoUrl.StartsWith("https://"))
            {
                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Globe))
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

        private bool DrawPluginImages(LocalPlugin? plugin, PluginManifest manifest, bool isThirdParty, int index)
        {
            var hasImages = this.pluginImagesMap.TryGetValue(manifest.InternalName, out var imageTextures);
            if (!hasImages)
            {
                this.pluginImagesMap.Add(manifest.InternalName, Array.Empty<TextureWrap>());
                Task.Run(async () => await this.DownloadPluginImagesAsync(plugin, manifest, isThirdParty));

                return false;
            }

            if (imageTextures.Length == 0)
                return false;

            const float thumbFactor = 2.7f;

            var scrollBarSize = 15;
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, scrollBarSize);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Vector4.Zero);

            var width = ImGui.GetWindowWidth();

            if (ImGui.BeginChild($"plugin{index}ImageScrolling", new Vector2(width - (70 * ImGuiHelpers.GlobalScale), (PluginImageHeight / thumbFactor) + scrollBarSize), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground))
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

                    if (ImGui.ImageButton(image.ImGuiHandle, ImGuiHelpers.ScaledVector2(image.Width / thumbFactor, image.Height / thumbFactor)))
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

        private bool IsManifestFiltered(PluginManifest manifest)
        {
            var searchString = this.searchText.ToLowerInvariant();
            var hasSearchString = !string.IsNullOrWhiteSpace(searchString);

            return hasSearchString && !(
                manifest.Name.ToLowerInvariant().Contains(searchString) ||
                manifest.Author.Equals(this.searchText, StringComparison.InvariantCultureIgnoreCase) ||
                (manifest.Tags != null && manifest.Tags.Contains(searchString, StringComparer.InvariantCultureIgnoreCase)));
        }

        private (bool IsInstalled, LocalPlugin Plugin) IsManifestInstalled(RemotePluginManifest manifest)
        {
            var plugin = this.pluginListInstalled.FirstOrDefault(plugin => plugin.Manifest.InternalName == manifest.InternalName);
            var isInstalled = plugin != default;

            return (isInstalled, plugin);
        }

        private void OnAvailablePluginsChanged()
        {
            var pluginManager = Service<PluginManager>.Get();

            // By removing installed plugins only when the available plugin list changes (basically when the window is
            // opened), plugins that have been newly installed remain in the available plugin list as installed.
            this.pluginListAvailable = pluginManager.AvailablePlugins
                .Where(manifest => !this.IsManifestInstalled(manifest).IsInstalled)
                .ToList();
            this.pluginListUpdatable = pluginManager.UpdatablePlugins.ToList();
            this.ResortPlugins();
        }

        private void OnInstalledPluginsChanged()
        {
            var pluginManager = Service<PluginManager>.Get();

            this.pluginListInstalled = pluginManager.InstalledPlugins.ToList();
            this.pluginListUpdatable = pluginManager.UpdatablePlugins.ToList();
            this.hasDevPlugins = this.pluginListInstalled.Any(plugin => plugin.IsDev);
            this.ResortPlugins();
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
                    this.pluginListInstalled.Sort((p1, p2) => p2.Manifest.LastUpdate.CompareTo(p1.Manifest.LastUpdate));
                    break;
                case PluginSortKind.NewOrNot:
                    this.pluginListAvailable.Sort((p1, p2) => this.WasPluginSeen(p1.InternalName)
                                                                  .CompareTo(this.WasPluginSeen(p2.InternalName)));
                    this.pluginListInstalled.Sort((p1, p2) => this.WasPluginSeen(p1.Manifest.InternalName)
                                                                  .CompareTo(this.WasPluginSeen(p2.Manifest.InternalName)));
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unknown plugin sort type.");
            }
        }

        private bool WasPluginSeen(string internalName) =>
            Service<DalamudConfiguration>.Get().SeenPluginInternalName.Contains(internalName);

        /// <summary>
        /// A continuation task that displays any errors received into the error modal.
        /// </summary>
        /// <param name="task">The previous task.</param>
        /// <param name="state">An error message to be displayed.</param>
        /// <returns>A value indicating whether to continue with the next task.</returns>
        private bool DisplayErrorContinuation(Task task, object state)
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

        private void ShowErrorModal(string message)
        {
            this.errorModalMessage = message;
            this.errorModalDrawing = true;
            this.errorModalOnNextFrame = true;
        }

        private async Task DownloadPluginIconAsync(LocalPlugin? plugin, PluginManifest manifest, bool isThirdParty)
        {
            var interfaceManager = Service<InterfaceManager>.Get();
            var pluginManager = Service<PluginManager>.Get();

            static bool ValidateIcon(TextureWrap icon, string loc)
            {
                if (icon == null)
                    return false;

                if (icon.Height > PluginIconHeight || icon.Width > PluginIconWidth)
                {
                    Log.Error($"Icon at {loc} was not of the correct resolution.");
                    return false;
                }

                if (icon.Height != icon.Width)
                {
                    Log.Error($"Icon at {loc} was not square.");
                    return false;
                }

                return true;
            }

            if (plugin != null && plugin.IsDev)
            {
                var file = GetPluginIconFileInfo(plugin);
                if (file != null)
                {
                    Log.Verbose($"Fetching icon for {manifest.InternalName} from {file.FullName}");

                    var icon = interfaceManager.LoadImage(file.FullName);

                    if (!ValidateIcon(icon, file.FullName))
                        return;

                    this.pluginIconMap[manifest.InternalName] = icon;
                    Log.Verbose($"Plugin icon for {manifest.InternalName} loaded from disk");
                }

                return;
            }

            var useTesting = pluginManager.UseTesting(manifest);
            var url = GetPluginIconUrl(manifest, isThirdParty, useTesting);
            if (url != null)
            {
                Log.Verbose($"Downloading icon for {manifest.InternalName} from {url}");

                var data = await this.httpClient.GetAsync(url);
                if (data.StatusCode == HttpStatusCode.NotFound)
                    return;

                data.EnsureSuccessStatusCode();

                var icon = interfaceManager.LoadImage(await data.Content.ReadAsByteArrayAsync());

                if (!ValidateIcon(icon, url))
                    return;

                this.pluginIconMap[manifest.InternalName] = icon;
                Log.Verbose($"Plugin icon for {manifest.InternalName} downloaded");

                return;
            }

            Log.Verbose($"Icon for {manifest.InternalName} is not available");
        }

        private async Task DownloadPluginImagesAsync(LocalPlugin? plugin, PluginManifest manifest, bool isThirdParty)
        {
            var interfaceManager = Service<InterfaceManager>.Get();
            var pluginManager = Service<PluginManager>.Get();

            static bool ValidateImage(TextureWrap image, string loc)
            {
                if (image == null)
                    return false;

                if (image.Height != PluginImageHeight || image.Width != PluginImageWidth)
                {
                    Log.Error($"Image at {loc} was not of the correct resolution.");
                    return false;
                }

                return true;
            }

            if (plugin != null && plugin.IsDev)
            {
                var files = GetPluginImageFileInfos(plugin);
                if (files != null)
                {
                    var didAny = false;
                    var pluginImages = new TextureWrap[files.Count];
                    for (var i = 0; i < files.Count; i++)
                    {
                        var file = files[i];

                        if (file == null)
                            continue;

                        Log.Verbose($"Loading image{i + 1} for {manifest.InternalName} from {file.FullName}");

                        var image = interfaceManager.LoadImage(await File.ReadAllBytesAsync(file.FullName));

                        if (!ValidateImage(image, file.FullName))
                            continue;

                        Log.Verbose($"Plugin image{i + 1} for {manifest.InternalName} loaded from disk");
                        pluginImages[i] = image;
                        didAny = true;
                    }

                    if (didAny)
                    {
                        Log.Verbose($"Plugin images for {manifest.InternalName} loaded from disk");
                        this.pluginImagesMap[manifest.InternalName] = pluginImages;

                        return;
                    }
                }

                // Dev plugins are loaded from disk only
                return;
            }

            var useTesting = pluginManager.UseTesting(manifest);
            var urls = GetPluginImageUrls(manifest, isThirdParty, useTesting);
            if (urls != null)
            {
                var didAny = false;
                var pluginImages = new TextureWrap[urls.Count];
                for (var i = 0; i < urls.Count; i++)
                {
                    var url = urls[i];

                    Log.Verbose($"Downloading image{i + 1} for {manifest.InternalName} from {url}");

                    var data = await this.httpClient.GetAsync(url);

                    if (data.StatusCode == HttpStatusCode.NotFound)
                        continue;

                    data.EnsureSuccessStatusCode();

                    var image = interfaceManager.LoadImage(await data.Content.ReadAsByteArrayAsync());

                    if (!ValidateImage(image, url))
                        continue;

                    Log.Verbose($"Plugin image{i + 1} for {manifest.InternalName} downloaded");
                    pluginImages[i] = image;
                    didAny = true;
                }

                if (didAny)
                {
                    Log.Verbose($"Plugin images for {manifest.InternalName} downloaded");
                    this.pluginImagesMap[manifest.InternalName] = pluginImages;

                    return;
                }
            }

            Log.Verbose($"Images for {manifest.InternalName} are not available");
        }

        [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Disregard here")]
        private static class Locs
        {
            #region Window Title

            public static string WindowTitle => Loc.Localize("InstallerHeader", "Plugin Installer");

            public static string WindowTitleMod_Testing => Loc.Localize("InstallerHeaderTesting", " (TESTING)");

            #endregion

            #region Header

            public static string Header_Hint => Loc.Localize("InstallerHint", "This window allows you to install and remove in-game plugins.\nThey are made by third-party developers.");

            public static string Header_SearchPlaceholder => Loc.Localize("InstallerSearch", "Search");

            #endregion

            #region SortBy

            public static string SortBy_Alphabetical => Loc.Localize("InstallerAlphabetical", "Alphabetical");

            public static string SortBy_DownloadCounts => Loc.Localize("InstallerDownloadCount", "Download Count");

            public static string SortBy_LastUpdate => Loc.Localize("InstallerLastUpdate", "Last Update");

            public static string SortBy_NewOrNot => Loc.Localize("InstallerNewOrNot", "New or not");

            public static string SortBy_Label => Loc.Localize("InstallerSortBy", "Sort By");

            #endregion

            #region Tabs

            public static string TabTitle_AvailablePlugins => Loc.Localize("InstallerAvailablePlugins", "Available Plugins");

            public static string TabTitle_InstalledPlugins => Loc.Localize("InstallerInstalledPlugins", "Installed Plugins");

            public static string TabTitle_InstalledDevPlugins => Loc.Localize("InstallerInstalledDevPlugins", "Installed Dev Plugins");

            #endregion

            #region Tab body

            public static string TabBody_LoadingPlugins => Loc.Localize("InstallerLoading", "Loading plugins...");

            public static string TabBody_DownloadFailed => Loc.Localize("InstallerDownloadFailed", "Download failed.");

            public static string TabBody_SafeMode => Loc.Localize("InstallerSafeMode", "Dalamud is running in Plugin Safe Mode, restart to activate plugins.");

            #endregion

            #region Search text

            public static string TabBody_SearchNoMatching => Loc.Localize("InstallerNoMatching", "No plugins were found matching your search.");

            public static string TabBody_SearchNoCompatible => Loc.Localize("InstallerNoCompatible", "No compatible plugins were found :( Please restart your game and try again.");

            public static string TabBody_SearchNoInstalled => Loc.Localize("InstallerNoInstalled", "No plugins are currently installed. You can install them from the Available Plugins tab.");

            #endregion

            #region Plugin title text

            public static string PluginTitleMod_Installed => Loc.Localize("InstallerInstalled", " (installed)");

            public static string PluginTitleMod_Disabled => Loc.Localize("InstallerDisabled", " (disabled)");

            public static string PluginTitleMod_Unloaded => Loc.Localize("InstallerUnloaded", " (unloaded)");

            public static string PluginTitleMod_HasUpdate => Loc.Localize("InstallerHasUpdate", " (has update)");

            public static string PluginTitleMod_Updated => Loc.Localize("InstallerUpdated", " (updated)");

            public static string PluginTitleMod_TestingVersion => Loc.Localize("InstallerTestingVersion", " (testing version)");

            public static string PluginTitleMod_UpdateFailed => Loc.Localize("InstallerUpdateFailed", " (update failed)");

            public static string PluginTitleMod_LoadError => Loc.Localize("InstallerLoadError", " (load error)");

            public static string PluginTitleMod_UnloadError => Loc.Localize("InstallerUnloadError", " (unload error)");

            public static string PluginTitleMod_OutdatedError => Loc.Localize("InstallerOutdatedError", " (outdated)");

            public static string PluginTitleMod_BannedError => Loc.Localize("InstallerOutdatedError", " (banned)");

            public static string PluginTitleMod_New => Loc.Localize("InstallerNewPlugin ", " New!");

            #endregion

            #region Plugin context menu

            public static string PluginContext_MarkAllSeen => Loc.Localize("InstallerMarkAllSeen", "Mark all as seen");

            public static string PluginContext_HidePlugin => Loc.Localize("InstallerHidePlugin", "Hide from installer");

            public static string PluginContext_DeletePluginConfig => Loc.Localize("InstallerDeletePluginConfig", "Reset plugin");

            public static string PluginContext_DeletePluginConfigReload => Loc.Localize("InstallerDeletePluginConfigReload", "Reset plugin settings & reload");

            #endregion

            #region Plugin body

            public static string PluginBody_AuthorWithoutDownloadCount(string author) => Loc.Localize("InstallerAuthorWithoutDownloadCount", " by {0}").Format(author);

            public static string PluginBody_AuthorWithDownloadCount(string author, long count) => Loc.Localize("InstallerAuthorWithDownloadCount", " by {0}, {1} downloads").Format(author, count);

            public static string PluginBody_AuthorWithDownloadCountUnavailable(string author) => Loc.Localize("InstallerAuthorWithDownloadCountUnavailable", " by {0}, download count unavailable").Format(author);

            public static string PluginBody_DevPluginPath(string path) => Loc.Localize("InstallerDevPluginPath", "From {0}").Format(path);

            public static string PluginBody_Plugin3rdPartyRepo(string url) => Loc.Localize("InstallerPlugin3rdPartyRepo", "From custom plugin repository {0}").Format(url);

            public static string PluginBody_AvailableDevPlugin => Loc.Localize("InstallerDevPlugin", " This plugin is available in one of your repos, please remove it from the devPlugins folder.");

            public static string PluginBody_DeleteDevPlugin => Loc.Localize("InstallerDeleteDevPlugin ", " To delete this plugin, please remove it from the devPlugins folder.");

            public static string PluginBody_Outdated => Loc.Localize("InstallerOutdatedPluginBody ", "This plugin is outdated and incompatible at the moment. Please wait for it to be updated by its author.");

            public static string PluginBody_Banned => Loc.Localize("InstallerBannedPluginBody ", "This plugin version is banned and not available at the moment. Please wait for it to be updated by its author.");

            #endregion

            #region Plugin buttons

            public static string PluginButton_InstallVersion(string version) => Loc.Localize("InstallerInstall", "Install v{0}").Format(version);

            public static string PluginButton_Working => Loc.Localize("InstallerWorking", "Working");

            public static string PluginButton_Disable => Loc.Localize("InstallerDisable", "Disable");

            public static string PluginButton_Load => Loc.Localize("InstallerLoad", "Load");

            public static string PluginButton_Unload => Loc.Localize("InstallerUnload", "Unload");

            #endregion

            #region Plugin button tooltips

            public static string PluginButtonToolTip_OpenConfiguration => Loc.Localize("InstallerOpenConfig", "Open Configuration");

            public static string PluginButtonToolTip_StartOnBoot => Loc.Localize("InstallerStartOnBoot", "Start on boot");

            public static string PluginButtonToolTip_AutomaticReloading => Loc.Localize("InstallerAutomaticReloading", "Automatic reloading");

            public static string PluginButtonToolTip_DeletePlugin => Loc.Localize("InstallerDeletePlugin ", "Delete plugin");

            public static string PluginButtonToolTip_VisitPluginUrl => Loc.Localize("InstallerVisitPluginUrl", "Visit plugin URL");

            public static string PluginButtonToolTip_UpdateSingle(string version) => Loc.Localize("InstallerUpdateSingle", "Update to {0}").Format(version);

            public static string PluginButtonToolTip_UnloadFailed => Loc.Localize("InstallerUnloadFailedTooltip", "Plugin unload failed, please restart your game and try again.");

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

            #endregion

            #region Footer

            public static string FooterButton_UpdatePlugins => Loc.Localize("InstallerUpdatePlugins", "Update plugins");

            public static string FooterButton_InProgress => Loc.Localize("InstallerInProgress", "Install in progress...");

            public static string FooterButton_NoUpdates => Loc.Localize("InstallerNoUpdates", "No updates found!");

            public static string FooterButton_UpdateComplete(int count) => Loc.Localize("InstallerUpdateComplete", "{0} plugins updated!").Format(count);

            public static string FooterButton_Settings => Loc.Localize("InstallerSettings", "Settings");

            public static string FooterButton_ScanDevPlugins => Loc.Localize("InstallerScanDevPlugins", "Scan Dev Plugins");

            public static string FooterButton_Close => Loc.Localize("InstallerClose", "Close");

            #endregion

            #region Error modal

            public static string ErrorModal_Title => Loc.Localize("InstallerError", "Installer Error");

            public static string ErrorModal_InstallContactAuthor => Loc.Localize(
                "InstallerContactAuthor",
                "Please restart your game and try again. If this error occurs again, please contact the plugin author.");

            public static string ErrorModal_InstallFail(string name) => Loc.Localize("InstallerInstallFail", "Failed to install plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

            public static string ErrorModal_SingleUpdateFail(string name) => Loc.Localize("InstallerSingleUpdateFail", "Failed to update plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

            public static string ErrorModal_DeleteConfigFail(string name) => Loc.Localize("InstallerDeleteConfigFail", "Failed to reset the plugin {0}.\n\nThe plugin may not support this action. You can try deleting the configuration manually while the game is shut down - please see the FAQ.").Format(name);

            public static string ErrorModal_EnableFail(string name) => Loc.Localize("InstallerEnableFail", "Failed to enable plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

            public static string ErrorModal_DisableFail(string name) => Loc.Localize("InstallerDisableFail", "Failed to disable plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

            public static string ErrorModal_UnloadFail(string name) => Loc.Localize("InstallerUnloadFail", "Failed to unload plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

            public static string ErrorModal_LoadFail(string name) => Loc.Localize("InstallerLoadFail", "Failed to load plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

            public static string ErrorModal_DeleteFail(string name) => Loc.Localize("InstallerDeleteFail", "Failed to delete plugin {0}.\n{1}").Format(name, ErrorModal_InstallContactAuthor);

            public static string ErrorModal_UpdaterFatal => Loc.Localize("InstallerUpdaterFatal", "Failed to update plugins.\nPlease restart your game and try again. If this error occurs again, please complain.");

            public static string ErrorModal_UpdaterFail(int failCount) => Loc.Localize("InstallerUpdaterFail", "Failed to update {0} plugins.\nPlease restart your game and try again. If this error occurs again, please complain.").Format(failCount);

            public static string ErrorModal_UpdaterFailPartial(int successCount, int failCount) => Loc.Localize("InstallerUpdaterFailPartial", "Updated {0} plugins, failed to update {1}.\nPlease restart your game and try again. If this error occurs again, please complain.").Format(successCount, failCount);

            public static string ErrorModal_HintBlame(string plugins) => Loc.Localize("InstallerErrorPluginInfo", "\n\nThe following plugins caused these issues:\n\n{0}\nYou may try removing these plugins manually and reinstalling them.").Format(plugins);

            // public static string ErrorModal_Hint => Loc.Localize("InstallerErrorHint", "The plugin installer ran into an issue or the plugin is incompatible.\nPlease restart the game and report this error on our discord.");

            #endregion

            #region Plugin Update chatbox

            public static string PluginUpdateHeader_Chatbox => Loc.Localize("DalamudPluginUpdates", "Updates:");

            #endregion

            #region Error modal buttons

            public static string ErrorModalButton_Ok => Loc.Localize("OK", "OK");

            #endregion
        }
    }
}

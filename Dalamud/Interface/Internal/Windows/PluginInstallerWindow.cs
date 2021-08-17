using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
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

        private const int PluginIconWidth = 300;
        private const int PluginIconHeight = 300;

        private static readonly ModuleLog Log = new("PLUGINW");

        private readonly Dalamud dalamud;

        private readonly TextureWrap defaultIcon;
        private readonly TextureWrap troubleIcon;
        private readonly TextureWrap updateIcon;

        private bool errorModalDrawing = true;
        private bool errorModalOnNextFrame = false;
        private string errorModalMessage = string.Empty;

        private int updatePluginCount = 0;
        private List<PluginUpdateStatus> updatedPlugins;

        private List<RemotePluginManifest> pluginListAvailable = new();
        private List<LocalPlugin> pluginListInstalled = new();
        private List<AvailablePluginUpdate> pluginListUpdatable = new();
        private bool hasDevPlugins = false;

        private bool downloadingIcons = false;
        private Dictionary<string, (bool IsDownloaded, TextureWrap[] Textures)> pluginImagesMap = new();
        private Dictionary<string, (bool IsDownloaded, TextureWrap? Texture)> pluginIconMap = new();

        private string searchText = string.Empty;

        private PluginSortKind sortKind = PluginSortKind.Alphabetical;
        private string filterText = Locs.SortBy_Alphabetical;

        private OperationStatus installStatus = OperationStatus.Idle;
        private OperationStatus updateStatus = OperationStatus.Idle;

        private List<int> openPluginCollapsibles = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstallerWindow"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        public PluginInstallerWindow(Dalamud dalamud)
            : base(
                Locs.WindowTitle + (dalamud.Configuration.DoPluginTest ? Locs.WindowTitleMod_Testing : string.Empty) + "###XlPluginInstaller",
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar)
        {
            this.dalamud = dalamud;
            this.IsOpen = true;

            this.Size = new Vector2(830, 570);
            this.SizeCondition = ImGuiCond.FirstUseEver;

            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = this.Size.Value,
                MaximumSize = new Vector2(5000, 5000),
            };

            // For debugging
            if (this.dalamud.PluginManager.PluginsReady)
                this.OnInstalledPluginsChanged();

            this.dalamud.PluginManager.OnAvailablePluginsChanged += this.OnAvailablePluginsChanged;
            this.dalamud.PluginManager.OnInstalledPluginsChanged += this.OnInstalledPluginsChanged;

            this.defaultIcon =
                this.dalamud.InterfaceManager.LoadImage(
                    Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "defaultIcon.png"));

            this.troubleIcon =
                this.dalamud.InterfaceManager.LoadImage(
                    Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "troubleIcon.png"));

            this.updateIcon =
                this.dalamud.InterfaceManager.LoadImage(
                    Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "updateIcon.png"));
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
            this.dalamud.PluginManager.OnAvailablePluginsChanged -= this.OnAvailablePluginsChanged;
            this.dalamud.PluginManager.OnInstalledPluginsChanged -= this.OnInstalledPluginsChanged;

            this.defaultIcon.Dispose();
            this.troubleIcon.Dispose();
            this.updateIcon.Dispose();
        }

        /// <inheritdoc/>
        public override void OnOpen()
        {
            Task.Run(this.dalamud.PluginManager.ReloadPluginMasters);

            this.updatePluginCount = 0;
            this.updatedPlugins = null;

            this.searchText = string.Empty;
            this.sortKind = PluginSortKind.Alphabetical;
            this.filterText = Locs.SortBy_Alphabetical;
        }

        /// <inheritdoc/>
        public override void OnClose()
        {
            this.dalamud.Configuration.Save();
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            this.DrawHeader();
            this.DrawPluginTabBar();
            this.DrawFooter();
            this.DrawErrorModal();
        }

        private static Vector2 GetButtonSize(string text) => ImGui.CalcTextSize(text) + (ImGui.GetStyle().FramePadding * 2);

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
            var windowSize = ImGui.GetWindowContentRegionMax();
            var placeholderButtonSize = GetButtonSize("placeholder");

            ImGui.Separator();

            ImGui.SetCursorPosY(windowSize.Y - placeholderButtonSize.Y);

            this.DrawUpdatePluginsButton();

            ImGui.SameLine();
            if (ImGui.Button(Locs.FooterButton_Settings))
            {
                this.dalamud.DalamudUi.OpenSettings();
            }

            // If any dev plugins are installed, allow a shortcut for the /xldev menu item
            if (this.hasDevPlugins)
            {
                ImGui.SameLine();
                if (ImGui.Button(Locs.FooterButton_ScanDevPlugins))
                {
                    this.dalamud.PluginManager.ScanDevPlugins();
                }
            }

            var closeText = Locs.FooterButton_Close;
            var closeButtonSize = GetButtonSize(closeText);

            ImGui.SameLine(windowSize.X - closeButtonSize.X - 20);
            if (ImGui.Button(closeText))
            {
                this.IsOpen = false;
                this.dalamud.Configuration.Save();
            }
        }

        private void DrawUpdatePluginsButton()
        {
            var ready = this.dalamud.PluginManager.PluginsReady && this.dalamud.PluginManager.ReposReady;

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

                    Task.Run(() => this.dalamud.PluginManager.UpdatePluginsAsync())
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
                                    this.dalamud.PluginManager.PrintUpdatedPlugins(this.updatedPlugins, Locs.PluginUpdateHeader_Chatbox);
                                    this.dalamud.InterfaceManager.Notifications.AddNotification(Locs.Notifications_UpdatesInstalled(this.updatePluginCount), Locs.Notifications_UpdatesInstalledTitle, Notifications.Notification.Type.Success);
                                }
                                else if (this.updatePluginCount == 0)
                                {
                                    this.dalamud.InterfaceManager.Notifications.AddNotification(Locs.Notifications_NoUpdatesFound, Locs.Notifications_NoUpdatesFoundTitle, Notifications.Notification.Type.Info);
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

        private bool DrawPluginListLoading()
        {
            var ready = this.dalamud.PluginManager.PluginsReady && this.dalamud.PluginManager.ReposReady;

            if (!ready)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.TabBody_LoadingPlugins);
            }

            var failedRepos = this.dalamud.PluginManager.Repos
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

        private bool DrawPluginCollapsingHeader(string label, PluginManifest manifest, bool trouble, bool updateAvailable, bool isNew, Action drawContextMenuAction, int index)
        {
            ImGui.Separator();

            var isOpen = this.openPluginCollapsibles.Contains(index);

            var sectionSize = ImGuiHelpers.GlobalScale * 42;
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

            var hasIcon = this.pluginIconMap.TryGetValue(manifest.InternalName, out var icon);

            var iconTex = this.defaultIcon;
            if (hasIcon && icon.IsDownloaded && icon.Texture != null)
            {
                iconTex = icon.Texture;
            }

            var cursorBeforeImage = ImGui.GetCursorPos();
            ImGui.Image(iconTex.ImGuiHandle, ImGuiHelpers.ScaledVector2(40, 40));
            ImGui.SameLine();

            if (trouble)
            {
                ImGui.SetCursorPos(cursorBeforeImage);
                ImGui.Image(this.troubleIcon.ImGuiHandle, ImGuiHelpers.ScaledVector2(40, 40));
                ImGui.SameLine();
            }
            else if (updateAvailable)
            {
                ImGui.SetCursorPos(cursorBeforeImage);
                ImGui.Image(this.updateIcon.ImGuiHandle, ImGuiHelpers.ScaledVector2(40, 40));
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
            var useTesting = this.dalamud.PluginManager.UseTesting(manifest);
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

            if (this.DrawPluginCollapsingHeader(label, manifest, false, false, !wasSeen, () => this.DrawAvailablePluginContextMenu(manifest), index))
            {
                if (!wasSeen)
                    this.dalamud.Configuration.SeenPluginInternalName.Add(manifest.InternalName);

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

                        Task.Run(() => this.dalamud.PluginManager.InstallPluginAsync(manifest, useTesting, PluginLoadReason.Installer))
                            .ContinueWith(task =>
                            {
                                // There is no need to set as Complete for an individual plugin installation
                                this.installStatus = OperationStatus.Idle;
                                if (this.DisplayErrorContinuation(task, Locs.ErrorModal_InstallFail(manifest.Name)))
                                {
                                    if (task.Result.State == PluginState.Loaded)
                                    {
                                        this.dalamud.InterfaceManager.Notifications.AddNotification(Locs.Notifications_PluginInstalled(manifest.Name), Locs.Notifications_PluginInstalledTitle, Notifications.Notification.Type.Success);
                                    }
                                    else
                                    {
                                        this.dalamud.InterfaceManager.Notifications.AddNotification(Locs.Notifications_PluginNotInstalled(manifest.Name), Locs.Notifications_PluginNotInstalledTitle, Notifications.Notification.Type.Error);
                                        this.ShowErrorModal(Locs.ErrorModal_InstallFail(manifest.Name));
                                    }
                                }
                            });
                    }
                }

                this.DrawVisitRepoUrlButton(manifest.RepoUrl);

                ImGuiHelpers.ScaledDummy(5);

                if (this.DrawPluginImages(manifest, index))
                    ImGuiHelpers.ScaledDummy(5);

                ImGui.Unindent();
            }

            ImGui.PopID();
        }

        private void DrawAvailablePluginContextMenu(PluginManifest manifest)
        {
            if (ImGui.BeginPopupContextItem("ItemContextMenu"))
            {
                if (ImGui.Selectable(Locs.PluginContext_MarkAllSeen))
                {
                    this.dalamud.Configuration.SeenPluginInternalName.AddRange(this.pluginListAvailable.Select(x => x.InternalName));
                    this.dalamud.Configuration.Save();
                    this.dalamud.PluginManager.RefilterPluginMasters();
                }

                if (ImGui.Selectable(Locs.PluginContext_HidePlugin))
                {
                    Log.Debug($"Adding {manifest.InternalName} to hidden plugins");
                    this.dalamud.Configuration.HiddenPluginInternalName.Add(manifest.InternalName);
                    this.dalamud.Configuration.Save();
                    this.dalamud.PluginManager.RefilterPluginMasters();
                }

                if (ImGui.Selectable(Locs.PluginContext_DeletePluginConfig))
                {
                    Log.Debug($"Deleting config for {manifest.InternalName}");

                    this.installStatus = OperationStatus.InProgress;

                    Task.Run(() =>
                        {
                            this.dalamud.PluginManager.PluginConfigs.Delete(manifest.InternalName);

                            var path = Path.Combine(this.dalamud.StartInfo.PluginDirectory, manifest.InternalName);
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
            if (plugin.Manifest.DalamudApiLevel < PluginManager.DalamudApiLevel)
            {
                label += Locs.PluginTitleMod_OutdatedError;
                trouble = true;
            }

            ImGui.PushID($"installed{index}{plugin.Manifest.InternalName}");

            if (this.DrawPluginCollapsingHeader(label, plugin.Manifest, trouble, availablePluginUpdate != default, false, () => this.DrawInstalledPluginContextMenu(plugin), index))
            {
                if (!this.WasPluginSeen(plugin.Manifest.InternalName))
                    this.dalamud.Configuration.SeenPluginInternalName.Add(plugin.Manifest.InternalName);

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

                // Installed from
                if (plugin.IsDev)
                {
                    var fileText = Locs.PluginBody_DevPluginPath(plugin.DllFile.FullName);
                    ImGui.TextColored(ImGuiColors.DalamudGrey3, fileText);
                }
                else if (!string.IsNullOrEmpty(manifest.InstalledFromUrl))
                {
                    var repoText = Locs.PluginBody_Plugin3rdPartyRepo(manifest.InstalledFromUrl);
                    ImGui.TextColored(ImGuiColors.DalamudGrey3, repoText);
                }

                // Description
                if (!string.IsNullOrWhiteSpace(manifest.Description))
                {
                    ImGui.TextWrapped(manifest.Description);
                }

                if (plugin.Manifest.DalamudApiLevel < PluginManager.DalamudApiLevel)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                    ImGui.TextWrapped(Locs.PluginBody_Outdated);
                    ImGui.PopStyleColor();
                }

                // Available commands (if loaded)
                if (plugin.IsLoaded)
                {
                    var commands = this.dalamud.CommandManager.Commands.Where(cInfo => cInfo.Value.ShowInHelp && cInfo.Value.LoaderAssemblyName == plugin.Manifest.InternalName);
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

                this.DrawPluginImages(manifest, index);

                ImGuiHelpers.ScaledDummy(5);

                ImGui.Unindent();
            }

            ImGui.PopID();
        }

        private void DrawInstalledPluginContextMenu(LocalPlugin plugin)
        {
            if (ImGui.BeginPopupContextItem("InstalledItemContextMenu"))
            {
                if (ImGui.Selectable(Locs.PluginContext_DeletePluginConfigReload))
                {
                    Log.Debug($"Deleting config for {plugin.Manifest.InternalName}");

                    this.installStatus = OperationStatus.InProgress;

                    Task.Run(() => this.dalamud.PluginManager.DeleteConfiguration(plugin))
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
            // Disable everything if the updater is running or another plugin is operating
            var disabled = this.updateStatus == OperationStatus.InProgress || this.installStatus == OperationStatus.InProgress;

            // Disable everything if the plugin is outdated
            disabled = disabled || (plugin.Manifest.DalamudApiLevel < PluginManager.DalamudApiLevel && !this.dalamud.Configuration.LoadAllApiLevels);

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
                                this.dalamud.PluginManager.RemovePlugin(plugin);
                            }

                            this.dalamud.InterfaceManager.Notifications.AddNotification(Locs.Notifications_PluginDisabled(plugin.Manifest.Name), Locs.Notifications_PluginDisabledTitle, Notifications.Notification.Type.Success);
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
            ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Download))
            {
                this.installStatus = OperationStatus.InProgress;

                Task.Run(() => this.dalamud.PluginManager.UpdateSinglePluginAsync(update, true, false))
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
                ImGui.SetTooltip(Locs.PluginButtonToolTip_UpdateSingle(update.UpdateManifest.AssemblyVersion.ToString()));
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
                    this.dalamud.Configuration.Save();
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
                    this.dalamud.Configuration.Save();
                }

                ImGui.PopStyleColor(2);

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Locs.PluginButtonToolTip_AutomaticReloading);
                }

                // Delete
                if (plugin.State == PluginState.Unloaded)
                {
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.TrashAlt))
                    {
                        try
                        {
                            plugin.DllFile.Delete();
                            this.dalamud.PluginManager.RemovePlugin(plugin);
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
                        ImGui.SetTooltip(Locs.PluginBody_DeleteDevPlugin);
                    }
                }
            }
        }

        private void DrawVisitRepoUrlButton(string repoUrl)
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

        private bool DrawPluginImages(PluginManifest manifest, int index)
        {
            if (!this.pluginImagesMap.TryGetValue(manifest.InternalName, out var images))
            {
                Task.Run(() => this.DownloadPluginImagesAsync(manifest));
                return false;
            }

            if (!images.IsDownloaded)
                return false;

            if (images.Textures == null)
                return false;

            const float thumbFactor = 2.7f;

            var scrollBarSize = 15;
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, scrollBarSize);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Vector4.Zero);

            var width = ImGui.GetWindowWidth();

            if (ImGui.BeginChild($"plugin{index}ImageScrolling", new Vector2(width - (70 * ImGuiHelpers.GlobalScale), (PluginImageHeight / thumbFactor) + scrollBarSize), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground))
            {
                if (images.Textures != null && images.Textures is { Length: > 0 })
                {
                    for (var i = 0; i < images.Textures.Length; i++)
                    {
                        var popupId = $"plugin{index}image{i}";
                        var image = images.Textures[i];

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

                        if (i < images.Textures.Length - 1)
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
            // By removing installed plugins only when the available plugin list changes (basically when the window is
            // opened), plugins that have been newly installed remain in the available plugin list as installed.
            this.pluginListAvailable = this.dalamud.PluginManager.AvailablePlugins
                .Where(manifest => !this.IsManifestInstalled(manifest).IsInstalled)
                .ToList();
            this.pluginListUpdatable = this.dalamud.PluginManager.UpdatablePlugins.ToList();
            this.ResortPlugins();

            this.DownloadPluginIcons();
        }

        private void OnInstalledPluginsChanged()
        {
            this.pluginListInstalled = this.dalamud.PluginManager.InstalledPlugins.ToList();
            this.pluginListUpdatable = this.dalamud.PluginManager.UpdatablePlugins.ToList();
            this.hasDevPlugins = this.pluginListInstalled.Any(plugin => plugin.IsDev);
            this.ResortPlugins();

            this.DownloadPluginIcons();
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
            this.dalamud.Configuration.SeenPluginInternalName.Contains(internalName);

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

        private void DownloadPluginIcons()
        {
            if (this.downloadingIcons)
            {
                Log.Error("Already downloading icons, skipping...");
                return;
            }

            this.downloadingIcons = true;

            Log.Verbose("Start downloading plugin icons...");

            Task.Run(async () =>
            {
                var plugins = this.pluginListAvailable.Select(x => x as PluginManifest)
                                  .Concat(this.pluginListInstalled.Select(x => x.Manifest)).ToList();

                foreach (var pluginManifest in plugins)
                {
                    if (!this.pluginIconMap.ContainsKey(pluginManifest.InternalName))
                        await this.DownloadPluginIconAsync(pluginManifest);
                }
            }).ContinueWith(t =>
            {
                Log.Verbose($"Icon download finished, faulted: {t.IsFaulted}");
                this.downloadingIcons = false;
            });
        }

        private async Task DownloadPluginIconAsync(PluginManifest manifest)
        {
            Log.Verbose($"Downloading icon for {manifest.InternalName}");
            this.pluginIconMap.Add(manifest.InternalName, (false, null));

            var client = new HttpClient();

            if (manifest.IconUrl != null)
            {
                var data = await client.GetAsync(manifest.IconUrl);
                data.EnsureSuccessStatusCode();
                var icon = this.dalamud.InterfaceManager.LoadImage(await data.Content.ReadAsByteArrayAsync());

                if (icon != null)
                {
                    if (icon.Height != PluginIconHeight || icon.Width != PluginIconWidth)
                    {
                        Log.Error($"Icon at {manifest.IconUrl} was not of the correct resolution.");
                        return;
                    }

                    this.pluginIconMap[manifest.InternalName] = (true, icon);
                }
            }
        }

        private async Task DownloadPluginImagesAsync(PluginManifest manifest)
        {
            Log.Verbose($"Downloading images for {manifest.InternalName}");

            this.pluginImagesMap.Add(manifest.InternalName, (false, null));

            var client = new HttpClient();

            if (manifest.ImageUrls != null)
            {
                if (manifest.ImageUrls.Count > 5)
                {
                    Log.Error($"Plugin {manifest.InternalName} has too many images.");
                    return;
                }

                var pluginImages = new TextureWrap[manifest.ImageUrls.Count];
                for (var i = 0; i < manifest.ImageUrls.Count; i++)
                {
                    var data = await client.GetAsync(manifest.ImageUrls[i]);
                    data.EnsureSuccessStatusCode();
                    var image = this.dalamud.InterfaceManager.LoadImage(await data.Content.ReadAsByteArrayAsync());

                    if (image == null)
                    {
                        return;
                    }

                    if (image.Height != PluginImageHeight || image.Width != PluginImageWidth)
                    {
                        Log.Error($"Image at {manifest.ImageUrls[i]} was not of the correct resolution.");
                        return;
                    }

                    pluginImages[i] = image;
                }

                this.pluginImagesMap[manifest.InternalName] = (true, pluginImages);
            }

            Log.Verbose($"Plugin images for {manifest.InternalName} downloaded");
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

            public static string PluginTitleMod_New => Loc.Localize("InstallerNewPlugin ", " New!");

            #endregion

            #region Plugin context menu

            public static string PluginContext_MarkAllSeen => Loc.Localize("InstallerMarkAllSeen", "Mark all as seen");

            public static string PluginContext_HidePlugin => Loc.Localize("InstallerHidePlugin", "Hide from installer");

            public static string PluginContext_DeletePluginConfig => Loc.Localize("InstallerDeletePluginConfig", "Reset plugin");

            public static string PluginContext_DeletePluginConfigReload => Loc.Localize("InstallerDeletePluginConfig", "Reset plugin settings & reload");

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

            public static string Notifications_PluginNotInstalled(string name) => Loc.Localize("NotificationsPluginInstalled", "'{0}' failed to install.").Format(name);

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

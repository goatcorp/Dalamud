using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Exceptions;
using Dalamud.Plugin.Internal.Types;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// Class responsible for drawing the plugin installer.
    /// </summary>
    internal class PluginInstallerWindow : Window, IDisposable
    {
        private static readonly ModuleLog Log = new("PLUGINW");

        private readonly Dalamud dalamud;

        private bool errorModalDrawing = true;
        private bool errorModalOnNextFrame = false;
        private string errorModalMessage = string.Empty;

        private int updatePluginCount = 0;
        private List<PluginUpdateStatus> updatedPlugins;

        private List<RemotePluginManifest> pluginListAvailable = new();
        private List<LocalPlugin> pluginListInstalled = new();
        private List<AvailablePluginUpdate> pluginListUpdatable = new();
        private bool hasDevPlugins = false;

        private string searchText = string.Empty;

        private PluginSortKind sortKind = PluginSortKind.Alphabetical;
        private string filterText = Locs.SortBy_Alphabetical;

        private OperationStatus installStatus = OperationStatus.Idle;
        private OperationStatus updateStatus = OperationStatus.Idle;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInstallerWindow"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        public PluginInstallerWindow(Dalamud dalamud)
            : base(
                Locs.WindowTitle + (dalamud.Configuration.DoPluginTest ? Locs.WindowTitleMod_Testing : string.Empty) + "###XlPluginInstaller",
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)
        {
            this.dalamud = dalamud;
            this.IsOpen = true;

            this.Size = new Vector2(810, 520);
            this.SizeCondition = ImGuiCond.Always;

            // For debugging
            if (this.dalamud.PluginManager.PluginsReady)
                this.OnInstalledPluginsChanged();

            this.dalamud.PluginManager.OnAvailablePluginsChanged += this.OnAvailablePluginsChanged;
            this.dalamud.PluginManager.OnInstalledPluginsChanged += this.OnInstalledPluginsChanged;
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
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.dalamud.PluginManager.OnAvailablePluginsChanged -= this.OnAvailablePluginsChanged;
            this.dalamud.PluginManager.OnInstalledPluginsChanged -= this.OnInstalledPluginsChanged;
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

            ImGui.SameLine(windowSize.X - closeButtonSize.X);
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

                    Task.Run(() => this.dalamud.PluginManager.UpdatePlugins())
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
                ImGui.BeginChild($"Scrolling{title}", ImGuiHelpers.ScaledVector2(0, 384), true, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoBackground);

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

        private void DrawAvailablePlugin(RemotePluginManifest manifest, int index)
        {
            var useTesting = this.dalamud.PluginManager.UseTesting(manifest);

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

            if (ImGui.CollapsingHeader($"{label}###Header{index}{manifest.InternalName}"))
            {
                ImGui.Indent();

                // Name
                ImGui.Text(manifest.Name);

                // Download count
                var downloadCountText = manifest.DownloadCount > 0
                    ? Locs.PluginBody_AuthorWithDownloadCount(manifest.Author, manifest.DownloadCount)
                    : Locs.PluginBody_AuthorWithDownloadCountUnavailable(manifest.Author);

                ImGui.SameLine();
                ImGui.TextColored(ImGuiColors.DalamudGrey3, downloadCountText);

                // Installable from
                if (manifest.SourceRepo.IsThirdParty)
                {
                    var repoText = Locs.PluginBody_Plugin3rdPartyRepo(manifest.SourceRepo.PluginMasterUrl);
                    ImGui.TextColored(ImGuiColors.DalamudGrey3, repoText);
                }

                // Description
                if (!string.IsNullOrWhiteSpace(manifest.Description))
                {
                    ImGui.TextWrapped(manifest.Description);
                }

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
                    if (ImGui.Button(Locs.PluginButton_InstallVersion(versionString)))
                    {
                        this.installStatus = OperationStatus.InProgress;

                        Task.Run(() => this.dalamud.PluginManager.InstallPlugin(manifest, useTesting, PluginLoadReason.Installer))
                            .ContinueWith(task =>
                            {
                                // There is no need to set as Complete for an individual plugin installation
                                this.installStatus = OperationStatus.Idle;
                                this.DisplayErrorContinuation(task, Locs.ErrorModal_InstallFail(manifest.Name));
                            });
                    }
                }

                this.DrawVisitRepoUrlButton(manifest.RepoUrl);

                ImGui.Unindent();
            }

            if (ImGui.BeginPopupContextItem("ItemContextMenu"))
            {
                if (ImGui.Selectable(Locs.PluginContext_HidePlugin))
                {
                    Log.Debug($"Adding {manifest.InternalName} to hidden plugins");
                    this.dalamud.Configuration.HiddenPluginInternalName.Add(manifest.InternalName);
                    this.dalamud.Configuration.Save();
                    this.dalamud.PluginManager.RefilterPluginMasters();
                }

                ImGui.EndPopup();
            }
        }

        private void DrawInstalledPlugin(LocalPlugin plugin, int index, bool showInstalled = false)
        {
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
            }

            // Load error
            if (plugin.State == PluginState.LoadError)
            {
                label += Locs.PluginTitleMod_LoadError;
            }

            // Unload error
            if (plugin.State == PluginState.UnloadError)
            {
                label += Locs.PluginTitleMod_UnloadError;
            }

            // Update available
            if (this.pluginListUpdatable.FirstOrDefault(up => up.InstalledPlugin == plugin) != default)
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

            if (ImGui.CollapsingHeader($"{label}###Header{index}{plugin.Manifest.InternalName}"))
            {
                var manifest = plugin.Manifest;

                ImGui.Indent();

                // Name
                ImGui.Text(manifest.Name);

                // Download count
                var downloadText = manifest.DownloadCount > 0
                    ? Locs.PluginBody_AuthorWithDownloadCount(manifest.Author, manifest.DownloadCount)
                    : Locs.PluginBody_AuthorWithDownloadCountUnavailable(manifest.Author);

                ImGui.SameLine();
                ImGui.TextColored(ImGuiColors.DalamudGrey3, downloadText);

                // Installed from
                if (!string.IsNullOrEmpty(manifest.InstalledFromUrl))
                {
                    var repoText = Locs.PluginBody_Plugin3rdPartyRepo(manifest.InstalledFromUrl);
                    ImGui.TextColored(ImGuiColors.DalamudGrey3, repoText);
                }

                // Description
                if (!string.IsNullOrWhiteSpace(manifest.Description))
                {
                    ImGui.TextWrapped(manifest.Description);
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

                ImGui.SameLine();
                ImGui.TextColored(ImGuiColors.DalamudGrey3, $" v{plugin.Manifest.AssemblyVersion}");

                if (plugin.IsDev)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(ImGuiColors.DalamudRed, Locs.PluginBody_DeleteDevPlugin);
                }

                ImGui.Unindent();
            }
        }

        private void DrawPluginControlButton(LocalPlugin plugin)
        {
            // Disable everything if the updater is running or another plugin is operating
            var disabled = this.updateStatus == OperationStatus.InProgress || this.installStatus == OperationStatus.InProgress;

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
                        plugin.DalamudInterface.UiBuilder.OpenConfigUi();
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
        }

        private void OnInstalledPluginsChanged()
        {
            this.pluginListInstalled = this.dalamud.PluginManager.InstalledPlugins.ToList();
            this.pluginListUpdatable = this.dalamud.PluginManager.UpdatablePlugins.ToList();
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
                default:
                    throw new InvalidEnumArgumentException("Unknown plugin sort type.");
            }
        }

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
                this.errorModalMessage = state as string;

                foreach (var ex in task.Exception.InnerExceptions)
                {
                    if (ex is PluginException)
                    {
                        Log.Error(ex, "Plugin installer threw an error");
#if DEBUG
                        if (!string.IsNullOrEmpty(ex.Message))
                            this.errorModalMessage += $"\n\n{ex.Message}";
#endif
                    }
                    else
                    {
                        Log.Error(ex, "Plugin installer threw an unexpected error");
#if DEBUG
                        if (!string.IsNullOrEmpty(ex.Message))
                            this.errorModalMessage += $"\n\n{ex.Message}";
#endif
                    }
                }

                this.errorModalDrawing = true;
                this.errorModalOnNextFrame = true;

                return false;
            }

            return true;
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

            #endregion

            #region Plugin context menu

            public static string PluginContext_HidePlugin => Loc.Localize("InstallerHidePlugin", "Hide from installer");

            #endregion

            #region Plugin body

            public static string PluginBody_AuthorWithDownloadCount(string author, long count) => Loc.Localize("InstallerAuthorWithDownloadCount", " by {0}, {1} downloads").Format(author, count);

            public static string PluginBody_AuthorWithDownloadCountUnavailable(string author) => Loc.Localize("InstallerAuthorWithDownloadCountUnavailable", " by {0}, download count unavailable").Format(author);

            public static string PluginBody_Plugin3rdPartyRepo(string url) => Loc.Localize("InstallerPlugin3rdPartyRepo", "From custom plugin repository {0}").Format(url);

            public static string PluginBody_AvailableDevPlugin => Loc.Localize("InstallerDevPlugin", " This plugin is available in one of your repos, please remove it from the devPlugins folder.");

            public static string PluginBody_DeleteDevPlugin => Loc.Localize("InstallerDeleteDevPlugin ", " To delete this plugin, please remove it from the devPlugins folder.");

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

            public static string ErrorModal_InstallFail(string name) => Loc.Localize("InstallerInstallFail", "Failed to install plugin {0}.").Format(name);

            public static string ErrorModal_EnableFail(string name) => Loc.Localize("InstallerEnableFail", "Failed to enable plugin {0}.").Format(name);

            public static string ErrorModal_DisableFail(string name) => Loc.Localize("InstallerDisableFail", "Failed to disable plugin {0}.").Format(name);

            public static string ErrorModal_UnloadFail(string name) => Loc.Localize("InstallerUnloadFail", "Failed to unload plugin {0}.").Format(name);

            public static string ErrorModal_LoadFail(string name) => Loc.Localize("InstallerLoadFail", "Failed to load plugin {0}.").Format(name);

            public static string ErrorModal_DeleteFail(string name) => Loc.Localize("InstallerDeleteFail", "Failed to delete plugin {0}.").Format(name);

            public static string ErrorModal_UpdaterFatal => Loc.Localize("InstallerUpdaterFatal", "Failed to update plugins.");

            public static string ErrorModal_UpdaterFail(int failCount) => Loc.Localize("InstallerUpdaterFail", "Failed to update {0} plugins.").Format(failCount);

            public static string ErrorModal_UpdaterFailPartial(int successCount, int failCount) => Loc.Localize("InstallerUpdaterFailPartial", "Updated {0} plugins, failed to update {1}.").Format(successCount, failCount);

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

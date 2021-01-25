using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using CheapLoc;
using Dalamud.Interface;
using ImGuiNET;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Plugin
{
    internal class PluginInstallerWindow {
        private readonly Dalamud dalamud;
        private string gameVersion;

        private bool errorModalDrawing = true;
        private bool errorModalOnNextFrame = false;

        private bool updateComplete = false;
        private int updatePluginCount = 0;
        private List<PluginRepository.PluginUpdateStatus> updatedPlugins;

        private List<PluginDefinition> pluginListAvailable;
        private List<PluginDefinition> pluginListInstalled;

        private string searchText = "";

        private readonly Vector4 colorGrey = new Vector4(0.70f, 0.70f, 0.70f, 1.00f);

        private enum PluginInstallStatus {
            None,
            InProgress,
            Success,
            Fail
        }

        private PluginInstallStatus installStatus = PluginInstallStatus.None;

        private enum PluginSortKind {
            Alphabetical,
            DownloadCount,
            LastUpdate
        }

        private PluginSortKind sortKind = PluginSortKind.Alphabetical;
        private string filterText = Loc.Localize("SortAlphabetical", "Alphabetical");

        public PluginInstallerWindow(Dalamud dalamud, string gameVersion) {
            this.dalamud = dalamud;
            this.gameVersion = gameVersion;

            if (this.dalamud.PluginRepository.State != PluginRepository.InitializationState.InProgress)
                this.dalamud.PluginRepository.ReloadPluginMasterAsync();
        }

        private void RefetchPlugins() {
            var hiddenPlugins = this.dalamud.PluginManager.Plugins.Where(
                        x => this.dalamud.PluginRepository.PluginMaster.All(
                            y => y.InternalName != x.Definition.InternalName || y.InternalName == x.Definition.InternalName && y.IsHide)).Select(x => x.Definition).ToList();
            this.pluginListInstalled = this.dalamud.PluginRepository.PluginMaster
                                           .Where(def => {
                                               return this.dalamud.PluginManager.Plugins.Where(x => x.Definition != null).Any(
                                                   x => x.Definition.InternalName == def.InternalName);
                                           })
                                           .ToList();
            this.pluginListInstalled.AddRange(hiddenPlugins);
            this.pluginListInstalled.Sort((x, y) => x.Name.CompareTo(y.Name));

            ResortPlugins();
        }

        private void ResortPlugins() {
            var availableDefs = this.dalamud.PluginRepository.PluginMaster.Where(
                x => this.pluginListInstalled.All(y => x.InternalName != y.InternalName)).ToList();

            switch (this.sortKind) {
                case PluginSortKind.Alphabetical:
                    this.pluginListAvailable = availableDefs.OrderBy(x => x.Name).ToList();
                    this.pluginListInstalled.Sort((x, y) => x.Name.CompareTo(y.Name));
                    break;
                case PluginSortKind.DownloadCount:
                    this.pluginListAvailable = availableDefs.OrderByDescending(x => x.DownloadCount).ToList();
                    this.pluginListInstalled.Sort((x, y) => y.DownloadCount.CompareTo(x.DownloadCount));
                    break;
                case PluginSortKind.LastUpdate:
                    this.pluginListAvailable = availableDefs.OrderByDescending(x => x.LastUpdate).ToList();
                    this.pluginListInstalled.Sort((x, y) => y.LastUpdate.CompareTo(x.LastUpdate));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool Draw() {
            var windowOpen = true;

            ImGui.SetNextWindowSize(new Vector2(810, 520) * ImGui.GetIO().FontGlobalScale);

            ImGui.Begin(Loc.Localize("InstallerHeader", "Plugin Installer") + (this.dalamud.Configuration.DoPluginTest ? " (TESTING)" : string.Empty) + "###XlPluginInstaller", ref windowOpen,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar);

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (5 * ImGui.GetIO().FontGlobalScale));
            var descriptionText = Loc.Localize("InstallerHint", "This window allows you to install and remove in-game plugins.\nThey are made by third-party developers.");
            ImGui.Text(descriptionText);

            var sortingTextSize = ImGui.CalcTextSize(Loc.Localize("SortDownloadCounts", "Download Count")) + ImGui.CalcTextSize(Loc.Localize("PluginSort", "Sort By"));
            ImGui.SameLine(ImGui.GetWindowWidth() - sortingTextSize.X - ((250 + 20) * ImGui.GetIO().FontGlobalScale));
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.CalcTextSize(descriptionText).Y / 4) - 2);
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - sortingTextSize.X - ((250 + 20) * ImGui.GetIO().FontGlobalScale));

            ImGui.SetNextItemWidth(240 * ImGui.GetIO().FontGlobalScale);
            ImGui.InputTextWithHint("###XPlPluginInstaller_Search", Loc.Localize("InstallerSearch", "Search"), ref this.searchText, 100);

            ImGui.SameLine();
            ImGui.SetNextItemWidth((10 * ImGui.GetIO().FontGlobalScale) + ImGui.CalcTextSize(Loc.Localize("SortDownloadCounts", "Download Count")).X);
            if (ImGui.BeginCombo(Loc.Localize("PluginSort", "Sort By"), this.filterText, ImGuiComboFlags.NoArrowButton)) {
                if (ImGui.Selectable(Loc.Localize("SortAlphabetical", "Alphabetical"))) {
                    this.sortKind = PluginSortKind.Alphabetical;
                    this.filterText = Loc.Localize("SortAlphabetical", "Alphabetical");

                    ResortPlugins();
                }
                    
                if (ImGui.Selectable(Loc.Localize("SortDownloadCounts", "Download Count"))) {
                    this.sortKind = PluginSortKind.DownloadCount;
                    this.filterText = Loc.Localize("SortDownloadCounts", "Download Count");

                    ResortPlugins();
                }
                    
                if (ImGui.Selectable(Loc.Localize("SortLastUpdate", "Last Update"))) {
                    this.sortKind = PluginSortKind.LastUpdate;
                    this.filterText = Loc.Localize("SortLastUpdate", "Last Update");

                    ResortPlugins();
                }

                ImGui.EndCombo();
            }

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (5 * ImGui.GetIO().FontGlobalScale));

            string initializationStatusText = null;
            if (this.dalamud.PluginRepository.State == PluginRepository.InitializationState.InProgress) {
                initializationStatusText = Loc.Localize("InstallerLoading", "Loading plugins...");
                this.pluginListAvailable = null;
            } else if (this.dalamud.PluginRepository.State == PluginRepository.InitializationState.Fail) {
                initializationStatusText = Loc.Localize("InstallerDownloadFailed", "Download failed.");
                this.pluginListAvailable = null;
            } else if (this.dalamud.PluginRepository.State == PluginRepository.InitializationState.FailThirdRepo) {
                initializationStatusText = Loc.Localize("InstallerDownloadFailedThird", "One of your third party repos is unreachable or there is no internet connection.");
                this.pluginListAvailable = null;
            } else {
                if (this.pluginListAvailable == null) {
                    RefetchPlugins();
                }
            }

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(1, 3) * ImGui.GetIO().FontGlobalScale);

            if (ImGui.BeginTabBar("PluginsTabBar", ImGuiTabBarFlags.NoTooltip)) {
                DrawTab(false, initializationStatusText);
                DrawTab(true, initializationStatusText);

                ImGui.EndTabBar();
                ImGui.Separator();
            }

            ImGui.PopStyleVar();

            ImGui.Dummy(new Vector2(3f, 3f) * ImGui.GetIO().FontGlobalScale);

            if (this.installStatus == PluginInstallStatus.InProgress) {
                ImGui.Button(Loc.Localize("InstallerUpdating", "Updating..."));
            } else {
                if (this.updateComplete) {
                    ImGui.Button(this.updatePluginCount == 0
                                     ? Loc.Localize("InstallerNoUpdates", "No updates found!")
                                     : string.Format(Loc.Localize("InstallerUpdateComplete", "{0} plugins updated!"), this.updatePluginCount));
                } else {
                    if (ImGui.Button(Loc.Localize("InstallerUpdatePlugins", "Update plugins"))) {
                        this.installStatus = PluginInstallStatus.InProgress;

                        Task.Run(() => this.dalamud.PluginRepository.UpdatePlugins()).ContinueWith(t => {
                            this.installStatus =
                                t.Result.Success ? PluginInstallStatus.Success : PluginInstallStatus.Fail;
                            this.installStatus =
                                t.IsFaulted ? PluginInstallStatus.Fail : this.installStatus;

                            if (this.installStatus == PluginInstallStatus.Success) {
                                this.updateComplete = true;
                            }

                            if (t.Result.UpdatedPlugins != null) {
                                this.updatePluginCount = t.Result.UpdatedPlugins.Count;
                                this.updatedPlugins = t.Result.UpdatedPlugins;
                            }

                            this.errorModalDrawing = this.installStatus == PluginInstallStatus.Fail;
                            this.errorModalOnNextFrame = this.installStatus == PluginInstallStatus.Fail;

                            this.dalamud.PluginRepository.PrintUpdatedPlugins(updatedPlugins, Loc.Localize("DalamudPluginUpdates", "Updates:"));
                        });
                    }
                }
            }
            

            ImGui.SameLine();

            if (ImGui.Button(Loc.Localize("SettingsInstaller", "Settings"))) {
                this.dalamud.DalamudUi.OpenSettings();
            }

            var closeText = Loc.Localize("Close", "Close");

            ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.CalcTextSize(closeText).X - (16 * ImGui.GetIO().FontGlobalScale));
            if (ImGui.Button(closeText)) {
                windowOpen = false;
                this.dalamud.Configuration.Save();
            }


            if (ImGui.BeginPopupModal(Loc.Localize("InstallerError","Installer failed"), ref this.errorModalDrawing, ImGuiWindowFlags.AlwaysAutoResize)) {
                var message = Loc.Localize("InstallerErrorHint",
                                           "The plugin installer ran into an issue or the plugin is incompatible.\nPlease restart the game and report this error on our discord.");

                if (this.updatedPlugins != null) {
                    if (this.updatedPlugins.Any(x => x.WasUpdated == false)) {
                        var extraInfoMessage = Loc.Localize("InstallerErrorPluginInfo",
                                                            "\n\nThe following plugins caused these issues:\n\n{0}\nYou may try removing these plugins manually and reinstalling them.");

                        var insert = this.updatedPlugins.Where(x => x.WasUpdated == false)
                                         .Aggregate(string.Empty,
                                                    (current, pluginUpdateStatus) =>
                                                        current + $"* {pluginUpdateStatus.InternalName}\n");
                        extraInfoMessage = string.Format(extraInfoMessage, insert);
                        message += extraInfoMessage;
                    }
                }

                ImGui.Text(message);

                ImGui.Spacing();

                if (ImGui.Button(Loc.Localize("OK", "OK"), new Vector2(120, 40))) { ImGui.CloseCurrentPopup(); }

                ImGui.EndPopup();
            }

            if (this.errorModalOnNextFrame) {
                ImGui.OpenPopup(Loc.Localize("InstallerError", "Installer failed"));
                this.errorModalOnNextFrame = false;
            }
                

            ImGui.End();

            return windowOpen;
        }

        private void DrawTab(bool installed, string statusText) {
            if (ImGui.BeginTabItem(installed ? Loc.Localize("InstallerInstalledPluginList", "Installed Plugins")
                                       : Loc.Localize("InstallerAvailablePluginList", "Available Plugins"))) {
                ImGui.BeginChild("Scrolling" + (installed ? "Installed" : "Available"),
                                 new Vector2(0, 384 * ImGui.GetIO().FontGlobalScale), true, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoBackground);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5);

                if (statusText != null) {
                    ImGui.TextColored(this.colorGrey, statusText);
                } else {
                    DrawPluginList(installed ? this.pluginListInstalled : this.pluginListAvailable, installed);
                }

                ImGui.EndChild();
                ImGui.EndTabItem();
            }
        }

        private void DrawPluginList(List<PluginDefinition> pluginDefinitions, bool installed) {
            var didAny = false;
            var didAnyWithSearch = false;
            var hasSearchString = !string.IsNullOrWhiteSpace(this.searchText);

            for (var index = 0; index < pluginDefinitions.Count; index++) {
                var pluginDefinition = pluginDefinitions[index];

                if (pluginDefinition.ApplicableVersion != this.gameVersion &&
                    pluginDefinition.ApplicableVersion != "any")
                    continue;

                if (pluginDefinition.IsHide)
                    continue;

                if (pluginDefinition.DalamudApiLevel < PluginManager.DALAMUD_API_LEVEL)
                    continue;

                if (this.dalamud.Configuration.HiddenPluginInternalName.Contains(pluginDefinition.InternalName))
                    continue;

                didAny = true;

                if (hasSearchString &&
                    !(pluginDefinition.Name.ToLowerInvariant().Contains(this.searchText.ToLowerInvariant()) ||
                      string.Equals(pluginDefinition.Author, this.searchText,
                                    StringComparison.InvariantCultureIgnoreCase) ||
                      pluginDefinition.Tags != null &&
                      pluginDefinition.Tags.Contains(this.searchText.ToLowerInvariant(),
                                                     StringComparer.InvariantCultureIgnoreCase)
                     ))
                    continue;

                didAnyWithSearch = true;

                var isInstalled = this.dalamud.PluginManager.Plugins.Where(x => x.Definition != null).Any(
                    x => x.Definition.InternalName == pluginDefinition.InternalName);

                var isTestingAvailable = false;
                if (Version.TryParse(pluginDefinition.AssemblyVersion, out var assemblyVersion) &&
                    Version.TryParse(pluginDefinition.TestingAssemblyVersion, out var testingAssemblyVersion))
                    isTestingAvailable = this.dalamud.Configuration.DoPluginTest &&
                                         testingAssemblyVersion > assemblyVersion;

                if (this.dalamud.Configuration.DoPluginTest && pluginDefinition.IsTestingExclusive)
                    isTestingAvailable = true;
                else if (!this.dalamud.Configuration.DoPluginTest && pluginDefinition.IsTestingExclusive) continue;

                var label = string.Empty;
                if (isInstalled && !installed) {
                    label += Loc.Localize("InstallerInstalled", " (installed)");
                } else if (!isInstalled && installed) {
                    label += Loc.Localize("InstallerDisabled", " (disabled)");
                }

                if (this.updatedPlugins != null &&
                    this.updatedPlugins.Any(x => x.InternalName == pluginDefinition.InternalName && x.WasUpdated))
                    label += Loc.Localize("InstallerUpdated", " (updated)");
                else if (this.updatedPlugins != null &&
                         this.updatedPlugins.Any(x => x.InternalName == pluginDefinition.InternalName &&
                                                      x.WasUpdated == false))
                    label += Loc.Localize("InstallerUpdateFailed", " (update failed)");

                if (isTestingAvailable)
                    label += Loc.Localize("InstallerTestingVersion", " (testing version)");

                ImGui.PushID(pluginDefinition.InternalName + pluginDefinition.AssemblyVersion + installed + index);

                if (ImGui.CollapsingHeader(pluginDefinition.Name + label + "###Header" + pluginDefinition.InternalName)) {
                    ImGui.Indent();

                    ImGui.Text(pluginDefinition.Name);

                    ImGui.SameLine();

                    var info = $" by {pluginDefinition.Author}";
                    info += pluginDefinition.DownloadCount != 0
                                ? $", {pluginDefinition.DownloadCount} downloads"
                                : ", download count unavailable";
                    if (pluginDefinition.FromRepo != "goatcorp.github.io" &&
                        !string.IsNullOrWhiteSpace(pluginDefinition.FromRepo))
                        info += $", from {pluginDefinition.FromRepo}";
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), info);

                    if (!string.IsNullOrWhiteSpace(pluginDefinition.Description))
                        ImGui.TextWrapped(pluginDefinition.Description);

                    if (!isInstalled) {
                        if (this.installStatus == PluginInstallStatus.InProgress) {
                            ImGui.Button(Loc.Localize("InstallerInProgress", "Install in progress..."));
                        } else {
                            var versionString = isTestingAvailable
                                                    ? pluginDefinition.TestingAssemblyVersion + " (testing version)"
                                                    : pluginDefinition.AssemblyVersion;

                            if (ImGui.Button($"Install v{versionString}")) {
                                this.installStatus = PluginInstallStatus.InProgress;

                                Task.Run(() => this.dalamud.PluginRepository.InstallPlugin(
                                             pluginDefinition, true, false, isTestingAvailable)).ContinueWith(t => {
                                                 this.installStatus =
                                                 t.Result ? PluginInstallStatus.Success : PluginInstallStatus.Fail;
                                                 this.installStatus =
                                                 t.IsFaulted ? PluginInstallStatus.Fail : this.installStatus;

                                                 this.errorModalDrawing = this.installStatus == PluginInstallStatus.Fail;
                                                 this.errorModalOnNextFrame = this.installStatus == PluginInstallStatus.Fail;
                                             });
                            }
                        }

                        if (!string.IsNullOrEmpty(pluginDefinition.RepoUrl)) {
                            ImGui.PushFont(InterfaceManager.IconFont);

                            ImGui.SameLine();
                            if (ImGui.Button(FontAwesomeIcon.Globe.ToIconString()) &&
                                pluginDefinition.RepoUrl.StartsWith("https://"))
                                Process.Start(pluginDefinition.RepoUrl);

                            ImGui.PopFont();
                        }
                    } else {
                        var installedPlugin = this.dalamud.PluginManager.Plugins.Where(x => x.Definition != null).First(
                            x => x.Definition.InternalName ==
                                 pluginDefinition.InternalName);

                        var commands = this.dalamud.CommandManager.Commands.Where(
                            x => x.Value.LoaderAssemblyName == installedPlugin.Definition?.InternalName &&
                                 x.Value.ShowInHelp);
                        if (commands.Any()) {
                            ImGui.Dummy(new Vector2(10f, 10f) * ImGui.GetIO().FontGlobalScale);
                            foreach (var command in commands)
                                ImGui.TextWrapped($"{command.Key} → {command.Value.HelpMessage}");
                        }

                        ImGui.NewLine();

                        if (!installedPlugin.IsRaw) {
                            ImGui.SameLine();

                            if (ImGui.Button(Loc.Localize("InstallerDisable", "Disable"))) {
                                try {
                                    this.dalamud.PluginManager.DisablePlugin(installedPlugin.Definition);
                                } catch (Exception exception) {
                                    Log.Error(exception, "Could not disable plugin.");
                                    this.errorModalDrawing = true;
                                    this.errorModalOnNextFrame = true;
                                }
                            }
                        }

                        if (installedPlugin.PluginInterface.UiBuilder.OnOpenConfigUi != null) {
                            ImGui.SameLine();

                            if (ImGui.Button(Loc.Localize("InstallerOpenConfig", "Open Configuration")))
                                installedPlugin.PluginInterface.UiBuilder.OnOpenConfigUi?.Invoke(null, null);
                        }

                        if (!string.IsNullOrEmpty(installedPlugin.Definition.RepoUrl)) {
                            ImGui.PushFont(InterfaceManager.IconFont);

                            ImGui.SameLine();
                            if (ImGui.Button(FontAwesomeIcon.Globe.ToIconString()) &&
                                installedPlugin.Definition.RepoUrl.StartsWith("https://"))
                                Process.Start(installedPlugin.Definition.RepoUrl);

                            ImGui.PopFont();
                        }

                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), $" v{installedPlugin.Definition.AssemblyVersion}");

                        if (installedPlugin.IsRaw) {
                            ImGui.SameLine();
                            ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                                              " To update or disable this plugin, please remove it from the devPlugins folder.");
                        }
                    }

                    ImGui.Unindent();
                }

                if (ImGui.BeginPopupContextItem("item context menu")) {
                    if (ImGui.Selectable("Hide from installer"))
                        this.dalamud.Configuration.HiddenPluginInternalName.Add(pluginDefinition.InternalName);
                    ImGui.EndPopup();
                }

                ImGui.PopID();
            }

            if (!didAny) {
                if (installed) {
                    ImGui.TextColored(this.colorGrey,
                                      Loc.Localize("InstallerNoInstalled",
                                                   "No plugins are currently installed. You can install them from the Available Plugins tab."));
                } else {
                    ImGui.TextColored(this.colorGrey,
                                      Loc.Localize("InstallerNoCompatible",
                                                   "No compatible plugins were found :( Please restart your game and try again."));
                }
            } else if (!didAnyWithSearch)
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                                  Loc.Localize("InstallNoMatching", "No plugins were found matching your search."));
        }
    }
}

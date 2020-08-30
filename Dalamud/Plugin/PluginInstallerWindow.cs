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

        private string searchText = "";

        private enum PluginInstallStatus {
            None,
            InProgress,
            Success,
            Fail
        }

        private PluginInstallStatus installStatus = PluginInstallStatus.None;

        public PluginInstallerWindow(Dalamud dalamud, string gameVersion) {
            this.dalamud = dalamud;
            this.gameVersion = gameVersion;

            if (this.dalamud.PluginRepository.State != PluginRepository.InitializationState.InProgress)
                this.dalamud.PluginRepository.ReloadPluginMasterAsync();
        }

        public bool Draw() {
            var windowOpen = true;

            ImGui.SetNextWindowSize(new Vector2(750, 520));

            ImGui.Begin(Loc.Localize("InstallerHeader", "Plugin Installer") + (this.dalamud.Configuration.DoPluginTest ? " (TESTING)" : string.Empty) + "###XlPluginInstaller", ref windowOpen,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar);

            ImGui.Text(Loc.Localize("InstallerHint", "This window allows you install and remove in-game plugins.\nThey are made by third-party developers."));
            ImGui.SameLine(ImGui.GetWindowWidth() - 250);
            ImGui.SetNextItemWidth(240);
            ImGui.InputTextWithHint("###XPlPluginInstaller_Search", Loc.Localize("InstallerSearch", "Search"), ref this.searchText, 100);
            ImGui.Separator();

            ImGui.BeginChild("scrolling", new Vector2(0, 400), true, ImGuiWindowFlags.HorizontalScrollbar);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(1, 3));

            if (this.dalamud.PluginRepository.State == PluginRepository.InitializationState.InProgress) {
                ImGui.Text(Loc.Localize("InstallerLoading", "Loading plugins..."));
            } else if (this.dalamud.PluginRepository.State == PluginRepository.InitializationState.Fail) {
                ImGui.Text(Loc.Localize("InstallerDownloadFailed", "Download failed."));
            }
            else {
                var didAny = false;
                var didAnyWithSearch = false;
                var hasSearchString = !string.IsNullOrWhiteSpace(this.searchText);

                foreach (var pluginDefinition in this.dalamud.PluginRepository.PluginMaster) {
                    if (pluginDefinition.ApplicableVersion != this.gameVersion &&
                        pluginDefinition.ApplicableVersion != "any")
                        continue;

                    if (pluginDefinition.IsHide)
                        continue;

                    if (pluginDefinition.DalamudApiLevel != PluginManager.DALAMUD_API_LEVEL)
                        continue;

                    didAny = true;

                    if (hasSearchString && 
                        !(pluginDefinition.Name.ToLowerInvariant().Contains(this.searchText.ToLowerInvariant()) ||
                          string.Equals(pluginDefinition.Author, this.searchText, StringComparison.InvariantCultureIgnoreCase) ||
                          pluginDefinition.Tags != null &&
                          pluginDefinition.Tags.Contains(this.searchText.ToLowerInvariant(), StringComparer.InvariantCultureIgnoreCase)
                        )) {
                        continue;
                    }

                    didAnyWithSearch = true;

                    ImGui.PushID(pluginDefinition.InternalName + pluginDefinition.AssemblyVersion);

                    var isInstalled = this.dalamud.PluginManager.Plugins.Where(x => x.Definition != null).Any(
                        x => x.Definition.InternalName == pluginDefinition.InternalName);

                    var label = isInstalled ? Loc.Localize("InstallerInstalled", " (installed)") : string.Empty;
                    label = this.updatedPlugins != null &&
                            this.updatedPlugins.Any(x => x.InternalName == pluginDefinition.InternalName && x.WasUpdated)
                                ? Loc.Localize("InstallerUpdated", " (updated)")
                                : label;

                    label = this.updatedPlugins != null &&
                            this.updatedPlugins.Any(x => x.InternalName == pluginDefinition.InternalName && x.WasUpdated == false)
                                ? Loc.Localize("InstallerUpdateFailed", " (update failed)")
                                : label;

                    if (ImGui.CollapsingHeader(pluginDefinition.Name + label + "###Header" + pluginDefinition.InternalName)) {
                        ImGui.Indent();

                        ImGui.Text(pluginDefinition.Name);
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), $" by {pluginDefinition.Author}, {pluginDefinition.DownloadCount} downloads");

                        ImGui.Text(pluginDefinition.Description);

                        if (!isInstalled) {
                            if (this.installStatus == PluginInstallStatus.InProgress) {
                                ImGui.Button(Loc.Localize("InstallerInProgress", "Install in progress..."));
                            } else {
                                if (ImGui.Button($"Install v{pluginDefinition.AssemblyVersion}")) {
                                    this.installStatus = PluginInstallStatus.InProgress;

                                    Task.Run(() => this.dalamud.PluginRepository.InstallPlugin(pluginDefinition)).ContinueWith(t => {
                                        this.installStatus =
                                            t.Result ? PluginInstallStatus.Success : PluginInstallStatus.Fail;
                                        this.installStatus =
                                            t.IsFaulted ? PluginInstallStatus.Fail : this.installStatus;

                                        this.errorModalDrawing = this.installStatus == PluginInstallStatus.Fail;
                                        this.errorModalOnNextFrame = this.installStatus == PluginInstallStatus.Fail;
                                    });
                                }
                            }
                            if (!string.IsNullOrEmpty(pluginDefinition.RepoUrl))
                            {
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

                            if (ImGui.Button(Loc.Localize("InstallerDisable", "Disable")))
                                try {
                                    this.dalamud.PluginManager.DisablePlugin(installedPlugin.Definition);
                                } catch (Exception exception) {
                                    Log.Error(exception, "Could not disable plugin.");
                                    this.errorModalDrawing = true;
                                    this.errorModalOnNextFrame = true;
                                }

                            if (installedPlugin.PluginInterface.UiBuilder.OnOpenConfigUi != null) {
                                ImGui.SameLine();

                                if (ImGui.Button(Loc.Localize("InstallerOpenConfig", "Open Configuration"))) installedPlugin.PluginInterface.UiBuilder.OnOpenConfigUi?.Invoke(null, null);
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
                            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), $" v{pluginDefinition.AssemblyVersion}");
                        }

                        ImGui.Unindent();
                    }

                    ImGui.PopID();
                }

                if (!didAny)
                    ImGui.TextColored(new Vector4(0.70f, 0.70f, 0.70f, 1.00f), Loc.Localize("InstallerNoCompatible", "No compatible plugins were found :( Please restart your game and try again."));
                else if (!didAnyWithSearch)
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), Loc.Localize("InstallNoMatching", "No plugins were found matching your search."));
            }

            ImGui.PopStyleVar();

            ImGui.EndChild();

            ImGui.Separator();

            if (this.installStatus == PluginInstallStatus.InProgress) {
                ImGui.Button(Loc.Localize("InstallerUpdating", "Updating..."));
            } else {
                if (this.updateComplete) {
                    ImGui.Button(this.updatePluginCount == 0
                                     ? Loc.Localize("InstallerNoUpdates", "No updates found!")
                                     : string.Format(Loc.Localize("InstallerUpdateComplete", "{0} plugins updated!"), this.updatePluginCount));
                } else {
                    if (ImGui.Button(Loc.Localize("InstallerUpdatePlugins", "Update plugins")))
                    {
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
                        });
                    }
                }
            }
            

            ImGui.SameLine();

            if (ImGui.Button(Loc.Localize("Close", "Close")))
            {
                windowOpen = false;
            }

            ImGui.Spacing();

            if (ImGui.BeginPopupModal(Loc.Localize("InstallerError","Installer failed"), ref this.errorModalDrawing, ImGuiWindowFlags.AlwaysAutoResize)) {
                var message = Loc.Localize("InstallerErrorHint",
                                           "The plugin installer ran into an issue or the plugin is incompatible.\nPlease restart the game and report this error on our discord.");

                if (this.updatedPlugins != null) {
                    if (this.updatedPlugins.Any(x => x.WasUpdated == false))
                    {
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
    }
}

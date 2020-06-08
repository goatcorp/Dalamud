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
        private const string PluginRepoBaseUrl = "https://goaaats.github.io/DalamudPlugins/";

        private PluginManager manager;
        private PluginRepository repository;
        private string gameVersion;

        private bool errorModalDrawing = true;
        private bool errorModalOnNextFrame = false;

        private bool updateComplete = false;
        private int updatePluginCount = 0;
        private PluginRepository.PluginUpdateStatus[] updatedInternalName;

        private enum PluginInstallStatus {
            None,
            InProgress,
            Success,
            Fail
        }

        private PluginInstallStatus installStatus = PluginInstallStatus.None;

        public PluginInstallerWindow(PluginManager manager, PluginRepository repository, string gameVersion) {
            this.manager = manager;
            this.repository = repository;
            this.gameVersion = gameVersion;
        }

        public bool Draw() {
            var windowOpen = true;

            ImGui.SetNextWindowSize(new Vector2(750, 520));

            ImGui.Begin(Loc.Localize("InstallerHeader", "Plugin Installer"), ref windowOpen,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar);

            ImGui.Text(Loc.Localize("InstallerHint", "This window allows you install and remove in-game plugins.\nThey are made by third-party developers."));
            ImGui.Separator();

            ImGui.BeginChild("scrolling", new Vector2(0, 400), true, ImGuiWindowFlags.HorizontalScrollbar);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(1, 3));

            if (this.repository.State == PluginRepository.InitializationState.InProgress) {
                ImGui.Text(Loc.Localize("InstallerLoading", "Loading plugins..."));
            } else if (this.repository.State == PluginRepository.InitializationState.Fail) {
                ImGui.Text(Loc.Localize("InstallerDownloadFailed", "Download failed."));
            }
            else
            {
                foreach (var pluginDefinition in this.repository.PluginMaster) {
                    if (pluginDefinition.ApplicableVersion != this.gameVersion &&
                        pluginDefinition.ApplicableVersion != "any")
                        continue;

                    if (pluginDefinition.IsHide)
                        continue;

                    ImGui.PushID(pluginDefinition.InternalName + pluginDefinition.AssemblyVersion);

                    var isInstalled = this.manager.Plugins.Where(x => x.Definition != null).Any(
                        x => x.Definition.InternalName == pluginDefinition.InternalName);

                    var label = isInstalled ? Loc.Localize("InstallerInstalled", " (installed)") : string.Empty;
                    label = this.updatedInternalName != null &&
                            this.updatedInternalName.Any(x => x.InternalName == pluginDefinition.InternalName && x.WasUpdated)
                                ? Loc.Localize("InstallerUpdated", " (updated)")
                                : label;

                    label = this.updatedInternalName != null &&
                            this.updatedInternalName.Any(x => x.InternalName == pluginDefinition.InternalName && x.WasUpdated == false)
                                ? Loc.Localize("InstallerUpdateFailed", " (update failed)")
                                : label;

                    if (ImGui.CollapsingHeader(pluginDefinition.Name + label + "###Header" + pluginDefinition.InternalName)) {
                        ImGui.Indent();

                        ImGui.Text(pluginDefinition.Name);
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), $" by {pluginDefinition.Author}");

                        ImGui.Text(pluginDefinition.Description);

                        if (!isInstalled) {
                            if (this.installStatus == PluginInstallStatus.InProgress) {
                                ImGui.Button(Loc.Localize("InstallerInProgress", "Install in progress..."));
                            } else {
                                if (ImGui.Button($"Install v{pluginDefinition.AssemblyVersion}")) {
                                    this.installStatus = PluginInstallStatus.InProgress;

                                    Task.Run(() => this.repository.InstallPlugin(pluginDefinition)).ContinueWith(t => {
                                        this.installStatus =
                                            t.Result ? PluginInstallStatus.Success : PluginInstallStatus.Fail;
                                        this.installStatus =
                                            t.IsFaulted ? PluginInstallStatus.Fail : this.installStatus;

                                        this.errorModalDrawing = this.installStatus == PluginInstallStatus.Fail;
                                        this.errorModalOnNextFrame = this.installStatus == PluginInstallStatus.Fail;
                                    });
                                }
                            }
                        } else {
                            var installedPlugin = this.manager.Plugins.Where(x => x.Definition != null).First(
                                x => x.Definition.InternalName ==
                                     pluginDefinition.InternalName);

                            if (ImGui.Button(Loc.Localize("InstallerDisable", "Disable")))
                                try {
                                    this.manager.DisablePlugin(installedPlugin.Definition);
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

                        Task.Run(() => this.repository.UpdatePlugins()).ContinueWith(t => {
                            this.installStatus =
                                t.Result.Success ? PluginInstallStatus.Success : PluginInstallStatus.Fail;
                            this.installStatus =
                                t.IsFaulted ? PluginInstallStatus.Fail : this.installStatus;

                            if (this.installStatus == PluginInstallStatus.Success) {
                                this.updateComplete = true;
                                this.updatePluginCount = t.Result.UpdatedPlugins.Length;
                                this.updatedInternalName = t.Result.UpdatedPlugins;
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

            if (ImGui.BeginPopupModal(Loc.Localize("InstallerError","Installer failed"), ref this.errorModalDrawing, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text(Loc.Localize("InstallerErrorHint", "The plugin installer ran into an issue or the plugin is incompatible.\nPlease restart the game and report this error on our discord."));

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

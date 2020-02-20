using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin.Features;
using ImGuiNET;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Plugin
{
    class PluginInstallerWindow {
        private const string PluginRepoBaseUrl = "https://goaaats.github.io/DalamudPlugins/";

        private PluginManager manager;
        private string pluginDirectory;
        private ReadOnlyCollection<PluginDefinition> pluginMaster;
        private bool errorModalDrawing = true;

        private enum PluginInstallStatus {
            None,
            InProgress,
            Success,
            Fail
        }

        private PluginInstallStatus installStatus = PluginInstallStatus.None;

        private bool masterDownloadFailed = false;

        public PluginInstallerWindow(PluginManager manager, string pluginDirectory) {
            this.manager = manager;
            this.pluginDirectory = pluginDirectory;
            Task.Run(CachePluginMaster).ContinueWith(t => { this.masterDownloadFailed = t.IsFaulted; });
        }

        private void CachePluginMaster() {
            try {
                using var client = new WebClient();

                var data = client.DownloadString(PluginRepoBaseUrl + "pluginmaster.json");

                this.pluginMaster = JsonConvert.DeserializeObject<ReadOnlyCollection<PluginDefinition>>(data);
            } catch {
                this.masterDownloadFailed = true;
            }
        }

        private void InstallPlugin(PluginDefinition definition) {
            try {
                var path = Path.GetTempFileName();

                Log.Information("Downloading plugin to {0}", path);
            
                using var client = new WebClient();

                client.DownloadFile(PluginRepoBaseUrl + $"/plugins/{definition.InternalName}/latest.zip", path);
                var outputDir = Path.Combine(this.pluginDirectory, definition.InternalName);

                if (File.Exists(Path.Combine(outputDir, ".disabled"))) {
                    Log.Information("Plugin was disabled, re-enabling.");
                    File.Delete(Path.Combine(outputDir, ".disabled"));
                }

                Log.Information("Extracting to {0}", outputDir);

                Directory.CreateDirectory(outputDir);

                ZipFile.ExtractToDirectory(path, outputDir);

                this.installStatus = PluginInstallStatus.Success;
                this.manager.LoadPluginFromAssembly(new FileInfo(Path.Combine(outputDir, $"{definition.InternalName}.dll")));
            } catch (Exception e) {
                Log.Error(e, "Plugin download failed hard.");
                this.installStatus = PluginInstallStatus.Fail;
            }
        }

        public bool Draw() {
            var windowOpen = true;

            ImGui.SetNextWindowSize(new Vector2(750, 520));

            ImGui.Begin("Plugin Installer", ref windowOpen,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar);

            ImGui.Text("This window allows you install and remove in-game plugins.");
            ImGui.Text("They are made by third-party developers.");
            ImGui.Separator();

            ImGui.BeginChild("scrolling", new Vector2(0, 400), true, ImGuiWindowFlags.HorizontalScrollbar);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(1, 3));

            if (this.pluginMaster == null) {
                ImGui.Text("Loading plugins...");
            } else if (this.masterDownloadFailed) {
                ImGui.Text("Download failed.");
            }
            else
            {
                foreach (var pluginDefinition in this.pluginMaster)
                {
                    if (ImGui.CollapsingHeader(pluginDefinition.Name))
                    {
                        ImGui.Indent();

                        ImGui.Text(pluginDefinition.Name);
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), $" by {pluginDefinition.Author}");

                        ImGui.Text(pluginDefinition.Description);

                        var isInstalled = this.manager.Plugins.Where(x=> x.Definition != null).Any(
                            x => x.Definition.InternalName == pluginDefinition.InternalName);

                        if (!isInstalled) {
                            if (this.installStatus == PluginInstallStatus.InProgress) {
                                ImGui.Button($"Install in progress...");
                            } else {
                                if (ImGui.Button($"Install v{pluginDefinition.AssemblyVersion}"))
                                {
                                    this.installStatus = PluginInstallStatus.InProgress;

                                    Task.Run(() => InstallPlugin(pluginDefinition)).ContinueWith(t => { this.installStatus = t.IsFaulted ? PluginInstallStatus.Fail : this.installStatus; });
                                }
                            }

                        } else {
                            var installedPlugin = this.manager.Plugins.Where(x => x.Definition != null).First(
                                x => x.Definition.InternalName == pluginDefinition.InternalName);

                            if (ImGui.Button("Disable"))
                            {
                                this.manager.DisablePlugin(installedPlugin.Definition);
                            }

                            if (installedPlugin.Plugin is IHasConfigUi v2Plugin && v2Plugin.OpenConfigUi != null) {
                                ImGui.SameLine();

                                if (ImGui.Button("Open Configuration"))
                                {
                                    v2Plugin.OpenConfigUi?.Invoke(null, null);
                                }
                            }
                        }

                        ImGui.Unindent();
                    }
                }
            }

            ImGui.PopStyleVar();

            ImGui.EndChild();

            ImGui.Separator();

            if (ImGui.Button("Remove All"))
            {

            }

            ImGui.SameLine();

            if (ImGui.Button("Open Plugin folder"))
            {

            }

            ImGui.SameLine();

            if (ImGui.Button("Close"))
            {
                windowOpen = false;
            }

            if (ImGui.Button("test modal")) {
                this.installStatus = PluginInstallStatus.Fail;
            }

            ImGui.Spacing();

            if (this.installStatus == PluginInstallStatus.Fail || this.masterDownloadFailed) {
                if (ImGui.BeginPopupModal("Installer failed", ref this.errorModalDrawing, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.TextWrapped("The installer ran into an issue. Please restart the game and report this error on our discord.");

                    if (ImGui.Button("OK", new Vector2(120, 40))) { ImGui.CloseCurrentPopup(); }

                    ImGui.EndPopup();
                }
            }

            ImGui.End();

            return windowOpen;
        }
    }
}

using System;
using System.Linq;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using ImGuiNET;

namespace Dalamud.Interface {
    internal class DalamudPluginStatWindow : IDisposable {

        private readonly PluginManager pluginManager;
        public DalamudPluginStatWindow(PluginManager pluginManager) {
            this.pluginManager = pluginManager;
        }

        public bool Draw() {
            bool doDraw = true;
            ImGui.PushID("DalamudPluginStatWindow");
            ImGui.Begin("Plugin Statistics", ref doDraw);
            ImGui.BeginTabBar("Stat Tabs");

            if (ImGui.BeginTabItem("Draw times")) {

                bool doStats = UiBuilder.DoStats;

                if (ImGui.Checkbox("Enable Draw Time Tracking", ref doStats)) {
                    UiBuilder.DoStats = doStats;
                }

                if (doStats) {

                    ImGui.SameLine();
                    if (ImGui.Button("Reset")) {
                        foreach (var a in this.pluginManager.Plugins) {
                            a.PluginInterface.UiBuilder.lastDrawTime = -1;
                            a.PluginInterface.UiBuilder.maxDrawTime = -1;
                            a.PluginInterface.UiBuilder.drawTimeHistory.Clear();
                        }
                    }


                    ImGui.Columns(4);
                    ImGui.SetColumnWidth(0, 180f);
                    ImGui.SetColumnWidth(1, 100f);
                    ImGui.SetColumnWidth(2, 100f);
                    ImGui.SetColumnWidth(3, 100f);
                    ImGui.Text("Plugin");
                    ImGui.NextColumn();
                    ImGui.Text("Last");
                    ImGui.NextColumn();
                    ImGui.Text("Longest");
                    ImGui.NextColumn();
                    ImGui.Text("Average");
                    ImGui.NextColumn();
                    ImGui.Separator();
                    foreach (var a in this.pluginManager.Plugins) {
                        ImGui.Text(a.Definition.Name);
                        ImGui.NextColumn();
                        ImGui.Text($"{a.PluginInterface.UiBuilder.lastDrawTime/10000f:F4}ms");
                        ImGui.NextColumn();
                        ImGui.Text($"{a.PluginInterface.UiBuilder.maxDrawTime/10000f:F4}ms");
                        ImGui.NextColumn();
                        if (a.PluginInterface.UiBuilder.drawTimeHistory.Count > 0) {
                            ImGui.Text($"{a.PluginInterface.UiBuilder.drawTimeHistory.Average()/10000f:F4}ms");
                        } else {
                            ImGui.Text("-");
                        }
                        ImGui.NextColumn();
                    }

                    ImGui.Columns(1);

                }
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Framework times")) {

                var doStats = Framework.StatsEnabled;

                if (ImGui.Checkbox("Enable Framework Update Tracking", ref doStats)) {
                    Framework.StatsEnabled = doStats;
                }

                if (doStats) {
                    ImGui.SameLine();
                    if (ImGui.Button("Reset")) {
                        Framework.StatsHistory.Clear();
                    }
                    
                    ImGui.Columns(4);
                    ImGui.SetColumnWidth(0, ImGui.GetWindowContentRegionWidth() - 300);
                    ImGui.SetColumnWidth(1, 100f);
                    ImGui.SetColumnWidth(2, 100f);
                    ImGui.SetColumnWidth(3, 100f);
                    ImGui.Text("Method");
                    ImGui.NextColumn();
                    ImGui.Text("Last");
                    ImGui.NextColumn();
                    ImGui.Text("Longest");
                    ImGui.NextColumn();
                    ImGui.Text("Average");
                    ImGui.NextColumn();
                    ImGui.Separator();
                    ImGui.Separator();

                    foreach (var handlerHistory in Framework.StatsHistory) {
                        if (handlerHistory.Value.Count == 0) continue;
                        ImGui.SameLine();
                        ImGui.Text($"{handlerHistory.Key}");
                        ImGui.NextColumn();
                        ImGui.Text($"{handlerHistory.Value.Last():F4}ms");
                        ImGui.NextColumn();
                        ImGui.Text($"{handlerHistory.Value.Max():F4}ms");
                        ImGui.NextColumn();
                        ImGui.Text($"{handlerHistory.Value.Average():F4}ms");
                        ImGui.NextColumn();
                        ImGui.Separator();
                    }
                    ImGui.Columns(0);
                }

                ImGui.EndTabItem();
            }

                }
                
            }

            ImGui.EndTabBar();

            ImGui.End();
            ImGui.PopID();

            return doDraw;
        }

        public void Dispose() {

        }
    }
}

using System;
using System.Linq;
using Dalamud.Plugin;
using ImGuiNET;

namespace Dalamud.Interface {
    class DalamudPluginStatWindow : IDisposable {

        private PluginManager pm;
        public DalamudPluginStatWindow(PluginManager pm) {
            this.pm = pm;
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
                        foreach (var a in this.pm.Plugins) {
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
                    foreach (var a in this.pm.Plugins) {
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

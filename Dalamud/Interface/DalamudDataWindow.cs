using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Data;
using ImGuiNET;
using Newtonsoft.Json;

namespace Dalamud.Interface
{
    class DalamudDataWindow {
        private DataManager dataMgr;

        private bool wasReady;
        private string serverOpString;
        private string cfcString = "N/A";

        private int currentKind;

        public DalamudDataWindow(DataManager dataMgr) {
            this.dataMgr = dataMgr;

            Load();
        }

        private void Load() {
            if (this.dataMgr.IsDataReady)
            {
                this.serverOpString = JsonConvert.SerializeObject(this.dataMgr.ServerOpCodes, Formatting.Indented);
                this.wasReady = true;
            }
        }

        public bool Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.Always);

            var isOpen = true;

            if (!ImGui.Begin("Dalamud Data", ref isOpen, ImGuiWindowFlags.NoCollapse))
            {
                ImGui.End();
                return false;
            }

            // Main window
            if (ImGui.Button("Force Reload"))
                Load();
            ImGui.SameLine();
            var copy = ImGui.Button("Copy all");
            ImGui.SameLine();
            ImGui.Combo("Data kind", ref currentKind, new[] {"ServerOpCode", "ContentFinderCondition"}, 2);

            ImGui.BeginChild("scrolling", new Vector2(0, 0), false, ImGuiWindowFlags.HorizontalScrollbar);

            if (copy)
                ImGui.LogToClipboard();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            if (this.wasReady) {
                switch (currentKind) {
                    case 0: ImGui.TextUnformatted(this.serverOpString);
                        break;
                    case 1: ImGui.TextUnformatted(this.cfcString);
                        break;
                }
            } else {
                ImGui.TextUnformatted("Data not ready.");
            }
            
            ImGui.PopStyleVar();

            ImGui.EndChild();
            ImGui.End();

            return isOpen;
        }
    }
}

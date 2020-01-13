using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace Dalamud.Interface
{
    class DalamudLogWindow : IDisposable {
        private bool autoScroll = true;
        private string logText = string.Empty;

        public DalamudLogWindow() {
            SerilogEventSink.Instance.OnLogLine += Serilog_OnLogLine;
        }

        public void Dispose() {
            SerilogEventSink.Instance.OnLogLine -= Serilog_OnLogLine;
        }

        private void Serilog_OnLogLine(object sender, string e)
        {
            AddLog(e + "\n");
        }

        public void Clear() {
            this.logText = string.Empty;
        }

        public void AddLog(string line) {
            this.logText += line;
        }

        public bool Draw() {
            ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);

            var isOpen = true;

            if (!ImGui.Begin("Dalamud LOG", ref isOpen, ImGuiWindowFlags.NoCollapse))
            {
                ImGui.End();
                return false;
            }

            // Options menu
            if (ImGui.BeginPopup("Options"))
            {
                ImGui.Checkbox("Auto-scroll", ref this.autoScroll);
                ImGui.EndPopup();
            }

            // Main window
            if (ImGui.Button("Options"))
                ImGui.OpenPopup("Options");
            ImGui.SameLine();
            var clear = ImGui.Button("Clear");
            ImGui.SameLine();
            var copy = ImGui.Button("Copy");

            ImGui.BeginChild("scrolling", new Vector2(0, 0), false, ImGuiWindowFlags.HorizontalScrollbar);

            if (clear)
                Clear();
            if (copy)
                ImGui.LogToClipboard();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            ImGui.TextUnformatted(this.logText);

            ImGui.PopStyleVar();

            if (this.autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                ImGui.SetScrollHereY(1.0f);

            ImGui.EndChild();
            ImGui.End();

            return isOpen;
        }
    }
}

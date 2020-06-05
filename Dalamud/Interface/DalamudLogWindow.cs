using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using ImGuiNET;
using Serilog;
using Serilog.Events;

namespace Dalamud.Interface
{
    class DalamudLogWindow : IDisposable {
        private readonly CommandManager commandManager;
        private bool autoScroll = true;
        private List<(string line, Vector4 color)> logText = new List<(string line, Vector4 color)>();

        private string commandText = string.Empty;

        public DalamudLogWindow(CommandManager commandManager) {
            this.commandManager = commandManager;
            SerilogEventSink.Instance.OnLogLine += Serilog_OnLogLine;
        }

        public void Dispose() {
            SerilogEventSink.Instance.OnLogLine -= Serilog_OnLogLine;
        }

        private void Serilog_OnLogLine(object sender, (string line, LogEventLevel level) logEvent)
        {
            var color = logEvent.level switch
            {
                LogEventLevel.Error => new Vector4(1f, 0f, 0f, 1f),
                LogEventLevel.Verbose => new Vector4(1f, 1f, 1f, 1f),
                LogEventLevel.Debug => new Vector4(0.878f, 0.878f, 0.878f, 1f),
                LogEventLevel.Information => new Vector4(1f, 1f, 1f, 1f),
                LogEventLevel.Warning => new Vector4(1f, 0.709f, 0f, 1f),
                LogEventLevel.Fatal => new Vector4(1f, 0f, 0f, 1f),
                _ => throw new ArgumentOutOfRangeException()
            };

            AddLog(logEvent.line, color);
        }

        public void Clear() {
            this.logText.Clear();
        }

        public void AddLog(string line, Vector4 color) {
            this.logText.Add((line, color));
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

            ImGui.Text("Enter command: ");
            ImGui.SameLine();
            ImGui.InputText("##commandbox", ref this.commandText, 255);
            ImGui.SameLine();
            if (ImGui.Button("Send")) {
                if (this.commandManager.ProcessCommand(this.commandText)) {
                    Log.Information("Command was dispatched.");
                } else {
                    Log.Information("Command {0} not registered.", this.commandText);
                }
            }

            ImGui.BeginChild("scrolling", new Vector2(0, 0), false, ImGuiWindowFlags.HorizontalScrollbar);

            if (clear)
                Clear();
            if (copy)
                ImGui.LogToClipboard();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            foreach (var valueTuple in this.logText) {
                ImGui.TextColored(valueTuple.color, valueTuple.line);
            }

            ImGui.PopStyleVar();

            if (this.autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                ImGui.SetScrollHereY(1.0f);

            ImGui.EndChild();
            ImGui.End();

            return isOpen;
        }
    }
}

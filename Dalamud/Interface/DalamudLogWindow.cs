using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Configuration;
using Dalamud.Game.Command;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Serilog;
using Serilog.Events;

namespace Dalamud.Interface
{
    class DalamudLogWindow : Window, IDisposable {
        private readonly CommandManager commandManager;
        private readonly DalamudConfiguration configuration;
        private bool autoScroll;
        private bool openAtStartup;
        private readonly List<(string line, Vector4 color)> logText = new List<(string line, Vector4 color)>();
        
        private readonly object renderLock = new object();

        private string commandText = string.Empty;

        public DalamudLogWindow(CommandManager commandManager, DalamudConfiguration configuration)
            : base("Dalamud LOG")
        {
            this.commandManager = commandManager;
            this.configuration = configuration;
            this.autoScroll = configuration.LogAutoScroll;
            this.openAtStartup = configuration.LogOpenAtStartup;
            SerilogEventSink.Instance.OnLogLine += Serilog_OnLogLine;

            this.Size = new Vector2(500, 400);
            this.SizeCondition = ImGuiCond.FirstUseEver;
        }

        public void Dispose() {
            SerilogEventSink.Instance.OnLogLine -= Serilog_OnLogLine;
        }

        private void Serilog_OnLogLine(object sender, (string line, LogEventLevel level) logEvent)
        {
            var color = logEvent.level switch
            {
                LogEventLevel.Error => ImGuiColors.DalamudRed,
                LogEventLevel.Verbose => ImGuiColors.DalamudWhite,
                LogEventLevel.Debug => ImGuiColors.DalamudWhite2,
                LogEventLevel.Information => ImGuiColors.DalamudWhite,
                LogEventLevel.Warning => ImGuiColors.DalamudOrange,
                LogEventLevel.Fatal => ImGuiColors.DalamudRed,
                _ => throw new ArgumentOutOfRangeException(),
            };

            AddLog(logEvent.line, color);
        }

        public void Clear() {
            lock (this.renderLock) {
                this.logText.Clear();
            }
        }

        public void AddLog(string line, Vector4 color) {
            lock (this.renderLock) {
                this.logText.Add((line, color));
            }
        }

        public override void Draw() {
            // Options menu
            if (ImGui.BeginPopup("Options"))
            {
                if (ImGui.Checkbox("Auto-scroll", ref this.autoScroll)) {
                    this.configuration.LogAutoScroll = this.autoScroll;
                    this.configuration.Save();
                };
                if (ImGui.Checkbox("Open at startup", ref this.openAtStartup)) {
                    this.configuration.LogOpenAtStartup = this.openAtStartup;
                    this.configuration.Save();
                };
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

            lock (this.renderLock) {
                foreach (var (line, color) in this.logText) {
                    ImGui.TextColored(color, line);
                }
            }

            ImGui.PopStyleVar();

            if (this.autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                ImGui.SetScrollHereY(1.0f);

            ImGui.EndChild();
        }
    }
}

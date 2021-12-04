using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Command;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using ImGuiNET;
using Serilog;
using Serilog.Events;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// The window that displays the Dalamud log file in-game.
    /// </summary>
    internal class ConsoleWindow : Window, IDisposable
    {
        private readonly List<LogEntry> logText = new();
        private readonly object renderLock = new();

        private readonly string[] logLevelStrings = new[] { "None", "Verbose", "Debug", "Information", "Warning", "Error", "Fatal" };

        private List<LogEntry> filteredLogText = new();
        private bool autoScroll;
        private bool openAtStartup;

        private bool? lastCmdSuccess;

        private string commandText = string.Empty;

        private string textFilter = string.Empty;
        private LogEventLevel? levelFilter = null;
        private bool isFiltered = false;

        private int historyPos;
        private List<string> history = new();

        private bool killGameArmed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleWindow"/> class.
        /// </summary>
        public ConsoleWindow()
            : base("Dalamud Console")
        {
            var configuration = Service<DalamudConfiguration>.Get();

            this.autoScroll = configuration.LogAutoScroll;
            this.openAtStartup = configuration.LogOpenAtStartup;
            SerilogEventSink.Instance.LogLine += this.OnLogLine;

            this.Size = new Vector2(500, 400);
            this.SizeCondition = ImGuiCond.FirstUseEver;

            this.RespectCloseHotkey = false;
        }

        private List<LogEntry> LogEntries => this.isFiltered ? this.filteredLogText : this.logText;

        /// <inheritdoc/>
        public override void OnOpen()
        {
            this.killGameArmed = false;
            base.OnOpen();
        }

        /// <summary>
        /// Dispose of managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            SerilogEventSink.Instance.LogLine -= this.OnLogLine;
        }

        /// <summary>
        /// Clear the window of all log entries.
        /// </summary>
        public void Clear()
        {
            lock (this.renderLock)
            {
                this.logText.Clear();
                this.filteredLogText.Clear();
            }
        }

        /// <summary>
        /// Add a single log line to the display.
        /// </summary>
        /// <param name="line">The line to add.</param>
        /// <param name="level">The level of the event.</param>
        /// <param name="offset">The <see cref="DateTimeOffset"/> of the event.</param>
        public void HandleLogLine(string line, LogEventLevel level, DateTimeOffset offset)
        {
            if (line.IndexOfAny(new[] { '\n', '\r' }) != -1)
            {
                var subLines = line.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                this.AddAndFilter(subLines[0], level, offset, false);

                for (var i = 1; i < subLines.Length; i++)
                {
                    this.AddAndFilter(subLines[i], level, offset, true);
                }
            }
            else
            {
                this.AddAndFilter(line, level, offset, false);
            }
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            // Options menu
            if (ImGui.BeginPopup("Options"))
            {
                var dalamud = Service<Dalamud>.Get();
                var configuration = Service<DalamudConfiguration>.Get();

                if (ImGui.Checkbox("Auto-scroll", ref this.autoScroll))
                {
                    configuration.LogAutoScroll = this.autoScroll;
                    configuration.Save();
                }

                if (ImGui.Checkbox("Open at startup", ref this.openAtStartup))
                {
                    configuration.LogOpenAtStartup = this.openAtStartup;
                    configuration.Save();
                }

                var prevLevel = (int)dalamud.LogLevelSwitch.MinimumLevel;
                if (ImGui.Combo("Log Level", ref prevLevel, Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>().Select(x => x.ToString()).ToArray(), 6))
                {
                    dalamud.LogLevelSwitch.MinimumLevel = (LogEventLevel)prevLevel;
                    configuration.LogLevel = (LogEventLevel)prevLevel;
                    configuration.Save();
                }

                ImGui.EndPopup();
            }

            // Filter menu
            if (ImGui.BeginPopup("Filters"))
            {
                ImGui.Checkbox("Enabled", ref this.isFiltered);

                if (ImGui.InputTextWithHint("##filterText", "Text Filter", ref this.textFilter, 255, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    this.Refilter();
                }

                ImGui.TextColored(ImGuiColors.DalamudGrey, "Enter to confirm.");

                var filterVal = this.levelFilter.HasValue ? (int)this.levelFilter.Value + 1 : 0;
                if (ImGui.Combo("Level", ref filterVal, this.logLevelStrings, 7))
                {
                    this.levelFilter = (LogEventLevel)(filterVal - 1);
                    this.Refilter();
                }

                ImGui.EndPopup();
            }

            ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
                ImGui.OpenPopup("Options");

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Options");

            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Search))
                ImGui.OpenPopup("Filters");

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Filters");

            ImGui.SameLine();
            var clear = ImGuiComponents.IconButton(FontAwesomeIcon.Trash);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Clear Log");

            ImGui.SameLine();
            var copy = ImGuiComponents.IconButton(FontAwesomeIcon.Copy);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Copy Log");

            ImGui.SameLine();
            if (this.killGameArmed)
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Flushed))
                    Process.GetCurrentProcess().Kill();
            }
            else
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Skull))
                    this.killGameArmed = true;
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Kill game");

            ImGui.BeginChild("scrolling", new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 55), false, ImGuiWindowFlags.AlwaysHorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar);

            if (clear)
            {
                this.Clear();
            }

            if (copy)
            {
                ImGui.LogToClipboard();
            }

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

            ImGuiListClipperPtr clipper;
            unsafe
            {
                clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            }

            ImGui.PushFont(InterfaceManager.MonoFont);

            var childPos = ImGui.GetWindowPos();
            var childDrawList = ImGui.GetWindowDrawList();
            var childSize = ImGui.GetWindowSize();

            var cursorDiv = ImGuiHelpers.GlobalScale * 92;
            var cursorLogLevel = ImGuiHelpers.GlobalScale * 100;
            var cursorLogLine = ImGuiHelpers.GlobalScale * 135;

            lock (this.renderLock)
            {
                clipper.Begin(this.LogEntries.Count);
                while (clipper.Step())
                {
                    for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        var line = this.LogEntries[i];

                        if (!line.IsMultiline)
                            ImGui.Separator();

                        ImGui.PushStyleColor(ImGuiCol.Header, this.GetColorForLogEventLevel(line.Level));
                        ImGui.PushStyleColor(ImGuiCol.HeaderActive, this.GetColorForLogEventLevel(line.Level));
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, this.GetColorForLogEventLevel(line.Level));

                        ImGui.Selectable("###consolenull", true, ImGuiSelectableFlags.AllowItemOverlap | ImGuiSelectableFlags.SpanAllColumns);
                        ImGui.SameLine();

                        ImGui.PopStyleColor(3);

                        if (!line.IsMultiline)
                        {
                            ImGui.TextUnformatted(line.TimeStamp.ToString("HH:mm:ss.fff"));
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(cursorDiv);
                            ImGui.TextUnformatted("|");
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(cursorLogLevel);
                            ImGui.TextUnformatted(this.GetTextForLogEventLevel(line.Level));
                            ImGui.SameLine();
                        }

                        ImGui.SetCursorPosX(cursorLogLine);
                        ImGui.TextUnformatted(line.Line);
                    }
                }

                clipper.End();
            }

            ImGui.PopFont();

            ImGui.PopStyleVar();

            if (this.autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }

            // Draw dividing line
            var offset = ImGuiHelpers.GlobalScale * 127;
            childDrawList.AddLine(new Vector2(childPos.X + offset, childPos.Y), new Vector2(childPos.X + offset, childPos.Y + childSize.Y), 0x4FFFFFFF, 1.0f);

            ImGui.EndChild();

            var hadColor = false;
            if (this.lastCmdSuccess.HasValue)
            {
                hadColor = true;
                if (this.lastCmdSuccess.Value)
                {
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiColors.HealerGreen - new Vector4(0, 0, 0, 0.7f));
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiColors.DalamudRed - new Vector4(0, 0, 0, 0.7f));
                }
            }

            ImGui.SetNextItemWidth(ImGui.GetWindowSize().X - 80);

            var getFocus = false;
            unsafe
            {
                if (ImGui.InputText("##commandbox", ref this.commandText, 255, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackCompletion | ImGuiInputTextFlags.CallbackHistory, this.CommandInputCallback))
                {
                    this.ProcessCommand();
                    getFocus = true;
                }

                ImGui.SameLine();
            }

            ImGui.SetItemDefaultFocus();
            if (getFocus)
                ImGui.SetKeyboardFocusHere(-1); // Auto focus previous widget

            if (hadColor)
                ImGui.PopStyleColor();

            if (ImGui.Button("Send"))
            {
                this.ProcessCommand();
            }
        }

        private void ProcessCommand()
        {
            try
            {
                this.historyPos = -1;
                for (var i = this.history.Count - 1; i >= 0; i--)
                {
                    if (this.history[i] == this.commandText)
                    {
                        this.history.RemoveAt(i);
                        break;
                    }
                }

                this.history.Add(this.commandText);

                if (this.commandText == "clear" || this.commandText == "cls")
                {
                    this.Clear();
                    return;
                }

                this.lastCmdSuccess = Service<CommandManager>.Get().ProcessCommand("/" + this.commandText);
                this.commandText = string.Empty;

                // TODO: Force scroll to bottom
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during command dispatch");
                this.lastCmdSuccess = false;
            }
        }

        private unsafe int CommandInputCallback(ImGuiInputTextCallbackData* data)
        {
            var ptr = new ImGuiInputTextCallbackDataPtr(data);

            switch (data->EventFlag)
            {
                case ImGuiInputTextFlags.CallbackCompletion:
                    var textBytes = new byte[data->BufTextLen];
                    Marshal.Copy((IntPtr)data->Buf, textBytes, 0, data->BufTextLen);
                    var text = Encoding.UTF8.GetString(textBytes);

                    var words = text.Split();

                    // We can't do any completion for parameters at the moment since it just calls into CommandHandler
                    if (words.Length > 1)
                        return 0;

                    // TODO: Improve this, add partial completion
                    // https://github.com/ocornut/imgui/blob/master/imgui_demo.cpp#L6443-L6484
                    var candidates = Service<CommandManager>.Get().Commands.Where(x => x.Key.Contains("/" + words[0])).ToList();
                    if (candidates.Count > 0)
                    {
                        ptr.DeleteChars(0, ptr.BufTextLen);
                        ptr.InsertChars(0, candidates[0].Key.Replace("/", string.Empty));
                    }

                    break;
                case ImGuiInputTextFlags.CallbackHistory:
                    var prevPos = this.historyPos;

                    if (ptr.EventKey == ImGuiKey.UpArrow)
                    {
                        if (this.historyPos == -1)
                            this.historyPos = this.history.Count - 1;
                        else if (this.historyPos > 0)
                            this.historyPos--;
                    }
                    else if (data->EventKey == ImGuiKey.DownArrow)
                    {
                        if (this.historyPos != -1)
                        {
                            if (++this.historyPos >= this.history.Count)
                            {
                                this.historyPos = -1;
                            }
                        }
                    }

                    if (prevPos != this.historyPos)
                    {
                        var historyStr = this.historyPos >= 0 ? this.history[this.historyPos] : string.Empty;

                        ptr.DeleteChars(0, ptr.BufTextLen);
                        ptr.InsertChars(0, historyStr);
                    }

                    break;
            }

            return 0;
        }

        private void AddAndFilter(string line, LogEventLevel level, DateTimeOffset offset, bool isMultiline)
        {
            if (line.StartsWith("TROUBLESHOOTING:") || line.StartsWith("LASTEXCEPTION:"))
                return;

            var entry = new LogEntry
            {
                IsMultiline = isMultiline,
                Level = level,
                Line = line,
                TimeStamp = offset,
            };

            this.logText.Add(entry);

            if (!this.isFiltered)
                return;

            if (this.IsFilterApplicable(entry))
                this.filteredLogText.Add(entry);
        }

        private bool IsFilterApplicable(LogEntry entry)
        {
            if (this.levelFilter.HasValue)
            {
                return entry.Level == this.levelFilter.Value;
            }

            if (!string.IsNullOrEmpty(this.textFilter))
                return entry.Line.Contains(this.textFilter);

            return true;
        }

        private void Refilter()
        {
            lock (this.renderLock)
            {
                this.filteredLogText = this.logText.Where(this.IsFilterApplicable).ToList();
            }
        }

        private string GetTextForLogEventLevel(LogEventLevel level) => level switch
        {
            LogEventLevel.Error => "ERR",
            LogEventLevel.Verbose => "VRB",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Fatal => "FTL",
            _ => throw new ArgumentOutOfRangeException(level.ToString(), "Invalid LogEventLevel"),
        };

        private uint GetColorForLogEventLevel(LogEventLevel level) => level switch
        {
            LogEventLevel.Error => 0x800000EE,
            LogEventLevel.Verbose => 0x00000000,
            LogEventLevel.Debug => 0x00000000,
            LogEventLevel.Information => 0x00000000,
            LogEventLevel.Warning => 0x8A0070EE,
            LogEventLevel.Fatal => 0xFF00000A,
            _ => throw new ArgumentOutOfRangeException(level.ToString(), "Invalid LogEventLevel"),
        };

        private void OnLogLine(object sender, (string Line, LogEventLevel Level, DateTimeOffset Offset, Exception? Exception) logEvent)
        {
            this.HandleLogLine(logEvent.Line, logEvent.Level, logEvent.Offset);
        }

        private class LogEntry
        {
            public string Line { get; set; }

            public LogEventLevel Level { get; set; }

            public DateTimeOffset TimeStamp { get; set; }

            public bool IsMultiline { get; set; }
        }
    }
}

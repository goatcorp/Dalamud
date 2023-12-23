using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Command;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;
using ImGuiNET;
using Serilog;
using Serilog.Events;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// The window that displays the Dalamud log file in-game.
/// </summary>
internal class ConsoleWindow : Window, IDisposable
{
    private readonly List<LogEntry> logText = new();
    private readonly object renderLock = new();

    private readonly List<string> history = new();
    private readonly List<PluginFilterEntry> pluginFilters = new();

    private bool? lastCmdSuccess;

    private string commandText = string.Empty;
    private string textFilter = string.Empty;
    private string selectedSource = "DalamudInternal";
    private string pluginFilter = string.Empty;

    private bool filterShowUncaughtExceptions;
    private bool showFilterToolbar;
    private bool clearLog;
    private bool copyLog;
    private bool copyMode;
    private bool killGameArmed;
    private bool autoScroll;
    private bool autoOpen;
    private bool regexError;

    private int historyPos;
    private int copyStart = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleWindow"/> class.
    /// </summary>
    /// <param name="configuration">An instance of <see cref="DalamudConfiguration"/>.</param>
    public ConsoleWindow(DalamudConfiguration configuration)
        : base("Dalamud Console", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.autoScroll = configuration.LogAutoScroll;
        this.autoOpen = configuration.LogOpenAtStartup;
        SerilogEventSink.Instance.LogLine += this.OnLogLine;

        this.Size = new Vector2(500, 400);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600.0f, 200.0f),
            MaximumSize = new Vector2(9999.0f, 9999.0f),
        };

        this.RespectCloseHotkey = false;
    }

    private List<LogEntry> FilteredLogEntries { get; set; } = new();

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
            this.FilteredLogEntries.Clear();
            this.clearLog = false;
        }
    }

    /// <summary>
    /// Copies the entire log contents to clipboard.
    /// </summary>
    public void CopyLog()
    {
        ImGui.LogToClipboard();
    }

    /// <summary>
    /// Add a single log line to the display.
    /// </summary>
    /// <param name="line">The line to add.</param>
    /// <param name="logEvent">The Serilog event associated with this line.</param>
    public void HandleLogLine(string line, LogEvent logEvent)
    {
        if (line.IndexOfAny(new[] { '\n', '\r' }) != -1)
        {
            var subLines = line.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            this.AddAndFilter(subLines[0], logEvent, false);

            for (var i = 1; i < subLines.Length; i++)
            {
                this.AddAndFilter(subLines[i], logEvent, true);
            }
        }
        else
        {
            this.AddAndFilter(line, logEvent, false);
        }
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        this.DrawOptionsToolbar();

        this.DrawFilterToolbar();

        if (this.regexError)
        {
            const string regexErrorString = "Regex Filter Error";
            ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X / 2.0f - ImGui.CalcTextSize(regexErrorString).X / 2.0f);
            ImGui.TextColored(ImGuiColors.DalamudRed, regexErrorString);
        }
        
        ImGui.BeginChild("scrolling", new Vector2(0, ImGui.GetFrameHeightWithSpacing() - 55 * ImGuiHelpers.GlobalScale), false, ImGuiWindowFlags.AlwaysHorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar);

        if (this.clearLog) this.Clear();

        if (this.copyLog) this.CopyLog();

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

        var cursorDiv = ImGuiHelpers.GlobalScale * 93;
        var cursorLogLevel = ImGuiHelpers.GlobalScale * 100;
        var cursorLogLine = ImGuiHelpers.GlobalScale * 135;

        lock (this.renderLock)
        {
            clipper.Begin(this.FilteredLogEntries.Count);
            while (clipper.Step())
            {
                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                {
                    var line = this.FilteredLogEntries[i];

                    if (!line.IsMultiline && !this.copyLog)
                        ImGui.Separator();
                    
                    if (line.SelectedForCopy)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Header, ImGuiColors.ParsedGrey);
                        ImGui.PushStyleColor(ImGuiCol.HeaderActive, ImGuiColors.ParsedGrey);
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGuiColors.ParsedGrey);
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Header, this.GetColorForLogEventLevel(line.Level));
                        ImGui.PushStyleColor(ImGuiCol.HeaderActive, this.GetColorForLogEventLevel(line.Level));
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, this.GetColorForLogEventLevel(line.Level));
                    }

                    ImGui.Selectable("###console_null", true, ImGuiSelectableFlags.AllowItemOverlap | ImGuiSelectableFlags.SpanAllColumns);

                    // This must be after ImGui.Selectable, it uses ImGui.IsItem... functions
                    this.HandleCopyMode(i, line);
                    
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
            clipper.Destroy();
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

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (80.0f * ImGuiHelpers.GlobalScale) - (ImGui.GetStyle().ItemSpacing.X * ImGuiHelpers.GlobalScale));

        var getFocus = false;
        unsafe
        {
            if (ImGui.InputText("##command_box", ref this.commandText, 255, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackCompletion | ImGuiInputTextFlags.CallbackHistory, this.CommandInputCallback))
            {
                this.ProcessCommand();
                getFocus = true;
            }

            ImGui.SameLine();
        }

        ImGui.SetItemDefaultFocus();
        if (getFocus) ImGui.SetKeyboardFocusHere(-1); // Auto focus previous widget

        if (hadColor) ImGui.PopStyleColor();

        if (ImGui.Button("Send", ImGuiHelpers.ScaledVector2(80.0f, 23.0f)))
        {
            this.ProcessCommand();
        }
        
        this.copyLog = false;
    }
    
    private void HandleCopyMode(int i, LogEntry line)
    {
        var selectionChanged = false;
        
        // If copyStart is -1, it means a drag has not been started yet, let's start one, and select the starting spot.
        if (this.copyMode && this.copyStart == -1 && ImGui.IsItemClicked())
        {
            this.copyStart = i;
            line.SelectedForCopy = !line.SelectedForCopy;

            selectionChanged = true;
        }

        // Update the selected range when dragging over entries
        if (this.copyMode && this.copyStart != -1 && ImGui.IsItemHovered() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            if (!line.SelectedForCopy)
            {
                foreach (var index in Enumerable.Range(0, this.FilteredLogEntries.Count))
                {
                    if (this.copyStart < i)
                    {
                        this.FilteredLogEntries[index].SelectedForCopy = index >= this.copyStart && index <= i;
                    }
                    else
                    {
                        this.FilteredLogEntries[index].SelectedForCopy = index >= i && index <= this.copyStart;
                    }
                }

                selectionChanged = true;
            }
        }

        // Finish the drag, we should have already marked all dragged entries as selected by now.
        if (this.copyMode && this.copyStart != -1 && ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            this.copyStart = -1;
        }

        if (selectionChanged)
        {
            var allSelectedLines = this.FilteredLogEntries
                                       .Where(entry => entry.SelectedForCopy)
                                       .Select(entry => $"{entry.TimeStamp:HH:mm:ss.fff} {this.GetTextForLogEventLevel(entry.Level)} | {entry.Line}");

            ImGui.SetClipboardText(string.Join("\n", allSelectedLines));
        }
    }

    private void DrawOptionsToolbar()
    {
        var configuration = Service<DalamudConfiguration>.Get();

        ImGui.PushItemWidth(150.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginCombo("##log_level", $"{EntryPoint.LogLevelSwitch.MinimumLevel}+"))
        {
            foreach (var value in Enum.GetValues<LogEventLevel>())
            {
                if (ImGui.Selectable(value.ToString(), value == EntryPoint.LogLevelSwitch.MinimumLevel))
                {
                    EntryPoint.LogLevelSwitch.MinimumLevel = value;
                    configuration.LogLevel = value;
                    configuration.QueueSave();
                    this.Refilter();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();

        this.autoScroll = configuration.LogAutoScroll;
        if (this.DrawToggleButtonWithTooltip("auto_scroll", "Auto-scroll", FontAwesomeIcon.Sync, ref this.autoScroll))
        {
            configuration.LogAutoScroll = !configuration.LogAutoScroll;
            configuration.QueueSave();
        }

        ImGui.SameLine();

        this.autoOpen = configuration.LogOpenAtStartup;
        if (this.DrawToggleButtonWithTooltip("auto_open", "Open at startup", FontAwesomeIcon.WindowRestore, ref this.autoOpen))
        {
            configuration.LogOpenAtStartup = !configuration.LogOpenAtStartup;
            configuration.QueueSave();
        }

        ImGui.SameLine();

        if (this.DrawToggleButtonWithTooltip("show_filters", "Show filter toolbar", FontAwesomeIcon.Search, ref this.showFilterToolbar))
        {
            this.showFilterToolbar = !this.showFilterToolbar;
        }

        ImGui.SameLine();

        if (this.DrawToggleButtonWithTooltip("show_uncaught_exceptions", "Show uncaught exception while filtering", FontAwesomeIcon.Bug, ref this.filterShowUncaughtExceptions))
        {
            this.filterShowUncaughtExceptions = !this.filterShowUncaughtExceptions;
        }

        ImGui.SameLine();

        if (ImGuiComponents.IconButton("clear_log", FontAwesomeIcon.Trash))
        {
            this.clearLog = true;
        }

        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Clear Log");

        ImGui.SameLine();

        if (this.DrawToggleButtonWithTooltip("copy_mode", "Enable Copy Mode\nRight-click to copy entire log", FontAwesomeIcon.Copy, ref this.copyMode))
        {
            this.copyMode = !this.copyMode;

            if (!this.copyMode)
            {
                foreach (var entry in this.FilteredLogEntries)
                {
                    entry.SelectedForCopy = false;
                }
            }
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) this.copyLog = true;
        
        ImGui.SameLine();
        if (this.killGameArmed)
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.ExclamationTriangle))
                Process.GetCurrentProcess().Kill();
        }
        else
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Stop))
                this.killGameArmed = true;
        }

        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Kill game");

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - (200.0f * ImGuiHelpers.GlobalScale));
        ImGui.PushItemWidth(200.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputTextWithHint("##global_filter", "regex global filter", ref this.textFilter, 2048, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
        {
            this.Refilter();
        }

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            this.Refilter();
        }
    }

    private void DrawFilterToolbar()
    {
        if (!this.showFilterToolbar) return;

        PluginFilterEntry? removalEntry = null;
        using var table = ImRaii.Table("plugin_filter_entries", 4, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (!table) return;
        
        ImGui.TableSetupColumn("##remove_button", ImGuiTableColumnFlags.WidthFixed, 25.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##source_name", ImGuiTableColumnFlags.WidthFixed, 150.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##log_level", ImGuiTableColumnFlags.WidthFixed, 150.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##filter_text", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextColumn();
        if (ImGuiComponents.IconButton("add_entry", FontAwesomeIcon.Plus))
        {
            if (this.pluginFilters.All(entry => entry.Source != this.selectedSource))
            {
                this.pluginFilters.Add(new PluginFilterEntry
                {
                    Source = this.selectedSource,
                    Filter = string.Empty,
                    Level = LogEventLevel.Debug,
                });
            }

            this.Refilter();
        }

        ImGui.TableNextColumn();
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.BeginCombo("##Sources", this.selectedSource, ImGuiComboFlags.HeightLarge))
        {
            var sourceNames = Service<PluginManager>.Get().InstalledPlugins
                                                    .Select(p => p.Manifest.InternalName)
                                                    .OrderBy(s => s)
                                                    .Prepend("DalamudInternal")
                                                    .Where(name => this.pluginFilter is "" || new FuzzyMatcher(this.pluginFilter.ToLowerInvariant(), MatchMode.Fuzzy).Matches(name.ToLowerInvariant()) != 0)
                                                    .ToList();

            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##PluginSearchFilter", "Filter Plugin List", ref this.pluginFilter, 2048);
            ImGui.Separator();
            
            if (!sourceNames.Any())
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, "No Results");
            }

            foreach (var selectable in sourceNames)
            {
                if (ImGui.Selectable(selectable, this.selectedSource == selectable))
                {
                    this.selectedSource = selectable;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.TableNextColumn();
        ImGui.TableNextColumn();

        foreach (var entry in this.pluginFilters)
        {
            ImGui.TableNextColumn();
            if (ImGuiComponents.IconButton($"remove{entry.Source}", FontAwesomeIcon.Trash))
            {
                removalEntry = entry;
            }

            ImGui.TableNextColumn();
            ImGui.Text(entry.Source);
                
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.BeginCombo($"##levels{entry.Source}", $"{entry.Level}+"))
            {
                foreach (var value in Enum.GetValues<LogEventLevel>())
                {
                    if (ImGui.Selectable(value.ToString(), value == entry.Level))
                    {
                        entry.Level = value;
                        this.Refilter();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var entryFilter = entry.Filter;
            if (ImGui.InputTextWithHint($"##filter{entry.Source}", $"{entry.Source} regex filter", ref entryFilter, 2048, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
            {
                entry.Filter = entryFilter;
                this.Refilter();
            }

            if (ImGui.IsItemDeactivatedAfterEdit()) this.Refilter();
        }

        if (removalEntry is { } toRemove)
        {
            this.pluginFilters.Remove(toRemove);
            this.Refilter();
        }
    }

    private void ProcessCommand()
    {
        try
        {
            if (this.commandText.StartsWith('/'))
            {
                this.commandText = this.commandText[1..];
            }

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

            if (this.commandText is "clear" or "cls")
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
                var candidates = Service<CommandManager>.Get().Commands
                                                        .Where(x => x.Key.Contains("/" + words[0]))
                                                        .ToList();
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

    private void AddAndFilter(string line, LogEvent logEvent, bool isMultiline)
    {
        if (line.StartsWith("TROUBLESHOOTING:") || line.StartsWith("LASTEXCEPTION:"))
            return;

        var entry = new LogEntry
        {
            IsMultiline = isMultiline,
            Level = logEvent.Level,
            Line = line,
            TimeStamp = logEvent.Timestamp,
            HasException = logEvent.Exception != null,
        };

        if (logEvent.Properties.ContainsKey("Dalamud.ModuleName"))
        {
            entry.Source = "DalamudInternal";
        }
        else if (logEvent.Properties.TryGetValue("Dalamud.PluginName", out var sourceProp) &&
                 sourceProp is ScalarValue { Value: string sourceValue })
        {
            entry.Source = sourceValue;
        }

        this.logText.Add(entry);

        if (this.IsFilterApplicable(entry))
            this.FilteredLogEntries.Add(entry);
    }

    private bool IsFilterApplicable(LogEntry entry)
    {
        if (this.regexError)
            return false;

        try
        {
            // If this entry is below a newly set minimum level, fail it
            if (EntryPoint.LogLevelSwitch.MinimumLevel > entry.Level)
                return false;
        
            // Show exceptions that weren't properly tagged with a Source (generally meaning they were uncaught)
            // After log levels because uncaught exceptions should *never* fall below Error.
            if (this.filterShowUncaughtExceptions && entry.HasException && entry.Source == null)
                return true;

            // If we have a global filter, check that first
            if (!this.textFilter.IsNullOrEmpty())
            {
                // Someone will definitely try to just text filter a source without using the actual filters, should allow that.
                var matchesSource = entry.Source is not null && Regex.IsMatch(entry.Source, this.textFilter, RegexOptions.IgnoreCase);
                var matchesContent = Regex.IsMatch(entry.Line, this.textFilter, RegexOptions.IgnoreCase);

                return matchesSource || matchesContent;
            }

            // If this entry has a filter, check the filter
            if (this.pluginFilters.FirstOrDefault(filter => string.Equals(filter.Source, entry.Source, StringComparison.InvariantCultureIgnoreCase)) is { } filterEntry)
            {
                var allowedLevel = filterEntry.Level <= entry.Level;
                var matchesContent = filterEntry.Filter.IsNullOrEmpty() || Regex.IsMatch(entry.Line, filterEntry.Filter, RegexOptions.IgnoreCase);

                return allowedLevel && matchesContent;
            }
        }
        catch (Exception)
        {
            this.regexError = true;
            return false;
        }

        this.regexError = false;
        
        // else we couldn't find a filter for this entry, if we have any filters, we need to block this entry.
        return !this.pluginFilters.Any();
    }

    private void Refilter()
    {
        lock (this.renderLock)
        {
            this.FilteredLogEntries = this.logText.Where(this.IsFilterApplicable).ToList();
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

    private void OnLogLine(object sender, (string Line, LogEvent LogEvent) logEvent)
    {
        this.HandleLogLine(logEvent.Line, logEvent.LogEvent);
    }

    private bool DrawToggleButtonWithTooltip(string buttonId, string tooltip, FontAwesomeIcon icon, ref bool enabledState)
    {
        var result = false;

        var buttonEnabled = enabledState;
        if (buttonEnabled) ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.HealerGreen with { W = 0.25f });
        if (ImGuiComponents.IconButton(buttonId, icon))
        {
            result = true;
        }

        if (ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);

        if (buttonEnabled) ImGui.PopStyleColor();

        return result;
    }

    private class LogEntry
    {
        public string Line { get; init; } = string.Empty;

        public LogEventLevel Level { get; init; }

        public DateTimeOffset TimeStamp { get; init; }

        public bool IsMultiline { get; init; }

        /// <summary>
        /// Gets or sets the system responsible for generating this log entry. Generally will be a plugin's
        /// InternalName.
        /// </summary>
        public string? Source { get; set; }
        
        public bool SelectedForCopy { get; set; }

        public bool HasException { get; init; }
    }

    private class PluginFilterEntry
    {
        public string Source { get; init; } = string.Empty;

        public string Filter { get; set; } = string.Empty;
        
        public LogEventLevel Level { get; set; }
    }
}

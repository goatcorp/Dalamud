using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Services;
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
    private const int LogLinesMinimum = 100;
    private const int LogLinesMaximum = 1000000;

    // Only this field may be touched from any thread.
    private readonly ConcurrentQueue<(string Line, LogEvent LogEvent)> newLogEntries;

    // Fields below should be touched only from the main thread.
    private readonly RollingList<LogEntry> logText;
    private readonly RollingList<LogEntry> filteredLogEntries;

    private readonly List<string> history = new();
    private readonly List<PluginFilterEntry> pluginFilters = new();

    private int newRolledLines;
    private bool pendingRefilter;
    private bool pendingClearLog;

    private bool? lastCmdSuccess;

    private ImGuiListClipperPtr clipperPtr;
    private string commandText = string.Empty;
    private string textFilter = string.Empty;
    private string textHighlight = string.Empty;
    private string selectedSource = "DalamudInternal";
    private string pluginFilter = string.Empty;

    private Regex? compiledLogFilter;
    private Regex? compiledLogHighlight;
    private Exception? exceptionLogFilter;
    private Exception? exceptionLogHighlight;

    private bool filterShowUncaughtExceptions;
    private bool settingsPopupWasOpen;
    private bool showFilterToolbar;
    private bool copyMode;
    private bool killGameArmed;
    private bool autoScroll;
    private int logLinesLimit;
    private bool autoOpen;

    private int historyPos;
    private int copyStart = -1;

    private IActiveNotification? prevCopyNotification;

    /// <summary>Initializes a new instance of the <see cref="ConsoleWindow"/> class.</summary>
    /// <param name="configuration">An instance of <see cref="DalamudConfiguration"/>.</param>
    public ConsoleWindow(DalamudConfiguration configuration)
        : base("Dalamud Console", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.autoScroll = configuration.LogAutoScroll;
        this.autoOpen = configuration.LogOpenAtStartup;
        SerilogEventSink.Instance.LogLine += this.OnLogLine;

        Service<Framework>.GetAsync().ContinueWith(r => r.Result.Update += this.FrameworkOnUpdate);

        this.Size = new Vector2(500, 400);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.RespectCloseHotkey = false;

        this.logLinesLimit = configuration.LogLinesLimit;

        var limit = Math.Max(LogLinesMinimum, this.logLinesLimit);
        this.newLogEntries = new();
        this.logText = new(limit);
        this.filteredLogEntries = new(limit);

        configuration.DalamudConfigurationSaved += this.OnDalamudConfigurationSaved;

        unsafe
        {
            this.clipperPtr = new(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        }
    }

    /// <inheritdoc/>
    public override void OnOpen()
    {
        this.killGameArmed = false;
        base.OnOpen();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        SerilogEventSink.Instance.LogLine -= this.OnLogLine;
        Service<DalamudConfiguration>.Get().DalamudConfigurationSaved -= this.OnDalamudConfigurationSaved;
        if (Service<Framework>.GetNullable() is { } framework)
            framework.Update -= this.FrameworkOnUpdate;

        this.clipperPtr.Destroy();
        this.clipperPtr = default;
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        this.DrawOptionsToolbar();

        this.DrawFilterToolbar();

        if (this.exceptionLogFilter is not null)
        {
            ImGui.TextColored(
                ImGuiColors.DalamudRed,
                $"Regex Filter Error: {this.exceptionLogFilter.GetType().Name}");
            ImGui.TextUnformatted(this.exceptionLogFilter.Message);
        }

        if (this.exceptionLogHighlight is not null)
        {
            ImGui.TextColored(
                ImGuiColors.DalamudRed,
                $"Regex Highlight Error: {this.exceptionLogHighlight.GetType().Name}");
            ImGui.TextUnformatted(this.exceptionLogHighlight.Message);
        }

        var sendButtonSize = ImGui.CalcTextSize("Send") +
                             ((new Vector2(16, 0) + (ImGui.GetStyle().FramePadding * 2)) * ImGuiHelpers.GlobalScale);
        var scrollingHeight = ImGui.GetContentRegionAvail().Y - sendButtonSize.Y;
        ImGui.BeginChild(
            "scrolling",
            new Vector2(0, scrollingHeight),
            false,
            ImGuiWindowFlags.AlwaysHorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar);

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        ImGui.PushFont(InterfaceManager.MonoFont);

        var childPos = ImGui.GetWindowPos();
        var childDrawList = ImGui.GetWindowDrawList();
        var childSize = ImGui.GetWindowSize();

        var timestampWidth = ImGui.CalcTextSize("00:00:00.000").X;
        var levelWidth = ImGui.CalcTextSize("AAA").X;
        var separatorWidth = ImGui.CalcTextSize(" | ").X;
        var cursorLogLevel = timestampWidth + separatorWidth;
        var cursorLogLine = cursorLogLevel + levelWidth + separatorWidth;

        var lastLinePosY = 0.0f;
        var logLineHeight = 0.0f;

        this.clipperPtr.Begin(this.filteredLogEntries.Count);
        while (this.clipperPtr.Step())
        {
            for (var i = this.clipperPtr.DisplayStart; i < this.clipperPtr.DisplayEnd; i++)
            {
                var index = Math.Max(
                    i - this.newRolledLines,
                    0); // Prevents flicker effect. Also workaround to avoid negative indexes.
                var line = this.filteredLogEntries[index];

                if (!line.IsMultiline)
                    ImGui.Separator();

                if (line.SelectedForCopy)
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, ImGuiColors.ParsedGrey);
                    ImGui.PushStyleColor(ImGuiCol.HeaderActive, ImGuiColors.ParsedGrey);
                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGuiColors.ParsedGrey);
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, GetColorForLogEventLevel(line.Level));
                    ImGui.PushStyleColor(ImGuiCol.HeaderActive, GetColorForLogEventLevel(line.Level));
                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, GetColorForLogEventLevel(line.Level));
                }

                ImGui.Selectable(
                    "###console_null",
                    true,
                    ImGuiSelectableFlags.AllowItemOverlap | ImGuiSelectableFlags.SpanAllColumns);

                // This must be after ImGui.Selectable, it uses ImGui.IsItem... functions
                this.HandleCopyMode(i, line);

                ImGui.SameLine();

                ImGui.PopStyleColor(3);

                if (!line.IsMultiline)
                {
                    ImGui.TextUnformatted(line.TimestampString);
                    ImGui.SameLine();

                    ImGui.SetCursorPosX(cursorLogLevel);
                    ImGui.TextUnformatted(GetTextForLogEventLevel(line.Level));
                    ImGui.SameLine();
                }

                ImGui.SetCursorPosX(cursorLogLine);
                line.HighlightMatches ??= (this.compiledLogHighlight ?? this.compiledLogFilter)?.Matches(line.Line);
                if (line.HighlightMatches is { } matches)
                {
                    this.DrawHighlighted(
                        line.Line,
                        matches,
                        ImGui.GetColorU32(ImGuiCol.Text),
                        ImGui.GetColorU32(ImGuiColors.HealerGreen));
                }
                else
                {
                    ImGui.TextUnformatted(line.Line);
                }

                var currentLinePosY = ImGui.GetCursorPosY();
                logLineHeight = currentLinePosY - lastLinePosY;
                lastLinePosY = currentLinePosY;
            }
        }

        this.clipperPtr.End();

        ImGui.PopFont();

        ImGui.PopStyleVar();

        if (!this.autoScroll || ImGui.GetScrollY() < ImGui.GetScrollMaxY())
        {
            ImGui.SetScrollY(ImGui.GetScrollY() - (logLineHeight * this.newRolledLines));
        }

        if (this.autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
        {
            ImGui.SetScrollHereY(1.0f);
        }

        // Draw dividing lines
        var div1Offset = MathF.Round((timestampWidth + (separatorWidth / 2)) - ImGui.GetScrollX());
        var div2Offset = MathF.Round((cursorLogLevel + levelWidth + (separatorWidth / 2)) - ImGui.GetScrollX());
        childDrawList.AddLine(
            new(childPos.X + div1Offset, childPos.Y),
            new(childPos.X + div1Offset, childPos.Y + childSize.Y),
            0x4FFFFFFF,
            1.0f);
        childDrawList.AddLine(
            new(childPos.X + div2Offset, childPos.Y),
            new(childPos.X + div2Offset, childPos.Y + childSize.Y),
            0x4FFFFFFF,
            1.0f);

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

        ImGui.SetNextItemWidth(
            ImGui.GetContentRegionAvail().X - sendButtonSize.X -
            (ImGui.GetStyle().ItemSpacing.X * ImGuiHelpers.GlobalScale));

        var getFocus = false;
        unsafe
        {
            if (ImGui.InputText(
                    "##command_box",
                    ref this.commandText,
                    255,
                    ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackCompletion |
                    ImGuiInputTextFlags.CallbackHistory,
                    this.CommandInputCallback))
            {
                this.ProcessCommand();
                getFocus = true;
            }

            ImGui.SameLine();
        }

        ImGui.SetItemDefaultFocus();
        if (getFocus) ImGui.SetKeyboardFocusHere(-1); // Auto focus previous widget

        if (hadColor) ImGui.PopStyleColor();

        if (ImGui.Button("Send", sendButtonSize))
        {
            this.ProcessCommand();
        }
    }

    private static string GetTextForLogEventLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Error => "ERR",
        LogEventLevel.Verbose => "VRB",
        LogEventLevel.Debug => "DBG",
        LogEventLevel.Information => "INF",
        LogEventLevel.Warning => "WRN",
        LogEventLevel.Fatal => "FTL",
        _ => "???",
    };

    private static uint GetColorForLogEventLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Error => 0x800000EE,
        LogEventLevel.Verbose => 0x00000000,
        LogEventLevel.Debug => 0x00000000,
        LogEventLevel.Information => 0x00000000,
        LogEventLevel.Warning => 0x8A0070EE,
        LogEventLevel.Fatal => 0x800000EE,
        _ => 0x30FFFFFF,
    };

    private void FrameworkOnUpdate(IFramework framework)
    {
        if (this.pendingClearLog)
        {
            this.pendingClearLog = false;
            this.logText.Clear();
            this.filteredLogEntries.Clear();
            this.newLogEntries.Clear();
        }

        if (this.pendingRefilter)
        {
            this.pendingRefilter = false;
            this.filteredLogEntries.Clear();
            foreach (var log in this.logText)
            {
                if (this.IsFilterApplicable(log))
                    this.filteredLogEntries.Add(log);
            }
        }

        var numPrevFilteredLogEntries = this.filteredLogEntries.Count;
        var addedLines = 0;
        while (this.newLogEntries.TryDequeue(out var logLine))
            addedLines += this.HandleLogLine(logLine.Line, logLine.LogEvent);
        this.newRolledLines = addedLines - (this.filteredLogEntries.Count - numPrevFilteredLogEntries);
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
        if (this.copyMode && this.copyStart != -1 && ImGui.IsItemHovered() &&
            ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            if (!line.SelectedForCopy)
            {
                foreach (var index in Enumerable.Range(0, this.filteredLogEntries.Count))
                {
                    if (this.copyStart < i)
                    {
                        this.filteredLogEntries[index].SelectedForCopy = index >= this.copyStart && index <= i;
                    }
                    else
                    {
                        this.filteredLogEntries[index].SelectedForCopy = index >= i && index <= this.copyStart;
                    }
                }

                selectionChanged = true;
            }
        }

        // Finish the drag, we should have already marked all dragged entries as selected by now.
        if (this.copyMode && this.copyStart != -1 && ImGui.IsItemHovered() &&
            ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            this.copyStart = -1;
        }

        if (selectionChanged)
            this.CopyFilteredLogEntries(true);
    }

    private void CopyFilteredLogEntries(bool selectedOnly)
    {
        var sb = new StringBuilder();
        var n = 0;
        foreach (var entry in this.filteredLogEntries)
        {
            if (selectedOnly && !entry.SelectedForCopy)
                continue;

            n++;
            sb.AppendLine(entry.ToString());
        }

        if (n == 0)
            return;

        ImGui.SetClipboardText(sb.ToString());
        this.prevCopyNotification?.DismissNow();
        this.prevCopyNotification = Service<NotificationManager>.Get().AddNotification(
            new()
            {
                Title = this.WindowName,
                Content = $"{n:n0} line(s) copied.",
                Type = NotificationType.Success,
            });
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
                    this.QueueRefilter();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();

        var settingsPopup = ImGui.BeginPopup("##console_settings");
        if (settingsPopup)
        {
            this.DrawSettingsPopup(configuration);
            ImGui.EndPopup();
        }
        else if (this.settingsPopupWasOpen)
        {
            // Prevent side effects in case Apply wasn't clicked
            this.logLinesLimit = configuration.LogLinesLimit;
        }

        this.settingsPopupWasOpen = settingsPopup;

        if (this.DrawToggleButtonWithTooltip("show_settings", "Show settings", FontAwesomeIcon.List, ref settingsPopup))
            ImGui.OpenPopup("##console_settings");

        ImGui.SameLine();

        if (this.DrawToggleButtonWithTooltip(
                "show_filters",
                "Show filter toolbar",
                FontAwesomeIcon.Search,
                ref this.showFilterToolbar))
        {
            this.showFilterToolbar = !this.showFilterToolbar;
        }

        ImGui.SameLine();

        if (this.DrawToggleButtonWithTooltip(
                "show_uncaught_exceptions",
                "Show uncaught exception while filtering",
                FontAwesomeIcon.Bug,
                ref this.filterShowUncaughtExceptions))
        {
            this.filterShowUncaughtExceptions = !this.filterShowUncaughtExceptions;
        }

        ImGui.SameLine();

        if (ImGuiComponents.IconButton("clear_log", FontAwesomeIcon.Trash))
        {
            this.QueueClear();
        }

        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Clear Log");

        ImGui.SameLine();

        if (this.DrawToggleButtonWithTooltip(
                "copy_mode",
                "Enable Copy Mode\nRight-click to copy entire log",
                FontAwesomeIcon.Copy,
                ref this.copyMode))
        {
            this.copyMode = !this.copyMode;

            if (!this.copyMode)
            {
                foreach (var entry in this.filteredLogEntries)
                {
                    entry.SelectedForCopy = false;
                }
            }
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            this.CopyFilteredLogEntries(false);

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

        var inputWidth = 200.0f * ImGuiHelpers.GlobalScale;
        var nextCursorPosX = ImGui.GetContentRegionMax().X - (2 * inputWidth) - ImGui.GetStyle().ItemSpacing.X;
        var breakInputLines = nextCursorPosX < 0;
        if (ImGui.GetCursorPosX() > nextCursorPosX)
        {
            ImGui.NewLine();
            inputWidth = ImGui.GetWindowWidth() - (ImGui.GetStyle().WindowPadding.X * 2);

            if (!breakInputLines)
                inputWidth = (inputWidth - ImGui.GetStyle().ItemSpacing.X) / 2; 
        }
        else
        {
            ImGui.SetCursorPosX(nextCursorPosX);
        }

        ImGui.PushItemWidth(inputWidth);
        if (ImGui.InputTextWithHint(
                "##textHighlight",
                "regex highlight",
                ref this.textHighlight,
                2048,
                ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll)
            || ImGui.IsItemDeactivatedAfterEdit())
        {
            this.compiledLogHighlight = null;
            this.exceptionLogHighlight = null;
            try
            {
                if (this.textHighlight != string.Empty)
                    this.compiledLogHighlight = new(this.textHighlight, RegexOptions.IgnoreCase);
            }
            catch (Exception e)
            {
                this.exceptionLogHighlight = e;
            }

            foreach (var log in this.logText)
                log.HighlightMatches = null;
        }

        if (!breakInputLines)
            ImGui.SameLine();

        ImGui.PushItemWidth(inputWidth);
        if (ImGui.InputTextWithHint(
                "##textFilter",
                "regex global filter",
                ref this.textFilter,
                2048,
                ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll)
            || ImGui.IsItemDeactivatedAfterEdit())
        {
            this.compiledLogFilter = null;
            this.exceptionLogFilter = null;
            try
            {
                this.compiledLogFilter = new(this.textFilter, RegexOptions.IgnoreCase);

                this.QueueRefilter();
            }
            catch (Exception e)
            {
                this.exceptionLogFilter = e;
            }

            foreach (var log in this.logText)
                log.HighlightMatches = null;
        }
    }

    private void DrawSettingsPopup(DalamudConfiguration configuration)
    {
        if (ImGui.Checkbox("Open at startup", ref this.autoOpen))
        {
            configuration.LogOpenAtStartup = this.autoOpen;
            configuration.QueueSave();
        }

        if (ImGui.Checkbox("Auto-scroll", ref this.autoScroll))
        {
            configuration.LogAutoScroll = this.autoScroll;
            configuration.QueueSave();
        }

        ImGui.TextUnformatted("Logs buffer");
        ImGui.SliderInt("lines", ref this.logLinesLimit, LogLinesMinimum, LogLinesMaximum);
        if (ImGui.Button("Apply"))
        {
            this.logLinesLimit = Math.Max(LogLinesMinimum, this.logLinesLimit);

            configuration.LogLinesLimit = this.logLinesLimit;
            configuration.QueueSave();

            ImGui.CloseCurrentPopup();
        }
    }

    private void DrawFilterToolbar()
    {
        if (!this.showFilterToolbar) return;

        PluginFilterEntry? removalEntry = null;
        using var table = ImRaii.Table(
            "plugin_filter_entries",
            4,
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
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
                this.pluginFilters.Add(
                    new PluginFilterEntry
                    {
                        Source = this.selectedSource,
                        Filter = string.Empty,
                        Level = LogEventLevel.Debug,
                    });
            }

            this.QueueRefilter();
        }

        ImGui.TableNextColumn();
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.BeginCombo("##Sources", this.selectedSource, ImGuiComboFlags.HeightLarge))
        {
            var sourceNames = Service<PluginManager>.Get().InstalledPlugins
                                                    .Select(p => p.Manifest.InternalName)
                                                    .OrderBy(s => s)
                                                    .Prepend("DalamudInternal")
                                                    .Where(
                                                        name => this.pluginFilter is "" || new FuzzyMatcher(
                                                                    this.pluginFilter.ToLowerInvariant(),
                                                                    MatchMode.Fuzzy).Matches(name.ToLowerInvariant()) !=
                                                                0)
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
            ImGui.PushID(entry.Source);

            ImGui.TableNextColumn();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            {
                removalEntry = entry;
            }

            ImGui.TableNextColumn();
            ImGui.Text(entry.Source);

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.BeginCombo("##levels", $"{entry.Level}+"))
            {
                foreach (var value in Enum.GetValues<LogEventLevel>())
                {
                    if (ImGui.Selectable(value.ToString(), value == entry.Level))
                    {
                        entry.Level = value;
                        this.QueueRefilter();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var entryFilter = entry.Filter;
            if (ImGui.InputTextWithHint(
                    "##filter",
                    $"{entry.Source} regex filter",
                    ref entryFilter,
                    2048,
                    ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll)
                || ImGui.IsItemDeactivatedAfterEdit())
            {
                entry.Filter = entryFilter;
                if (entry.FilterException is null)
                    this.QueueRefilter();
            }

            ImGui.PopID();
        }

        if (removalEntry is { } toRemove)
        {
            this.pluginFilters.Remove(toRemove);
            this.QueueRefilter();
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
                this.QueueClear();
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

    /// <summary>Add a log entry to the display.</summary>
    /// <param name="line">The line to add.</param>
    /// <param name="logEvent">The Serilog event associated with this line.</param>
    /// <returns>Number of lines added to <see cref="filteredLogEntries"/>.</returns>
    private int HandleLogLine(string line, LogEvent logEvent)
    {
        ThreadSafety.DebugAssertMainThread();

        // These lines are too huge, and only useful for troubleshooting after the game exist.
        if (line.StartsWith("TROUBLESHOOTING:") || line.StartsWith("LASTEXCEPTION:"))
            return 0;

        // Create a log entry template.
        var entry = new LogEntry
        {
            Level = logEvent.Level,
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

        var ssp = line.AsSpan();
        var numLines = 0;
        while (true)
        {
            var next = ssp.IndexOfAny('\r', '\n');
            if (next == -1)
            {
                // Last occurrence; transfer the ownership of the new entry to the queue.
                entry.Line = ssp.ToString();
                numLines += this.AddAndFilter(entry);
                break;
            }

            // There will be more; create a clone of the entry with the current line.
            numLines += this.AddAndFilter(entry with { Line = ssp[..next].ToString() });

            // Mark further lines as multiline.
            entry.IsMultiline = true;

            // Skip the detected line break.
            ssp = ssp[next..];
            ssp = ssp.StartsWith("\r\n") ? ssp[2..] : ssp[1..];
        }

        return numLines;
    }

    /// <summary>Adds a line to the log list and the filtered log list accordingly.</summary>
    /// <param name="entry">The new log entry to add.</param>
    /// <returns>Number of lines added to <see cref="filteredLogEntries"/>.</returns>
    private int AddAndFilter(LogEntry entry)
    {
        ThreadSafety.DebugAssertMainThread();

        this.logText.Add(entry);

        if (!this.IsFilterApplicable(entry))
            return 0;

        this.filteredLogEntries.Add(entry);
        return 1;
    }

    /// <summary>Determines if a log entry passes the user-specified filter.</summary>
    /// <param name="entry">The entry to test.</param>
    /// <returns><c>true</c> if it passes the filter.</returns>
    private bool IsFilterApplicable(LogEntry entry)
    {
        ThreadSafety.DebugAssertMainThread();

        if (this.exceptionLogFilter is not null)
            return false;

        // If this entry is below a newly set minimum level, fail it
        if (EntryPoint.LogLevelSwitch.MinimumLevel > entry.Level)
            return false;

        // Show exceptions that weren't properly tagged with a Source (generally meaning they were uncaught)
        // After log levels because uncaught exceptions should *never* fall below Error.
        if (this.filterShowUncaughtExceptions && entry.HasException && entry.Source == null)
            return true;

        // (global filter) && (plugin filter) must be satisfied.
        var wholeCond = true;

        // If we have a global filter, check that first
        if (this.compiledLogFilter is { } logFilter)
        {
            // Someone will definitely try to just text filter a source without using the actual filters, should allow that.
            var matchesSource = entry.Source is not null && logFilter.IsMatch(entry.Source);
            var matchesContent = logFilter.IsMatch(entry.Line);

            wholeCond &= matchesSource || matchesContent;
        }

        // If this entry has a filter, check the filter
        if (this.pluginFilters.Count > 0)
        {
            var matchesAny = false;

            foreach (var filterEntry in this.pluginFilters)
            {
                if (!string.Equals(filterEntry.Source, entry.Source, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var allowedLevel = filterEntry.Level <= entry.Level;
                var matchesContent = filterEntry.FilterRegex?.IsMatch(entry.Line) is not false;

                matchesAny |= allowedLevel && matchesContent;
                if (matchesAny)
                    break;
            }

            wholeCond &= matchesAny;
        }

        return wholeCond;
    }

    /// <summary>Queues clearing the window of all log entries, before next call to <see cref="Draw"/>.</summary>
    private void QueueClear() => this.pendingClearLog = true;

    /// <summary>Queues filtering the log entries again, before next call to <see cref="Draw"/>.</summary>
    private void QueueRefilter() => this.pendingRefilter = true;

    /// <summary>Enqueues the new log line to the log-to-be-processed queue.</summary>
    /// <remarks>See <see cref="FrameworkOnUpdate"/> for the handler for the queued log entries.</remarks>
    private void OnLogLine(object sender, (string Line, LogEvent LogEvent) logEvent) =>
        this.newLogEntries.Enqueue(logEvent);

    private bool DrawToggleButtonWithTooltip(
        string buttonId, string tooltip, FontAwesomeIcon icon, ref bool enabledState)
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

    private void OnDalamudConfigurationSaved(DalamudConfiguration dalamudConfiguration)
    {
        this.logLinesLimit = dalamudConfiguration.LogLinesLimit;
        var limit = Math.Max(LogLinesMinimum, this.logLinesLimit);
        this.logText.Size = limit;
        this.filteredLogEntries.Size = limit;
    }

    private unsafe void DrawHighlighted(
        ReadOnlySpan<char> line,
        MatchCollection matches,
        uint col,
        uint highlightCol)
    {
        Span<int> charOffsets = stackalloc int[(matches.Count * 2) + 2];
        var charOffsetsIndex = 1;
        for (var j = 0; j < matches.Count; j++)
        {
            var g = matches[j].Groups[0];
            charOffsets[charOffsetsIndex++] = g.Index;
            charOffsets[charOffsetsIndex++] = g.Index + g.Length;
        }

        charOffsets[charOffsetsIndex++] = line.Length;

        var screenPos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList().NativePtr;
        var font = ImGui.GetFont();
        var size = ImGui.GetFontSize();
        var scale = size / font.FontSize;
        var hotData = font.IndexedHotDataWrapped();
        var lookup = font.IndexLookupWrapped();
        var kern = (ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.NoKerning) == 0;
        var lastc = '\0';
        for (var i = 0; i < charOffsetsIndex - 1; i++)
        {
            var begin = charOffsets[i];
            var end = charOffsets[i + 1];
            if (begin == end)
                continue;

            for (var j = begin; j < end; j++)
            {
                var currc = line[j];
                if (currc >= lookup.Length || lookup[currc] == ushort.MaxValue)
                    currc = (char)font.FallbackChar;

                if (kern)
                    screenPos.X += scale * ImGui.GetFont().GetDistanceAdjustmentForPair(lastc, currc);
                font.RenderChar(drawList, size, screenPos, i % 2 == 1 ? highlightCol : col, currc);

                screenPos.X += scale * hotData[currc].AdvanceX;
                lastc = currc;
            }
        }

        ImGui.Dummy(screenPos - ImGui.GetCursorScreenPos());
    }

    private record LogEntry
    {
        public string Line { get; set; } = string.Empty;

        public LogEventLevel Level { get; init; }

        public DateTimeOffset TimeStamp { get; init; }

        public bool IsMultiline { get; set; }

        /// <summary>
        /// Gets or sets the system responsible for generating this log entry. Generally will be a plugin's
        /// InternalName.
        /// </summary>
        public string? Source { get; set; }

        public bool SelectedForCopy { get; set; }

        public bool HasException { get; init; }

        public MatchCollection? HighlightMatches { get; set; }

        public string TimestampString => this.TimeStamp.ToString("HH:mm:ss.fff");

        public override string ToString() =>
            this.IsMultiline
                ? $"\t{this.Line}"
                : $"{this.TimestampString} | {GetTextForLogEventLevel(this.Level)} | {this.Line}";
    }

    private class PluginFilterEntry
    {
        private string filter = string.Empty;

        public string Source { get; init; } = string.Empty;

        public string Filter
        {
            get => this.filter;
            set
            {
                this.filter = value;
                this.FilterRegex = null;
                this.FilterException = null;
                if (value == string.Empty)
                    return;

                try
                {
                    this.FilterRegex = new(value, RegexOptions.IgnoreCase);
                }
                catch (Exception e)
                {
                    this.FilterException = e;
                }
            }
        }

        public LogEventLevel Level { get; set; }

        public Regex? FilterRegex { get; private set; }

        public Exception? FilterException { get; private set; }
    }
}

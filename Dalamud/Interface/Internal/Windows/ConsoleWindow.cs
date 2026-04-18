using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Console;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

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
    private const int HistorySize = 50;

    // Fields below should be touched only from the main thread.
    private readonly RollingList<LogLine> logText;
    // use linked list for entries for O(1) removal and addition
    private readonly LinkedList<LogEntry> logEntries;
    private LinkedList<LogEntry> filteredLogEntries;

    private readonly List<PluginFilterEntry> pluginFilters = [];

    private readonly DalamudConfiguration configuration;

    private int newRolledLines;
    private int newFilteredEntriesAdded;
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

    private string? completionZipText = null;
    private int completionTabIdx = 0;

    private IActiveNotification? prevCopyNotification;

    /// <summary>Initializes a new instance of the <see cref="ConsoleWindow"/> class.</summary>
    /// <param name="configuration">An instance of <see cref="DalamudConfiguration"/>.</param>
    public ConsoleWindow(DalamudConfiguration configuration)
        : base("Dalamud Console", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.configuration = configuration;

        this.autoScroll = configuration.LogAutoScroll;
        this.autoOpen = configuration.LogOpenAtStartup;

        Service<Framework>.GetAsync().ContinueWith(r => r.Result.Update += this.FrameworkOnUpdate);

        var cm = Service<ConsoleManager>.Get();
        cm.AddCommand("clear", "Clear the console log", () =>
        {
            this.QueueClear();
            return true;
        });
        cm.AddAlias("clear", "cls");

        this.Size = new Vector2(500, 400);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.RespectCloseHotkey = false;

        this.logLinesLimit = configuration.LogLinesLimit;

        var limit = Math.Max(LogLinesMinimum, this.logLinesLimit);
        this.logText = new(limit);

        this.logEntries = [];
        this.filteredLogEntries = this.logEntries;

        this.logText.OnEviction += this.HandleLogLineEviction;

        this.configuration.DalamudConfigurationSaved += this.OnDalamudConfigurationSaved;

        this.clipperPtr = ImGui.ImGuiListClipper();
    }

    /// <summary>Gets the queue where log entries that are not processed yet are stored.</summary>
    public static ConcurrentQueue<(string Line, LogEvent LogEvent)> NewLogEntries { get; } = new();

    /// <summary>
    /// Gets or sets the current text filter.
    /// </summary>
    public string TextFilter
    {
        get => this.textFilter;
        set
        {
            this.textFilter = value;
            this.RecompileLogFilter();
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
        this.configuration.DalamudConfigurationSaved -= this.OnDalamudConfigurationSaved;
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
                ImGuiColors.ErrorForeground,
                $"Regex Filter Error: {this.exceptionLogFilter.GetType().Name}");
            ImGui.Text(this.exceptionLogFilter.Message);
        }

        if (this.exceptionLogHighlight is not null)
        {
            ImGui.TextColored(
                ImGuiColors.ErrorForeground,
                $"Regex Highlight Error: {this.exceptionLogHighlight.GetType().Name}");
            ImGui.Text(this.exceptionLogHighlight.Message);
        }

        var sendButtonSize = ImGui.CalcTextSize("Send"u8) +
                             ((new Vector2(16, 0) + (ImGui.GetStyle().FramePadding * 2)) * ImGuiHelpers.GlobalScale);
        var scrollingHeight = ImGui.GetContentRegionAvail().Y - sendButtonSize.Y;
        ImGui.BeginChild(
            "scrolling"u8,
            new Vector2(0, scrollingHeight),
            false,
            ImGuiWindowFlags.AlwaysHorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar);

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        ImGui.PushFont(InterfaceManager.MonoFont);

        var childPos = ImGui.GetWindowPos();
        var childDrawList = ImGui.GetWindowDrawList();
        var childSize = ImGui.GetWindowSize();

        var timestampWidth = ImGui.CalcTextSize("00:00:00.000"u8).X;
        var levelWidth = ImGui.CalcTextSize("AAA"u8).X;
        var separatorWidth = ImGui.CalcTextSize(" | "u8).X;
        var cursorLogLevel = timestampWidth + separatorWidth;
        var cursorLogLine = cursorLogLevel + levelWidth + separatorWidth;

        var lastLinePosY = ImGui.GetCursorPosY();
        var logLineHeight = 0.0f;

        this.clipperPtr.Begin(this.filteredLogEntries.Count);
        var clipperNode = this.filteredLogEntries.First;
        var clipperNodeIdx = 0;
        while (this.clipperPtr.Step())
        {
            for (var i = this.clipperPtr.DisplayStart; i < this.clipperPtr.DisplayEnd; i++)
            {
                var index = Math.Max(i - this.newRolledLines, 0);

                while (clipperNodeIdx < index && clipperNode?.Next != null)
                {
                    clipperNode = clipperNode.Next;
                    clipperNodeIdx++;
                }

                if (clipperNode == null) break;
                var entry = clipperNode.Value;

                ImGui.Separator();

                if (entry.SelectedForCopy)
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, ImGuiColors.ParsedGrey);
                    ImGui.PushStyleColor(ImGuiCol.HeaderActive, ImGuiColors.ParsedGrey);
                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGuiColors.ParsedGrey);
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, GetColorForLogEventLevel(entry.Level));
                    ImGui.PushStyleColor(ImGuiCol.HeaderActive, GetColorForLogEventLevel(entry.Level));
                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, GetColorForLogEventLevel(entry.Level));
                }

                ImGui.Selectable(
                    "###console_null"u8,
                    true,
                    ImGuiSelectableFlags.AllowItemOverlap | ImGuiSelectableFlags.SpanAllColumns);

                this.HandleCopyMode(i, entry);

                ImGui.SameLine();

                ImGui.PopStyleColor(3);

                var isFirstLine = true;
                var nextLineY = 0.0f;
                var activeRegex = this.compiledLogHighlight ?? this.compiledLogFilter;
                foreach (var logLine in entry.Lines)
                {
                    if (!isFirstLine)
                    {
                        ImGui.SetCursorPosY(nextLineY);
                    }

                    if (isFirstLine)
                    {
                        ImGui.Text(entry.TimestampString);
                        ImGui.SameLine();
                        ImGui.SetCursorPosX(cursorLogLevel);
                        ImGui.Text(GetTextForLogEventLevel(entry.Level));
                        ImGui.SameLine();
                        isFirstLine = false;
                    }

                    ImGui.SetCursorPosX(cursorLogLine);
                    var beforeY = ImGui.GetCursorPosY();
                    logLine.HighlightMatches ??= activeRegex?.Matches(logLine.Text);
                    if (logLine.HighlightMatches is { Count: > 0 } matches)
                    {
                        this.DrawHighlighted(
                            logLine.Text,
                            matches,
                            ImGui.GetColorU32(ImGuiCol.Text),
                            ImGui.GetColorU32(ImGuiColors.HealerGreen));
                    }
                    else
                    {
                        ImGui.Text(logLine.Text);
                    }

                    // DrawHighlighted uses Dummy(width, 0) which only advances Y when DC.CurrLineSize.y > 0
                    // (true for line 1 due to timestamp/level Text items, false for continuation lines).
                    // Track nextLineY explicitly so continuation lines are always positioned correctly.
                    var afterY = ImGui.GetCursorPosY();
                    nextLineY = afterY > beforeY ? afterY : beforeY + ImGui.GetTextLineHeight();
                }

                ImGui.SetCursorPosY(nextLineY);
                var currentLinePosY = ImGui.GetCursorPosY();
                logLineHeight = currentLinePosY - lastLinePosY;
                lastLinePosY = currentLinePosY;
            }
        }

        this.clipperPtr.End();

        ImGui.PopFont();

        ImGui.PopStyleVar();

        if (this.autoScroll && this.newFilteredEntriesAdded > 0)
        {
            ImGui.SetScrollHereY(1.0f);
        }
        else if (this.newRolledLines > 0 && logLineHeight > 0)
        {
            ImGui.SetScrollY(ImGui.GetScrollY() - (logLineHeight * this.newRolledLines));
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
            ImGui.PushStyleColor(ImGuiCol.FrameBg, this.lastCmdSuccess.Value ? ImGuiColors.SuccessBackground : ImGuiColors.ErrorBackground);
        }

        ImGui.SetNextItemWidth(
            ImGui.GetContentRegionAvail().X - sendButtonSize.X -
            (ImGui.GetStyle().ItemSpacing.X * ImGuiHelpers.GlobalScale));

        var getFocus = false;
        unsafe
        {
            if (ImGui.InputText(
                    "##command_box"u8,
                    ref this.commandText,
                    255,
                    ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackCompletion |
                    ImGuiInputTextFlags.CallbackHistory | ImGuiInputTextFlags.CallbackEdit,
                    this.CommandInputCallback))
            {
                NewLogEntries.Enqueue((this.commandText, new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate(string.Empty, []), [])));
                this.ProcessCommand();
                getFocus = true;
            }

            ImGui.SameLine();
        }

        ImGui.SetItemDefaultFocus();
        if (getFocus) ImGui.SetKeyboardFocusHere(-1); // Auto focus previous widget

        if (hadColor) ImGui.PopStyleColor();

        if (ImGui.Button("Send"u8, sendButtonSize))
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
            this.logEntries.Clear();
            this.filteredLogEntries = this.logEntries;
            NewLogEntries.Clear();
        }

        if (this.pendingRefilter)
        {
            this.pendingRefilter = false;
            this.filteredLogEntries = new LinkedList<LogEntry>(this.logEntries.Where(this.IsFilterApplicable));
        }

        var numPrevFilteredLogEntries = this.filteredLogEntries.Count;
        var addedEntries = 0;
        while (NewLogEntries.TryDequeue(out var logLine))
            addedEntries += this.HandleLogLine(logLine.Line, logLine.LogEvent);
        this.newFilteredEntriesAdded = addedEntries;
        this.newRolledLines = addedEntries - (this.filteredLogEntries.Count - numPrevFilteredLogEntries);
    }

    private void HandleLogLineEviction(object? sender, LogLine logLine)
    {
        var entry = logLine.Entry;
        entry.Lines.Remove(logLine);
        if (entry.Lines.Count == 0)
        {
            this.logEntries.Remove(entry);
            this.filteredLogEntries.Remove(entry);
        }
    }

    private void HandleCopyMode(int i, LogEntry entry)
    {
        var selectionChanged = false;

        // If copyStart is -1, it means a drag has not been started yet, let's start one, and select the starting spot.
        if (this.copyMode && this.copyStart == -1 && ImGui.IsItemClicked())
        {
            this.copyStart = i;
            entry.SelectedForCopy = !entry.SelectedForCopy;

            selectionChanged = true;
        }

        // Update the selected range when dragging over entries
        if (this.copyMode && this.copyStart != -1 && ImGui.IsItemHovered() &&
            ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            if (!entry.SelectedForCopy)
            {
                var entryIdx = 0;
                foreach (var e in this.filteredLogEntries)
                {
                    e.SelectedForCopy = this.copyStart < i
                        ? entryIdx >= this.copyStart && entryIdx <= i
                        : entryIdx >= i && entryIdx <= this.copyStart;
                    entryIdx++;
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
        ImGui.PushItemWidth(150.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginCombo("##log_level"u8, $"{EntryPoint.LogLevelSwitch.MinimumLevel}+"))
        {
            foreach (var value in Enum.GetValues<LogEventLevel>())
            {
                if (ImGui.Selectable(value.ToString(), value == EntryPoint.LogLevelSwitch.MinimumLevel))
                {
                    EntryPoint.LogLevelSwitch.MinimumLevel = value;
                    this.configuration.LogLevel = value;
                    this.configuration.QueueSave();
                    this.QueueRefilter();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();

        var settingsPopup = ImGui.BeginPopup("##console_settings"u8);
        if (settingsPopup)
        {
            this.DrawSettingsPopup();
            ImGui.EndPopup();
        }
        else if (this.settingsPopupWasOpen)
        {
            // Prevent side effects in case Apply wasn't clicked
            this.logLinesLimit = this.configuration.LogLinesLimit;
        }

        this.settingsPopupWasOpen = settingsPopup;

        if (this.DrawToggleButtonWithTooltip("show_settings", "Show settings", FontAwesomeIcon.List, ref settingsPopup))
            ImGui.OpenPopup("##console_settings"u8);

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

        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Clear Log"u8);

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

        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Kill game"u8);

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
                "##textHighlight"u8,
                "regex highlight"u8,
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

            foreach (var logEntry in this.logEntries)
                foreach (var logLine in logEntry.Lines)
                    logLine.HighlightMatches = null;
        }

        if (!breakInputLines)
            ImGui.SameLine();

        ImGui.PushItemWidth(inputWidth);
        if (ImGui.InputTextWithHint(
                "##textFilter"u8,
                "regex global filter"u8,
                ref this.textFilter,
                2048,
                ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll)
            || ImGui.IsItemDeactivatedAfterEdit())
        {
            this.RecompileLogFilter();
        }
    }

    private void RecompileLogFilter()
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

        foreach (var logEntry in this.logEntries)
            foreach (var logLine in logEntry.Lines)
                logLine.HighlightMatches = null;
    }

    private void DrawSettingsPopup()
    {
        if (ImGui.Checkbox("Open at startup"u8, ref this.autoOpen))
        {
            this.configuration.LogOpenAtStartup = this.autoOpen;
            this.configuration.QueueSave();
        }

        if (ImGui.Checkbox("Auto-scroll"u8, ref this.autoScroll))
        {
            this.configuration.LogAutoScroll = this.autoScroll;
            this.configuration.QueueSave();
        }

        ImGui.Text("Logs buffer"u8);
        ImGui.SliderInt("lines"u8, ref this.logLinesLimit, LogLinesMinimum, LogLinesMaximum);
        if (ImGui.Button("Apply"u8))
        {
            this.logLinesLimit = Math.Max(LogLinesMinimum, this.logLinesLimit);

            this.configuration.LogLinesLimit = this.logLinesLimit;
            this.configuration.QueueSave();

            ImGui.CloseCurrentPopup();
        }
    }

    private void DrawFilterToolbar()
    {
        if (!this.showFilterToolbar) return;

        PluginFilterEntry? removalEntry = null;
        using var table = ImRaii.Table(
            "plugin_filter_entries"u8,
            4,
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupColumn("##remove_button"u8, ImGuiTableColumnFlags.WidthFixed, 25.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##source_name"u8, ImGuiTableColumnFlags.WidthFixed, 150.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##log_level"u8, ImGuiTableColumnFlags.WidthFixed, 150.0f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##filter_text"u8, ImGuiTableColumnFlags.WidthStretch);

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
        if (ImGui.BeginCombo("##Sources"u8, this.selectedSource, ImGuiComboFlags.HeightLarge))
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
            ImGui.InputTextWithHint("##PluginSearchFilter"u8, "Filter Plugin List"u8, ref this.pluginFilter, 2048);
            ImGui.Separator();

            if (sourceNames.Count == 0)
            {
                ImGui.TextColored(ImGuiColors.AttentionForeground, "No Results"u8);
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
            if (ImGui.BeginCombo("##levels"u8, $"{entry.Level}+"))
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
                    "##filter"u8,
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
            if (string.IsNullOrEmpty(this.commandText))
                return;

            this.historyPos = -1;

            if (this.commandText != this.configuration.LogCommandHistory.LastOrDefault())
                this.configuration.LogCommandHistory.Add(this.commandText);

            if (this.configuration.LogCommandHistory.Count > HistorySize)
                this.configuration.LogCommandHistory.RemoveAt(0);

            this.configuration.QueueSave();

            this.lastCmdSuccess = Service<ConsoleManager>.Get().ProcessCommand(this.commandText);
            this.commandText = string.Empty;

            // TODO: Force scroll to bottom
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during command dispatch");
            this.lastCmdSuccess = false;
        }
    }

    private int CommandInputCallback(ref ImGuiInputTextCallbackData data)
    {
        switch (data.EventFlag)
        {
            case ImGuiInputTextFlags.CallbackEdit:
                this.completionZipText = null;
                this.completionTabIdx = 0;
                break;

            case ImGuiInputTextFlags.CallbackCompletion:
                var text = Encoding.UTF8.GetString(data.BufTextSpan);

                var words = text.Split();

                // We can't do any completion for parameters at the moment since it just calls into CommandHandler
                if (words.Length > 1)
                    return 0;

                var wordToComplete = words[0];
                if (wordToComplete.IsNullOrWhitespace())
                    return 0;

                if (this.completionZipText is not null)
                    wordToComplete = this.completionZipText;

                // TODO: Improve this, add partial completion, arguments, description, etc.
                // https://github.com/ocornut/imgui/blob/master/imgui_demo.cpp#L6443-L6484
                var candidates = Service<ConsoleManager>.Get().Entries
                                                        .Where(x => x.Key.StartsWith(wordToComplete))
                                                        .Select(x => x.Key);

                candidates = candidates.Union(
                    Service<CommandManager>.Get().Commands
                                           .Where(x => x.Key.StartsWith(wordToComplete)).Select(x => x.Key))
                                       .ToArray();

                if (candidates.Any())
                {
                    string? toComplete = null;
                    if (this.completionZipText == null)
                    {
                        // Find the "common" prefix of all matches
                        toComplete = candidates.Aggregate(
                            (prefix, candidate) => string.Concat(prefix.Zip(candidate, (a, b) => a == b ? a : '\0')));

                        this.completionZipText = toComplete;
                    }
                    else
                    {
                        toComplete = candidates.ElementAt(this.completionTabIdx);
                        this.completionTabIdx = (this.completionTabIdx + 1) % candidates.Count();
                    }

                    if (toComplete != null)
                    {
                        data.DeleteChars(0, data.BufTextLen);
                        data.InsertChars(0, toComplete);
                    }
                }

                break;

            case ImGuiInputTextFlags.CallbackHistory:
                var prevPos = this.historyPos;

                if (data.EventKey == ImGuiKey.UpArrow)
                {
                    if (this.historyPos == -1)
                        this.historyPos = this.configuration.LogCommandHistory.Count - 1;
                    else if (this.historyPos > 0)
                        this.historyPos--;
                }
                else if (data.EventKey == ImGuiKey.DownArrow)
                {
                    if (this.historyPos != -1)
                    {
                        if (++this.historyPos >= this.configuration.LogCommandHistory.Count)
                        {
                            this.historyPos = -1;
                        }
                    }
                }

                if (prevPos != this.historyPos)
                {
                    var historyStr = this.historyPos >= 0 ? this.configuration.LogCommandHistory[this.historyPos] : string.Empty;

                    data.DeleteChars(0, data.BufTextLen);
                    data.InsertChars(0, historyStr);
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
        {
            return 0;
        }

        string? logSource = null;

        if (logEvent.Properties.ContainsKey("Dalamud.ModuleName"))
        {
            logSource = "DalamudInternal";
        }
        else if (logEvent.Properties.TryGetValue("Dalamud.PluginName", out var sourceProp) &&
                 sourceProp is ScalarValue { Value: string sourceValue })
        {
            logSource = sourceValue;
        }

        // Create a log entry template.
        var linesLinkedList = new LinkedList<LogLine>();
        var entry = new LogEntry
        {
            Level = logEvent.Level,
            TimeStamp = logEvent.Timestamp,
            HasException = logEvent.Exception != null,
            Lines = linesLinkedList,
            Source = logSource,
        };

        // the 'line' string may actually contain multiple lines, delimited by newlines. split the lines here,
        // but maintain a single LogEntry which contains multiple lines.
        var ssp = line.AsSpan();

        // parse out all the lines and append them to the linked list
        var nextStart = 0;
        for (var i = 0; i < ssp.Length; i++)
        {
            var c = ssp[i];
            var isNewLine = c == '\n';
            var isCarriageReturn = c == '\r';
            if (!isNewLine && !isCarriageReturn)
                continue;

            linesLinkedList.AddLast(new LogLine(ssp[nextStart..i].ToString(), entry));

            // skip \r\n as a single delimiter; i++ here so the for loop's i++ lands past the \n
            if (isCarriageReturn && i + 1 < ssp.Length && ssp[i + 1] == '\n')
            {
                nextStart = i + 2;
                i++;
            }
            else
            {
                nextStart = i + 1;
            }
        }

        // append trailing text after the last newline (or the whole string if no newlines)
        linesLinkedList.AddLast(new LogLine(ssp[nextStart..].ToString(), entry));

        return this.AddAndFilter(entry);
    }

    /// <summary>Adds a line to the log list and the filtered log list accordingly.</summary>
    /// <param name="entry">The new log entry to add.</param>
    /// <returns>Number of lines added to <see cref="filteredLogEntries"/>.</returns>
    private int AddAndFilter(LogEntry entry)
    {
        ThreadSafety.DebugAssertMainThread();

        // add entry first, because theoretically the lines of one entry could instantly evict each other
        this.logEntries.AddLast(entry);
        this.logText.AddRange(entry.Lines);

        if (!this.IsFilterApplicable(entry))
            return 0;

        // When no filter is active, or we just cleared the list,
        // filteredLogEntries refers to the same object as logEntries —
        // logEntries.AddLast above already added the entry, don't double-add.
        if (!ReferenceEquals(this.filteredLogEntries, this.logEntries))
            this.filteredLogEntries.AddLast(entry);

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

        // If we have a global filter, check that first
        if (this.compiledLogFilter is { } logFilter)
        {
            // Someone will definitely try to just text filter a source without using the actual filters, should allow that.
            var matchesSource = entry.Source is not null && logFilter.IsMatch(entry.Source);
            // check whether the filter matches any of the lines of the entry
            var matchesContent = entry.Lines.Any(line => logFilter.IsMatch(line));

            // (global filter) && (plugin filter) must be satisfied, so if global is wrong, we can stop already.
            // saves us some regex matches.
            if (!(matchesSource || matchesContent))
            {
                return false;
            }
        }

        return CheckPluginFilters();

        bool CheckPluginFilters()
        {
            // if no plugin filters exist, implicitly accept all log entries
            if (this.pluginFilters.Count == 0)
            {
                return true;
            }

            // otherwise, there has to be a filter that matches this source, loglevel and regex
            var applicableFilters = this.pluginFilters
                                 .Where(filter => string.Equals(
                                            filter.Source,
                                            entry.Source,
                                            StringComparison.InvariantCultureIgnoreCase));

            return applicableFilters.Any(filter =>
            {
                var allowedLevel = filter.Level <= entry.Level;
                var matchesContent = filter.FilterRegex is null ||
                                     entry.Lines.Any(line => filter.FilterRegex.IsMatch(line));
                return allowedLevel && matchesContent;
            });
        }
    }

    /// <summary>Queues clearing the window of all log entries, before next call to <see cref="Draw"/>.</summary>
    private void QueueClear() => this.pendingClearLog = true;

    /// <summary>Queues filtering the log entries again, before next call to <see cref="Draw"/>.</summary>
    private void QueueRefilter() => this.pendingRefilter = true;

    private bool DrawToggleButtonWithTooltip(
        string buttonId, string tooltip, FontAwesomeIcon icon, ref bool enabledState)
    {
        var result = false;

        var buttonEnabled = enabledState;
        if (buttonEnabled) ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.SuccessBackground with { W = 0.25f });
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
        // separately maintaining a max entries limit is pointless before the value is added to the configuration,
        // as every entry has 1 or more lines, so simply monitoring the lines is good enough.
        this.logText.Size = limit;
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
        var drawList = ImGui.GetWindowDrawList().Handle;
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

    /// <summary>
    /// Represents a single, renderable line for the Table.
    /// </summary>
    /// <param name="Text">The text that line should contain.</param>
    /// <param name="Entry">The <see cref="LogEntry"/> that this line is a part of.</param>
    private sealed record LogLine(string Text, LogEntry Entry)
    {
        public static implicit operator string(LogLine logLine) => logLine.Text;

        public MatchCollection? HighlightMatches { get; set; }
    }

    private sealed record LogEntry
    {
        public required LinkedList<LogLine> Lines { get; init; } = [];

        // keep LogLevel, timestamp etc. as part of LogEntry, because these properties are the same for all LogLines of an entry
        public required LogEventLevel Level { get; init; }

        public required DateTimeOffset TimeStamp { get; init; }

        /// <summary>
        /// Gets or sets the system responsible for generating this log entry. Generally will be a plugin's
        /// InternalName.
        /// </summary>
        public string? Source { get; set; }

        public bool SelectedForCopy { get; set; }

        public required bool HasException { get; init; }

        public string TimestampString => this.TimeStamp.ToString("HH:mm:ss.fff");

        public override string ToString()
        {
            var first = this.Lines.First;
            if (first == null) return string.Empty;
            var sb = new StringBuilder($"{this.TimestampString} | {GetTextForLogEventLevel(this.Level)} | {first.Value.Text}");
            for (var node = first.Next; node != null; node = node.Next)
                sb.Append($"\n\t{node.Value.Text}");
            return sb.ToString();
        }
    }

    private sealed class PluginFilterEntry
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
                if (string.IsNullOrWhiteSpace(value))
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

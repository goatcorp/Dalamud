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
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Spannables;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Rendering.Internal;
using Dalamud.Interface.Spannables.Strings;
using Dalamud.Interface.Spannables.Styles;
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

    private static readonly string[] WordBreakModeNames =
    {
        "Normal",
        "Break All",
        "Keep All",
        "Break Word",
    };

    // Only this field may be touched from any thread.
    private readonly ConcurrentQueue<(string Line, LogEvent LogEvent)> newLogEntries;

    // Fields below should be touched only from the main thread.
    private readonly RollingList<LogEntry> logText;
    private readonly RollingList<LogEntry> filteredLogEntries;

    private readonly List<string> history = new();
    private readonly List<PluginFilterEntry> pluginFilters = new();

    private readonly DalamudConfiguration activeConfiguration;

    private readonly ISpannable ellipsisSpannable;
    private readonly ISpannable wrapMarkerSpannable;

    private bool pendingRefilter;
    private bool pendingClearLog;
    private int newRolledLines;
    private int totalRolledLines;
    private int totalWrappedLines;

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
    private DalamudConfiguration? newSettings;
    private bool showFilterToolbar;
    private bool copyMode;
    private bool killGameArmed;

    private Vector2 prevWindowSize;
    private int configGeneration;
    private int prevConfigGeneration;

    private int historyPos;
    private int copyStart = -1;

    private IActiveNotification? prevCopyNotification;

    /// <summary>Initializes a new instance of the <see cref="ConsoleWindow"/> class.</summary>
    /// <param name="configuration">An instance of <see cref="DalamudConfiguration"/>.</param>
    public ConsoleWindow(DalamudConfiguration configuration)
        : base("Dalamud Console", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.activeConfiguration = configuration;
        SerilogEventSink.Instance.LogLine += this.OnLogLine;

        Service<Framework>.GetAsync().ContinueWith(r => r.Result.Update += this.FrameworkOnUpdate);
        Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync().ContinueWith(
            r => r.Result.Manager.MonoFontHandle!.ImFontChanged += this.MonoFontOnImFontChanged);

        this.Size = new Vector2(500, 400);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.RespectCloseHotkey = false;

        var limit = Math.Clamp(configuration.LogLinesLimit, LogLinesMinimum, LogLinesMaximum);
        this.newLogEntries = new();
        this.logText = new(limit);
        this.filteredLogEntries = new(limit);

        this.ellipsisSpannable = new SpannedStringBuilder().PushForeColor(0x80FFFFFF).Append("…");
        this.wrapMarkerSpannable = new SpannedStringBuilder()
                                   .PushFontSet(
                                       new(DalamudAssetFontAndFamilyId.From(DalamudAsset.InconsolataRegular)),
                                       out _)
                                   .PushEdgeColor(0xFF000044)
                                   .PushBorderWidth(1)
                                   .PushForeColor(0xFFCCCCFF)
                                   .PushItalic(true)
                                   .PushFontSize(-0.6f)
                                   .PushVerticalAlignment(VerticalAlignment.Middle)
                                   .Append(FontAwesomeIcon.ArrowTurnDown.ToIconString());

        configuration.DalamudConfigurationSaved += this.OnDalamudConfigurationSaved;

        unsafe
        {
            this.clipperPtr = new(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        }
    }

    private static SpannableRenderer Renderer => Service<SpannableRenderer>.Get();

    /// <summary>
    /// Gets an instance of <see cref="DalamudConfiguration"/> that may point to the live object or our temporary object
    /// for storing the configuration being changed.
    /// </summary>
    /// <remarks>Do not modify the return value.</remarks>
    private DalamudConfiguration StagingConfig => this.newSettings ?? this.activeConfiguration;

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
        this.activeConfiguration.DalamudConfigurationSaved -= this.OnDalamudConfigurationSaved;
        if (Service<Framework>.GetNullable() is { } framework)
            framework.Update -= this.FrameworkOnUpdate;
        if (Service<InterfaceManager.InterfaceManagerWithScene>.GetNullable() is { } imws)
            imws.Manager.MonoFontHandle!.ImFontChanged -= this.MonoFontOnImFontChanged;

        this.clipperPtr.Destroy();
        this.clipperPtr = default;
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        if (this.prevWindowSize != ImGui.GetWindowSize() || this.configGeneration != this.prevConfigGeneration)
        {
            this.prevWindowSize = ImGui.GetWindowSize();
            this.prevConfigGeneration = this.configGeneration;

            // These two lists are rolled over separately.
            foreach (var e in this.logText)
                e.NumLines = 0;
            foreach (var e in this.filteredLogEntries)
                e.NumLines = 0;
        }

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
        var useHorizontalScrolling =
            this.activeConfiguration.LogLineBreakMode == WordBreakType.KeepAll;
        ImGui.BeginChild(
            "scrolling",
            new(0, scrollingHeight),
            false,
            (useHorizontalScrolling ? ImGuiWindowFlags.AlwaysHorizontalScrollbar : 0)
            | ImGuiWindowFlags.AlwaysVerticalScrollbar);

        if (!useHorizontalScrolling)
            ImGui.SetScrollX(0);

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        ImGui.PushFont(InterfaceManager.MonoFont);

        var childPos = ImGui.GetWindowPos();
        var childDrawList = ImGui.GetWindowDrawList();
        var childSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();

        var timestampWidth = ImGui.CalcTextSize("00:00:00.000").X;
        var levelWidth = ImGui.CalcTextSize("AAA").X;
        var separatorWidth = ImGui.CalcTextSize(" | ").X;
        var cursorLogLevel = timestampWidth + separatorWidth;
        var cursorLogLine = cursorLogLevel + levelWidth + separatorWidth;

        var logLineHeight = ImGui.GetTextLineHeight();
        var messageAreaWidth = childSize.X - cursorLogLine;
        for (var i = this.filteredLogEntries.Count - 1; i >= 0; i--)
        {
            var entry = this.filteredLogEntries[i];
            if (entry.NumLines > 0)
            {
                for (i++; i < this.filteredLogEntries.Count; i++)
                {
                    this.filteredLogEntries[i].FirstLine =
                        this.filteredLogEntries[i - 1].FirstLine +
                        this.filteredLogEntries[i - 1].NumLines;
                }

                break;
            }

            entry.NumLines = Service<SpannableRenderer>.Get().Render(
                entry.Line,
                new(true, new() { MaxSize = new(messageAreaWidth, float.MaxValue) }),
                this.GetTextStateOptions()).FinalTextState.LineCount;
            this.totalWrappedLines += entry.NumLines;

            if (i == 0)
            {
                this.totalRolledLines = 0;
                this.filteredLogEntries[i].FirstLine = 0;
                this.totalWrappedLines = this.filteredLogEntries[i].NumLines;
                for (i++; i < this.filteredLogEntries.Count; i++)
                {
                    this.totalWrappedLines += this.filteredLogEntries[i].NumLines;
                    this.filteredLogEntries[i].FirstLine =
                        this.filteredLogEntries[i - 1].FirstLine +
                        this.filteredLogEntries[i - 1].NumLines;
                }

                break;
            }
        }

        this.clipperPtr.Begin(this.totalWrappedLines - this.totalRolledLines, logLineHeight);

        while (this.clipperPtr.Step())
        {
            var entryIndex = this.clipperPtr.DisplayStart - this.newRolledLines;
            var entryIndexEnd = this.clipperPtr.DisplayEnd - this.newRolledLines;
            entryIndex = this.BinarySearchFilteredLogEntries(entryIndex + this.totalRolledLines);
            entryIndexEnd = this.BinarySearchFilteredLogEntries(entryIndexEnd + this.totalRolledLines);
            if (entryIndex < 0)
                entryIndex = ~entryIndex - 1;
            if (entryIndexEnd < 0)
                entryIndexEnd = ~entryIndexEnd;
            entryIndex = Math.Clamp(entryIndex, 0, Math.Max(0, this.filteredLogEntries.Count - 1));
            entryIndexEnd = Math.Clamp(entryIndexEnd, 0, this.filteredLogEntries.Count);

            for (var i = entryIndex; i < entryIndexEnd; i++)
            {
                var entry = this.filteredLogEntries[i];
                var pos = new Vector2(
                    0,
                    ((entry.FirstLine - this.totalRolledLines) + this.newRolledLines) * logLineHeight);

                ImGui.SetCursorPos(pos);
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

                ImGui.SetCursorPos(pos + new Vector2(ImGui.GetScrollX(), 0));
                ImGui.Selectable(
                    "###console_null",
                    true,
                    ImGuiSelectableFlags.AllowItemOverlap,
                    childSize with { Y = logLineHeight * entry.NumLines });

                // This must be after ImGui.Selectable, it uses ImGui.IsItem... functions
                this.HandleCopyMode(i, entry);

                ImGui.SetCursorPos(pos);

                ImGui.PopStyleColor(3);

                ImGui.TextUnformatted(entry.TimestampString);
                ImGui.SameLine();

                ImGui.SetCursorPosX(cursorLogLevel);
                ImGui.TextUnformatted(GetTextForLogEventLevel(entry.Level));
                ImGui.SameLine();

                ImGui.SetCursorPosX(cursorLogLine);
                entry.HighlightMatches ??= (this.compiledLogHighlight ?? this.compiledLogFilter)?.Matches(entry.Line);
                this.DrawHighlighted(
                    entry.Line,
                    entry.HighlightMatches,
                    ImGui.GetColorU32(ImGuiCol.Text),
                    ImGui.GetColorU32(ImGuiColors.HealerGreen));
            }
        }

        this.clipperPtr.End();

        ImGui.PopFont();

        ImGui.PopStyleVar();

        if (!this.activeConfiguration.LogAutoScroll || ImGui.GetScrollY() < ImGui.GetScrollMaxY())
        {
            ImGui.SetScrollY(ImGui.GetScrollY() - (logLineHeight * this.newRolledLines));
        }

        if (this.activeConfiguration.LogAutoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
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
        LogEventLevel.Fatal => 0xFF00000A,
        _ => 0x30FFFFFF,
    };

    private void MonoFontOnImFontChanged(IFontHandle fonthandle, ILockedImFont lockedfont) =>
        this.configGeneration++;

    private void FrameworkOnUpdate(IFramework framework)
    {
        if (this.pendingClearLog)
        {
            this.pendingClearLog = false;
            this.logText.Clear();
            this.filteredLogEntries.Clear();
            this.newLogEntries.Clear();
            this.totalWrappedLines = this.totalRolledLines = 0;
        }

        if (this.pendingRefilter)
        {
            this.pendingRefilter = false;
            this.filteredLogEntries.Clear();
            this.totalWrappedLines = this.totalRolledLines = 0;
            foreach (var log in this.logText)
            {
                if (this.IsFilterApplicable(log))
                {
                    log.FirstLine = this.totalWrappedLines;
                    this.totalWrappedLines += log.NumLines;
                    this.filteredLogEntries.Add(log);
                }
            }
        }

        this.newRolledLines = 0;
        while (this.newLogEntries.TryDequeue(out var logLine))
            this.newRolledLines += this.HandleLogLine(logLine.Line, logLine.LogEvent);
        this.totalRolledLines += this.newRolledLines;
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
        var configuration = this.activeConfiguration;

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
            this.newSettings = null;
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
        var boolt = this.StagingConfig.LogOpenAtStartup;
        if (ImGui.Checkbox("Open at startup", ref boolt))
            (this.newSettings ??= this.activeConfiguration with { }).LogOpenAtStartup = boolt;

        boolt = this.StagingConfig.LogAutoScroll;
        if (ImGui.Checkbox("Auto-scroll", ref boolt))
            (this.newSettings ??= this.activeConfiguration with { }).LogAutoScroll = boolt;

        var intt = (int)this.StagingConfig.LogLineBreakMode;
        if (ImGui.Combo("Word Break", ref intt, WordBreakModeNames, WordBreakModeNames.Length))
        {
            (this.newSettings ??= this.activeConfiguration with { }).LogLineBreakMode =
                (WordBreakType)intt;
        }

        ImGui.TextUnformatted("Logs buffer");
        intt = this.StagingConfig.LogLinesLimit;
        if (ImGui.SliderInt("lines", ref intt, LogLinesMinimum, LogLinesMaximum))
            (this.newSettings ??= this.activeConfiguration with { }).LogLinesLimit = intt;

        if (this.newSettings is null)
        {
            ImGui.BeginDisabled();
            ImGui.Button("Apply");
            ImGui.EndDisabled();
        }
        else
        {
            if (ImGui.Button("Apply"))
            {
                configuration.LogOpenAtStartup = this.StagingConfig.LogOpenAtStartup;
                configuration.LogAutoScroll = this.StagingConfig.LogAutoScroll;
                configuration.LogLinesLimit = Math.Clamp(
                    this.StagingConfig.LogLinesLimit,
                    LogLinesMinimum,
                    LogLinesMaximum);
                configuration.LogLineBreakMode = this.StagingConfig.LogLineBreakMode;
                configuration.QueueSave();
                this.newSettings = null;

                ImGui.CloseCurrentPopup();
            }
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
    /// <param name="line">The log entry to add.</param>
    /// <param name="logEvent">The Serilog event associated with this line.</param>
    /// <returns>Sum of number of lines of the entries removed from <see cref="filteredLogEntries"/>.</returns>
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
            Line = line,
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

        ThreadSafety.DebugAssertMainThread();

        this.logText.Add(entry);

        if (!this.IsFilterApplicable(entry))
            return 0;

        var numLinesRemoved =
            this.filteredLogEntries.Size == this.filteredLogEntries.Count
                ? this.filteredLogEntries[0].NumLines
                : 0;
        this.filteredLogEntries.Add(entry);

        return numLinesRemoved;
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
        var limit = Math.Clamp(this.activeConfiguration.LogLinesLimit, LogLinesMinimum, LogLinesMaximum);
        this.logText.Size = limit;
        this.filteredLogEntries.Size = limit;
        this.configGeneration++;
    }

    private int BinarySearchFilteredLogEntries(int lineIndex)
    {
        var l = 0;
        var r = this.filteredLogEntries.Count - 1;
        while (l <= r)
        {
            var middle = l + (r - l) / 2;
            var comparisonResult = lineIndex.CompareTo(this.filteredLogEntries[middle].FirstLine);
            switch (comparisonResult)
            {
                case 0:
                    return middle;
                case < 0:
                    r = middle - 1;
                    break;
                default:
                    l = middle + 1;
                    break;
            }
        }

        return ~l;
    }

    private TextState.Options GetTextStateOptions() => new()
    {
        WordBreak = this.activeConfiguration.LogLineBreakMode,
        InitialStyle = new() { BorderWidth = 1f },
        ControlCharactersStyle = new()
        {
            Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.InconsolataRegular)),
            BackColor = 0xFF333333,
            BorderWidth = 1,
            ForeColor = 0xFFFFFFFF,
            FontSize = ImGui.GetFont().FontSize * 0.6f,
            VerticalAlignment = 0.5f,
        },
        WrapMarker =
            this.activeConfiguration.LogLineBreakMode == WordBreakType.KeepAll
                ? this.ellipsisSpannable
                : this.wrapMarkerSpannable,
    };

    private unsafe void DrawHighlighted(
        ReadOnlySpan<char> line,
        MatchCollection? matches,
        uint col,
        uint highlightCol)
    {
        Span<int> charOffsets = stackalloc int[((matches?.Count ?? 0) * 2) + 2];
        var charOffsetsIndex = 1;
        if (matches is not null)
        {
            for (var j = 0; j < matches.Count; j++)
            {
                var g = matches[j].Groups[0];
                charOffsets[charOffsetsIndex++] = g.Index;
                charOffsets[charOffsetsIndex++] = g.Index + g.Length;
            }
        }

        charOffsets[charOffsetsIndex++] = line.Length;

        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var width = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;

        var ssb = Renderer.RentBuilder();
        for (var i = 0; i < charOffsetsIndex - 1; i++)
        {
            var begin = charOffsets[i];
            var end = charOffsets[i + 1];
            ssb.PushForeColor(i % 2 == 1 ? highlightCol : col)
               .PushItalic(i % 2 == 1)
               .Append(line[begin..end])
               .PopItalic()
               .PopForeColor();
        }

        Renderer.Render(
            ssb,
            new(
                ImGui.GetWindowDrawList(),
                new() { MaxSize = new((width + ImGui.GetScrollX()) - ImGui.GetCursorPosX()) }),
            this.GetTextStateOptions());

        // Allocate scroll region
        if (this.activeConfiguration.LogLineBreakMode == WordBreakType.KeepAll)
        {
            ssb.Clear();
            for (var i = 0; i < charOffsetsIndex - 1; i++)
            {
                var begin = charOffsets[i];
                var end = charOffsets[i + 1];
                ssb.PushItalic(i % 2 == 1)
                   .Append(line[begin..end])
                   .PopItalic();
            }

            ImGui.SetCursorScreenPos(cursorScreenPos);
            Renderer.Render(
                ssb,
                new(false, new() { MaxSize = new(float.MaxValue) }),
                this.GetTextStateOptions());
        }

        Renderer.ReturnBuilder(ssb);
    }

    private record LogEntry
    {
        public string Line { get; set; } = string.Empty;

        public LogEventLevel Level { get; init; }

        public DateTimeOffset TimeStamp { get; init; }

        /// <summary>
        /// Gets or sets the system responsible for generating this log entry. Generally will be a plugin's
        /// InternalName.
        /// </summary>
        public string? Source { get; set; }

        public bool SelectedForCopy { get; set; }

        public bool HasException { get; init; }

        public int FirstLine { get; set; }

        public int NumLines { get; set; }

        public MatchCollection? HighlightMatches { get; set; }

        public string TimestampString => this.TimeStamp.ToString("HH:mm:ss.fff");

        public override string ToString() =>
            $"{this.TimestampString} | {GetTextForLogEventLevel(this.Level)} | {this.Line.Trim().ReplaceLineEndings("\n\t")}";
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

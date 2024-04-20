using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Dalamud.Game;
using Dalamud.Hooking.Internal;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// This window displays plugin statistics for troubleshooting.
/// </summary>
internal class PluginStatWindow : Window
{
    private bool showDalamudHooks;
    private string drawSearchText = string.Empty;
    private string frameworkSearchText = string.Empty;
    private string hookSearchText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginStatWindow"/> class.
    /// </summary>
    public PluginStatWindow()
        : base("Plugin Statistics###DalamudPluginStatWindow")
    {
        this.RespectCloseHotkey = false;

        this.Size = new Vector2(810, 520);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        var pluginManager = Service<PluginManager>.Get();

        if (!ImGui.BeginTabBar("Stat Tabs"))
            return;

        if (ImGui.BeginTabItem("Draw times"))
        {
            var doStats = UiBuilder.DoStats;

            if (ImGui.Checkbox("Enable Draw Time Tracking", ref doStats))
            {
                UiBuilder.DoStats = doStats;
            }

            if (doStats)
            {
                ImGui.SameLine();
                if (ImGui.Button("Reset"))
                {
                    foreach (var plugin in pluginManager.InstalledPlugins)
                    {
                        if (plugin.DalamudInterface != null)
                        {
                            plugin.DalamudInterface.UiBuilder.LastDrawTime = -1;
                            plugin.DalamudInterface.UiBuilder.MaxDrawTime = -1;
                            plugin.DalamudInterface.UiBuilder.DrawTimeHistory.Clear();
                        }
                    }
                }

                var loadedPlugins = pluginManager.InstalledPlugins.Where(plugin => plugin.State == PluginState.Loaded);
                var totalLast = loadedPlugins.Sum(plugin => plugin.DalamudInterface?.UiBuilder.LastDrawTime ?? 0);
                var totalAverage = loadedPlugins.Sum(plugin => plugin.DalamudInterface?.UiBuilder.DrawTimeHistory.DefaultIfEmpty().Average() ?? 0);

                ImGuiComponents.TextWithLabel("Total Last", $"{totalLast / 10000f:F4}ms", "All last draw times added together");
                ImGui.SameLine();
                ImGuiComponents.TextWithLabel("Total Average", $"{totalAverage / 10000f:F4}ms", "All average draw times added together");
                ImGui.SameLine();
                ImGuiComponents.TextWithLabel("Collective Average", $"{(loadedPlugins.Any() ? totalAverage / loadedPlugins.Count() / 10000f : 0):F4}ms", "Average of all average draw times");

                ImGui.InputTextWithHint(
                    "###PluginStatWindow_DrawSearch",
                    "Search",
                    ref this.drawSearchText,
                    500);

                if (ImGui.BeginTable(
                        "##PluginStatsDrawTimes",
                        4,
                        ImGuiTableFlags.RowBg
                        | ImGuiTableFlags.SizingStretchProp
                        | ImGuiTableFlags.Sortable
                        | ImGuiTableFlags.Resizable
                        | ImGuiTableFlags.ScrollY
                        | ImGuiTableFlags.Reorderable
                        | ImGuiTableFlags.Hideable))
                {
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableSetupColumn("Plugin");
                    ImGui.TableSetupColumn("Last", ImGuiTableColumnFlags.NoSort); // Changes too fast to sort
                    ImGui.TableSetupColumn("Longest");
                    ImGui.TableSetupColumn("Average");
                    ImGui.TableHeadersRow();

                    var sortSpecs = ImGui.TableGetSortSpecs();
                    loadedPlugins = sortSpecs.Specs.ColumnIndex switch
                    {
                        0 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                                 ? loadedPlugins.OrderBy(plugin => plugin.Name)
                                 : loadedPlugins.OrderByDescending(plugin => plugin.Name),
                        2 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                                 ? loadedPlugins.OrderBy(plugin => plugin.DalamudInterface?.UiBuilder.MaxDrawTime ?? 0)
                                 : loadedPlugins.OrderByDescending(plugin => plugin.DalamudInterface?.UiBuilder.MaxDrawTime ?? 0),
                        3 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                                 ? loadedPlugins.OrderBy(plugin => plugin.DalamudInterface?.UiBuilder.DrawTimeHistory.DefaultIfEmpty().Average() ?? 0)
                                 : loadedPlugins.OrderByDescending(plugin => plugin.DalamudInterface?.UiBuilder.DrawTimeHistory.DefaultIfEmpty().Average() ?? 0),
                        _ => loadedPlugins,
                    };

                    foreach (var plugin in loadedPlugins)
                    {
                        if (!this.drawSearchText.IsNullOrEmpty()
                            && !plugin.Manifest.Name.Contains(this.drawSearchText, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text(plugin.Manifest.Name);

                        if (plugin.DalamudInterface != null)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text($"{plugin.DalamudInterface.UiBuilder.LastDrawTime / 10000f:F4}ms");

                            ImGui.TableNextColumn();
                            ImGui.Text($"{plugin.DalamudInterface.UiBuilder.MaxDrawTime / 10000f:F4}ms");

                            ImGui.TableNextColumn();
                            ImGui.Text(plugin.DalamudInterface.UiBuilder.DrawTimeHistory.Count > 0
                                           ? $"{plugin.DalamudInterface.UiBuilder.DrawTimeHistory.Average() / 10000f:F4}ms"
                                           : "-");
                        }
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Framework times"))
        {
            var doStats = Framework.StatsEnabled;

            if (ImGui.Checkbox("Enable Framework Update Tracking", ref doStats))
            {
                Framework.StatsEnabled = doStats;
            }

            if (doStats)
            {
                ImGui.SameLine();
                if (ImGui.Button("Reset"))
                {
                    Framework.StatsHistory.Clear();
                }

                var statsHistory = Framework.StatsHistory.ToArray();
                var totalLast = statsHistory.Sum(stats => stats.Value.LastOrDefault());
                var totalAverage = statsHistory.Sum(stats => stats.Value.DefaultIfEmpty().Average());

                ImGuiComponents.TextWithLabel("Total Last", $"{totalLast:F4}ms", "All last update times added together");
                ImGui.SameLine();
                ImGuiComponents.TextWithLabel("Total Average", $"{totalAverage:F4}ms", "All average update times added together");
                ImGui.SameLine();
                ImGuiComponents.TextWithLabel("Collective Average", $"{(statsHistory.Any() ? totalAverage / statsHistory.Length : 0):F4}ms", "Average of all average update times");

                ImGui.InputTextWithHint(
                    "###PluginStatWindow_FrameworkSearch",
                    "Search",
                    ref this.frameworkSearchText,
                    500);

                if (ImGui.BeginTable(
                        "##PluginStatsFrameworkTimes",
                        4,
                        ImGuiTableFlags.RowBg
                        | ImGuiTableFlags.SizingStretchProp
                        | ImGuiTableFlags.Sortable
                        | ImGuiTableFlags.Resizable
                        | ImGuiTableFlags.ScrollY
                        | ImGuiTableFlags.Reorderable
                        | ImGuiTableFlags.Hideable))
                {
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableSetupColumn("Method", ImGuiTableColumnFlags.None, 250);
                    ImGui.TableSetupColumn("Last", ImGuiTableColumnFlags.NoSort, 50); // Changes too fast to sort
                    ImGui.TableSetupColumn("Longest", ImGuiTableColumnFlags.None, 50);
                    ImGui.TableSetupColumn("Average", ImGuiTableColumnFlags.None, 50);
                    ImGui.TableHeadersRow();

                    var sortSpecs = ImGui.TableGetSortSpecs();
                    statsHistory = sortSpecs.Specs.ColumnIndex switch
                    {
                        0 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                                 ? statsHistory.OrderBy(handler => handler.Key).ToArray()
                                 : statsHistory.OrderByDescending(handler => handler.Key).ToArray(),
                        2 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                                 ? statsHistory.OrderBy(handler => handler.Value.DefaultIfEmpty().Max()).ToArray()
                                 : statsHistory.OrderByDescending(handler => handler.Value.DefaultIfEmpty().Max()).ToArray(),
                        3 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                                 ? statsHistory.OrderBy(handler => handler.Value.DefaultIfEmpty().Average()).ToArray()
                                 : statsHistory.OrderByDescending(handler => handler.Value.DefaultIfEmpty().Average()).ToArray(),
                        _ => statsHistory,
                    };

                    foreach (var handlerHistory in statsHistory)
                    {
                        if (!handlerHistory.Value.Any())
                        {
                            continue;
                        }

                        if (!this.frameworkSearchText.IsNullOrEmpty()
                            && handlerHistory.Key != null
                            && !handlerHistory.Key.Contains(this.frameworkSearchText, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text($"{handlerHistory.Key}");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{handlerHistory.Value.Last():F4}ms");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{handlerHistory.Value.Max():F4}ms");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{handlerHistory.Value.Average():F4}ms");
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.EndTabItem();
        }

        var toRemove = new List<Guid>();

        if (ImGui.BeginTabItem("Hooks"))
        {
            ImGui.Checkbox("Show Dalamud Hooks", ref this.showDalamudHooks);

            ImGui.InputTextWithHint(
                "###PluginStatWindow_HookSearch",
                "Search",
                ref this.hookSearchText,
                500);

            if (ImGui.BeginTable(
                    "##PluginStatsHooks",
                    4,
                    ImGuiTableFlags.RowBg
                    | ImGuiTableFlags.SizingStretchProp
                    | ImGuiTableFlags.Resizable
                    | ImGuiTableFlags.ScrollY
                    | ImGuiTableFlags.Reorderable
                    | ImGuiTableFlags.Hideable))
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("Detour Method", ImGuiTableColumnFlags.None, 250);
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.None, 100);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.None, 40);
                ImGui.TableSetupColumn("Backend", ImGuiTableColumnFlags.None, 40);
                ImGui.TableHeadersRow();

                foreach (var (guid, trackedHook) in HookManager.TrackedHooks)
                {
                    try
                    {
                        if (trackedHook.Hook.IsDisposed)
                            toRemove.Add(guid);

                        if (!this.showDalamudHooks && trackedHook.Assembly == Assembly.GetExecutingAssembly())
                            continue;

                        if (!this.hookSearchText.IsNullOrEmpty())
                        {
                            if ((trackedHook.Delegate.Target == null || !trackedHook.Delegate.Target.ToString().Contains(this.hookSearchText, StringComparison.OrdinalIgnoreCase))
                                && !trackedHook.Delegate.Method.Name.Contains(this.hookSearchText, StringComparison.OrdinalIgnoreCase))
                                continue;
                        }

                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();

                        ImGui.Text($"{trackedHook.Delegate.Target} :: {trackedHook.Delegate.Method.Name}");
                        ImGui.TextDisabled(trackedHook.Assembly.FullName);
                        ImGui.TableNextColumn();
                        if (!trackedHook.Hook.IsDisposed)
                        {
                            if (ImGui.Selectable($"{trackedHook.Hook.Address.ToInt64():X}"))
                            {
                                ImGui.SetClipboardText($"{trackedHook.Hook.Address.ToInt64():X}");
                                Service<NotificationManager>.Get().AddNotification($"{trackedHook.Hook.Address.ToInt64():X}", "Copied to clipboard", NotificationType.Success);
                            }

                            var processMemoryOffset = trackedHook.InProcessMemory;
                            if (processMemoryOffset.HasValue)
                            {
                                if (ImGui.Selectable($"ffxiv_dx11.exe+{processMemoryOffset:X}"))
                                {
                                    ImGui.SetClipboardText($"ffxiv_dx11.exe+{processMemoryOffset:X}");
                                    Service<NotificationManager>.Get().AddNotification($"ffxiv_dx11.exe+{processMemoryOffset:X}", "Copied to clipboard", NotificationType.Success);
                                }
                            }
                        }

                        ImGui.TableNextColumn();

                        if (trackedHook.Hook.IsDisposed)
                        {
                            ImGui.Text("Disposed");
                        }
                        else
                        {
                            ImGui.Text(trackedHook.Hook.IsEnabled ? "Enabled" : "Disabled");
                        }

                        ImGui.TableNextColumn();

                        ImGui.Text(trackedHook.Hook.BackendName);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error drawing hooks in plugin stats");
                    }
                }

                ImGui.EndTable();
            }
        }

        if (ImGui.IsWindowAppearing())
        {
            foreach (var guid in toRemove)
            {
                HookManager.TrackedHooks.TryRemove(guid, out _);
            }
        }

        ImGui.EndTabBar();
    }
}

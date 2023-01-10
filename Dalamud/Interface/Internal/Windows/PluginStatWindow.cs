using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Dalamud.Game;
using Dalamud.Hooking.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// This window displays plugin statistics for troubleshooting.
/// </summary>
internal class PluginStatWindow : Window
{
    private bool showDalamudHooks;

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

        ImGui.BeginTabBar("Stat Tabs");

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

                    var loadedPlugins = pluginManager.InstalledPlugins.Where(plugin => plugin.State == PluginState.Loaded);

                    var sortSpecs = ImGui.TableGetSortSpecs();
                    loadedPlugins = sortSpecs.Specs.ColumnIndex switch
                    {
                        0 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                                 ? loadedPlugins.OrderBy(plugin => plugin.Name)
                                 : loadedPlugins.OrderByDescending(plugin => plugin.Name),
                        2 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                                 ? loadedPlugins.OrderBy(plugin => plugin.DalamudInterface?.UiBuilder.MaxDrawTime)
                                 : loadedPlugins.OrderByDescending(plugin => plugin.DalamudInterface?.UiBuilder.MaxDrawTime),
                        3 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                                 ? loadedPlugins.OrderBy(plugin => plugin.DalamudInterface?.UiBuilder.DrawTimeHistory.Average())
                                 : loadedPlugins.OrderByDescending(plugin => plugin.DalamudInterface?.UiBuilder.DrawTimeHistory.Average()),
                        _ => loadedPlugins,
                    };

                    foreach (var plugin in loadedPlugins)
                    {
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

                    var statsHistory = Framework.StatsHistory.ToArray();

                    var sortSpecs = ImGui.TableGetSortSpecs();
                    statsHistory = sortSpecs.Specs.ColumnIndex switch
                    {
                        0 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                                 ? statsHistory.OrderBy(handler => handler.Key).ToArray()
                                 : statsHistory.OrderByDescending(handler => handler.Key).ToArray(),
                        2 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                                 ? statsHistory.OrderBy(handler => handler.Value.Max()).ToArray()
                                 : statsHistory.OrderByDescending(handler => handler.Value.Max()).ToArray(),
                        3 => sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending
                                 ? statsHistory.OrderBy(handler => handler.Value.Average()).ToArray()
                                 : statsHistory.OrderByDescending(handler => handler.Value.Average()).ToArray(),
                        _ => statsHistory,
                    };

                    foreach (var handlerHistory in statsHistory)
                    {
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
                        ImGui.Text(ex.Message);
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

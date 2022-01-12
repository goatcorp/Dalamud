using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Dalamud.Game;
using Dalamud.Game.Internal;
using Dalamud.Hooking;
using Dalamud.Hooking.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows
{
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
                            plugin.DalamudInterface.UiBuilder.LastDrawTime = -1;
                            plugin.DalamudInterface.UiBuilder.MaxDrawTime = -1;
                            plugin.DalamudInterface.UiBuilder.DrawTimeHistory.Clear();
                        }
                    }

                    ImGui.Columns(4);
                    ImGui.SetColumnWidth(0, 180f);
                    ImGui.SetColumnWidth(1, 100f);
                    ImGui.SetColumnWidth(2, 100f);
                    ImGui.SetColumnWidth(3, 100f);

                    ImGui.Text("Plugin");
                    ImGui.NextColumn();

                    ImGui.Text("Last");
                    ImGui.NextColumn();

                    ImGui.Text("Longest");
                    ImGui.NextColumn();

                    ImGui.Text("Average");
                    ImGui.NextColumn();

                    ImGui.Separator();

                    foreach (var plugin in pluginManager.InstalledPlugins.Where(plugin => plugin.State == PluginState.Loaded))
                    {
                        ImGui.Text(plugin.Manifest.Name);
                        ImGui.NextColumn();

                        ImGui.Text($"{plugin.DalamudInterface.UiBuilder.LastDrawTime / 10000f:F4}ms");
                        ImGui.NextColumn();

                        ImGui.Text($"{plugin.DalamudInterface.UiBuilder.MaxDrawTime / 10000f:F4}ms");
                        ImGui.NextColumn();

                        if (plugin.DalamudInterface.UiBuilder.DrawTimeHistory.Count > 0)
                        {
                            ImGui.Text($"{plugin.DalamudInterface.UiBuilder.DrawTimeHistory.Average() / 10000f:F4}ms");
                        }
                        else
                        {
                            ImGui.Text("-");
                        }

                        ImGui.NextColumn();
                    }

                    ImGui.Columns(1);
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

                    ImGui.Columns(4);

                    ImGui.SetColumnWidth(0, ImGui.GetWindowContentRegionWidth() - 300);
                    ImGui.SetColumnWidth(1, 100f);
                    ImGui.SetColumnWidth(2, 100f);
                    ImGui.SetColumnWidth(3, 100f);

                    ImGui.Text("Method");
                    ImGui.NextColumn();

                    ImGui.Text("Last");
                    ImGui.NextColumn();

                    ImGui.Text("Longest");
                    ImGui.NextColumn();

                    ImGui.Text("Average");
                    ImGui.NextColumn();

                    ImGui.Separator();
                    ImGui.Separator();

                    foreach (var handlerHistory in Framework.StatsHistory)
                    {
                        if (handlerHistory.Value.Count == 0)
                            continue;

                        ImGui.SameLine();

                        ImGui.Text($"{handlerHistory.Key}");
                        ImGui.NextColumn();

                        ImGui.Text($"{handlerHistory.Value.Last():F4}ms");
                        ImGui.NextColumn();

                        ImGui.Text($"{handlerHistory.Value.Max():F4}ms");
                        ImGui.NextColumn();

                        ImGui.Text($"{handlerHistory.Value.Average():F4}ms");
                        ImGui.NextColumn();

                        ImGui.Separator();
                    }

                    ImGui.Columns(0);
                }

                ImGui.EndTabItem();
            }

            var toRemove = new List<Guid>();

            if (ImGui.BeginTabItem("Hooks"))
            {
                ImGui.Columns(4);

                ImGui.SetColumnWidth(0, ImGui.GetWindowContentRegionWidth() - 330);
                ImGui.SetColumnWidth(1, 180f);
                ImGui.SetColumnWidth(2, 100f);
                ImGui.SetColumnWidth(3, 100f);

                ImGui.Text("Detour Method");
                ImGui.SameLine();

                ImGui.Text("   ");
                ImGui.SameLine();

                ImGui.Checkbox("Show Dalamud Hooks ###showDalamudHooksCheckbox", ref this.showDalamudHooks);
                ImGui.NextColumn();

                ImGui.Text("Address");
                ImGui.NextColumn();

                ImGui.Text("Status");
                ImGui.NextColumn();

                ImGui.Text("Backend");
                ImGui.NextColumn();

                ImGui.Separator();
                ImGui.Separator();

                foreach (var (guid, trackedHook) in HookManager.TrackedHooks)
                {
                    try
                    {
                        if (trackedHook.Hook.IsDisposed)
                            toRemove.Add(guid);

                        if (!this.showDalamudHooks && trackedHook.Assembly == Assembly.GetExecutingAssembly())
                            continue;

                        ImGui.Text($"{trackedHook.Delegate.Target} :: {trackedHook.Delegate.Method.Name}");
                        ImGui.TextDisabled(trackedHook.Assembly.FullName);
                        ImGui.NextColumn();
                        if (!trackedHook.Hook.IsDisposed)
                        {
                            ImGui.Text($"{trackedHook.Hook.Address.ToInt64():X}");
                            if (ImGui.IsItemClicked())
                            {
                                ImGui.SetClipboardText($"{trackedHook.Hook.Address.ToInt64():X}");
                            }

                            var processMemoryOffset = trackedHook.InProcessMemory;
                            if (processMemoryOffset.HasValue)
                            {
                                ImGui.Text($"ffxiv_dx11.exe + {processMemoryOffset:X}");
                                if (ImGui.IsItemClicked())
                                {
                                    ImGui.SetClipboardText($"ffxiv_dx11.exe+{processMemoryOffset:X}");
                                }
                            }
                        }

                        ImGui.NextColumn();

                        if (trackedHook.Hook.IsDisposed)
                        {
                            ImGui.Text("Disposed");
                        }
                        else
                        {
                            ImGui.Text(trackedHook.Hook.IsEnabled ? "Enabled" : "Disabled");
                        }

                        ImGui.NextColumn();

                        ImGui.Text(trackedHook.Hook.BackendName);

                        ImGui.NextColumn();
                    }
                    catch (Exception ex)
                    {
                        ImGui.Text(ex.Message);
                        ImGui.NextColumn();
                        while (ImGui.GetColumnIndex() != 0) ImGui.NextColumn();
                    }

                    ImGui.Separator();
                }

                ImGui.Columns();
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
}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using CheapLoc;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ImGuiNET;
using Serilog;
using Serilog.Events;

namespace Dalamud.Interface
{
    /// <summary>
    /// Class handling Dalamud core interface.
    /// </summary>
    internal class DalamudInterface : IDisposable
    {
        private readonly Dalamud dalamud;

        private readonly DalamudLogWindow logWindow;
        private readonly DalamudDataWindow dataWindow;
        private readonly DalamudCreditsWindow creditsWindow;
        private readonly DalamudSettingsWindow settingsWindow;
        private readonly PluginInstallerWindow pluginWindow;
        private readonly DalamudPluginStatWindow pluginStatWindow;
        private readonly DalamudChangelogWindow changelogWindow;
        private readonly ComponentDemoWindow componentDemoWindow;
        private readonly ColorDemoWindow colorDemoWindow;

        private readonly WindowSystem windowSystem = new WindowSystem("DalamudCore");

        private ulong frameCount = 0;

        private bool isImguiDrawDemoWindow = false;

#if DEBUG
        private bool isImguiDrawDevMenu = true;
#else
        private bool isImguiDrawDevMenu = false;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="DalamudInterface"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance to register to.</param>
        public DalamudInterface(Dalamud dalamud)
        {
            this.dalamud = dalamud;

            this.logWindow = new DalamudLogWindow(this.dalamud.CommandManager, this.dalamud.Configuration)
            {
                IsOpen = false,
            };
            this.windowSystem.AddWindow(this.logWindow);

            this.dataWindow = new DalamudDataWindow(this.dalamud)
            {
                IsOpen = false,
            };
            this.windowSystem.AddWindow(this.dataWindow);

            this.creditsWindow = new DalamudCreditsWindow(this.dalamud)
            {
                IsOpen = false,
            };
            this.windowSystem.AddWindow(this.creditsWindow);

            this.settingsWindow = new DalamudSettingsWindow(this.dalamud)
            {
                IsOpen = false,
            };
            this.windowSystem.AddWindow(this.settingsWindow);

            this.pluginWindow = new PluginInstallerWindow(this.dalamud, this.dalamud.StartInfo.GameVersion)
            {
                IsOpen = false,
            };
            this.windowSystem.AddWindow(this.pluginWindow);

            this.pluginStatWindow = new DalamudPluginStatWindow(this.dalamud.PluginManager)
            {
                IsOpen = false,
            };
            this.windowSystem.AddWindow(this.pluginStatWindow);

            this.changelogWindow = new DalamudChangelogWindow(this.dalamud)
            {
                IsOpen = false,
            };
            this.windowSystem.AddWindow(this.changelogWindow);

            this.componentDemoWindow = new ComponentDemoWindow()
            {
                IsOpen = false,
            };
            this.windowSystem.AddWindow(this.componentDemoWindow);

            this.colorDemoWindow = new ColorDemoWindow()
            {
                IsOpen = false,
            };
            this.windowSystem.AddWindow(this.colorDemoWindow);

            Log.Information("[DUI] Windows added");

            if (dalamud.Configuration.LogOpenAtStartup)
                this.OpenLog();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Dalamud dev menu is drawing.
        /// </summary>
        public bool IsDevMenu
        {
            get => this.isImguiDrawDevMenu;
            set => this.isImguiDrawDevMenu = value;
        }

        /// <summary>
        /// Draw the Dalamud core interface via ImGui.
        /// </summary>
        public void Draw()
        {
            this.frameCount++;

            if (!this.IsDevMenu && !this.dalamud.ClientState.Condition.Any())
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, 0, 0, 0));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0, 0, 0, 0));
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 1));
                ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, new Vector4(0, 0, 0, 1));
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 1));
                ImGui.PushStyleColor(ImGuiCol.BorderShadow, new Vector4(0, 0, 0, 1));
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 1));

                var mainViewportPos = ImGui.GetMainViewport().Pos;
                ImGui.SetNextWindowPos(new Vector2(mainViewportPos.X, mainViewportPos.Y), ImGuiCond.Always);
                ImGui.SetNextWindowBgAlpha(1);

                if (ImGui.Begin("DevMenu Opener", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings))
                {
                    if (ImGui.Button("###devMenuOpener", new Vector2(40, 25)))
                        this.IsDevMenu = true;

                    ImGui.End();
                }

                ImGui.PopStyleColor(8);
            }

            if (this.IsDevMenu)
            {
                if (ImGui.BeginMainMenuBar())
                {
                    if (ImGui.BeginMenu("Dalamud"))
                    {
                        ImGui.MenuItem("Draw Dalamud dev menu", string.Empty, ref this.isImguiDrawDevMenu);

                        ImGui.Separator();

                        if (ImGui.MenuItem("Open Log window"))
                        {
                            this.OpenLog();
                        }

                        if (ImGui.BeginMenu("Set log level..."))
                        {
                            foreach (var logLevel in Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>())
                            {
                                if (ImGui.MenuItem(logLevel + "##logLevelSwitch", string.Empty, this.dalamud.LogLevelSwitch.MinimumLevel == logLevel))
                                {
                                    this.dalamud.LogLevelSwitch.MinimumLevel = logLevel;
                                }
                            }

                            ImGui.EndMenu();
                        }

                        if (ImGui.MenuItem("Enable AntiDebug", null, this.dalamud.AntiDebug.IsEnabled))
                        {
                            this.dalamud.AntiDebug.Enable();
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Open Data window"))
                        {
                            this.OpenData();
                        }

                        if (ImGui.MenuItem("Open Credits window"))
                        {
                            this.OpenCredits();
                        }

                        if (ImGui.MenuItem("Open Settings window"))
                        {
                            this.OpenSettings();
                        }

                        if (ImGui.MenuItem("Open Changelog window"))
                        {
                            this.OpenChangelog();
                        }

                        if (ImGui.MenuItem("Open Components Demo"))
                        {
                            this.OpenComponentDemo();
                        }

                        if (ImGui.MenuItem("Open Colors Demo"))
                        {
                            this.OpenColorsDemo();
                        }

                        ImGui.MenuItem("Draw ImGui demo", string.Empty, ref this.isImguiDrawDemoWindow);

                        ImGui.Separator();

                        if (ImGui.MenuItem("Unload Dalamud"))
                        {
                            this.dalamud.Unload();
                        }

                        if (ImGui.MenuItem("Kill game"))
                        {
                            Process.GetCurrentProcess().Kill();
                        }

                        if (ImGui.MenuItem("Cause AccessViolation"))
                        {
                            var a = Marshal.ReadByte(IntPtr.Zero);
                        }

                        ImGui.Separator();
                        if (ImGui.MenuItem("Enable Dalamud testing", string.Empty, this.dalamud.Configuration.DoDalamudTest))
                        {
                            this.dalamud.Configuration.DoDalamudTest = !this.dalamud.Configuration.DoDalamudTest;
                            this.dalamud.Configuration.Save();
                        }

                        ImGui.MenuItem(Util.AssemblyVersion, false);
                        ImGui.MenuItem(this.dalamud.StartInfo.GameVersion, false);

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Game"))
                    {
                        if (ImGui.MenuItem("Replace ExceptionHandler"))
                        {
                            this.dalamud.ReplaceExceptionHandler();
                        }

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Plugins"))
                    {
                        if (ImGui.MenuItem("Open Plugin installer"))
                        {
                            this.OpenPluginInstaller();
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Open Plugin Stats"))
                        {
                            this.OpenPluginStats();
                        }

                        if (ImGui.MenuItem("Print plugin info"))
                        {
                            foreach (var plugin in this.dalamud.PluginManager.Plugins)
                            {
                                // TODO: some more here, state maybe?
                                Log.Information($"{plugin.Plugin.Name}");
                            }
                        }

                        if (ImGui.MenuItem("Reload plugins"))
                        {
                            try
                            {
                                this.dalamud.PluginManager.ReloadPlugins();
                            }
                            catch (Exception ex)
                            {
                                this.dalamud.Framework.Gui.Chat.PrintError("Reload failed.");
                                Log.Error(ex, "Plugin reload failed.");
                            }
                        }

                        ImGui.Separator();
                        ImGui.MenuItem("API Level:" + PluginManager.DalamudApiLevel, false);
                        ImGui.MenuItem("Loaded plugins:" + this.dalamud.PluginManager?.Plugins.Count, false);
                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Localization"))
                    {
                        if (ImGui.MenuItem("Export localizable"))
                        {
                            this.dalamud.LocalizationManager.ExportLocalizable();
                        }

                        if (ImGui.BeginMenu("Load language..."))
                        {
                            if (ImGui.MenuItem("From Fallbacks"))
                            {
                                this.dalamud.LocalizationManager.SetupWithFallbacks();
                            }

                            if (ImGui.MenuItem("From UICulture"))
                            {
                                this.dalamud.LocalizationManager.SetupWithUiCulture();
                            }

                            foreach (var applicableLangCode in Localization.ApplicableLangCodes)
                            {
                                if (ImGui.MenuItem($"Applicable: {applicableLangCode}"))
                                {
                                    this.dalamud.LocalizationManager.SetupWithLangCode(applicableLangCode);
                                }
                            }

                            ImGui.EndMenu();
                        }

                        ImGui.EndMenu();
                    }

                    if (this.dalamud.Framework.Gui.GameUiHidden)
                        ImGui.BeginMenu("UI is hidden...", false);

                    ImGui.BeginMenu(Util.GetGitHash(), false);
                    ImGui.BeginMenu(this.frameCount.ToString(), false);
                    ImGui.BeginMenu(ImGui.GetIO().Framerate.ToString("F2"), false);

                    ImGui.EndMainMenuBar();
                }
            }

            if (this.dalamud.Framework.Gui.GameUiHidden)
                return;

            this.windowSystem.Draw();

            if (this.isImguiDrawDemoWindow)
                ImGui.ShowDemoWindow();
        }

        /// <summary>
        /// Dispose the window system and all windows that require it.
        /// </summary>
        public void Dispose()
        {
            this.windowSystem.RemoveAllWindows();

            this.logWindow?.Dispose();
            this.creditsWindow?.Dispose();
        }

        /// <summary>
        /// Open the Plugin Installer window.
        /// </summary>
        internal void OpenPluginInstaller()
        {
            this.pluginWindow.IsOpen = true;
        }

        /// <summary>
        /// Open the changelog window.
        /// </summary>
        internal void OpenChangelog()
        {
            this.changelogWindow.IsOpen = true;
        }

        /// <summary>
        /// Open the settings window.
        /// </summary>
        internal void OpenSettings()
        {
            this.settingsWindow.IsOpen = true;
        }

        /// <summary>
        /// Open the log window.
        /// </summary>
        internal void OpenLog()
        {
            this.logWindow.IsOpen = true;
        }

        /// <summary>
        /// Open the data window.
        /// </summary>
        internal void OpenData()
        {
            this.dataWindow.IsOpen = true;
        }

        /// <summary>
        /// Open the credits window.
        /// </summary>
        internal void OpenCredits()
        {
            this.creditsWindow.IsOpen = true;
        }

        /// <summary>
        /// Open the stats window.
        /// </summary>
        internal void OpenPluginStats()
        {
            this.pluginStatWindow.IsOpen = true;
        }

        /// <summary>
        /// Open the component test window.
        /// </summary>
        internal void OpenComponentDemo()
        {
            this.componentDemoWindow.IsOpen = true;
        }

        /// <summary>
        /// Open the colors test window.
        /// </summary>
        internal void OpenColorsDemo()
        {
            this.colorDemoWindow.IsOpen = true;
        }

        /// <summary>
        /// Toggle the Plugin Installer window.
        /// </summary>
        internal void TogglePluginInstaller()
        {
            this.pluginWindow.IsOpen ^= true;
        }

        /// <summary>
        /// Toggle the changelog window.
        /// </summary>
        internal void ToggleChangelog()
        {
            this.changelogWindow.IsOpen ^= true;
        }

        /// <summary>
        /// Toggle the settings window.
        /// </summary>
        internal void ToggleSettings()
        {
            this.settingsWindow.IsOpen ^= true;
        }

        /// <summary>
        /// Toggle the log window.
        /// </summary>
        internal void ToggleLog()
        {
            this.logWindow.IsOpen ^= true;
        }

        /// <summary>
        /// Toggle the data window.
        /// </summary>
        internal void ToggleData()
        {
            this.dataWindow.IsOpen ^= true;
        }

        /// <summary>
        /// Toggle the data window and preset the dropdown.
        /// </summary>
        internal void ToggleData(string dataKind)
        {
            this.dataWindow.IsOpen ^= true;
            if (this.dataWindow.IsOpen)
                this.dataWindow.SetDataKind(dataKind);
        }

        /// <summary>
        /// Toggle the credits window.
        /// </summary>
        internal void ToggleCredits()
        {
            this.creditsWindow.IsOpen ^= true;
        }

        /// <summary>
        /// Toggle the stats window.
        /// </summary>
        internal void TogglePluginStats()
        {
            this.pluginStatWindow.IsOpen ^= true;
        }

        /// <summary>
        /// Toggle the component test window.
        /// </summary>
        internal void ToggleComponentDemo()
        {
            this.componentDemoWindow.IsOpen ^= true;
        }
    }
}

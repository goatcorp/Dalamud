using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using CheapLoc;
using Dalamud.Plugin;
using ImGuiNET;
using Serilog;
using Serilog.Events;

namespace Dalamud.Interface
{
    /// <summary>
    /// Class handling Dalamud core interface.
    /// </summary>
    internal class DalamudInterface
    {
        private readonly Dalamud dalamud;

        private ulong frameCount = 0;

        private bool isImguiDrawDemoWindow = false;

#if DEBUG
        private bool isImguiDrawDevMenu = true;
#else
        private bool isImguiDrawDevMenu = false;
#endif

        private bool isImguiDrawLogWindow = false;
        private bool isImguiDrawDataWindow = false;
        private bool isImguiDrawPluginWindow = false;
        private bool isImguiDrawCreditsWindow = false;
        private bool isImguiDrawSettingsWindow = false;
        private bool isImguiDrawPluginStatWindow = false;
        private bool isImguiDrawChangelogWindow = false;

        private DalamudLogWindow logWindow;
        private DalamudDataWindow dataWindow;
        private DalamudCreditsWindow creditsWindow;
        private DalamudSettingsWindow settingsWindow;
        private PluginInstallerWindow pluginWindow;
        private DalamudPluginStatWindow pluginStatWindow;
        private DalamudChangelogWindow changelogWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="DalamudInterface"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance to register to.</param>
        public DalamudInterface(Dalamud dalamud)
        {
            this.dalamud = dalamud;
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
                            this.logWindow = new DalamudLogWindow(this.dalamud.CommandManager, this.dalamud.Configuration);
                            this.isImguiDrawLogWindow = true;
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
                            this.dataWindow = new DalamudDataWindow(this.dalamud);
                            this.isImguiDrawDataWindow = true;
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
                            this.pluginWindow = new PluginInstallerWindow(this.dalamud, this.dalamud.StartInfo.GameVersion);
                            this.isImguiDrawPluginWindow = true;
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Open Plugin Stats"))
                        {
                            if (!this.isImguiDrawPluginStatWindow)
                            {
                                this.pluginStatWindow = new DalamudPluginStatWindow(this.dalamud.PluginManager);
                                this.isImguiDrawPluginStatWindow = true;
                            }
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
                            Loc.ExportLocalizable();
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

                    ImGui.BeginMenu(this.frameCount.ToString(), false);
                    ImGui.BeginMenu(ImGui.GetIO().Framerate.ToString("F2"), false);

                    ImGui.EndMainMenuBar();
                }
            }

            if (this.dalamud.Framework.Gui.GameUiHidden)
                return;

            if (this.isImguiDrawLogWindow)
            {
                this.isImguiDrawLogWindow = this.logWindow != null && this.logWindow.Draw();

                if (this.isImguiDrawLogWindow == false)
                {
                    this.logWindow?.Dispose();
                    this.logWindow = null;
                }
            }

            if (this.isImguiDrawDataWindow)
            {
                this.isImguiDrawDataWindow = this.dataWindow != null && this.dataWindow.Draw();
            }

            if (this.isImguiDrawPluginWindow)
            {
                this.isImguiDrawPluginWindow = this.pluginWindow != null && this.pluginWindow.Draw();

                if (!this.isImguiDrawPluginWindow)
                    this.pluginWindow = null;
            }

            if (this.isImguiDrawCreditsWindow)
            {
                this.isImguiDrawCreditsWindow = this.creditsWindow != null && this.creditsWindow.Draw();

                if (this.isImguiDrawCreditsWindow == false)
                {
                    this.creditsWindow?.Dispose();
                    this.creditsWindow = null;
                }
            }

            if (this.isImguiDrawSettingsWindow)
            {
                this.isImguiDrawSettingsWindow = this.settingsWindow != null && this.settingsWindow.Draw();
            }

            if (this.isImguiDrawDemoWindow)
                ImGui.ShowDemoWindow();

            if (this.isImguiDrawPluginStatWindow)
            {
                this.isImguiDrawPluginStatWindow = this.pluginStatWindow != null && this.pluginStatWindow.Draw();
                if (!this.isImguiDrawPluginStatWindow)
                {
                    this.pluginStatWindow?.Dispose();
                    this.pluginStatWindow = null;
                }
            }

            if (this.isImguiDrawChangelogWindow)
            {
                this.isImguiDrawChangelogWindow = this.changelogWindow != null && this.changelogWindow.Draw();
            }
        }

        /// <summary>
        /// Open the Plugin Installer window.
        /// </summary>
        internal void OpenPluginInstaller()
        {
            this.pluginWindow ??= new PluginInstallerWindow(this.dalamud, this.dalamud.StartInfo.GameVersion);
            this.isImguiDrawPluginWindow ^= true;
        }

        /// <summary>
        /// Open the changelog window.
        /// </summary>
        internal void OpenChangelog()
        {
            this.changelogWindow = new DalamudChangelogWindow(this.dalamud);
            this.isImguiDrawChangelogWindow = true;
        }

        /// <summary>
        /// Open the settings window.
        /// </summary>
        internal void OpenSettings()
        {
            this.settingsWindow = new DalamudSettingsWindow(this.dalamud);
            this.isImguiDrawSettingsWindow ^= true;
        }

        /// <summary>
        /// Open the log window.
        /// </summary>
        internal void OpenLog()
        {
            this.logWindow = new DalamudLogWindow(this.dalamud.CommandManager, this.dalamud.Configuration);
            this.isImguiDrawLogWindow = true;
        }

        /// <summary>
        /// Open the credits window.
        /// </summary>
        internal void OpenCredits()
        {
            var logoGraphic =
                this.dalamud.InterfaceManager.LoadImage(
                    Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "logo.png"));
            this.creditsWindow = new DalamudCreditsWindow(this.dalamud, logoGraphic, this.dalamud.Framework);
            this.isImguiDrawCreditsWindow = true;
        }
    }
}

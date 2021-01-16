using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CheapLoc;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using ImGuiNET;
using Serilog;
using Serilog.Events;

namespace Dalamud.Interface
{
    internal class DalamudInterface
    {
        private readonly Dalamud dalamud;

        public DalamudInterface(Dalamud dalamud) {
            this.dalamud = dalamud;
        }

        private bool isImguiDrawDemoWindow = false;

#if DEBUG
        private bool isImguiDrawDevMenu = true;
#else
        private bool isImguiDrawDevMenu = false;
#endif

        public bool IsDevMenu
        {
            get => this.isImguiDrawDevMenu;
            set => this.isImguiDrawDevMenu = value;
        }

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

        public void Draw()
        {
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

                ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);
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
                        ImGui.MenuItem("Draw Dalamud dev menu", "", ref this.isImguiDrawDevMenu);
                        ImGui.Separator();
                        if (ImGui.MenuItem("Open Log window"))
                        {
                            this.logWindow = new DalamudLogWindow(this.dalamud.CommandManager);
                            this.isImguiDrawLogWindow = true;
                        }
                        if (ImGui.BeginMenu("Set log level..."))
                        {
                            foreach (var logLevel in Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>())
                            {
                                if (ImGui.MenuItem(logLevel + "##logLevelSwitch", "", this.dalamud.LogLevelSwitch.MinimumLevel == logLevel))
                                {
                                    this.dalamud.LogLevelSwitch.MinimumLevel = logLevel;
                                }
                            }

                            ImGui.EndMenu();
                        }
                        if (this.dalamud.AntiDebug == null && ImGui.MenuItem("Enable AntiDebug"))
                        {
                            this.dalamud.AntiDebug = new AntiDebug(this.dalamud.SigScanner);
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
                            OpenCredits();
                        }
                        if (ImGui.MenuItem("Open Settings window"))
                        {
                            OpenSettings();
                        }
                        if (ImGui.MenuItem("Open Changelog window"))
                        {
                            OpenChangelog();
                        }
                        ImGui.MenuItem("Draw ImGui demo", "", ref this.isImguiDrawDemoWindow);
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
                        ImGui.MenuItem("API Level:" + PluginManager.DALAMUD_API_LEVEL, false);
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
                                Loc.SetupWithFallbacks();
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
        internal void OpenPluginInstaller()
        {
            if (this.pluginWindow == null)
            {
                this.pluginWindow = new PluginInstallerWindow(this.dalamud, this.dalamud.StartInfo.GameVersion);
            }
            this.isImguiDrawPluginWindow ^= true;
        }

        internal void OpenChangelog()
        {
            this.changelogWindow = new DalamudChangelogWindow(this.dalamud);
            this.isImguiDrawChangelogWindow = true;
        }

        internal void OpenSettings()
        {
            this.settingsWindow = new DalamudSettingsWindow(this.dalamud);
            this.isImguiDrawSettingsWindow ^= true;
        }

        public void OpenLog() {
            this.logWindow = new DalamudLogWindow(this.dalamud.CommandManager);
            this.isImguiDrawLogWindow = true;
        }

        public void OpenCredits() {
            var logoGraphic =
                this.dalamud.InterfaceManager.LoadImage(
                    Path.Combine(this.dalamud.StartInfo.WorkingDirectory, "UIRes", "logo.png"));
            this.creditsWindow = new DalamudCreditsWindow(this.dalamud, logoGraphic, this.dalamud.Framework);
            this.isImguiDrawCreditsWindow = true;
        }
    }
}

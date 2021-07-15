using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Internal.Windows;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Internal;
using ImGuiNET;
using Serilog.Events;

namespace Dalamud.Interface.Internal
{
    /// <summary>
    /// This plugin implements all of the Dalamud interface separately, to allow for reloading of the interface and rapid prototyping.
    /// </summary>
    internal class DalamudInterface : IDisposable
    {
        private static readonly ModuleLog Log = new("DUI");

        private readonly Dalamud dalamud;
        private readonly WindowSystem windowSystem;

        private readonly ChangelogWindow changelogWindow;
        private readonly ColorDemoWindow colorDemoWindow;
        private readonly ComponentDemoWindow componentDemoWindow;
        private readonly CreditsWindow creditsWindow;
        private readonly DataWindow dataWindow;
        private readonly GamepadModeNotifierWindow gamepadModeNotifierWindow;
        private readonly LogWindow logWindow;
        private readonly PluginStatWindow pluginStatWindow;
        private readonly PluginInstallerWindow pluginWindow;
        private readonly ScratchpadWindow scratchpadWindow;
        private readonly SettingsWindow settingsWindow;

        private ulong frameCount = 0;

#if DEBUG
        private bool isImGuiDrawDevMenu = true;
#else
        private bool isImGuiDrawDevMenu = false;
#endif

        private bool isImGuiDrawDemoWindow = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DalamudInterface"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        public DalamudInterface(Dalamud dalamud)
        {
            this.dalamud = dalamud;
            this.windowSystem = new WindowSystem("DalamudCore");

            this.changelogWindow = new ChangelogWindow(dalamud) { IsOpen = false };
            this.colorDemoWindow = new ColorDemoWindow() { IsOpen = false };
            this.componentDemoWindow = new ComponentDemoWindow() { IsOpen = false };
            this.creditsWindow = new CreditsWindow(dalamud) { IsOpen = false };
            this.dataWindow = new DataWindow(dalamud) { IsOpen = false };
            this.gamepadModeNotifierWindow = new GamepadModeNotifierWindow();
            this.logWindow = new LogWindow(dalamud) { IsOpen = this.dalamud.Configuration.LogOpenAtStartup };
            this.pluginStatWindow = new PluginStatWindow(dalamud) { IsOpen = false };
            this.pluginWindow = new PluginInstallerWindow(dalamud) { IsOpen = false };
            this.scratchpadWindow = new ScratchpadWindow(dalamud) { IsOpen = false };
            this.settingsWindow = new SettingsWindow(dalamud) { IsOpen = false };

            this.windowSystem.AddWindow(this.changelogWindow);
            this.windowSystem.AddWindow(this.colorDemoWindow);
            this.windowSystem.AddWindow(this.componentDemoWindow);
            this.windowSystem.AddWindow(this.creditsWindow);
            this.windowSystem.AddWindow(this.dataWindow);
            this.windowSystem.AddWindow(this.gamepadModeNotifierWindow);
            this.windowSystem.AddWindow(this.logWindow);
            this.windowSystem.AddWindow(this.pluginStatWindow);
            this.windowSystem.AddWindow(this.pluginWindow);
            this.windowSystem.AddWindow(this.scratchpadWindow);
            this.windowSystem.AddWindow(this.settingsWindow);

            this.dalamud.InterfaceManager.OnDraw += this.OnDraw;

            Log.Information("Windows added");
        }

        /// <summary>
        /// Gets or sets a value indicating whether the /xldev menu is open.
        /// </summary>
        public bool IsDevMenuOpen
        {
            get => this.isImGuiDrawDevMenu;
            set => this.isImGuiDrawDevMenu = value;
        }

        /// <summary>
        /// Gets a value indicating whether the current Dalamud version warrants displaying the changelog.
        /// </summary>
        public bool WarrantsChangelog => ChangelogWindow.WarrantsChangelog;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.dalamud.InterfaceManager.OnDraw -= this.OnDraw;

            this.windowSystem.RemoveAllWindows();

            this.creditsWindow.Dispose();
            this.logWindow.Dispose();
            this.scratchpadWindow.Dispose();
        }

        #region Open

        /// <summary>
        /// Opens the <see cref="ChangelogWindow"/>.
        /// </summary>
        public void OpenChangelogWindow() => this.changelogWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="ColorDemoWindow"/>.
        /// </summary>
        public void OpenColorsDemoWindow() => this.colorDemoWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="ComponentDemoWindow"/>.
        /// </summary>
        public void OpenComponentDemoWindow() => this.componentDemoWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="CreditsWindow"/>.
        /// </summary>
        public void OpenCreditsWindow() => this.creditsWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="DataWindow"/>.
        /// </summary>
        /// <param name="dataKind">The data kind to switch to after opening.</param>
        public void OpenDataWindow(string dataKind = null)
        {
            this.dataWindow.IsOpen = true;
            if (dataKind != null && this.dataWindow.IsOpen)
            {
                this.dataWindow.SetDataKind(dataKind);
            }
        }

        /// <summary>
        /// Opens the dev menu bar.
        /// </summary>
        public void OpenDevMenu() => this.isImGuiDrawDevMenu = true;

        /// <summary>
        /// Opens the <see cref="GamepadModeNotifierWindow"/>.
        /// </summary>
        public void OpenGamepadModeNotifierWindow() => this.gamepadModeNotifierWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="LogWindow"/>.
        /// </summary>
        public void OpenLogWindow() => this.logWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="PluginStatWindow"/>.
        /// </summary>
        public void OpenPluginStats() => this.pluginStatWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="PluginInstallerWindow"/>.
        /// </summary>
        public void OpenPluginInstaller() => this.pluginWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="ScratchpadWindow"/>.
        /// </summary>
        public void OpenScratchpadWindow() => this.scratchpadWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="SettingsWindow"/>.
        /// </summary>
        public void OpenSettings() => this.settingsWindow.IsOpen = true;

        #endregion

        #region Toggle

        /// <summary>
        /// Toggles the <see cref="ChangelogWindow"/>.
        /// </summary>
        public void ToggleChangelogWindow() => this.changelogWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="ColorDemoWindow"/>.
        /// </summary>
        public void ToggleColorsDemoWindow() => this.colorDemoWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="ComponentDemoWindow"/>.
        /// </summary>
        public void ToggleComponentDemoWindow() => this.componentDemoWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="CreditsWindow"/>.
        /// </summary>
        public void ToggleCreditsWindow() => this.creditsWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="DataWindow"/>.
        /// </summary>
        /// <param name="dataKind">The data kind to switch to after opening.</param>
        public void ToggleDataWindow(string dataKind = null)
        {
            this.dataWindow.Toggle();
            if (dataKind != null && this.dataWindow.IsOpen)
            {
                this.dataWindow.SetDataKind(dataKind);
            }
        }

        /// <summary>
        /// Toggles the dev menu bar.
        /// </summary>
        public void ToggleDevMenu() => this.isImGuiDrawDevMenu ^= true;

        /// <summary>
        /// Toggles the <see cref="GamepadModeNotifierWindow"/>.
        /// </summary>
        public void ToggleGamepadModeNotifierWindow() => this.gamepadModeNotifierWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="LogWindow"/>.
        /// </summary>
        public void ToggleLogWindow() => this.logWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="PluginStatWindow"/>.
        /// </summary>
        public void TogglePluginStatsWindow() => this.pluginStatWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="PluginInstallerWindow"/>.
        /// </summary>
        public void TogglePluginInstallerWindow() => this.pluginWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="ScratchpadWindow"/>.
        /// </summary>
        public void ToggleScratchpadWindow() => this.scratchpadWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="SettingsWindow"/>.
        /// </summary>
        public void ToggleSettingsWindow() => this.settingsWindow.Toggle();

        #endregion

        private void OnDraw()
        {
            this.frameCount++;

            try
            {
                this.DrawHiddenDevMenuOpener();
                this.DrawDevMenu();

                if (this.dalamud.Framework.Gui.GameUiHidden)
                    return;

                this.windowSystem.Draw();

                if (this.isImGuiDrawDemoWindow)
                    ImGui.ShowDemoWindow();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error during OnDraw");
            }
        }

        private void DrawHiddenDevMenuOpener()
        {
            if (!this.isImGuiDrawDevMenu && !this.dalamud.ClientState.Condition.Any())
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
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
                        this.isImGuiDrawDevMenu = true;

                    ImGui.End();
                }

                ImGui.PopStyleColor(8);
            }
        }

        private void DrawDevMenu()
        {
            if (this.isImGuiDrawDevMenu)
            {
                if (ImGui.BeginMainMenuBar())
                {
                    if (ImGui.BeginMenu("Dalamud"))
                    {
                        ImGui.MenuItem("Draw Dalamud dev menu", string.Empty, ref this.isImGuiDrawDevMenu);

                        ImGui.Separator();

                        if (ImGui.MenuItem("Open Log window"))
                        {
                            this.OpenLogWindow();
                        }

                        if (ImGui.BeginMenu("Set log level..."))
                        {
                            foreach (var logLevel in Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>())
                            {
                                if (ImGui.MenuItem(logLevel + "##logLevelSwitch", string.Empty, this.dalamud.LogLevelSwitch.MinimumLevel == logLevel))
                                {
                                    this.dalamud.LogLevelSwitch.MinimumLevel = logLevel;
                                    this.dalamud.Configuration.LogLevel = logLevel;
                                    this.dalamud.Configuration.Save();
                                }
                            }

                            ImGui.EndMenu();
                        }

                        if (ImGui.MenuItem("Enable AntiDebug", null, this.dalamud.AntiDebug.IsEnabled))
                        {
                            var newEnabled = !this.dalamud.AntiDebug.IsEnabled;
                            if (newEnabled)
                                this.dalamud.AntiDebug.Enable();
                            else
                                this.dalamud.AntiDebug.Disable();

                            this.dalamud.Configuration.IsAntiAntiDebugEnabled = newEnabled;
                            this.dalamud.Configuration.Save();
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Open Data window"))
                        {
                            this.OpenDataWindow();
                        }

                        if (ImGui.MenuItem("Open Credits window"))
                        {
                            this.OpenCreditsWindow();
                        }

                        if (ImGui.MenuItem("Open Settings window"))
                        {
                            this.OpenSettings();
                        }

                        if (ImGui.MenuItem("Open Changelog window"))
                        {
                            this.OpenChangelogWindow();
                        }

                        if (ImGui.MenuItem("Open Components Demo"))
                        {
                            this.OpenComponentDemoWindow();
                        }

                        if (ImGui.MenuItem("Open Colors Demo"))
                        {
                            this.OpenColorsDemoWindow();
                        }

                        ImGui.MenuItem("Draw ImGui demo", string.Empty, ref this.isImGuiDrawDemoWindow);

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
                            Marshal.ReadByte(IntPtr.Zero);
                        }

                        ImGui.Separator();
                        if (ImGui.MenuItem("Enable Dalamud testing", string.Empty, this.dalamud.Configuration.DoDalamudTest))
                        {
                            this.dalamud.Configuration.DoDalamudTest ^= true;
                            this.dalamud.Configuration.Save();
                        }

                        ImGui.MenuItem(Util.AssemblyVersion, false);
                        ImGui.MenuItem(this.dalamud.StartInfo.GameVersion.ToString(), false);

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
                            foreach (var plugin in this.dalamud.PluginManager.InstalledPlugins)
                            {
                                // TODO: some more here, state maybe?
                                PluginLog.Information($"{plugin.Name}");
                            }
                        }

                        if (ImGui.MenuItem("Reload plugins"))
                        {
                            try
                            {
                                this.dalamud.PluginManager.ReloadAllPlugins();
                            }
                            catch (Exception ex)
                            {
                                this.dalamud.Framework.Gui.Chat.PrintError("Reload failed.");
                                PluginLog.Error(ex, "Plugin reload failed.");
                            }
                        }

                        if (ImGui.MenuItem("Scan dev plugins"))
                        {
                            this.dalamud.PluginManager.ScanDevPlugins();
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Load all API levels", null, this.dalamud.Configuration.LoadAllApiLevels))
                        {
                            this.dalamud.Configuration.LoadAllApiLevels = !this.dalamud.Configuration.LoadAllApiLevels;
                            this.dalamud.Configuration.Save();
                        }

                        ImGui.Separator();
                        ImGui.MenuItem("API Level:" + PluginManager.DalamudApiLevel, false);
                        ImGui.MenuItem("Loaded plugins:" + this.dalamud.PluginManager?.InstalledPlugins.Count, false);
                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Scratchpad"))
                    {
                        if (ImGui.MenuItem("Open Scratchpad"))
                        {
                            this.OpenScratchpadWindow();
                        }

                        if (ImGui.MenuItem("Dispose all scratches"))
                        {
                            this.scratchpadWindow.Execution.DisposeAllScratches();
                        }

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
        }
    }
}

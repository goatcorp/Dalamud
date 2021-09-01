using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Configuration.Internal;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui;
using Dalamud.Game.Internal;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Interface.Internal.Windows.SelfTest;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene.ManagedAsserts;
using Serilog.Events;

namespace Dalamud.Interface.Internal
{
    /// <summary>
    /// This plugin implements all of the Dalamud interface separately, to allow for reloading of the interface and rapid prototyping.
    /// </summary>
    internal class DalamudInterface : IDisposable
    {
        private static readonly ModuleLog Log = new("DUI");

        private readonly ChangelogWindow changelogWindow;
        private readonly ColorDemoWindow colorDemoWindow;
        private readonly ComponentDemoWindow componentDemoWindow;
        private readonly CreditsWindow creditsWindow;
        private readonly DataWindow dataWindow;
        private readonly GamepadModeNotifierWindow gamepadModeNotifierWindow;
        private readonly IMEWindow imeWindow;
        private readonly ConsoleWindow consoleWindow;
        private readonly PluginStatWindow pluginStatWindow;
        private readonly PluginInstallerWindow pluginWindow;
        private readonly ScratchpadWindow scratchpadWindow;
        private readonly SettingsWindow settingsWindow;
        private readonly SelfTestWindow selfTestWindow;

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
        public DalamudInterface()
        {
            var configuration = Service<DalamudConfiguration>.Get();

            this.WindowSystem = new WindowSystem("DalamudCore");

            this.changelogWindow = new ChangelogWindow() { IsOpen = false };
            this.colorDemoWindow = new ColorDemoWindow() { IsOpen = false };
            this.componentDemoWindow = new ComponentDemoWindow() { IsOpen = false };
            this.creditsWindow = new CreditsWindow() { IsOpen = false };
            this.dataWindow = new DataWindow() { IsOpen = false };
            this.gamepadModeNotifierWindow = new GamepadModeNotifierWindow() { IsOpen = false };
            this.imeWindow = new IMEWindow() { IsOpen = false };
            this.consoleWindow = new ConsoleWindow() { IsOpen = configuration.LogOpenAtStartup };
            this.pluginStatWindow = new PluginStatWindow() { IsOpen = false };
            this.pluginWindow = new PluginInstallerWindow() { IsOpen = false };
            this.scratchpadWindow = new ScratchpadWindow() { IsOpen = false };
            this.settingsWindow = new SettingsWindow() { IsOpen = false };
            this.selfTestWindow = new SelfTestWindow() { IsOpen = false };

            this.WindowSystem.AddWindow(this.changelogWindow);
            this.WindowSystem.AddWindow(this.colorDemoWindow);
            this.WindowSystem.AddWindow(this.componentDemoWindow);
            this.WindowSystem.AddWindow(this.creditsWindow);
            this.WindowSystem.AddWindow(this.dataWindow);
            this.WindowSystem.AddWindow(this.gamepadModeNotifierWindow);
            this.WindowSystem.AddWindow(this.imeWindow);
            this.WindowSystem.AddWindow(this.consoleWindow);
            this.WindowSystem.AddWindow(this.pluginStatWindow);
            this.WindowSystem.AddWindow(this.pluginWindow);
            this.WindowSystem.AddWindow(this.scratchpadWindow);
            this.WindowSystem.AddWindow(this.settingsWindow);
            this.WindowSystem.AddWindow(this.selfTestWindow);

            ImGuiManagedAsserts.EnableAsserts = true;

            Service<InterfaceManager>.Get().Draw += this.OnDraw;

            Log.Information("Windows added");
        }

        /// <summary>
        /// Gets the <see cref="WindowSystem"/> controlling all Dalamud-internal windows.
        /// </summary>
        public WindowSystem WindowSystem { get; init; }

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
            Service<InterfaceManager>.Get().Draw -= this.OnDraw;

            this.WindowSystem.RemoveAllWindows();

            this.creditsWindow.Dispose();
            this.consoleWindow.Dispose();
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
        /// Opens the <see cref="IMEWindow"/>.
        /// </summary>
        public void OpenIMEWindow() => this.imeWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="ConsoleWindow"/>.
        /// </summary>
        public void OpenLogWindow() => this.consoleWindow.IsOpen = true;

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

        /// <summary>
        /// Opens the <see cref="SelfTestWindow"/>.
        /// </summary>
        public void OpenSelfTest() => this.selfTestWindow.IsOpen = true;

        #endregion

        #region Close

        /// <summary>
        /// Closes the <see cref="IMEWindow"/>.
        /// </summary>
        public void CloseIMEWindow() => this.imeWindow.IsOpen = false;

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
        /// Toggles the <see cref="IMEWindow"/>.
        /// </summary>
        public void ToggleIMEWindow() => this.imeWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="ConsoleWindow"/>.
        /// </summary>
        public void ToggleLogWindow() => this.consoleWindow.Toggle();

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

        /// <summary>
        /// Toggles the <see cref="SelfTestWindow"/>.
        /// </summary>
        public void ToggleSelfTestWindow() => this.selfTestWindow.Toggle();

        #endregion

        private void OnDraw()
        {
            this.frameCount++;

            try
            {
                this.DrawHiddenDevMenuOpener();
                this.DrawDevMenu();

                if (Service<GameGui>.Get().GameUiHidden)
                    return;

                this.WindowSystem.Draw();

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
            var condition = Service<Condition>.Get();

            if (!this.isImGuiDrawDevMenu && !condition.Any())
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
                    var dalamud = Service<Dalamud>.Get();
                    var configuration = Service<DalamudConfiguration>.Get();
                    var pluginManager = Service<PluginManager>.Get();

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
                                if (ImGui.MenuItem(logLevel + "##logLevelSwitch", string.Empty, dalamud.LogLevelSwitch.MinimumLevel == logLevel))
                                {
                                    dalamud.LogLevelSwitch.MinimumLevel = logLevel;
                                    configuration.LogLevel = logLevel;
                                    configuration.Save();
                                }
                            }

                            ImGui.EndMenu();
                        }

                        var antiDebug = Service<AntiDebug>.Get();
                        if (ImGui.MenuItem("Enable AntiDebug", null, antiDebug.IsEnabled))
                        {
                            var newEnabled = !antiDebug.IsEnabled;
                            if (newEnabled)
                                antiDebug.Enable();
                            else
                                antiDebug.Disable();

                            configuration.IsAntiAntiDebugEnabled = newEnabled;
                            configuration.Save();
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

                        if (ImGui.MenuItem("Open Self-Test"))
                        {
                            this.OpenSelfTest();
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Unload Dalamud"))
                        {
                            Service<Dalamud>.Get().Unload();
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
                        if (ImGui.MenuItem("Enable Dalamud testing", string.Empty, configuration.DoDalamudTest))
                        {
                            configuration.DoDalamudTest ^= true;
                            configuration.Save();
                        }

                        var startInfo = Service<DalamudStartInfo>.Get();
                        ImGui.MenuItem(Util.AssemblyVersion, false);
                        ImGui.MenuItem(startInfo.GameVersion.ToString(), false);

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("GUI"))
                    {
                        ImGui.MenuItem("Draw ImGui demo", string.Empty, ref this.isImGuiDrawDemoWindow);

                        ImGui.Separator();

                        ImGui.MenuItem("Enable Asserts", string.Empty, ref ImGuiManagedAsserts.EnableAsserts);

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Game"))
                    {
                        if (ImGui.MenuItem("Replace ExceptionHandler"))
                        {
                            dalamud.ReplaceExceptionHandler();
                        }

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Plugins"))
                    {
                        if (ImGui.MenuItem("Open Plugin installer"))
                        {
                            this.OpenPluginInstaller();
                        }

                        if (ImGui.MenuItem("Clear cached images/icons"))
                        {
                            this.pluginWindow?.ClearIconCache();
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Open Plugin Stats"))
                        {
                            this.OpenPluginStats();
                        }

                        if (ImGui.MenuItem("Print plugin info"))
                        {
                            foreach (var plugin in pluginManager.InstalledPlugins)
                            {
                                // TODO: some more here, state maybe?
                                PluginLog.Information($"{plugin.Name}");
                            }
                        }

                        if (ImGui.MenuItem("Reload plugins"))
                        {
                            try
                            {
                                pluginManager.ReloadAllPlugins();
                            }
                            catch (Exception ex)
                            {
                                Service<ChatGui>.Get().PrintError("Reload failed.");
                                PluginLog.Error(ex, "Plugin reload failed.");
                            }
                        }

                        if (ImGui.MenuItem("Scan dev plugins"))
                        {
                            pluginManager.ScanDevPlugins();
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Load all API levels", null, configuration.LoadAllApiLevels))
                        {
                            configuration.LoadAllApiLevels = !configuration.LoadAllApiLevels;
                            configuration.Save();
                        }

                        ImGui.Separator();
                        ImGui.MenuItem("API Level:" + PluginManager.DalamudApiLevel, false);
                        ImGui.MenuItem("Loaded plugins:" + pluginManager.InstalledPlugins.Count, false);
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
                        var localization = Service<Localization>.Get();

                        if (ImGui.MenuItem("Export localizable"))
                        {
                            localization.ExportLocalizable();
                        }

                        if (ImGui.BeginMenu("Load language..."))
                        {
                            if (ImGui.MenuItem("From Fallbacks"))
                            {
                                localization.SetupWithFallbacks();
                            }

                            if (ImGui.MenuItem("From UICulture"))
                            {
                                localization.SetupWithUiCulture();
                            }

                            foreach (var applicableLangCode in Localization.ApplicableLangCodes)
                            {
                                if (ImGui.MenuItem($"Applicable: {applicableLangCode}"))
                                {
                                    localization.SetupWithLangCode(applicableLangCode);
                                }
                            }

                            ImGui.EndMenu();
                        }

                        ImGui.EndMenu();
                    }

                    if (Service<GameGui>.Get().GameUiHidden)
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

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui;
using Dalamud.Game.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.ManagedAsserts;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Interface.Internal.Windows.PluginInstaller;
using Dalamud.Interface.Internal.Windows.SelfTest;
using Dalamud.Interface.Internal.Windows.StyleEditor;
using Dalamud.Interface.Style;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using ImGuiScene;
using ImPlotNET;
using PInvoke;
using Serilog.Events;

namespace Dalamud.Interface.Internal
{
    /// <summary>
    /// This plugin implements all of the Dalamud interface separately, to allow for reloading of the interface and rapid prototyping.
    /// </summary>
    [ServiceManager.EarlyLoadedService]
    internal class DalamudInterface : IDisposable, IServiceType
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
        private readonly SettingsWindow settingsWindow;
        private readonly SelfTestWindow selfTestWindow;
        private readonly StyleEditorWindow styleEditorWindow;
        private readonly TitleScreenMenuWindow titleScreenMenuWindow;
        private readonly ProfilerWindow profilerWindow;
        private readonly BranchSwitcherWindow branchSwitcherWindow;

        private readonly TextureWrap logoTexture;
        private readonly TextureWrap tsmLogoTexture;

#if DEBUG
        private bool isImGuiDrawDevMenu = true;
#else
        private bool isImGuiDrawDevMenu = false;
#endif

#if BOOT_AGING
        private bool signaledBoot = false;
#endif

        private bool isImGuiDrawDemoWindow = false;
        private bool isImPlotDrawDemoWindow = false;
        private bool isImGuiTestWindowsInMonospace = false;
        private bool isImGuiDrawMetricsWindow = false;

        [ServiceManager.ServiceConstructor]
        private DalamudInterface(
            Dalamud dalamud,
            DalamudConfiguration configuration,
            InterfaceManager.InterfaceManagerWithScene interfaceManagerWithScene,
            PluginImageCache pluginImageCache)
        {
            var interfaceManager = interfaceManagerWithScene.Manager;
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
            this.pluginWindow = new PluginInstallerWindow(pluginImageCache) { IsOpen = false };
            this.settingsWindow = new SettingsWindow() { IsOpen = false };
            this.selfTestWindow = new SelfTestWindow() { IsOpen = false };
            this.styleEditorWindow = new StyleEditorWindow() { IsOpen = false };
            this.titleScreenMenuWindow = new TitleScreenMenuWindow() { IsOpen = false };
            this.profilerWindow = new ProfilerWindow() { IsOpen = false };
            this.branchSwitcherWindow = new BranchSwitcherWindow() { IsOpen = false };

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
            this.WindowSystem.AddWindow(this.settingsWindow);
            this.WindowSystem.AddWindow(this.selfTestWindow);
            this.WindowSystem.AddWindow(this.styleEditorWindow);
            this.WindowSystem.AddWindow(this.titleScreenMenuWindow);
            this.WindowSystem.AddWindow(this.profilerWindow);
            this.WindowSystem.AddWindow(this.branchSwitcherWindow);

            ImGuiManagedAsserts.AssertsEnabled = configuration.AssertsEnabledAtStartup;
            this.isImGuiDrawDevMenu = this.isImGuiDrawDevMenu || configuration.DevBarOpenAtStartup;

            interfaceManager.Draw += this.OnDraw;

            var logoTex =
                interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "logo.png"));
            var tsmLogoTex =
                interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "tsmLogo.png"));

            if (logoTex == null || tsmLogoTex == null)
            {
                throw new Exception("Failed to load logo textures");
            }

            this.logoTexture = logoTex;
            this.tsmLogoTexture = tsmLogoTex;

            var tsm = Service<TitleScreenMenu>.Get();
            tsm.AddEntryCore(Loc.Localize("TSMDalamudPlugins", "Plugin Installer"), this.tsmLogoTexture, () => this.pluginWindow.IsOpen = true);
            tsm.AddEntryCore(Loc.Localize("TSMDalamudSettings", "Dalamud Settings"), this.tsmLogoTexture, () => this.settingsWindow.IsOpen = true);

            if (!configuration.DalamudBetaKind.IsNullOrEmpty())
            {
                tsm.AddEntryCore(Loc.Localize("TSMDalamudDevMenu", "Developer Menu"), this.tsmLogoTexture, () => this.isImGuiDrawDevMenu = true);
            }
        }

        /// <summary>
        /// Gets the number of frames since Dalamud has loaded.
        /// </summary>
        public ulong FrameCount { get; private set; }

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

        /// <inheritdoc/>
        public void Dispose()
        {
            Service<InterfaceManager>.Get().Draw -= this.OnDraw;

            this.WindowSystem.RemoveAllWindows();

            this.changelogWindow.Dispose();
            this.creditsWindow.Dispose();
            this.consoleWindow.Dispose();
            this.pluginWindow.Dispose();
            this.titleScreenMenuWindow.Dispose();

            this.logoTexture.Dispose();
            this.tsmLogoTexture.Dispose();
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
        public void OpenDataWindow(string? dataKind = null)
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
        public void OpenImeWindow() => this.imeWindow.IsOpen = true;

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
        /// Opens the <see cref="SettingsWindow"/>.
        /// </summary>
        public void OpenSettings() => this.settingsWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="SelfTestWindow"/>.
        /// </summary>
        public void OpenSelfTest() => this.selfTestWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="StyleEditorWindow"/>.
        /// </summary>
        public void OpenStyleEditor() => this.styleEditorWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="ProfilerWindow"/>.
        /// </summary>
        public void OpenProfiler() => this.profilerWindow.IsOpen = true;

        /// <summary>
        /// Opens the <see cref="BranchSwitcherWindow"/>
        /// </summary>
        public void OpenBranchSwitcher() => this.branchSwitcherWindow.IsOpen = true;

        #endregion

        #region Close

        /// <summary>
        /// Closes the <see cref="IMEWindow"/>.
        /// </summary>
        public void CloseImeWindow() => this.imeWindow.IsOpen = false;

        /// <summary>
        /// Closes the <see cref="GamepadModeNotifierWindow"/>.
        /// </summary>
        public void CloseGamepadModeNotifierWindow() => this.gamepadModeNotifierWindow.IsOpen = false;

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
        /// Toggles the <see cref="SettingsWindow"/>.
        /// </summary>
        public void ToggleSettingsWindow() => this.settingsWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="SelfTestWindow"/>.
        /// </summary>
        public void ToggleSelfTestWindow() => this.selfTestWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="StyleEditorWindow"/>.
        /// </summary>
        public void ToggleStyleEditorWindow() => this.selfTestWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="ProfilerWindow"/>.
        /// </summary>
        public void ToggleProfilerWindow() => this.profilerWindow.Toggle();

        /// <summary>
        /// Toggles the <see cref="BranchSwitcherWindow"/>.
        /// </summary>
        public void ToggleBranchSwitcher() => this.branchSwitcherWindow.Toggle();

        #endregion

        private void OnDraw()
        {
            this.FrameCount++;

#if BOOT_AGING
            if (this.frameCount > 500 && !this.signaledBoot)
            {
                this.signaledBoot = true;

                System.Threading.Tasks.Task.Run(async () =>
                {
                    using var client = new System.Net.Http.HttpClient();
                    await client.PostAsync("http://localhost:1415/aging/success", new System.Net.Http.StringContent(string.Empty));
                });
            }
#endif

            try
            {
                this.DrawHiddenDevMenuOpener();
                this.DrawDevMenu();

                if (Service<GameGui>.Get().GameUiHidden)
                    return;

                this.WindowSystem.Draw();

                if (this.isImGuiTestWindowsInMonospace)
                    ImGui.PushFont(InterfaceManager.MonoFont);

                if (this.isImGuiDrawDemoWindow)
                    ImGui.ShowDemoWindow(ref this.isImGuiDrawDemoWindow);

                if (this.isImPlotDrawDemoWindow)
                    ImPlot.ShowDemoWindow(ref this.isImPlotDrawDemoWindow);

                if (this.isImGuiDrawMetricsWindow)
                    ImGui.ShowMetricsWindow(ref this.isImGuiDrawMetricsWindow);

                if (this.isImGuiTestWindowsInMonospace)
                    ImGui.PopFont();

                // Release focus of any ImGui window if we click into the game.
                var io = ImGui.GetIO();
                if (!io.WantCaptureMouse && (User32.GetKeyState((int)User32.VirtualKey.VK_LBUTTON) & 0x8000) != 0)
                {
                    ImGui.SetWindowFocus(null);
                }
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
                ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, new Vector4(0, 0, 0, 1));
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 1));
                ImGui.PushStyleColor(ImGuiCol.BorderShadow, new Vector4(0, 0, 0, 1));
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 1));

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);

                var windowPos = ImGui.GetMainViewport().Pos + new Vector2(20);
                ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
                ImGui.SetNextWindowBgAlpha(1);

                if (ImGui.Begin("DevMenu Opener", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings))
                {
                    ImGui.SetNextItemWidth(40);
                    if (ImGui.Button("###devMenuOpener", new Vector2(20, 20)))
                        this.isImGuiDrawDevMenu = true;

                    ImGui.End();
                }

                if (EnvironmentConfiguration.DalamudForceMinHook)
                {
                    ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
                    ImGui.SetNextWindowBgAlpha(1);

                    if (ImGui.Begin(
                            "Disclaimer",
                            ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground |
                            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
                            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMouseInputs |
                            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings))
                    {
                        ImGui.TextColored(ImGuiColors.DalamudRed, "Is force MinHook!");
                    }

                    ImGui.End();
                }

                ImGui.PopStyleVar(4);
                ImGui.PopStyleColor(7);
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
                        ImGui.MenuItem("Draw dev menu", string.Empty, ref this.isImGuiDrawDevMenu);
                        var devBarAtStartup = configuration.DevBarOpenAtStartup;
                        if (ImGui.MenuItem("Draw dev menu at startup", string.Empty, ref devBarAtStartup))
                        {
                            configuration.DevBarOpenAtStartup ^= true;
                            configuration.Save();
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Open Log window"))
                        {
                            this.OpenLogWindow();
                        }

                        if (ImGui.BeginMenu("Set log level..."))
                        {
                            foreach (var logLevel in Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>())
                            {
                                if (ImGui.MenuItem(logLevel + "##logLevelSwitch", string.Empty, EntryPoint.LogLevelSwitch.MinimumLevel == logLevel))
                                {
                                    EntryPoint.LogLevelSwitch.MinimumLevel = logLevel;
                                    configuration.LogLevel = logLevel;
                                    configuration.Save();
                                }
                            }

                            ImGui.EndMenu();
                        }

                        var logSynchronously = configuration.LogSynchronously;
                        if (ImGui.MenuItem("Log Synchronously", null, ref logSynchronously))
                        {
                            configuration.LogSynchronously = logSynchronously;
                            configuration.Save();

                            var startupInfo = Service<DalamudStartInfo>.Get();
                            EntryPoint.InitLogging(
                                startupInfo.WorkingDirectory!,
                                startupInfo.BootShowConsole,
                                configuration.LogSynchronously);
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

                        if (ImGui.MenuItem("Open Style Editor"))
                        {
                            this.OpenStyleEditor();
                        }

                        if (ImGui.MenuItem("Open Profiler"))
                        {
                            this.OpenProfiler();
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

                        ImGui.Separator();

                        if (ImGui.MenuItem("Access Violation"))
                        {
                            Marshal.ReadByte(IntPtr.Zero);
                        }

                        if (ImGui.MenuItem("Crash game (nullptr)"))
                        {
                            unsafe
                            {
                                var framework = Framework.Instance();
                                framework->UIModule = (UIModule*)0;
                            }
                        }

                        if (ImGui.MenuItem("Crash game (non-nullptr)"))
                        {
                            unsafe
                            {
                                var framework = Framework.Instance();
                                framework->UIModule = (UIModule*)0x12345678;
                            }
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Open Dalamud branch switcher"))
                        {
                            this.OpenBranchSwitcher();
                        }

                        var startInfo = Service<DalamudStartInfo>.Get();
                        ImGui.MenuItem(Util.AssemblyVersion, false);
                        ImGui.MenuItem(startInfo.GameVersion.ToString(), false);
                        ImGui.MenuItem($"D: {Util.GetGitHash()} CS: {Util.GetGitHashClientStructs()}", false);

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("GUI"))
                    {
                        ImGui.MenuItem("Use Monospace font for following windows", string.Empty, ref this.isImGuiTestWindowsInMonospace);
                        ImGui.MenuItem("Draw ImGui demo", string.Empty, ref this.isImGuiDrawDemoWindow);
                        ImGui.MenuItem("Draw ImPlot demo", string.Empty, ref this.isImPlotDrawDemoWindow);
                        ImGui.MenuItem("Draw metrics", string.Empty, ref this.isImGuiDrawMetricsWindow);

                        ImGui.Separator();

                        var val = ImGuiManagedAsserts.AssertsEnabled;
                        if (ImGui.MenuItem("Enable Asserts", string.Empty, ref val))
                        {
                            ImGuiManagedAsserts.AssertsEnabled = val;
                        }

                        if (ImGui.MenuItem("Enable asserts at startup", null, configuration.AssertsEnabledAtStartup))
                        {
                            configuration.AssertsEnabledAtStartup = !configuration.AssertsEnabledAtStartup;
                            configuration.Save();
                        }

                        if (ImGui.MenuItem("Clear focus"))
                        {
                            ImGui.SetWindowFocus(null);
                        }

                        if (ImGui.MenuItem("Dump style"))
                        {
                            var info = string.Empty;
                            var style = StyleModelV1.Get();
                            var enCulture = new CultureInfo("en-US");

                            foreach (var propertyInfo in typeof(StyleModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                            {
                                if (propertyInfo.PropertyType == typeof(Vector2))
                                {
                                    var vec2 = (Vector2)propertyInfo.GetValue(style);
                                    info += $"{propertyInfo.Name} = new Vector2({vec2.X.ToString(enCulture)}f, {vec2.Y.ToString(enCulture)}f),\n";
                                }
                                else
                                {
                                    info += $"{propertyInfo.Name} = {propertyInfo.GetValue(style)},\n";
                                }
                            }

                            info += "Colors = new Dictionary<string, Vector4>()\n";
                            info += "{\n";

                            foreach (var color in style.Colors)
                            {
                                info +=
                                    $"{{\"{color.Key}\", new Vector4({color.Value.X.ToString(enCulture)}f, {color.Value.Y.ToString(enCulture)}f, {color.Value.Z.ToString(enCulture)}f, {color.Value.W.ToString(enCulture)}f)}},\n";
                            }

                            info += "},";

                            Log.Information(info);
                        }

                        if (ImGui.MenuItem("Show dev bar info", null, configuration.ShowDevBarInfo))
                        {
                            configuration.ShowDevBarInfo = !configuration.ShowDevBarInfo;
                        }

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
                                pluginManager.ReloadAllPluginsAsync().Wait();
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

                        if (ImGui.MenuItem("Load all API levels (ONLY FOR DEVELOPERS!!!)", null, pluginManager.LoadAllApiLevels))
                        {
                            pluginManager.LoadAllApiLevels = !pluginManager.LoadAllApiLevels;
                        }

                        if (ImGui.MenuItem("Load blacklisted plugins", null, pluginManager.LoadBannedPlugins))
                        {
                            pluginManager.LoadBannedPlugins = !pluginManager.LoadBannedPlugins;
                        }

                        ImGui.Separator();
                        ImGui.MenuItem("API Level:" + PluginManager.DalamudApiLevel, false);
                        ImGui.MenuItem("Loaded plugins:" + pluginManager.InstalledPlugins.Count, false);
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

                    if (configuration.ShowDevBarInfo)
                    {
                        ImGui.PushFont(InterfaceManager.MonoFont);

                        ImGui.BeginMenu(Util.GetGitHash(), false);
                        ImGui.BeginMenu(this.FrameCount.ToString("000000"), false);
                        ImGui.BeginMenu(ImGui.GetIO().Framerate.ToString("000"), false);
                        ImGui.BeginMenu($"{Util.FormatBytes(GC.GetTotalMemory(false))}", false);

                        ImGui.PopFont();
                    }

                    ImGui.EndMainMenuBar();
                }
            }
        }
    }
}

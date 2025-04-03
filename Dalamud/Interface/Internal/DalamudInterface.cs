using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Console;
using Dalamud.Data;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Gui;
using Dalamud.Game.Internal;
using Dalamud.Hooking;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Interface.Internal.Windows.Data;
using Dalamud.Interface.Internal.Windows.PluginInstaller;
using Dalamud.Interface.Internal.Windows.SelfTest;
using Dalamud.Interface.Internal.Windows.Settings;
using Dalamud.Interface.Internal.Windows.StyleEditor;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Style;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

using ImGuiNET;

using ImPlotNET;
using PInvoke;
using Serilog.Events;

namespace Dalamud.Interface.Internal;

/// <summary>
/// This plugin implements all of the Dalamud interface separately, to allow for reloading of the interface and rapid prototyping.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class DalamudInterface : IInternalDisposableService
{
    private const float CreditsDarkeningMaxAlpha = 0.8f;

    private static readonly ModuleLog Log = new("DUI");

    private readonly Dalamud dalamud;
    private readonly DalamudConfiguration configuration;
    private readonly InterfaceManager interfaceManager;

    private readonly ChangelogWindow changelogWindow;
    private readonly ColorDemoWindow colorDemoWindow;
    private readonly ComponentDemoWindow componentDemoWindow;
    private readonly DataWindow dataWindow;
    private readonly GamepadModeNotifierWindow gamepadModeNotifierWindow;
    private readonly ConsoleWindow consoleWindow;
    private readonly PluginStatWindow pluginStatWindow;
    private readonly PluginInstallerWindow pluginWindow;
    private readonly SettingsWindow settingsWindow;
    private readonly SelfTestWindow selfTestWindow;
    private readonly StyleEditorWindow styleEditorWindow;
    private readonly TitleScreenMenuWindow titleScreenMenuWindow;
    private readonly ProfilerWindow profilerWindow;
    private readonly BranchSwitcherWindow branchSwitcherWindow;
    private readonly HitchSettingsWindow hitchSettingsWindow;

    private bool isCreditsDarkening = false;
    private OutCubic creditsDarkeningAnimation = new(TimeSpan.FromSeconds(10));

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
        FontAtlasFactory fontAtlasFactory,
        InterfaceManager interfaceManager,
        PluginImageCache pluginImageCache,
        DalamudAssetManager dalamudAssetManager,
        Game.Framework framework,
        ClientState clientState,
        TitleScreenMenu titleScreenMenu,
        GameGui gameGui,
        ConsoleManager consoleManager,
        AddonLifecycle addonLifecycle)
    {
        this.dalamud = dalamud;
        this.configuration = configuration;
        this.interfaceManager = interfaceManager;

        this.WindowSystem = new WindowSystem("DalamudCore");

        this.colorDemoWindow = new ColorDemoWindow() { IsOpen = false };
        this.componentDemoWindow = new ComponentDemoWindow() { IsOpen = false };
        this.dataWindow = new DataWindow() { IsOpen = false };
        this.gamepadModeNotifierWindow = new GamepadModeNotifierWindow() { IsOpen = false };
        this.consoleWindow = new ConsoleWindow(configuration) { IsOpen = configuration.LogOpenAtStartup };
        this.pluginStatWindow = new PluginStatWindow() { IsOpen = false };
        this.pluginWindow = new PluginInstallerWindow(pluginImageCache, configuration) { IsOpen = false };
        this.settingsWindow = new SettingsWindow() { IsOpen = false };
        this.selfTestWindow = new SelfTestWindow() { IsOpen = false };
        this.styleEditorWindow = new StyleEditorWindow() { IsOpen = false };
        this.titleScreenMenuWindow = new TitleScreenMenuWindow(
            clientState,
            configuration,
            dalamudAssetManager,
            fontAtlasFactory,
            framework,
            gameGui,
            titleScreenMenu,
            consoleManager,
            addonLifecycle) { IsOpen = false };
        this.changelogWindow = new ChangelogWindow(
            this.titleScreenMenuWindow,
            fontAtlasFactory,
            dalamudAssetManager,
            gameGui,
            framework) { IsOpen = false };
        this.profilerWindow = new ProfilerWindow() { IsOpen = false };
        this.branchSwitcherWindow = new BranchSwitcherWindow() { IsOpen = false };
        this.hitchSettingsWindow = new HitchSettingsWindow() { IsOpen = false };

        this.WindowSystem.AddWindow(this.changelogWindow);
        this.WindowSystem.AddWindow(this.colorDemoWindow);
        this.WindowSystem.AddWindow(this.componentDemoWindow);
        this.WindowSystem.AddWindow(this.dataWindow);
        this.WindowSystem.AddWindow(this.gamepadModeNotifierWindow);
        this.WindowSystem.AddWindow(this.consoleWindow);
        this.WindowSystem.AddWindow(this.pluginStatWindow);
        this.WindowSystem.AddWindow(this.pluginWindow);
        this.WindowSystem.AddWindow(this.settingsWindow);
        this.WindowSystem.AddWindow(this.selfTestWindow);
        this.WindowSystem.AddWindow(this.styleEditorWindow);
        this.WindowSystem.AddWindow(this.titleScreenMenuWindow);
        this.WindowSystem.AddWindow(this.profilerWindow);
        this.WindowSystem.AddWindow(this.branchSwitcherWindow);
        this.WindowSystem.AddWindow(this.hitchSettingsWindow);

        this.interfaceManager.ShowAsserts = configuration.ImGuiAssertsEnabledAtStartup ?? false;
        this.isImGuiDrawDevMenu = this.isImGuiDrawDevMenu || configuration.DevBarOpenAtStartup;

        this.interfaceManager.Draw += this.OnDraw;

        Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync().ContinueWith(
            _ =>
            {
                titleScreenMenu.AddEntryCore(
                    Loc.Localize("TSMDalamudPlugins", "Plugin Installer"),
                    new ForwardingSharedImmediateTexture(dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.LogoSmall)),
                    this.OpenPluginInstaller);
                titleScreenMenu.AddEntryCore(
                    Loc.Localize("TSMDalamudSettings", "Dalamud Settings"),
                    new ForwardingSharedImmediateTexture(dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.LogoSmall)),
                    this.OpenSettings);

                titleScreenMenu.AddEntryCore(
                    "Toggle Dev Menu",
                    new ForwardingSharedImmediateTexture(dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.LogoSmall)),
                    () => Service<DalamudInterface>.GetNullable()?.ToggleDevMenu(),
                    VirtualKey.SHIFT);

                if (!configuration.DalamudBetaKind.IsNullOrEmpty())
                {
                    titleScreenMenu.AddEntryCore(
                        Loc.Localize("TSMDalamudDevMenu", "Developer Menu"),
                        new ForwardingSharedImmediateTexture(dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.LogoSmall)),
                        () => this.isImGuiDrawDevMenu = true);
                }
            });

        this.creditsDarkeningAnimation.Point1 = Vector2.Zero;
        this.creditsDarkeningAnimation.Point2 = new Vector2(CreditsDarkeningMaxAlpha);

        // This is temporary, until we know the repercussions of vtable hooking mode
        consoleManager.AddCommand(
            "dalamud.interface.swapchain_mode",
            "Set swapchain hooking mode",
            (string mode) =>
            {
                switch (mode)
                {
                    case "vtable":
                        this.configuration.SwapChainHookMode = SwapChainHelper.HookMode.VTable;
                        break;
                    case "bytecode":
                        this.configuration.SwapChainHookMode = SwapChainHelper.HookMode.ByteCode;
                        break;
                    default:
                        Log.Error("Unknown swapchain mode: {Mode}", mode);
                        return false;
                }

                this.configuration.QueueSave();
                return true;
            });
    }

    private delegate nint CrashDebugDelegate(nint self);

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

    /// <summary>
    /// Gets or sets a value indicating whether the plugin installer is open.
    /// </summary>
    public bool IsPluginInstallerOpen
    {
        get => this.pluginWindow.IsOpen;
        set => this.pluginWindow.IsOpen = value;
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.interfaceManager.Draw -= this.OnDraw;

        this.WindowSystem.Windows.OfType<IDisposable>().AggregateToDisposable().Dispose();
        this.WindowSystem.RemoveAllWindows();

        this.changelogWindow.Dispose();
        this.consoleWindow.Dispose();
        this.pluginWindow.Dispose();
        this.titleScreenMenuWindow.Dispose();
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
    /// Opens the <see cref="ConsoleWindow"/>.
    /// </summary>
    public void OpenLogWindow()
    {
        this.consoleWindow.IsOpen = true;
        this.consoleWindow.BringToFront();
    }

    /// <summary>
    /// Opens the <see cref="PluginStatWindow"/>.
    /// </summary>
    public void OpenPluginStats()
    {
        this.pluginStatWindow.IsOpen = true;
        this.pluginStatWindow.BringToFront();
    }

    /// <summary>
    /// Opens the <see cref="PluginInstallerWindow"/> on the plugin installed.
    /// </summary>
    public void OpenPluginInstaller()
    {
        this.pluginWindow.OpenTo(this.configuration.PluginInstallerOpen);
        this.pluginWindow.BringToFront();
    }

    /// <summary>
    /// Opens the <see cref="PluginInstallerWindow"/> on the specified page.
    /// </summary>
    /// <param name="kind">The page of the installer to open.</param>
    public void OpenPluginInstallerTo(PluginInstallerOpenKind kind)
    {
        this.pluginWindow.OpenTo(kind);
        this.pluginWindow.BringToFront();
    }

    /// <summary>
    /// Opens the <see cref="SettingsWindow"/>.
    /// </summary>
    public void OpenSettings()
    {
        this.settingsWindow.IsOpen = true;
        this.settingsWindow.BringToFront();
    }

    /// <summary>
    /// Opens the <see cref="SettingsWindow"/> on the specified tab.
    /// </summary>
    /// <param name="kind">The tab of the settings to open.</param>
    public void OpenSettingsTo(SettingsOpenKind kind)
    {
        this.settingsWindow.OpenTo(kind);
        this.settingsWindow.BringToFront();
    }

    /// <summary>
    /// Opens the <see cref="SelfTestWindow"/>.
    /// </summary>
    public void OpenSelfTest()
    {
        this.selfTestWindow.IsOpen = true;
        this.selfTestWindow.BringToFront();
    }

    /// <summary>
    /// Opens the <see cref="StyleEditorWindow"/>.
    /// </summary>
    public void OpenStyleEditor()
    {
        this.styleEditorWindow.IsOpen = true;
        this.styleEditorWindow.BringToFront();
    }

    /// <summary>
    /// Opens the <see cref="ProfilerWindow"/>.
    /// </summary>
    public void OpenProfiler()
    {
        this.profilerWindow.IsOpen = true;
        this.profilerWindow.BringToFront();
    }

    /// <summary>
    /// Opens the <see cref="HitchSettingsWindow"/>.
    /// </summary>
    public void OpenHitchSettings()
    {
        this.hitchSettingsWindow.IsOpen = true;
        this.hitchSettingsWindow.BringToFront();
    }

    /// <summary>
    /// Opens the <see cref="BranchSwitcherWindow"/>.
    /// </summary>
    public void OpenBranchSwitcher()
    {
        this.branchSwitcherWindow.IsOpen = true;
        this.branchSwitcherWindow.BringToFront();
    }

    #endregion

    #region Close

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
    /// Toggles the <see cref="DataWindow"/>.
    /// </summary>
    /// <param name="dataKind">The data kind to switch to after opening.</param>
    public void ToggleDataWindow(string? dataKind = null)
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
    /// <param name="kind">The page of the installer to open.</param>
    public void TogglePluginInstallerWindowTo(PluginInstallerOpenKind kind) => this.pluginWindow.ToggleTo(kind);

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

    /// <summary>
    /// Sets the current search text for the plugin installer.
    /// </summary>
    /// <param name="text">The search term.</param>
    public void SetPluginInstallerSearchText(string text)
    {
        this.pluginWindow.SetSearchText(text);
    }

    /// <summary>
    /// Sets the current search text for the settings window.
    /// </summary>
    /// <param name="text">The search term.</param>
    public void SetSettingsSearchText(string text)
    {
        this.settingsWindow.SetSearchText(text);
    }

    /// <summary>
    /// Toggle the screen darkening effect used for the credits.
    /// </summary>
    /// <param name="status">Whether or not to turn the effect on.</param>
    public void SetCreditsDarkeningAnimation(bool status)
    {
        this.isCreditsDarkening = status;

        if (status)
            this.creditsDarkeningAnimation.Restart();
    }

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

            if (this.isCreditsDarkening)
                this.DrawCreditsDarkeningAnimation();

            // Release focus of any ImGui window if we click into the game.
            var io = ImGui.GetIO();
            if (!io.WantCaptureMouse && (User32.GetKeyState((int)User32.VirtualKey.VK_LBUTTON) & 0x8000) != 0)
            {
                ImGui.SetWindowFocus(null);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during OnDraw");
        }
    }

    private void DrawCreditsDarkeningAnimation()
    {
        using var style1 = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 0f);
        using var style2 = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 0f);
        using var color = ImRaii.PushColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0));

        ImGui.SetNextWindowPos(ImGuiHelpers.MainViewport.Pos);
        ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
        ImGuiHelpers.ForceNextWindowMainViewport();

        this.creditsDarkeningAnimation.Update();
        ImGui.SetNextWindowBgAlpha(Math.Min(this.creditsDarkeningAnimation.EasedPoint.X, CreditsDarkeningMaxAlpha));

        ImGui.Begin(
            "###CreditsDarkenWindow",
            ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNav);
        ImGui.End();
    }

    private void DrawHiddenDevMenuOpener()
    {
        var condition = Service<Condition>.Get();

        if (!this.isImGuiDrawDevMenu && !condition.Any())
        {
            using var color = ImRaii.PushColor(ImGuiCol.Button, Vector4.Zero);
            color.Push(ImGuiCol.ButtonActive, Vector4.Zero);
            color.Push(ImGuiCol.ButtonHovered, Vector4.Zero);
            color.Push(ImGuiCol.TextSelectedBg, new Vector4(0, 0, 0, 1));
            color.Push(ImGuiCol.Border, new Vector4(0, 0, 0, 1));
            color.Push(ImGuiCol.BorderShadow, new Vector4(0, 0, 0, 1));
            color.Push(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 1));

            using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            style.Push(ImGuiStyleVar.WindowBorderSize, 0);
            style.Push(ImGuiStyleVar.FrameBorderSize, 0);

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
        }
    }

    private void DrawDevMenu()
    {
        if (this.isImGuiDrawDevMenu)
        {
            if (ImGui.BeginMainMenuBar())
            {
                var pluginManager = Service<PluginManager>.Get();

                if (ImGui.BeginMenu("Dalamud"))
                {
                    ImGui.MenuItem("Draw dev menu", string.Empty, ref this.isImGuiDrawDevMenu);
                    var devBarAtStartup = this.configuration.DevBarOpenAtStartup;
                    if (ImGui.MenuItem("Draw dev menu at startup", string.Empty, ref devBarAtStartup))
                    {
                        this.configuration.DevBarOpenAtStartup ^= true;
                        this.configuration.QueueSave();
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
                                this.configuration.LogLevel = logLevel;
                                this.configuration.QueueSave();
                            }
                        }

                        ImGui.EndMenu();
                    }

                    var logSynchronously = this.configuration.LogSynchronously;
                    if (ImGui.MenuItem("Log Synchronously", null, ref logSynchronously))
                    {
                        this.configuration.LogSynchronously = logSynchronously;
                        this.configuration.QueueSave();

                        EntryPoint.InitLogging(
                            this.dalamud.StartInfo.LogPath!,
                            this.dalamud.StartInfo.BootShowConsole,
                            this.configuration.LogSynchronously,
                            this.dalamud.StartInfo.LogName);
                    }

                    var antiDebug = Service<AntiDebug>.Get();
                    if (ImGui.MenuItem("Disable Debugging Protections", null, antiDebug.IsEnabled))
                    {
                        var newEnabled = !antiDebug.IsEnabled;
                        if (newEnabled)
                            antiDebug.Enable();
                        else
                            antiDebug.Disable();

                        this.configuration.IsAntiAntiDebugEnabled = newEnabled;
                        this.configuration.QueueSave();
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Open Data window"))
                    {
                        this.OpenDataWindow();
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

                    if (ImGui.MenuItem("Open Hitch Settings"))
                    {
                        this.OpenHitchSettings();
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Unload Dalamud"))
                    {
                        Service<Dalamud>.Get().Unload();
                    }

                    if (ImGui.MenuItem("Restart game"))
                    {
                        Dalamud.RestartGame();
                    }

                    if (ImGui.MenuItem("Kill game"))
                    {
                        Process.GetCurrentProcess().Kill();
                    }

                    ImGui.Separator();

                    if (ImGui.BeginMenu("Crash game"))
                    {
                        if (ImGui.MenuItem("Access Violation"))
                        {
                            Marshal.ReadByte(IntPtr.Zero);
                        }

                        if (ImGui.MenuItem("Set UiModule to NULL"))
                        {
                            unsafe
                            {
                                var framework = Framework.Instance();
                                framework->UIModule = (UIModule*)0;
                            }
                        }

                        if (ImGui.MenuItem("Set UiModule to invalid ptr"))
                        {
                            unsafe
                            {
                                var framework = Framework.Instance();
                                framework->UIModule = (UIModule*)0x12345678;
                            }
                        }

                        if (ImGui.MenuItem("Deref nullptr in Hook"))
                        {
                            unsafe
                            {
                                var hook = Hook<CrashDebugDelegate>.FromAddress(
                                    (nint)UIModule.StaticVirtualTablePointer->GetUIInputData,
                                    self =>
                                    {
                                        _ = *(byte*)0;
                                        return (nint)UIModule.Instance()->GetUIInputData();
                                    });
                                hook.Enable();
                            }
                        }

                        if (ImGui.MenuItem("Cause CLR fastfail"))
                        {
                            unsafe void CauseFastFail()
                            {
                                // ReSharper disable once NotAccessedVariable
                                var texture = Unsafe.AsRef<AtkTexture>((void*)0x12345678);
                                texture.TextureType = TextureType.Crest;
                            }

                            Service<Game.Framework>.Get().RunOnTick(CauseFastFail);
                        }

                        if (ImGui.MenuItem("Cause ImGui assert"))
                        {
                            ImGui.PopStyleVar();
                            ImGui.PopStyleVar();
                        }

                        ImGui.EndMenu();
                    }

                    if (ImGui.MenuItem("Report crashes at shutdown", null, this.configuration.ReportShutdownCrashes))
                    {
                        this.configuration.ReportShutdownCrashes = !this.configuration.ReportShutdownCrashes;
                        this.configuration.QueueSave();
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Open Dalamud branch switcher"))
                    {
                        this.OpenBranchSwitcher();
                    }

                    ImGui.MenuItem(this.dalamud.StartInfo.GameVersion?.ToString() ?? "Unknown version", false);
                    ImGui.MenuItem($"D: {Util.GetScmVersion()} CS: {Util.GetGitHashClientStructs()}[{FFXIVClientStructs.ThisAssembly.Git.Commits}]", false);
                    ImGui.MenuItem($"CLR: {Environment.Version}", false);

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("GUI"))
                {
                    ImGui.MenuItem("Use Monospace font for following windows", string.Empty, ref this.isImGuiTestWindowsInMonospace);
                    ImGui.MenuItem("Draw ImGui demo", string.Empty, ref this.isImGuiDrawDemoWindow);
                    ImGui.MenuItem("Draw ImPlot demo", string.Empty, ref this.isImPlotDrawDemoWindow);
                    ImGui.MenuItem("Draw metrics", string.Empty, ref this.isImGuiDrawMetricsWindow);

                    ImGui.Separator();

                    var showAsserts = this.interfaceManager.ShowAsserts;
                    if (ImGui.MenuItem("Enable assert popups", string.Empty, ref showAsserts))
                    {
                        this.interfaceManager.ShowAsserts = showAsserts;
                    }

                    var enableVerboseAsserts = this.interfaceManager.EnableVerboseAssertLogging;
                    if (ImGui.MenuItem("Enable verbose assert logging", string.Empty, ref enableVerboseAsserts))
                    {
                        this.interfaceManager.EnableVerboseAssertLogging = enableVerboseAsserts;
                    }

                    var assertsEnabled = this.configuration.ImGuiAssertsEnabledAtStartup ?? false;
                    if (ImGui.MenuItem("Enable asserts at startup", null, assertsEnabled))
                    {
                        this.configuration.ImGuiAssertsEnabledAtStartup = !assertsEnabled;
                        this.configuration.QueueSave();
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Clear focus"))
                    {
                        ImGui.SetWindowFocus(null);
                    }

                    if (ImGui.MenuItem("Clear stacks"))
                    {
                        this.interfaceManager.ClearStacks();
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
                                var vec2 = (Vector2)propertyInfo.GetValue(style)!;
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

                    if (ImGui.MenuItem("Show dev bar info", null, this.configuration.ShowDevBarInfo))
                    {
                        this.configuration.ShowDevBarInfo = !this.configuration.ShowDevBarInfo;
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Show loading window"))
                    {
                        var dialog = new LoadingDialog();
                        dialog.Show();
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Game"))
                {
                    if (ImGui.MenuItem("Use in-game default ExceptionHandler"))
                    {
                        this.dalamud.UseDefaultExceptionHandler();
                    }

                    if (ImGui.MenuItem("Use in-game debug ExceptionHandler"))
                    {
                        this.dalamud.UseDebugExceptionHandler();
                    }

                    if (ImGui.MenuItem("Disable in-game ExceptionHandler"))
                    {
                        this.dalamud.UseNoExceptionHandler();
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
                            Log.Information($"{plugin.Name}");
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

                    if (pluginManager.SafeMode && ImGui.MenuItem("Disable Safe Mode"))
                    {
                        pluginManager.SafeMode = false;
                    }

                    ImGui.Separator();
                    ImGui.MenuItem("API Level:" + PluginManager.DalamudApiLevel, false);
                    ImGui.MenuItem("Loaded plugins:" + pluginManager.InstalledPlugins.Count(), false);
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Localization"))
                {
                    var localization = Service<Localization>.Get();

                    if (ImGui.MenuItem("Export localizable"))
                    {
                        localization.ExportLocalizable(true);
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

                if (this.configuration.ShowDevBarInfo)
                {
                    ImGui.PushFont(InterfaceManager.MonoFont);

                    ImGui.BeginMenu(Util.GetScmVersion(), false);
                    ImGui.BeginMenu(this.FrameCount.ToString("000000"), false);
                    ImGui.BeginMenu(ImGui.GetIO().Framerate.ToString("000"), false);
                    ImGui.BeginMenu($"W:{Util.FormatBytes(GC.GetTotalMemory(false))}", false);

                    var videoMem = this.interfaceManager.GetD3dMemoryInfo();
                    ImGui.BeginMenu(
                        !videoMem.HasValue ? "V:???" : $"V:{Util.FormatBytes(videoMem.Value.Used)}",
                        false);

                    ImGui.PopFont();
                }

                ImGui.EndMainMenuBar();
            }
        }
    }
}

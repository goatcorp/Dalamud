using System.Linq;
using System.Numerics;

using CheapLoc;
using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Windows.Settings.Tabs;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.Settings;

/// <summary>
/// The window that allows for general configuration of Dalamud itself.
/// </summary>
internal class SettingsWindow : Window
{
    private readonly SettingsTab[] tabs;

    private string searchInput = string.Empty;
    private bool isSearchInputPrefilled = false;

    private SettingsTab setActiveTab = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
    /// </summary>
    public SettingsWindow()
        : base(Loc.Localize("DalamudSettingsHeader", "Dalamud Settings") + "###XlSettings2", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar)
    {
        this.Size = new Vector2(740, 550);
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(752, 610),
            MaximumSize = new Vector2(1780, 940),
        };

        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.tabs =
        [
            new SettingsTabGeneral(),
            new SettingsTabLook(),
            new SettingsTabAutoUpdates(),
            new SettingsTabDtr(),
            new SettingsTabExperimental(),
            new SettingsTabAbout()
        ];
    }

    /// <summary>
    /// Open the settings window to the tab specified by <paramref name="kind"/>.
    /// </summary>
    /// <param name="kind">The tab of the settings window to open.</param>
    public void OpenTo(SettingsOpenKind kind)
    {
        this.IsOpen = true;
        this.SetOpenTab(kind);
    }

    /// <summary>
    /// Sets the current search text and marks it as prefilled.
    /// </summary>
    /// <param name="text">The search term.</param>
    public void SetSearchText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            this.isSearchInputPrefilled = false;
            this.searchInput = string.Empty;
        }
        else
        {
            this.isSearchInputPrefilled = true;
            this.searchInput = text;
        }
    }

    /// <inheritdoc/>
    public override void OnOpen()
    {
        foreach (var settingsTab in this.tabs)
        {
            settingsTab.Load();
        }

        if (!this.isSearchInputPrefilled) this.searchInput = string.Empty;

        base.OnOpen();
    }

    /// <inheritdoc/>
    public override void OnClose()
    {
        var configuration = Service<DalamudConfiguration>.Get();
        var interfaceManager = Service<InterfaceManager>.Get();
        var fontAtlasFactory = Service<FontAtlasFactory>.Get();

        var scaleChanged = !Equals(ImGui.GetIO().FontGlobalScale, configuration.GlobalUiScale);
        var rebuildFont = !Equals(fontAtlasFactory.DefaultFontSpec, configuration.DefaultFontSpec);
        rebuildFont |= scaleChanged;

        ImGui.GetIO().FontGlobalScale = configuration.GlobalUiScale;
        if (scaleChanged)
        {
            Service<InterfaceManager>.Get().InvokeGlobalScaleChanged();
        }

        fontAtlasFactory.DefaultFontSpecOverride = null;

        if (rebuildFont)
        {
            interfaceManager.RebuildFonts();
            Service<InterfaceManager>.Get().InvokeFontChanged();
        }

        foreach (var settingsTab in this.tabs)
        {
            if (settingsTab.IsOpen)
            {
                settingsTab.OnClose();
            }

            settingsTab.IsOpen = false;
        }

        if (this.isSearchInputPrefilled)
        {
            this.isSearchInputPrefilled = false;
            this.searchInput = string.Empty;
        }
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        var windowSize = ImGui.GetWindowSize();

        if (ImGui.BeginTabBar("###settingsTabs"u8))
        {
            if (string.IsNullOrEmpty(this.searchInput))
            {
                foreach (var settingsTab in this.tabs.Where(x => x.IsVisible))
                {
                    var flags = ImGuiTabItemFlags.NoCloseWithMiddleMouseButton;
                    if (this.setActiveTab == settingsTab)
                    {
                        flags |= ImGuiTabItemFlags.SetSelected;
                        this.setActiveTab = null;
                    }

                    using var tab = ImRaii.TabItem(settingsTab.Title, flags);
                    if (tab)
                    {
                        if (!settingsTab.IsOpen)
                        {
                            settingsTab.IsOpen = true;
                            settingsTab.OnOpen();
                        }

                        // Don't add padding for the about tab(credits)
                        {
                            using var padding = ImRaii.PushStyle(
                                ImGuiStyleVar.WindowPadding,
                                new Vector2(2, 2),
                                settingsTab is not SettingsTabAbout);
                            using var borderColor = ImRaii.PushColor(
                                ImGuiCol.Border,
                                ImGui.GetColorU32(ImGuiCol.ChildBg));
                            using var tabChild = ImRaii.Child(
                                $"###settings_scrolling_{settingsTab.Title}",
                                new Vector2(-1, -1),
                                true);
                            if (tabChild)
                                settingsTab.Draw();
                        }

                        settingsTab.PostDraw();
                    }
                    else if (settingsTab.IsOpen)
                    {
                        settingsTab.IsOpen = false;
                        settingsTab.OnClose();
                    }
                }
            }
            else
            {
                if (ImGui.BeginTabItem("Search Results"u8))
                {
                    var any = false;

                    foreach (var settingsTab in this.tabs.Where(x => x.IsVisible))
                    {
                        var eligible = settingsTab.Entries.Where(x => !x.Name.IsNullOrEmpty() && x.Name.ToLowerInvariant().Contains(this.searchInput.ToLowerInvariant())).ToArray();

                        if (!eligible.Any())
                            continue;

                        any = true;

                        ImGui.TextColored(ImGuiColors.DalamudGrey, settingsTab.Title);
                        ImGui.Dummy(new Vector2(5));

                        foreach (var settingsTabEntry in eligible)
                        {
                            settingsTabEntry.Draw();
                            ImGuiHelpers.ScaledDummy(3);
                        }

                        ImGui.Separator();

                        ImGui.Dummy(new Vector2(10));
                    }

                    if (!any)
                        ImGui.TextColored(ImGuiColors.DalamudGrey, "No results found..."u8);

                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }

        ImGui.SetCursorPos(windowSize - ImGuiHelpers.ScaledVector2(70));

        using (var buttonChild = ImRaii.Child("###settingsFinishButton"u8))
        {
            if (buttonChild)
            {
                using var disabled = ImRaii.Disabled(this.tabs.Any(x => x.Entries.Any(y => !y.IsValid)));

                using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 100f))
                {
                    using var font = ImRaii.PushFont(InterfaceManager.IconFont);

                    if (ImGui.Button(FontAwesomeIcon.Save.ToIconString(), new Vector2(40)))
                    {
                        this.Save();

                        if (!ImGui.IsKeyDown(ImGuiKey.ModShift))
                            this.IsOpen = false;
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(!ImGui.IsKeyDown(ImGuiKey.ModShift)
                                         ? Loc.Localize("DalamudSettingsSaveAndExit", "Save changes and close")
                                         : Loc.Localize("DalamudSettingsSave", "Save changes"));
                }
            }
        }

        ImGui.SetCursorPos(new Vector2(windowSize.X - 250, ImGui.GetTextLineHeightWithSpacing() + (ImGui.GetStyle().FramePadding.Y * 2)));
        ImGui.SetNextItemWidth(240);
        ImGui.InputTextWithHint("###searchInput"u8, "Search for settings..."u8, ref this.searchInput, 100);
    }

    private void Save()
    {
        var configuration = Service<DalamudConfiguration>.Get();

        foreach (var settingsTab in this.tabs)
        {
            settingsTab.Save();
        }

        // Apply docking flag
        if (!configuration.IsDocking)
        {
            ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.DockingEnable;
        }
        else
        {
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        }

        // NOTE (Chiv) Toggle gamepad navigation via setting
        if (!configuration.IsGamepadNavigationEnabled)
        {
            ImGui.GetIO().BackendFlags &= ~ImGuiBackendFlags.HasGamepad;
            ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.NavEnableSetMousePos;

            var di = Service<DalamudInterface>.Get();
            di.CloseGamepadModeNotifierWindow();
        }
        else
        {
            ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.HasGamepad;
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableSetMousePos;
        }

        configuration.QueueSave();

        Service<InterfaceManager>.Get().RebuildFonts();
    }

    private void SetOpenTab(SettingsOpenKind kind)
    {
        this.setActiveTab = kind switch
        {
            SettingsOpenKind.General => this.tabs[0],
            SettingsOpenKind.LookAndFeel => this.tabs[1],
            SettingsOpenKind.AutoUpdates => this.tabs[2],
            SettingsOpenKind.ServerInfoBar => this.tabs[3],
            SettingsOpenKind.Experimental => this.tabs[4],
            SettingsOpenKind.About => this.tabs[5],
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }
}

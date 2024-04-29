using System.Linq;
using System.Numerics;

using CheapLoc;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Windows.Settings.Tabs;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Settings;

/// <summary>
/// The window that allows for general configuration of Dalamud itself.
/// </summary>
internal class SettingsWindow : Window
{
    private SettingsTab[]? tabs;

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
        this.tabs ??= new SettingsTab[]
        {
            new SettingsTabGeneral(),
            new SettingsTabLook(),
            new SettingsTabDtr(),
            new SettingsTabExperimental(),
            new SettingsTabAbout(),
        };

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

        var rebuildFont = !Equals(fontAtlasFactory.DefaultFontSpec, configuration.DefaultFontSpec);
        rebuildFont |= !Equals(ImGui.GetIO().FontGlobalScale, configuration.GlobalUiScale);

        ImGui.GetIO().FontGlobalScale = configuration.GlobalUiScale;
        fontAtlasFactory.DefaultFontSpecOverride = null;

        if (rebuildFont)
            interfaceManager.RebuildFonts();

        foreach (var settingsTab in this.tabs)
        {
            if (settingsTab.IsOpen)
                settingsTab.OnClose();

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

        if (ImGui.BeginTabBar("###settingsTabs"))
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

                        if (ImGui.BeginChild($"###settings_scrolling_{settingsTab.Title}", new Vector2(-1, -1), false))
                        {
                            settingsTab.Draw();
                        }

                        ImGui.EndChild();
                        ImGui.EndTabItem();
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
                if (ImGui.BeginTabItem("Search Results"))
                {
                    var any = false;

                    foreach (var settingsTab in this.tabs.Where(x => x.IsVisible))
                    {
                        var eligible = settingsTab.Entries.Where(x => !x.Name.IsNullOrEmpty() && x.Name.ToLower().Contains(this.searchInput.ToLower())).ToArray();

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
                        ImGui.TextColored(ImGuiColors.DalamudGrey, "No results found...");

                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }

        ImGui.SetCursorPos(windowSize - ImGuiHelpers.ScaledVector2(70));

        if (ImGui.BeginChild("###settingsFinishButton"))
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

        ImGui.EndChild();

        ImGui.SetCursorPos(new Vector2(windowSize.X - 250, ImGui.GetTextLineHeightWithSpacing() + (ImGui.GetStyle().FramePadding.Y * 2)));
        ImGui.SetNextItemWidth(240);
        ImGui.InputTextWithHint("###searchInput", "Search for settings...", ref this.searchInput, 100);
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
        switch (kind)
        {
            case SettingsOpenKind.General:
                this.setActiveTab = this.tabs[0];
                break;
            case SettingsOpenKind.LookAndFeel:
                this.setActiveTab = this.tabs[1];
                break;
            case SettingsOpenKind.ServerInfoBar:
                this.setActiveTab = this.tabs[2];
                break;
            case SettingsOpenKind.Experimental:
                this.setActiveTab = this.tabs[3];
                break;
            case SettingsOpenKind.About:
                this.setActiveTab = this.tabs[4];
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Internal;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// The window that allows for general configuration of Dalamud itself.
    /// </summary>
    internal class SettingsWindow : Window
    {
        private const float MinScale = 0.3f;
        private const float MaxScale = 2.0f;

        private readonly string[] languages;
        private readonly string[] locLanguages;
        private int langIndex;

        private XivChatType dalamudMessagesChatType;

        private bool doCfTaskBarFlash;
        private bool doCfChatMessage;

        private float globalUiScale;
        private bool doToggleUiHide;
        private bool doToggleUiHideDuringCutscenes;
        private bool doToggleUiHideDuringGpose;
        private bool doDocking;
        private bool doViewport;
        private bool doGamepad;
        private bool doFocus;

        private List<ThirdPartyRepoSettings> thirdRepoList;
        private bool thirdRepoListChanged;
        private string thirdRepoTempUrl = string.Empty;
        private string thirdRepoAddError = string.Empty;

        private List<DevPluginLocationSettings> devPluginLocations;
        private bool devPluginLocationsChanged;
        private string devPluginTempLocation = string.Empty;
        private string devPluginLocationAddError = string.Empty;

        private bool printPluginsWelcomeMsg;
        private bool autoUpdatePlugins;
        private bool doButtonsSystemMenu;
        private bool disableRmtFiltering;

        #region Experimental

        private bool doPluginTest;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        public SettingsWindow()
            : base(Loc.Localize("DalamudSettingsHeader", "Dalamud Settings") + "###XlSettings2", ImGuiWindowFlags.NoCollapse)
        {
            var configuration = Service<DalamudConfiguration>.Get();

            this.Size = new Vector2(740, 550);
            this.SizeCondition = ImGuiCond.FirstUseEver;

            this.dalamudMessagesChatType = configuration.GeneralChatType;

            this.doCfTaskBarFlash = configuration.DutyFinderTaskbarFlash;
            this.doCfChatMessage = configuration.DutyFinderChatMessage;

            this.globalUiScale = configuration.GlobalUiScale;
            this.doToggleUiHide = configuration.ToggleUiHide;
            this.doToggleUiHideDuringCutscenes = configuration.ToggleUiHideDuringCutscenes;
            this.doToggleUiHideDuringGpose = configuration.ToggleUiHideDuringGpose;

            this.doDocking = configuration.IsDocking;
            this.doViewport = !configuration.IsDisableViewport;
            this.doGamepad = configuration.IsGamepadNavigationEnabled;
            this.doFocus = configuration.IsFocusManagementEnabled;

            this.doPluginTest = configuration.DoPluginTest;
            this.thirdRepoList = configuration.ThirdRepoList.Select(x => x.Clone()).ToList();
            this.devPluginLocations = configuration.DevPluginLoadLocations.Select(x => x.Clone()).ToList();

            this.printPluginsWelcomeMsg = configuration.PrintPluginsWelcomeMsg;
            this.autoUpdatePlugins = configuration.AutoUpdatePlugins;
            this.doButtonsSystemMenu = configuration.DoButtonsSystemMenu;
            this.disableRmtFiltering = configuration.DisableRmtFiltering;

            this.languages = Localization.ApplicableLangCodes.Prepend("en").ToArray();
            try
            {
                if (string.IsNullOrEmpty(configuration.LanguageOverride))
                {
                    var currentUiLang = CultureInfo.CurrentUICulture;

                    if (Localization.ApplicableLangCodes.Any(x => currentUiLang.TwoLetterISOLanguageName == x))
                        this.langIndex = Array.IndexOf(this.languages, currentUiLang.TwoLetterISOLanguageName);
                    else
                        this.langIndex = 0;
                }
                else
                {
                    this.langIndex = Array.IndexOf(this.languages, configuration.LanguageOverride);
                }
            }
            catch (Exception)
            {
                this.langIndex = 0;
            }

            try
            {
                var locLanguagesList = new List<string>();
                string locLanguage;
                foreach (var language in this.languages)
                {
                    if (language != "ko")
                    {
                        locLanguage = CultureInfo.GetCultureInfo(language).NativeName;
                        locLanguagesList.Add(char.ToUpper(locLanguage[0]) + locLanguage[1..]);
                    }
                    else
                    {
                        locLanguagesList.Add("Korean");
                    }
                }

                this.locLanguages = locLanguagesList.ToArray();
            }
            catch (Exception)
            {
                this.locLanguages = this.languages; // Languages not localized, only codes.
            }
        }

        /// <inheritdoc/>
        public override void OnOpen()
        {
            this.thirdRepoListChanged = false;
            this.devPluginLocationsChanged = false;
        }

        /// <inheritdoc/>
        public override void OnClose()
        {
            var configuration = Service<DalamudConfiguration>.Get();

            ImGui.GetIO().FontGlobalScale = configuration.GlobalUiScale;
            this.thirdRepoList = configuration.ThirdRepoList.Select(x => x.Clone()).ToList();
            this.devPluginLocations = configuration.DevPluginLoadLocations.Select(x => x.Clone()).ToList();
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            var windowSize = ImGui.GetWindowSize();
            ImGui.BeginChild("scrolling", new Vector2(windowSize.X - 5 - (5 * ImGuiHelpers.GlobalScale), windowSize.Y - 35 - (35 * ImGuiHelpers.GlobalScale)), false, ImGuiWindowFlags.HorizontalScrollbar);

            if (ImGui.BeginTabBar("SetTabBar"))
            {
                if (ImGui.BeginTabItem(Loc.Localize("DalamudSettingsGeneral", "General")))
                {
                    this.DrawGeneralTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("DalamudSettingsVisual", "Look & Feel")))
                {
                    this.DrawLookAndFeelTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("DalamudSettingsExperimental", "Experimental")))
                {
                    this.DrawExperimentalTab();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.EndChild();

            this.DrawSaveCloseButtons();
        }

        private void DrawGeneralTab()
        {
            ImGui.Text(Loc.Localize("DalamudSettingsLanguage", "Language"));
            ImGui.Combo("##XlLangCombo", ref this.langIndex, this.locLanguages, this.locLanguages.Length);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsLanguageHint", "Select the language Dalamud will be displayed in."));

            ImGuiHelpers.ScaledDummy(5);

            ImGui.Text(Loc.Localize("DalamudSettingsChannel", "General Chat Channel"));
            if (ImGui.BeginCombo("##XlChatTypeCombo", this.dalamudMessagesChatType.ToString()))
            {
                foreach (var type in Enum.GetValues(typeof(XivChatType)).Cast<XivChatType>())
                {
                    if (ImGui.Selectable(type.ToString(), type == this.dalamudMessagesChatType))
                    {
                        this.dalamudMessagesChatType = type;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsChannelHint", "Select the chat channel that is to be used for general Dalamud messages."));

            ImGuiHelpers.ScaledDummy(5);

            ImGui.Checkbox(Loc.Localize("DalamudSettingsFlash", "Flash FFXIV window on duty pop"), ref this.doCfTaskBarFlash);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsFlashHint", "Flash the FFXIV window in your task bar when a duty is ready."));

            ImGui.Checkbox(Loc.Localize("DalamudSettingsDutyFinderMessage", "Chatlog message on duty pop"), ref this.doCfChatMessage);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsDutyFinderMessageHint", "Send a message in FFXIV chat when a duty is ready."));

            ImGui.Checkbox(Loc.Localize("DalamudSettingsPrintPluginsWelcomeMsg", "Display loaded plugins in the welcome message"), ref this.printPluginsWelcomeMsg);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsPrintPluginsWelcomeMsgHint", "Display loaded plugins in FFXIV chat when logging in with a character."));

            ImGui.Checkbox(Loc.Localize("DalamudSettingsAutoUpdatePlugins", "Auto-update plugins"), ref this.autoUpdatePlugins);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsAutoUpdatePluginsMsgHint", "Automatically update plugins when logging in with a character."));

            ImGui.Checkbox(Loc.Localize("DalamudSettingsSystemMenu", "Dalamud buttons in system menu"), ref this.doButtonsSystemMenu);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsSystemMenuMsgHint", "Add buttons for Dalamud plugins and settings to the system menu."));

            ImGui.Checkbox(Loc.Localize("DalamudSettingsDisableRmtFiltering", "Disable RMT Filtering"), ref this.disableRmtFiltering);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsDisableRmtFilteringMsgHint", "Disable dalamud's built-in RMT ad filtering."));
        }

        private void DrawLookAndFeelTab()
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);
            ImGui.Text(Loc.Localize("DalamudSettingsGlobalUiScale", "Global UI Scale"));
            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3);
            if (ImGui.Button("Reset"))
            {
                this.globalUiScale = 1.0f;
                ImGui.GetIO().FontGlobalScale = this.globalUiScale;
            }

            if (ImGui.DragFloat("##DalamudSettingsGlobalUiScaleDrag", ref this.globalUiScale, 0.005f, MinScale, MaxScale, "%.2f"))
                ImGui.GetIO().FontGlobalScale = this.globalUiScale;

            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsGlobalUiScaleHint", "Scale all XIVLauncher UI elements - useful for 4K displays."));

            ImGuiHelpers.ScaledDummy(10, 16);

            if (ImGui.Button(Loc.Localize("DalamudSettingsOpenStyleEditor", "Open Style Editor")))
            {
                Service<DalamudInterface>.Get().OpenStyleEditor();
            }

            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsStyleEditorHint", "Modify the look & feel of Dalamud windows."));

            ImGuiHelpers.ScaledDummy(10, 16);

            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingToggleUiHideOptOutNote", "Plugins may independently opt out of the settings below."));

            ImGui.Checkbox(Loc.Localize("DalamudSettingToggleUiHide", "Hide plugin UI when the game UI is toggled off"), ref this.doToggleUiHide);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingToggleUiHideHint", "Hide any open windows by plugins when toggling the game overlay."));

            ImGui.Checkbox(Loc.Localize("DalamudSettingToggleUiHideDuringCutscenes", "Hide plugin UI during cutscenes"), ref this.doToggleUiHideDuringCutscenes);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingToggleUiHideDuringCutscenesHint", "Hide any open windows by plugins during cutscenes."));

            ImGui.Checkbox(Loc.Localize("DalamudSettingToggleUiHideDuringGpose", "Hide plugin UI while gpose is active"), ref this.doToggleUiHideDuringGpose);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingToggleUiHideDuringGposeHint", "Hide any open windows by plugins while gpose is active."));

            ImGuiHelpers.ScaledDummy(10, 16);

            ImGui.Checkbox(Loc.Localize("DalamudSettingToggleFocusManagement", "Use escape to close Dalamud windows"), ref this.doFocus);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingToggleFocusManagementHint", "This will cause Dalamud windows to behave like in-game windows when pressing escape.\nThey will close one after another until all are closed. May not work for all plugins."));

            ImGui.Checkbox(Loc.Localize("DalamudSettingToggleViewports", "Enable multi-monitor windows"), ref this.doViewport);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingToggleViewportsHint", "This will allow you move plugin windows onto other monitors.\nWill only work in Borderless Window or Windowed mode."));

            ImGui.Checkbox(Loc.Localize("DalamudSettingToggleDocking", "Enable window docking"), ref this.doDocking);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingToggleDockingHint", "This will allow you to fuse and tab plugin windows."));

            ImGui.Checkbox(Loc.Localize("DalamudSettingToggleGamepadNavigation", "Control plugins via gamepad"), ref this.doGamepad);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingToggleGamepadNavigationHint", "This will allow you to toggle between game and plugin navigation via L1+L3.\nToggle the PluginInstaller window via R3 if ImGui navigation is enabled."));
        }

        private void DrawExperimentalTab()
        {
            var configuration = Service<DalamudConfiguration>.Get();
            var pluginManager = Service<PluginManager>.Get();

            #region Plugin testing

            ImGui.Checkbox(Loc.Localize("DalamudSettingsPluginTest", "Get plugin testing builds"), ref this.doPluginTest);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsPluginTestHint", "Receive testing prereleases for plugins."));
            ImGui.TextColored(ImGuiColors.DalamudRed, Loc.Localize("DalamudSettingsPluginTestWarning", "Testing plugins may not have been vetted before being published. Please only enable this if you are aware of the risks."));

            #endregion

            ImGuiHelpers.ScaledDummy(12);

            #region Hidden plugins

            if (ImGui.Button(Loc.Localize("DalamudSettingsClearHidden", "Clear hidden plugins")))
            {
                configuration.HiddenPluginInternalName.Clear();
                pluginManager.RefilterPluginMasters();
            }

            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsClearHiddenHint", "Restore plugins you have previously hidden from the plugin installer."));

            #endregion

            ImGuiHelpers.ScaledDummy(12);

            this.DrawCustomReposSection();

            ImGuiHelpers.ScaledDummy(12);

            this.DrawDevPluginLocationsSection();

            ImGuiHelpers.ScaledDummy(12);
        }

        private void DrawCustomReposSection()
        {
            ImGui.Text(Loc.Localize("DalamudSettingsCustomRepo", "Custom Plugin Repositories"));
            if (this.thirdRepoListChanged)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
                ImGui.SameLine();
                ImGui.Text(Loc.Localize("DalamudSettingsChanged", "(Changed)"));
                ImGui.PopStyleColor();
            }

            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingCustomRepoHint", "Add custom plugin repositories."));
            ImGui.TextColored(ImGuiColors.DalamudRed, Loc.Localize("DalamudSettingCustomRepoWarning", "We cannot take any responsibility for third-party plugins and repositories.\nTake care when installing third-party plugins from untrusted sources."));

            ImGuiHelpers.ScaledDummy(5);

            ImGui.Columns(4);
            ImGui.SetColumnWidth(0, 18 + (5 * ImGuiHelpers.GlobalScale));
            ImGui.SetColumnWidth(1, ImGui.GetWindowContentRegionWidth() - (18 + 16 + 14) - ((5 + 45 + 26) * ImGuiHelpers.GlobalScale));
            ImGui.SetColumnWidth(2, 16 + (45 * ImGuiHelpers.GlobalScale));
            ImGui.SetColumnWidth(3, 14 + (26 * ImGuiHelpers.GlobalScale));

            ImGui.Separator();

            ImGui.Text("#");
            ImGui.NextColumn();
            ImGui.Text("URL");
            ImGui.NextColumn();
            ImGui.Text("Enabled");
            ImGui.NextColumn();
            ImGui.Text(string.Empty);
            ImGui.NextColumn();

            ImGui.Separator();

            ImGui.Text("0");
            ImGui.NextColumn();
            ImGui.Text("XIVLauncher");
            ImGui.NextColumn();
            ImGui.NextColumn();
            ImGui.NextColumn();
            ImGui.Separator();

            ThirdPartyRepoSettings repoToRemove = null;

            var repoNumber = 1;
            foreach (var thirdRepoSetting in this.thirdRepoList)
            {
                var isEnabled = thirdRepoSetting.IsEnabled;

                ImGui.PushID($"thirdRepo_{thirdRepoSetting.Url}");

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(repoNumber.ToString()).X / 2));
                ImGui.Text(repoNumber.ToString());
                ImGui.NextColumn();

                ImGui.SetNextItemWidth(-1);
                var url = thirdRepoSetting.Url;
                if (ImGui.InputText($"##thirdRepoInput", ref url, 65535, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    var contains = this.thirdRepoList.Select(repo => repo.Url).Contains(url);
                    if (thirdRepoSetting.Url == url)
                    {
                        // no change.
                    }
                    else if (contains && thirdRepoSetting.Url != url)
                    {
                        this.thirdRepoAddError = Loc.Localize("DalamudThirdRepoExists", "Repo already exists.");
                        Task.Delay(5000).ContinueWith(t => this.thirdRepoAddError = string.Empty);
                    }
                    else
                    {
                        thirdRepoSetting.Url = url;
                        this.thirdRepoListChanged = url != thirdRepoSetting.Url;
                    }
                }

                ImGui.NextColumn();

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 7 - (12 * ImGuiHelpers.GlobalScale));
                ImGui.Checkbox("##thirdRepoCheck", ref isEnabled);
                ImGui.NextColumn();

                if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
                {
                    repoToRemove = thirdRepoSetting;
                }

                ImGui.PopID();

                ImGui.NextColumn();
                ImGui.Separator();

                thirdRepoSetting.IsEnabled = isEnabled;

                repoNumber++;
            }

            if (repoToRemove != null)
            {
                this.thirdRepoList.Remove(repoToRemove);
                this.thirdRepoListChanged = true;
            }

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(repoNumber.ToString()).X / 2));
            ImGui.Text(repoNumber.ToString());
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##thirdRepoUrlInput", ref this.thirdRepoTempUrl, 300);
            ImGui.NextColumn();
            // Enabled button
            ImGui.NextColumn();
            if (!string.IsNullOrEmpty(this.thirdRepoTempUrl) && ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
            {
                this.thirdRepoTempUrl = this.thirdRepoTempUrl.TrimEnd();
                if (this.thirdRepoList.Any(r => string.Equals(r.Url, this.thirdRepoTempUrl, StringComparison.InvariantCultureIgnoreCase)))
                {
                    this.thirdRepoAddError = Loc.Localize("DalamudThirdRepoExists", "Repo already exists.");
                    Task.Delay(5000).ContinueWith(t => this.thirdRepoAddError = string.Empty);
                }
                else
                {
                    this.thirdRepoList.Add(new ThirdPartyRepoSettings
                    {
                        Url = this.thirdRepoTempUrl,
                        IsEnabled = true,
                    });
                    this.thirdRepoListChanged = true;
                    this.thirdRepoTempUrl = string.Empty;
                }
            }

            ImGui.Columns(1);

            if (!string.IsNullOrEmpty(this.thirdRepoAddError))
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), this.thirdRepoAddError);
            }
        }

        private void DrawDevPluginLocationsSection()
        {
            ImGui.Text(Loc.Localize("DalamudSettingsDevPluginLocation", "Dev Plugin Locations"));
            if (this.devPluginLocationsChanged)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
                ImGui.SameLine();
                ImGui.Text(Loc.Localize("DalamudSettingsChanged", "(Changed)"));
                ImGui.PopStyleColor();
            }

            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsDevPluginLocationsHint", "Add additional dev plugin load locations.\nThese can be either the directory or DLL path."));

            ImGuiHelpers.ScaledDummy(5);

            ImGui.Columns(4);
            ImGui.SetColumnWidth(0, 18 + (5 * ImGuiHelpers.GlobalScale));
            ImGui.SetColumnWidth(1, ImGui.GetWindowContentRegionWidth() - (18 + 16 + 14) - ((5 + 45 + 26) * ImGuiHelpers.GlobalScale));
            ImGui.SetColumnWidth(2, 16 + (45 * ImGuiHelpers.GlobalScale));
            ImGui.SetColumnWidth(3, 14 + (26 * ImGuiHelpers.GlobalScale));

            ImGui.Separator();

            ImGui.Text("#");
            ImGui.NextColumn();
            ImGui.Text("Path");
            ImGui.NextColumn();
            ImGui.Text("Enabled");
            ImGui.NextColumn();
            ImGui.Text(string.Empty);
            ImGui.NextColumn();

            ImGui.Separator();

            DevPluginLocationSettings locationToRemove = null;

            var locNumber = 1;
            foreach (var devPluginLocationSetting in this.devPluginLocations)
            {
                var isEnabled = devPluginLocationSetting.IsEnabled;

                ImGui.PushID($"devPluginLocation_{devPluginLocationSetting.Path}");

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(locNumber.ToString()).X / 2));
                ImGui.Text(locNumber.ToString());
                ImGui.NextColumn();

                ImGui.SetNextItemWidth(-1);
                var path = devPluginLocationSetting.Path;
                if (ImGui.InputText($"##devPluginLocationInput", ref path, 65535, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    var contains = this.devPluginLocations.Select(loc => loc.Path).Contains(path);
                    if (devPluginLocationSetting.Path == path)
                    {
                        // no change.
                    }
                    else if (contains && devPluginLocationSetting.Path != path)
                    {
                        this.devPluginLocationAddError = Loc.Localize("DalamudDevPluginLocationExists", "Location already exists.");
                        Task.Delay(5000).ContinueWith(t => this.devPluginLocationAddError = string.Empty);
                    }
                    else
                    {
                        devPluginLocationSetting.Path = path;
                        this.devPluginLocationsChanged = path != devPluginLocationSetting.Path;
                    }
                }

                ImGui.NextColumn();

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 7 - (12 * ImGuiHelpers.GlobalScale));
                ImGui.Checkbox("##devPluginLocationCheck", ref isEnabled);
                ImGui.NextColumn();

                if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
                {
                    locationToRemove = devPluginLocationSetting;
                }

                ImGui.PopID();

                ImGui.NextColumn();
                ImGui.Separator();

                devPluginLocationSetting.IsEnabled = isEnabled;

                locNumber++;
            }

            if (locationToRemove != null)
            {
                this.devPluginLocations.Remove(locationToRemove);
                this.devPluginLocationsChanged = true;
            }

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(locNumber.ToString()).X / 2));
            ImGui.Text(locNumber.ToString());
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##devPluginLocationInput", ref this.devPluginTempLocation, 300);
            ImGui.NextColumn();
            // Enabled button
            ImGui.NextColumn();
            if (!string.IsNullOrEmpty(this.devPluginTempLocation) && ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
            {
                if (this.devPluginLocations.Any(r => string.Equals(r.Path, this.devPluginTempLocation, StringComparison.InvariantCultureIgnoreCase)))
                {
                    this.devPluginLocationAddError = Loc.Localize("DalamudDevPluginLocationExists", "Location already exists.");
                    Task.Delay(5000).ContinueWith(t => this.devPluginLocationAddError = string.Empty);
                }
                else
                {
                    this.devPluginLocations.Add(new DevPluginLocationSettings
                    {
                        Path = this.devPluginTempLocation,
                        IsEnabled = true,
                    });
                    this.devPluginLocationsChanged = true;
                    this.devPluginTempLocation = string.Empty;
                }
            }

            ImGui.Columns(1);

            if (!string.IsNullOrEmpty(this.devPluginLocationAddError))
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), this.devPluginLocationAddError);
            }
        }

        private void DrawSaveCloseButtons()
        {
            var buttonSave = false;
            var buttonClose = false;

            var pluginManager = Service<PluginManager>.Get();

            if (ImGui.Button(Loc.Localize("Save", "Save")))
                buttonSave = true;

            ImGui.SameLine();

            if (ImGui.Button(Loc.Localize("Close", "Close")))
                buttonClose = true;

            ImGui.SameLine();

            if (ImGui.Button(Loc.Localize("SaveAndClose", "Save and Close")))
                buttonSave = buttonClose = true;

            if (buttonSave)
            {
                this.Save();

                if (this.thirdRepoListChanged)
                {
                    _ = pluginManager.SetPluginReposFromConfigAsync(true);
                    this.thirdRepoListChanged = false;
                }

                if (this.devPluginLocationsChanged)
                {
                    pluginManager.ScanDevPlugins();
                    this.devPluginLocationsChanged = false;
                }
            }

            if (buttonClose)
            {
                this.IsOpen = false;
            }
        }

        private void Save()
        {
            var configuration = Service<DalamudConfiguration>.Get();
            var localization = Service<Localization>.Get();

            localization.SetupWithLangCode(this.languages[this.langIndex]);
            configuration.LanguageOverride = this.languages[this.langIndex];

            configuration.GeneralChatType = this.dalamudMessagesChatType;

            configuration.DutyFinderTaskbarFlash = this.doCfTaskBarFlash;
            configuration.DutyFinderChatMessage = this.doCfChatMessage;

            configuration.GlobalUiScale = this.globalUiScale;
            configuration.ToggleUiHide = this.doToggleUiHide;
            configuration.ToggleUiHideDuringCutscenes = this.doToggleUiHideDuringCutscenes;
            configuration.ToggleUiHideDuringGpose = this.doToggleUiHideDuringGpose;

            configuration.IsDocking = this.doDocking;
            configuration.IsGamepadNavigationEnabled = this.doGamepad;
            configuration.IsFocusManagementEnabled = this.doFocus;

            // This is applied every frame in InterfaceManager::CheckViewportState()
            configuration.IsDisableViewport = !this.doViewport;

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
            }
            else
            {
                ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.HasGamepad;
                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableSetMousePos;
            }

            configuration.DoPluginTest = this.doPluginTest;
            configuration.ThirdRepoList = this.thirdRepoList.Select(x => x.Clone()).ToList();
            configuration.DevPluginLoadLocations = this.devPluginLocations.Select(x => x.Clone()).ToList();

            configuration.PrintPluginsWelcomeMsg = this.printPluginsWelcomeMsg;
            configuration.AutoUpdatePlugins = this.autoUpdatePlugins;
            configuration.DoButtonsSystemMenu = this.doButtonsSystemMenu;
            configuration.DisableRmtFiltering = this.disableRmtFiltering;

            configuration.Save();

            _ = Service<PluginManager>.Get().ReloadPluginMastersAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
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

        private readonly Dalamud dalamud;

        private readonly string[] languages;
        private readonly string[] locLanguages;
        private int langIndex;

        private Vector4 hintTextColor = ImGuiColors.DalamudGrey;
        private Vector4 warnTextColor = ImGuiColors.DalamudRed;

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
        private List<ThirdPartyRepoSettings> thirdRepoList;

        private bool printPluginsWelcomeMsg;
        private bool autoUpdatePlugins;
        private bool doButtonsSystemMenu;

        private string thirdRepoTempUrl = string.Empty;
        private string thirdRepoAddError = string.Empty;

        #region Experimental

        private bool doPluginTest;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud Instance.</param>
        public SettingsWindow(Dalamud dalamud)
            : base(Loc.Localize("DalamudSettingsHeader", "Dalamud Settings") + "###XlSettings2", ImGuiWindowFlags.NoCollapse)
        {
            this.dalamud = dalamud;

            this.Size = new Vector2(740, 550);
            this.SizeCondition = ImGuiCond.FirstUseEver;

            this.dalamudMessagesChatType = this.dalamud.Configuration.GeneralChatType;

            this.doCfTaskBarFlash = this.dalamud.Configuration.DutyFinderTaskbarFlash;
            this.doCfChatMessage = this.dalamud.Configuration.DutyFinderChatMessage;

            this.globalUiScale = this.dalamud.Configuration.GlobalUiScale;
            this.doToggleUiHide = this.dalamud.Configuration.ToggleUiHide;
            this.doToggleUiHideDuringCutscenes = this.dalamud.Configuration.ToggleUiHideDuringCutscenes;
            this.doToggleUiHideDuringGpose = this.dalamud.Configuration.ToggleUiHideDuringGpose;

            this.doDocking = this.dalamud.Configuration.IsDocking;
            this.doViewport = !this.dalamud.Configuration.IsDisableViewport;
            this.doGamepad = this.dalamud.Configuration.IsGamepadNavigationEnabled;

            this.doPluginTest = this.dalamud.Configuration.DoPluginTest;
            this.thirdRepoList = this.dalamud.Configuration.ThirdRepoList.Select(x => x.Clone()).ToList();

            this.printPluginsWelcomeMsg = this.dalamud.Configuration.PrintPluginsWelcomeMsg;
            this.autoUpdatePlugins = this.dalamud.Configuration.AutoUpdatePlugins;
            this.doButtonsSystemMenu = this.dalamud.Configuration.DoButtonsSystemMenu;

            this.languages = Localization.ApplicableLangCodes.Prepend("en").ToArray();
            try
            {
                if (string.IsNullOrEmpty(this.dalamud.Configuration.LanguageOverride))
                {
                    var currentUiLang = CultureInfo.CurrentUICulture;

                    if (Localization.ApplicableLangCodes.Any(x => currentUiLang.TwoLetterISOLanguageName == x))
                        this.langIndex = Array.IndexOf(this.languages, currentUiLang.TwoLetterISOLanguageName);
                    else
                        this.langIndex = 0;
                }
                else
                {
                    this.langIndex = Array.IndexOf(this.languages, this.dalamud.Configuration.LanguageOverride);
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
        }

        /// <inheritdoc/>
        public override void OnClose()
        {
            ImGui.GetIO().FontGlobalScale = this.dalamud.Configuration.GlobalUiScale;

            this.thirdRepoList = this.dalamud.Configuration.ThirdRepoList.Select(x => x.Clone()).ToList();
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
                    ImGui.Text(Loc.Localize("DalamudSettingsLanguage", "Language"));
                    ImGui.Combo("##XlLangCombo", ref this.langIndex, this.locLanguages, this.locLanguages.Length);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsLanguageHint", "Select the language Dalamud will be displayed in."));

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

                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsChannelHint", "Select the chat channel that is to be used for general Dalamud messages."));

                    ImGuiHelpers.ScaledDummy(5);

                    ImGui.Checkbox(Loc.Localize("DalamudSettingsFlash", "Flash FFXIV window on duty pop"), ref this.doCfTaskBarFlash);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsFlashHint", "Flash the FFXIV window in your task bar when a duty is ready."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingsDutyFinderMessage", "Chatlog message on duty pop"), ref this.doCfChatMessage);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsDutyFinderMessageHint", "Send a message in FFXIV chat when a duty is ready."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingsPrintPluginsWelcomeMsg", "Display loaded plugins in the welcome message"), ref this.printPluginsWelcomeMsg);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsPrintPluginsWelcomeMsgHint", "Display loaded plugins in FFXIV chat when logging in with a character."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingsAutoUpdatePlugins", "Auto-update plugins"), ref this.autoUpdatePlugins);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsAutoUpdatePluginsMsgHint", "Automatically update plugins when logging in with a character."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingsSystemMenu", "Dalamud buttons in system menu"), ref this.doButtonsSystemMenu);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsSystemMenuMsgHint", "Add buttons for Dalamud plugins and settings to the system menu."));

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("DalamudSettingsVisual", "Look & Feel")))
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

                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsGlobalUiScaleHint", "Scale all XIVLauncher UI elements - useful for 4K displays."));

                    ImGuiHelpers.ScaledDummy(10, 16);

                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingToggleUiHideOptOutNote", "Plugins may independently opt out of the settings below."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingToggleUiHide", "Hide plugin UI when the game UI is toggled off"), ref this.doToggleUiHide);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingToggleUiHideHint", "Hide any open windows by plugins when toggling the game overlay."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingToggleUiHideDuringCutscenes", "Hide plugin UI during cutscenes"), ref this.doToggleUiHideDuringCutscenes);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingToggleUiHideDuringCutscenesHint", "Hide any open windows by plugins during cutscenes."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingToggleUiHideDuringGpose", "Hide plugin UI while gpose is active"), ref this.doToggleUiHideDuringGpose);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingToggleUiHideDuringGposeHint", "Hide any open windows by plugins while gpose is active."));

                    ImGuiHelpers.ScaledDummy(10, 16);

                    ImGui.Checkbox(Loc.Localize("DalamudSettingToggleViewports", "Enable multi-monitor windows"), ref this.doViewport);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingToggleViewportsHint", "This will allow you move plugin windows onto other monitors.\nWill only work in Borderless Window or Windowed mode."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingToggleDocking", "Enable window docking"), ref this.doDocking);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingToggleDockingHint", "This will allow you to fuse and tab plugin windows."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingToggleGamepadNavigation", "Enable navigation of ImGui windows via gamepad."), ref this.doGamepad);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingToggleGamepadNavigationHint", "This will allow you to toggle between game and ImGui navigation via L1+L3.\nToggle the PluginInstaller window via R3 if ImGui navigation is enabled."));

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("DalamudSettingsExperimental", "Experimental")))
                {
                    ImGui.Checkbox(Loc.Localize("DalamudSettingsPluginTest", "Get plugin testing builds"), ref this.doPluginTest);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsPluginTestHint", "Receive testing prereleases for plugins."));
                    ImGui.TextColored(this.warnTextColor, Loc.Localize("DalamudSettingsPluginTestWarning", "Testing plugins may not have been vetted before being published. Please only enable this if you are aware of the risks."));

                    ImGuiHelpers.ScaledDummy(12);

                    if (ImGui.Button(Loc.Localize("DalamudSettingsClearHidden", "Clear hidden plugins")))
                    {
                        this.dalamud.Configuration.HiddenPluginInternalName.Clear();
                        this.dalamud.PluginManager.RefilterPluginMasters();
                    }

                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsClearHiddenHint", "Restore plugins you have previously hidden from the plugin installer."));

                    ImGuiHelpers.ScaledDummy(12);

                    ImGuiHelpers.ScaledDummy(12);

                    ImGui.Text(Loc.Localize("DalamudSettingsCustomRepo", "Custom Plugin Repositories"));
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingCustomRepoHint", "Add custom plugin repositories."));
                    ImGui.TextColored(this.warnTextColor, Loc.Localize("DalamudSettingCustomRepoWarning", "We cannot take any responsibility for third-party plugins and repositories.\nTake care when installing third-party plugins from untrusted sources."));

                    ImGuiHelpers.ScaledDummy(5);

                    ImGui.Columns(4);
                    ImGui.SetColumnWidth(0, 18 + (5 * ImGuiHelpers.GlobalScale));
                    ImGui.SetColumnWidth(1, ImGui.GetWindowWidth() - (18 + 16 + 14) - ((5 + 45 + 26) * ImGuiHelpers.GlobalScale));
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

                    ThirdPartyRepoSettings toRemove = null;

                    var repoNumber = 1;
                    foreach (var thirdRepoSetting in this.thirdRepoList)
                    {
                        var isEnabled = thirdRepoSetting.IsEnabled;

                        ImGui.PushID($"thirdRepo_{thirdRepoSetting.Url}");

                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(repoNumber.ToString()).X / 2));
                        ImGui.Text(repoNumber.ToString());
                        ImGui.NextColumn();

                        ImGui.TextWrapped(thirdRepoSetting.Url);
                        ImGui.NextColumn();

                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 7 - (12 * ImGuiHelpers.GlobalScale));
                        ImGui.Checkbox("##thirdRepoCheck", ref isEnabled);
                        ImGui.NextColumn();

                        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
                        {
                            toRemove = thirdRepoSetting;
                        }

                        ImGui.NextColumn();
                        ImGui.Separator();

                        thirdRepoSetting.IsEnabled = isEnabled;

                        repoNumber++;
                    }

                    if (toRemove != null)
                    {
                        this.thirdRepoList.Remove(toRemove);
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

                            this.thirdRepoTempUrl = string.Empty;
                        }
                    }

                    ImGui.Columns(1);

                    ImGui.EndTabItem();

                    if (!string.IsNullOrEmpty(this.thirdRepoAddError))
                    {
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), this.thirdRepoAddError);
                    }
                }

                ImGui.EndTabBar();
            }

            ImGui.EndChild();

            if (ImGui.Button(Loc.Localize("Save", "Save")))
            {
                this.Save();
            }

            ImGui.SameLine();

            if (ImGui.Button(Loc.Localize("SaveAndClose", "Save and Close")))
            {
                this.Save();
                this.IsOpen = false;
            }
        }

        private void Save()
        {
            this.dalamud.LocalizationManager.SetupWithLangCode(this.languages[this.langIndex]);
            this.dalamud.Configuration.LanguageOverride = this.languages[this.langIndex];

            this.dalamud.Configuration.GeneralChatType = this.dalamudMessagesChatType;

            this.dalamud.Configuration.DutyFinderTaskbarFlash = this.doCfTaskBarFlash;
            this.dalamud.Configuration.DutyFinderChatMessage = this.doCfChatMessage;

            this.dalamud.Configuration.GlobalUiScale = this.globalUiScale;
            this.dalamud.Configuration.ToggleUiHide = this.doToggleUiHide;
            this.dalamud.Configuration.ToggleUiHideDuringCutscenes = this.doToggleUiHideDuringCutscenes;
            this.dalamud.Configuration.ToggleUiHideDuringGpose = this.doToggleUiHideDuringGpose;

            this.dalamud.Configuration.IsDocking = this.doDocking;
            this.dalamud.Configuration.IsGamepadNavigationEnabled = this.doGamepad;

            // This is applied every frame in InterfaceManager::CheckViewportState()
            this.dalamud.Configuration.IsDisableViewport = !this.doViewport;

            // Apply docking flag
            if (!this.dalamud.Configuration.IsDocking)
            {
                ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.DockingEnable;
            }
            else
            {
                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            }

            // NOTE (Chiv) Toggle gamepad navigation via setting
            if (!this.dalamud.Configuration.IsGamepadNavigationEnabled)
            {
                ImGui.GetIO().BackendFlags &= ~ImGuiBackendFlags.HasGamepad;
                ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.NavEnableSetMousePos;
            }
            else
            {
                ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.HasGamepad;
                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableSetMousePos;
            }

            this.dalamud.Configuration.DoPluginTest = this.doPluginTest;
            this.dalamud.Configuration.ThirdRepoList = this.thirdRepoList.Select(x => x.Clone()).ToList();

            this.dalamud.Configuration.PrintPluginsWelcomeMsg = this.printPluginsWelcomeMsg;
            this.dalamud.Configuration.AutoUpdatePlugins = this.autoUpdatePlugins;
            this.dalamud.Configuration.DoButtonsSystemMenu = this.doButtonsSystemMenu;

            this.dalamud.Configuration.Save();

            this.dalamud.PluginManager.ReloadPluginMasters();
        }
    }
}

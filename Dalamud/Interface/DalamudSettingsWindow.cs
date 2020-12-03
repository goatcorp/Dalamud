using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using CheapLoc;
using Dalamud.Game.Chat;
using ImGuiNET;

namespace Dalamud.Interface
{
    internal class DalamudSettingsWindow {
        private readonly Dalamud dalamud;

        public DalamudSettingsWindow(Dalamud dalamud) {
            this.dalamud = dalamud;

            this.chatTypes = Enum.GetValues(typeof(XivChatType)).Cast<XivChatType>().Select(x => x.ToString()).ToArray();
            this.dalamudMessagesChatType = (int) this.dalamud.Configuration.GeneralChatType;

            this.doCfTaskBarFlash = this.dalamud.Configuration.DutyFinderTaskbarFlash;
            this.doCfChatMessage = this.dalamud.Configuration.DutyFinderChatMessage;

            this.globalUiScale = this.dalamud.Configuration.GlobalUiScale;
            this.doToggleUiHide = this.dalamud.Configuration.ToggleUiHide;
            this.doToggleUiHideDuringCutscenes = this.dalamud.Configuration.ToggleUiHideDuringCutscenes;
            this.doToggleUiHideDuringGpose = this.dalamud.Configuration.ToggleUiHideDuringGpose;

            this.doPluginTest = this.dalamud.Configuration.DoPluginTest;
            this.doDalamudTest = this.dalamud.Configuration.DoDalamudTest;

            this.printPluginsWelcomeMsg = this.dalamud.Configuration.PrintPluginsWelcomeMsg;

            this.languages = Localization.ApplicableLangCodes.Prepend("en").ToArray();
            try {
                if (string.IsNullOrEmpty(this.dalamud.Configuration.LanguageOverride)) {
                    var currentUiLang = CultureInfo.CurrentUICulture;

                    if (Localization.ApplicableLangCodes.Any(x => currentUiLang.TwoLetterISOLanguageName == x))
                        this.langIndex = Array.IndexOf(this.languages, currentUiLang.TwoLetterISOLanguageName);
                    else
                        this.langIndex = 0;
                }
                else {
                    this.langIndex = Array.IndexOf(this.languages, this.dalamud.Configuration.LanguageOverride);
                }
            } catch (Exception) {
                this.langIndex = 0;
            }

            try {
                List<string> locLanguagesList = new List<string>();
                string locLanguage;
                foreach (var language in this.languages) {
                    if (language != "ko") {
                        locLanguage = CultureInfo.GetCultureInfo(language).NativeName;
                        locLanguagesList.Add(char.ToUpper(locLanguage[0]) + locLanguage.Substring(1));
                    } else {
                        locLanguagesList.Add("Korean");
                    }
                }
                this.locLanguages = locLanguagesList.ToArray();
            }
            catch (Exception) {
                this.locLanguages = this.languages; // Languages not localized, only codes.
            }
        }

        private string[] languages;
        private string[] locLanguages;
        private int langIndex;

        private string[] chatTypes;

        private Vector4 hintTextColor = new Vector4(0.70f, 0.70f, 0.70f, 1.00f);

        private int dalamudMessagesChatType;

        private bool doCfTaskBarFlash;
        private bool doCfChatMessage;

        private const float MinScale = 0.3f;
        private const float MaxScale = 2.0f;
        private float globalUiScale;
        private bool doToggleUiHide;
        private bool doToggleUiHideDuringCutscenes;
        private bool doToggleUiHideDuringGpose;

        private bool printPluginsWelcomeMsg;

        #region Experimental

        private bool doPluginTest;
        private bool doDalamudTest;

        #endregion

        public bool Draw() {
            ImGui.SetNextWindowSize(new Vector2(500, 500) * ImGui.GetIO().FontGlobalScale, ImGuiCond.Always);

            var isOpen = true;

            if (!ImGui.Begin(Loc.Localize("DalamudSettingsHeader", "Dalamud Settings") + "###XlSettings", ref isOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize)) {
                ImGui.End();
                return false;
            }

            ImGui.BeginChild("scrolling", new Vector2(499, 430) * ImGui.GetIO().FontGlobalScale, false, ImGuiWindowFlags.HorizontalScrollbar);

            if (ImGui.BeginTabBar("SetTabBar")) {
                if (ImGui.BeginTabItem(Loc.Localize("DalamudSettingsGeneral", "General"))) {
                    ImGui.Text(Loc.Localize("DalamudSettingsLanguage","Language"));
                    ImGui.Combo("##XlLangCombo", ref this.langIndex, this.locLanguages,
                                this.locLanguages.Length);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsLanguageHint", "Select the language Dalamud will be displayed in."));

                    ImGui.Dummy(new Vector2(5f, 5f) * ImGui.GetIO().FontGlobalScale);

                    ImGui.Text(Loc.Localize("DalamudSettingsChannel", "General Chat Channel"));
                    ImGui.Combo("##XlChatTypeCombo", ref this.dalamudMessagesChatType, this.chatTypes,
                                this.chatTypes.Length);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsChannelHint", "Select the chat channel that is to be used for general Dalamud messages."));

                    ImGui.Dummy(new Vector2(5f, 5f) * ImGui.GetIO().FontGlobalScale);

                    ImGui.Checkbox(Loc.Localize("DalamudSettingsFlash", "Flash FFXIV window on duty pop"), ref this.doCfTaskBarFlash);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsFlashHint", "Flash the FFXIV window in your task bar when a duty is ready."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingsDutyFinderMessage", "Chatlog message on duty pop"), ref this.doCfChatMessage);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsDutyFinderMessageHint", "Send a message in FFXIV chat when a duty is ready."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingsPrintPluginsWelcomeMsg", "Display loaded plugins in the welcome message"), ref this.printPluginsWelcomeMsg);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsPrintPluginsWelcomeMsgHint", "Display loaded plugins in FFXIV chat when logging in with a character."));

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("DalamudSettingsVisual", "Look & Feel"))) {
                    ImGui.Text(Loc.Localize("DalamudSettingsGlobalUiScale", "Global UI Scale"));
                    if (ImGui.DragFloat("##DalamudSettingsGlobalUiScaleDrag", ref this.globalUiScale, 0.005f, MinScale, MaxScale, "%.2f"))
                        ImGui.GetIO().FontGlobalScale = this.globalUiScale;

                    ImGui.SameLine();
                    if (ImGui.Button("Reset")) {
                        this.globalUiScale = 1.0f;
                        ImGui.GetIO().FontGlobalScale = this.globalUiScale;
                    }

                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsGlobalUiScaleHint", "Scale all XIVLauncher UI elements - useful for 4K displays."));

                    ImGui.Dummy(new Vector2(10f, 16f) * ImGui.GetIO().FontGlobalScale);

                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingToggleUiHideOptOutNote", "Plugins may independently opt out of the settings below."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingToggleUiHide", "Hide plugin UI when the game UI is toggled off"), ref this.doToggleUiHide);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingToggleUiHideHint", "Hide any open windows by plugins when toggling the game overlay."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingToggleUiHideDuringCutscenes", "Hide plugin UI during cutscenes"), ref this.doToggleUiHideDuringCutscenes);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingToggleUiHideDuringCutscenesHint", "Hide any open windows by plugins during cutscenes."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingToggleUiHideDuringGpose", "Hide plugin UI while gpose is active"), ref this.doToggleUiHideDuringGpose);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingToggleUiHideDuringGposeHint", "Hide any open windows by plugins while gpose is active."));

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("DalamudSettingsExperimental", "Experimental"))) {
                    ImGui.Text(Loc.Localize("DalamudSettingsRestartHint", "You need to restart your game after changing these settings."));

                    ImGui.Dummy(new Vector2(10f, 10f) * ImGui.GetIO().FontGlobalScale);

                    ImGui.Checkbox(Loc.Localize("DalamudSettingsPluginTest", "Get plugin testing builds"), ref this.doPluginTest);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingsPluginTestHint", "Receive testing prereleases for plugins."));

                    ImGui.Checkbox(Loc.Localize("DalamudSettingDalamudTest", "Get Dalamud testing builds"), ref this.doDalamudTest);
                    ImGui.TextColored(this.hintTextColor, Loc.Localize("DalamudSettingDalamudTestHint", "Receive testing prereleases for Dalamud."));

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.EndChild();

            if (!isOpen) {
                ImGui.GetIO().FontGlobalScale = this.dalamud.Configuration.GlobalUiScale;
            }
            if (ImGui.Button(Loc.Localize("Save", "Save"))) {
                Save();
            }
            ImGui.SameLine();
            if (ImGui.Button(Loc.Localize("SaveAndClose", "Save and Close"))) {
                Save();
                isOpen = false;
            }

            ImGui.End();

            return isOpen;
        }

        private void Save() {
            this.dalamud.LocalizationManager.SetupWithLangCode(this.languages[this.langIndex]);
            this.dalamud.Configuration.LanguageOverride = this.languages[this.langIndex];

            this.dalamud.Configuration.GeneralChatType = (XivChatType) this.dalamudMessagesChatType;

            this.dalamud.Configuration.DutyFinderTaskbarFlash = this.doCfTaskBarFlash;
            this.dalamud.Configuration.DutyFinderChatMessage = this.doCfChatMessage;

            this.dalamud.Configuration.GlobalUiScale = this.globalUiScale;
            this.dalamud.Configuration.ToggleUiHide = this.doToggleUiHide;
            this.dalamud.Configuration.ToggleUiHideDuringCutscenes = this.doToggleUiHideDuringCutscenes;
            this.dalamud.Configuration.ToggleUiHideDuringGpose = this.doToggleUiHideDuringGpose;

            this.dalamud.Configuration.DoPluginTest = this.doPluginTest;
            this.dalamud.Configuration.DoDalamudTest = this.doDalamudTest;

            this.dalamud.Configuration.PrintPluginsWelcomeMsg = this.printPluginsWelcomeMsg;

            this.dalamud.Configuration.Save();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Mime;
using System.Numerics;
using CheapLoc;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Plugin;
using ImGuiNET;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Serilog;
using SharpDX.Direct3D11;

namespace Dalamud.Interface
{
    class DalamudSettingsWindow {
        private readonly Dalamud dalamud;

        public DalamudSettingsWindow(Dalamud dalamud) {
            this.dalamud = dalamud;

            this.chatTypes = Enum.GetValues(typeof(XivChatType)).Cast<XivChatType>().Select(x => x.ToString()).ToArray();
            this.dalamudMessagesChatType = (int) this.dalamud.Configuration.GeneralChatType;

            this.doCfTaskBarFlash = this.dalamud.Configuration.DutyFinderTaskbarFlash;

            this.doPluginTest = this.dalamud.Configuration.DoPluginTest;
            this.doDalamudTest = this.dalamud.Configuration.DoDalamudTest;
        }

        private string[] chatTypes;

        private Vector4 hintTextColor = new Vector4(0.70f, 0.70f, 0.70f, 1.00f);

        private int dalamudMessagesChatType;

        private bool doCfTaskBarFlash;

        #region Experimental

        private bool doPluginTest;
        private bool doDalamudTest;

        #endregion

        public bool Draw() {
            ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.Always);

            var isOpen = true;

            if (!ImGui.Begin("XIVLauncher Settings", ref isOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize)) {
                ImGui.End();
                return false;
            }

            ImGui.BeginChild("scrolling", new Vector2(499, 430), false, ImGuiWindowFlags.HorizontalScrollbar);

            if (ImGui.BeginTabBar("SetTabBar")) {
                if (ImGui.BeginTabItem("General")) {
                    ImGui.Text("General Chat Channel");
                    ImGui.Combo("##XlChatTypeCombo", ref this.dalamudMessagesChatType, this.chatTypes,
                                this.chatTypes.Length);
                    ImGui.TextColored(this.hintTextColor, "Select the chat channel that is to be used for general XIVLauncher messages.");

                    ImGui.Dummy(new Vector2(5f, 5f));

                    ImGui.Checkbox("Flash FFXIV window on duty pop", ref this.doCfTaskBarFlash);
                    ImGui.TextColored(this.hintTextColor, "Select, if the FFXIV window should be flashed in your task bar when a duty is ready.");

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Experimental"))
                {
                    ImGui.Text("All of these settings require a restart of the game to take effect.");

                    ImGui.Dummy(new Vector2(10f, 10f));

                    ImGui.Checkbox("Get plugin testing builds", ref this.doPluginTest);
                    ImGui.TextColored(this.hintTextColor, "Check this box to receive testing prereleases for plugins.");

                    ImGui.Checkbox("Get Dalamud testing builds", ref this.doDalamudTest);
                    ImGui.TextColored(this.hintTextColor, "Check this box to receive testing prereleases for Dalamud.");

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.EndChild();

            
            if (ImGui.Button("Save")) {
                Save();
            }
            ImGui.SameLine();
            if (ImGui.Button("Save and Close")) {
                Save();
                isOpen = false;
            }

            ImGui.End();

            return isOpen;
        }

        private void Save() {
            this.dalamud.Configuration.GeneralChatType = (XivChatType) this.dalamudMessagesChatType;

            this.dalamud.Configuration.DutyFinderTaskbarFlash = this.doCfTaskBarFlash;

            this.dalamud.Configuration.DoPluginTest = this.doPluginTest;
            this.dalamud.Configuration.DoDalamudTest = this.doDalamudTest;

            this.dalamud.Configuration.Save();
        }
    }
}

using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Data.Parsing.Layer;

namespace Dalamud.Interface {
    class DalamudChangelogWindow : IDisposable {
        private readonly Dalamud dalamud;
        private string assemblyVersion = Util.AssemblyVersion;

        private const bool WarrantsChangelog = true;
        private const string ChangeLog =
            @"* All plugin windows now hide together with the in-game UI when you toggle it.
You can change this behaviour with /xlsettings under the ""Look&Feel"" tab.
* The ""Item hovering"" feature, which was previously broken due to patch 5.3 is now working again.
* Added some extra infos about the state of the addon to the log, so we can help you better in case you encounter crashes.
* Added this changelog window.";

        public DalamudChangelogWindow(Dalamud dalamud) {
            this.dalamud = dalamud;
        }

        public bool Draw() {
            var doDraw = true;

            if (!WarrantsChangelog)
                return false;

            ImGui.PushID("DalamudChangelogWindow");
            ImGui.Begin("What's new in XIVLauncher?", ref doDraw, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
            
            ImGui.Text($"The in-game addon has been updated to version D{this.assemblyVersion}.");

            ImGui.Dummy(new Vector2(10, 10));

            ImGui.Text("The following changes were introduced:");
            ImGui.Text(ChangeLog);

            ImGui.Dummy(new Vector2(10, 10));

            ImGui.Text("Thank you for using our tools!");

            ImGui.Dummy(new Vector2(10, 10));

            ImGui.PushFont(InterfaceManager.IconFont);

            if (ImGui.Button(FontAwesomeIcon.Download.ToIconString())) 
                this.dalamud.OpenPluginInstaller();

            if (ImGui.IsItemHovered()) {
                ImGui.PopFont();
                ImGui.SetTooltip("Open Plugin Installer");
                ImGui.PushFont(InterfaceManager.IconFont);
            }
            
            ImGui.SameLine();

            if (ImGui.Button(FontAwesomeIcon.LaughBeam.ToIconString())) 
                Process.Start("https://discord.gg/3NMcUV5");

            if (ImGui.IsItemHovered()) {
                ImGui.PopFont();
                ImGui.SetTooltip("Join our Discord server");
                ImGui.PushFont(InterfaceManager.IconFont);
            }

            ImGui.SameLine();

            if (ImGui.Button(FontAwesomeIcon.Globe.ToIconString()))
                Process.Start("https://github.com/goatcorp/FFXIVQuickLauncher");

            if (ImGui.IsItemHovered()) {
                ImGui.PopFont();
                ImGui.SetTooltip("See our GitHub repository");
                ImGui.PushFont(InterfaceManager.IconFont);
            }
                

            ImGui.PopFont();

            ImGui.SameLine();
            ImGui.Dummy(new Vector2(20, 0));
            ImGui.SameLine();

            if (ImGui.Button("Close")) {
                doDraw = false;
            }

            ImGui.End();
            ImGui.PopID();

            return doDraw;
        }

        public void Dispose() {

        }
    }
}

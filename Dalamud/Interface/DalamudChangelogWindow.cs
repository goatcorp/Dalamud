using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Data.Parsing.Layer;

namespace Dalamud.Interface {
    class DalamudChangelogWindow : Window {
        private readonly Dalamud dalamud;
        private string assemblyVersion = Util.AssemblyVersion;

        public const bool WarrantsChangelog = false;
        private const string ChangeLog =
            @"* Various behind-the-scenes changes to improve stability
* Faster startup times

If you note any issues or need help, please make sure to ask on our discord server.";

        public DalamudChangelogWindow(Dalamud dalamud)
            : base("What's new in XIVLauncher?", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize)
        {
            this.dalamud = dalamud;

            this.Namespace = "DalamudChangelogWindow";

            this.IsOpen = WarrantsChangelog;
        }

        public override void Draw() {
            ImGui.Text($"The in-game addon has been updated to version D{this.assemblyVersion}.");

            ImGui.Dummy(new Vector2(10, 10) * ImGui.GetIO().FontGlobalScale);

            ImGui.Text("The following changes were introduced:");
            ImGui.Text(ChangeLog);

            ImGui.Dummy(new Vector2(10, 10) * ImGui.GetIO().FontGlobalScale);

            ImGui.Text("Thank you for using our tools!");

            ImGui.Dummy(new Vector2(10, 10) * ImGui.GetIO().FontGlobalScale);

            ImGui.PushFont(InterfaceManager.IconFont);

            if (ImGui.Button(FontAwesomeIcon.Download.ToIconString())) 
                this.dalamud.DalamudUi.OpenPluginInstaller();

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
            ImGui.Dummy(new Vector2(20, 0) * ImGui.GetIO().FontGlobalScale);
            ImGui.SameLine();

            if (ImGui.Button("Close"))
            {
                this.IsOpen = false;
            }
        }
    }
}

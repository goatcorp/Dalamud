using System.Diagnostics;

using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// For major updates, an in-game Changelog window.
    /// </summary>
    internal sealed class ChangelogWindow : Window
    {
        /// <summary>
        /// Whether the latest update warrants a changelog window.
        /// </summary>
        public const bool WarrantsChangelog = false;

        private const string ChangeLog =
            @"* Various behind-the-scenes changes to improve stability
* Faster startup times

If you note any issues or need help, please make sure to ask on our discord server.";

        private readonly Dalamud dalamud;
        private readonly string assemblyVersion = Util.AssemblyVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangelogWindow"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        public ChangelogWindow(Dalamud dalamud)
            : base("What's new in XIVLauncher?", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize)
        {
            this.dalamud = dalamud;

            this.Namespace = "DalamudChangelogWindow";

            this.IsOpen = WarrantsChangelog;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.Text($"The in-game addon has been updated to version D{this.assemblyVersion}.");

            ImGuiHelpers.ScaledDummy(10);

            ImGui.Text("The following changes were introduced:");
            ImGui.Text(ChangeLog);

            ImGuiHelpers.ScaledDummy(10);

            ImGui.Text("Thank you for using our tools!");

            ImGuiHelpers.ScaledDummy(10);

            ImGui.PushFont(UiBuilder.IconFont);

            if (ImGui.Button(FontAwesomeIcon.Download.ToIconString()))
            {
                this.dalamud.DalamudUi.OpenPluginInstaller();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.PopFont();
                ImGui.SetTooltip("Open Plugin Installer");
                ImGui.PushFont(UiBuilder.IconFont);
            }

            ImGui.SameLine();

            if (ImGui.Button(FontAwesomeIcon.LaughBeam.ToIconString()))
            {
                Process.Start("https://discord.gg/3NMcUV5");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.PopFont();
                ImGui.SetTooltip("Join our Discord server");
                ImGui.PushFont(UiBuilder.IconFont);
            }

            ImGui.SameLine();

            if (ImGui.Button(FontAwesomeIcon.Globe.ToIconString()))
            {
                Process.Start("https://github.com/goatcorp/FFXIVQuickLauncher");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.PopFont();
                ImGui.SetTooltip("See our GitHub repository");
                ImGui.PushFont(UiBuilder.IconFont);
            }

            ImGui.PopFont();

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(20, 0);
            ImGui.SameLine();

            if (ImGui.Button("Close"))
            {
                this.IsOpen = false;
            }
        }
    }
}

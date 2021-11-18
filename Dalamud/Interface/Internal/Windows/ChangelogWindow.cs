using System.Diagnostics;
using System.Numerics;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
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
        public const string WarrantsChangelogForMajorMinor = "6.0.";

        private const string ChangeLog =
            @"This is the biggest update of the in-game addon to date.
We have redone most of the underlying systems, providing you with a better experience playing and browsing for plugins, including better performance, and developers with a better API and more comfortable development environment.

We have also added some new features:
• Redesigned plugin installer, featuring icons, screenshots, and filterable categories
• A new look for Dalamud windows and a style editor that lets you adjust colors & other variables to your liking
• Pressing Escape in-game will now close the focused Dalamud window and keep game windows open, until all windows are closed, to unify behaviour with the game windows (you can disable this in the settings)

If you note any issues or need help, please make sure to ask on our discord server.";

        private const string UpdatePluginsInfo =
            @"• All of your plugins were disabled automatically, due to this update. This is normal.
• Open the plugin installer, then click 'update plugins'. Updated plugins should update and then re-enable themselves.
   => Please keep in mind that not all of your plugins may already be updated for the new version.
   => If some plugins are displayed with a red cross in the 'Installed Plugins' tab, they may not yet be available.";

        private readonly string assemblyVersion = Util.AssemblyVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangelogWindow"/> class.
        /// </summary>
        public ChangelogWindow()
            : base("What's new in XIVLauncher?")
        {
            this.Namespace = "DalamudChangelogWindow";

            this.Size = new Vector2(885, 463);
            this.SizeCondition = ImGuiCond.Appearing;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.Text($"The in-game addon has been updated to version D{this.assemblyVersion}.");

            ImGuiHelpers.ScaledDummy(10);

            ImGui.Text("The following changes were introduced:");
            ImGui.TextWrapped(ChangeLog);

            ImGuiHelpers.ScaledDummy(5);

            ImGui.TextColored(ImGuiColors.DalamudRed, " !!! ATTENTION !!!");

            ImGui.TextWrapped(UpdatePluginsInfo);

            ImGuiHelpers.ScaledDummy(10);

            ImGui.Text("Thank you for using our tools!");

            ImGuiHelpers.ScaledDummy(10);

            ImGui.PushFont(UiBuilder.IconFont);

            if (ImGui.Button(FontAwesomeIcon.Download.ToIconString()))
            {
                Service<DalamudInterface>.Get().OpenPluginInstaller();
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

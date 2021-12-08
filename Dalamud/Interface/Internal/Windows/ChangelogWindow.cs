using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// For major updates, an in-game Changelog window.
    /// </summary>
    internal sealed class ChangelogWindow : Window, IDisposable
    {
        /// <summary>
        /// Whether the latest update warrants a changelog window.
        /// </summary>
        public const string WarrantsChangelogForMajorMinor = "6.2.";

        private const string ChangeLog =
            @"• Internal adjustments to allow plugins to work on the new version of the game

If you note any issues or need help, please make sure to ask on our discord server.
Thanks and have fun with the new expansion!";

        private const string UpdatePluginsInfo =
            @"• All of your plugins were disabled automatically, due to this update. This is normal.
• Open the plugin installer, then click 'update plugins'. Updated plugins should update and then re-enable themselves.
   => Please keep in mind that not all of your plugins may already be updated for the new version.
   => If some plugins are displayed with a red cross in the 'Installed Plugins' tab, they may not yet be available.

While we tested the released plugins considerably with a smaller set of people and believe that they are stable, we cannot guarantee to you that you will not run into crashes.

Considering current queue times, this is why we recommend that for now, you only use a set of plugins that are most essential to you, so you can go on playing the game instead of waiting endlessly.";

        private readonly string assemblyVersion = Util.AssemblyVersion;

        private readonly TextureWrap logoTexture;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangelogWindow"/> class.
        /// </summary>
        public ChangelogWindow()
            : base("What's new in XIVLauncher?", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize)
        {
            this.Namespace = "DalamudChangelogWindow";

            this.Size = new Vector2(885, 463);
            this.SizeCondition = ImGuiCond.Appearing;

            var interfaceManager = Service<InterfaceManager>.Get();
            var dalamud = Service<Dalamud>.Get();

            this.logoTexture =
                interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "logo.png"))!;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.Text($"Dalamud has been updated to version D{this.assemblyVersion}.");

            ImGuiHelpers.ScaledDummy(10);

            ImGui.Text("The following changes were introduced:");

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(0);
            var imgCursor = ImGui.GetCursorPos();

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
                Util.OpenLink("https://discord.gg/3NMcUV5");
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
                Util.OpenLink("https://github.com/goatcorp/FFXIVQuickLauncher");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.PopFont();
                ImGui.SetTooltip("See our GitHub repository");
                ImGui.PushFont(UiBuilder.IconFont);
            }

            ImGui.SameLine();

            if (ImGui.Button(FontAwesomeIcon.Heart.ToIconString()))
            {
                Util.OpenLink("https://goatcorp.github.io/faq/support");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.PopFont();
                ImGui.SetTooltip("Support what we care about");
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

            imgCursor.X += 520;
            imgCursor.Y -= 30;
            ImGui.SetCursorPos(imgCursor);

            ImGui.Image(this.logoTexture.ImGuiHandle, new Vector2(100));
        }

        /// <summary>
        /// Dispose this window.
        /// </summary>
        public void Dispose()
        {
            this.logoTexture.Dispose();
        }
    }
}

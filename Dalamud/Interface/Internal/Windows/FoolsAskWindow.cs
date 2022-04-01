using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Dalamud.Configuration.Internal;
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
    internal sealed class FoolsAskWindow : Window, IDisposable
    {
        private readonly string assemblyVersion = Util.AssemblyVersion;

        private readonly TextureWrap logoTexture;

        /// <summary>
        /// Initializes a new instance of the <see cref="FoolsAskWindow"/> class.
        /// </summary>
        public FoolsAskWindow()
            : base("New in Dalamud!", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.Namespace = "DalamudChangelogWindow";

            var interfaceManager = Service<InterfaceManager>.Get();
            var dalamud = Service<Dalamud>.Get();

            this.logoTexture =
                interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "logo.png"))!;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            this.Size = new Vector2(885, 250);
            this.SizeCondition = ImGuiCond.Always;
            this.Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

            ImGui.TextWrapped("Today, we are proud to announce \"Dalamud: Prepare To Die Edition\".\nIt's a new initiative intended to improve your immersion when playing FFXIV, featuring all new and unintrusive visual and sound effects.\nIt's only available today, so join while you can.");

            ImGuiHelpers.ScaledDummy(10);

            ImGui.TextWrapped("You can choose to opt-in here - thank you for your support!");

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(0);
            var imgCursor = ImGui.GetCursorPos();

            ImGuiHelpers.ScaledDummy(40);

            ImGuiHelpers.ScaledDummy(240);

            ImGui.SameLine();

            var btnSize = new Vector2(140, 40);

            var config = Service<DalamudConfiguration>.Get();

            if (ImGui.Button("No, don't ask again", btnSize))
            {
                config.AskedFools22 = true;
                config.Fools22Newer = false;
                config.Save();

                this.IsOpen = false;
            }

            ImGui.SameLine();

            if (ImGui.Button("Yes!", btnSize))
            {
                config.AskedFools22 = true;
                config.Fools22Newer = true;
                config.Save();

                this.IsOpen = false;
            }

            imgCursor.X += 750;
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

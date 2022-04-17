using System;
using System.Numerics;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// For major updates, an in-game Changelog window.
    /// </summary>
    internal sealed class FallbackFontNoticeWindow : Window, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackFontNoticeWindow"/> class.
        /// </summary>
        public FallbackFontNoticeWindow()
            : base(Title, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus)
        {
            this.Namespace = "FallbackFontNoticeWindow";
            this.RespectCloseHotkey = false;

            this.Size = new Vector2(885, 463);
            this.SizeCondition = ImGuiCond.Appearing;

            var interfaceManager = Service<InterfaceManager>.Get();
            var dalamud = Service<Dalamud>.Get();

            Service<InterfaceManager>.Get().OnFallbackFontModeChange += this.OnFallbackFontModeChange;
        }

        private static string Title => Loc.Localize("FallbackFontNoticeWindowTitle", "Fallback Font Mode Active");

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.Text(Title);
            ImGuiHelpers.ScaledDummy(10);

            ImGui.Text(Loc.Localize("FallbackFontNoticeWindowBody", "The text used by Dalamud and plugins has been made blurry in order to prevent possible crash."));
            ImGuiHelpers.ScaledDummy(10);

            ImGui.Text(Loc.Localize("FallbackFontNoticeWindowSolution1", "* You may attempt to increase the limits on text quality. This may result in a crash."));
            ImGuiHelpers.ScaledDummy(10);
            ImGui.SameLine();
            if (ImGui.Button(Loc.Localize("FallbackFontNoticeWindowOpenDalamudSettings", "Open Dalamud Settings")))
                Service<DalamudInterface>.Get().OpenSettings();
            ImGuiHelpers.ScaledDummy(10);
            ImGui.SameLine();
            ImGui.Text(string.Format(
                Loc.Localize(
                    "FallbackFontNoticeWindowSolution1Instructions",
                    "In \"{0}\" tab, choose a better option for \"{1}\"."),
                Loc.Localize("DalamudSettingsVisual", "Look & Feel"),
                Loc.Localize("DalamudSettingsFontResolutionLevel", "Font resolution level")));

            ImGuiHelpers.ScaledDummy(10);

            ImGui.Text(Loc.Localize("FallbackFontNoticeWindowSolution2", "* You may disable custom fonts, or make fonts smaller, from individual plugin settings."));
            ImGuiHelpers.ScaledDummy(10);
            ImGui.SameLine();
            if (ImGui.Button(Loc.Localize("FallbackFontNoticeWindowOpenDalamudPlugins", "Open Plugin Installer")))
                Service<DalamudInterface>.Get().OpenPluginInstaller();

            ImGuiHelpers.ScaledDummy(10);

            if (ImGui.Button(Loc.Localize("FallbackFontNoticeWindowDoNotShowAgain", "Do not show again")))
            {
                this.IsOpen = false;
                Service<DalamudConfiguration>.Get().DisableFontFallbackNotice = true;
                Service<DalamudConfiguration>.Get().Save();
            }
        }

        /// <summary>
        /// Dispose this window.
        /// </summary>
        public void Dispose()
        {
            Service<InterfaceManager>.Get().OnFallbackFontModeChange -= this.OnFallbackFontModeChange;
        }

        private void OnFallbackFontModeChange(bool mode)
        {
            Log.Verbose("[{0}] OnFallbackFontModeChange called: {1} (disable={2})", this.Namespace, mode, Service<DalamudConfiguration>.Get().DisableFontFallbackNotice);
            if (!mode)
                this.IsOpen = false;
            else if (!Service<DalamudConfiguration>.Get().DisableFontFallbackNotice)
                this.IsOpen = true;
        }
    }
}

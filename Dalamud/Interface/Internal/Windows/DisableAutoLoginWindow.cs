using System;
using System.IO;
using System.Numerics;

using CheapLoc;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Newtonsoft.Json.Linq;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// Window for displaying the 'Disable Auto Login' button.
    /// </summary>
    public class DisableAutoLoginWindow : Window
    {
        private readonly Dalamud dalamud;
        private bool hasCheckedEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisableAutoLoginWindow"/> class.
        /// </summary>
        /// <param name="dalamud">Instance of Dalamud.</param>
        internal DisableAutoLoginWindow(Dalamud dalamud)
            : base("DisableAutoLoginWindow", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBackground)
        {
            this.dalamud = dalamud;

            this.ForceMainWindow = true;

            this.SizeCondition = ImGuiCond.Always;
            this.Size = new Vector2(150, 50) * ImGui.GetIO().FontGlobalScale;

            this.PositionCondition = ImGuiCond.Always;
            this.Position = new Vector2(10 * ImGui.GetIO().FontGlobalScale, ImGui.GetMainViewport().Size.Y - (60 * ImGui.GetIO().FontGlobalScale));
        }

        /// <summary>
        /// Draws the Disable Auto Login button.
        /// </summary>
        /// <exception cref="Exception">Any exception will hide the window.</exception>
        public override void Draw()
        {
            try
            {
                if (this.dalamud.ClientState.Condition.Any()) throw new Exception("Logged in");
                if (this.dalamud.Framework.Gui.GetUiObjectByName("_TitleMenu", 1) == IntPtr.Zero) return;

                if (!this.hasCheckedEnabled)
                {
                    this.hasCheckedEnabled = true;
                    var jObject = this.GetConfig();
                    var autoLoginEnabled = jObject.GetValue("AutologinEnabled")?.Value<bool>();
                    if (!(autoLoginEnabled.HasValue && autoLoginEnabled.Value)) throw new Exception("Auto login is not enabled.");
                }

                this.SizeCondition = ImGuiCond.Always;
                this.Size = new Vector2(150, 50) * ImGui.GetIO().FontGlobalScale;

                this.PositionCondition = ImGuiCond.Always;
                this.Position = new Vector2(10 * ImGui.GetIO().FontGlobalScale, ImGui.GetMainViewport().Size.Y - (60 * ImGui.GetIO().FontGlobalScale));
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.4f, 0.4f, 0.4f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.8f, 0.4f, 0.4f, 0.6f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.4f, 0.4f, 0.5f));
                var clicked = ImGui.Button(Loc.Localize("DalamudDisableAutologin", "Disable Auto Login"), new Vector2(-1));
                ImGui.PopStyleColor(3);
                if (clicked)
                {
                    var c = this.GetConfig();
                    c["AutologinEnabled"] = false;
                    this.SetConfig(c);
                    throw new Exception("Disabled AutoLogin");
                }
            }
            catch
            {
                this.IsOpen = false;
            }
        }

        private string GetConfigFile()
        {
            var mainDir = new FileInfo(this.dalamud.StartInfo.ConfigurationPath).DirectoryName;
            if (string.IsNullOrEmpty(mainDir)) throw new Exception("Could not find config directory.");
            var launcherConfig = new FileInfo(Path.Combine(mainDir, "launcherConfigV3.json"));
            if (!launcherConfig.Exists) throw new Exception("Could not find config file.");
            return launcherConfig.FullName;
        }

        private JObject GetConfig()
        {
            return JObject.Parse(File.ReadAllText(this.GetConfigFile()));
        }

        private void SetConfig(JObject config)
        {
            File.WriteAllText(this.GetConfigFile(), config.ToString());
        }
    }
}

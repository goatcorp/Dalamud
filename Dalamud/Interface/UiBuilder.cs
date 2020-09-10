using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using Dalamud.Game.Internal.Gui;
using ImGuiNET;
using ImGuiScene;
using Serilog;
using SharpDX.Direct3D11;

namespace Dalamud.Interface
{
    /// <summary>
    /// This class represents the Dalamud UI that is drawn on top of the game.
    /// It can be used to draw custom windows and overlays.
    /// </summary>
    public class UiBuilder : IDisposable {
        private readonly string namespaceName;

        /// <summary>
        /// The default Dalamud font based on Noto Sans CJK Medium in 17pt - supporting all game languages and icons.
        /// </summary>
        public static ImFontPtr DefaultFont => InterfaceManager.DefaultFont;
        /// <summary>
        /// The default Dalamud icon font based on FontAwesome 5 Free solid in 17pt.
        /// </summary>
        public static ImFontPtr IconFont => InterfaceManager.IconFont;

        /// <summary>
        /// The delegate that gets called when Dalamud is ready to draw your windows or overlays.
        /// When it is called, you can use static ImGui calls.
        /// </summary>
        public event RawDX11Scene.BuildUIDelegate OnBuildUi;

        /// <summary>
        /// Choose if this plugin should hide its UI automatically when the game's UI is hidden.
        /// </summary>
        public bool DisableAutomaticUiHide { get; set; } = false;

        /// <summary>
        /// Choose if this plugin should hide its UI automatically when the user toggles the UI.
        /// </summary>
        public bool DisableUserUiHide { get; set; } = false;

        /// <summary>
        /// Choose if this plugin should hide its UI automatically during cutscenes.
        /// </summary>
        public bool DisableCutsceneUiHide { get; set; } = false;

        /// <summary>
        /// Choose if this plugin should hide its UI automatically while gpose is active.
        /// </summary>
        public bool DisableGposeUiHide { get; set; } = false;

        private bool CutsceneActive => this.dalamud.ClientState != null &&
                                       this.dalamud.ClientState.Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
                                       this.dalamud.ClientState.Condition[ConditionFlag.WatchingCutscene78];

        private bool GposeActive => this.dalamud.ClientState != null &&
                                    this.dalamud.ClientState.Condition[ConditionFlag.WatchingCutscene];

        private Dalamud dalamud;

#if DEBUG
        internal static bool DoStats { get; set; } = true;
        #else
        internal static bool DoStats { get; set; } = false;
        #endif
        private System.Diagnostics.Stopwatch stopwatch;
        internal long lastDrawTime = -1;
        internal long maxDrawTime = -1;
        internal List<long> drawTimeHistory = new List<long>();

        /// <summary>
        /// Create a new UiBuilder and register it. You do not have to call this manually.
        /// </summary>
        /// <param name="interfaceManager">The interface manager to register on.</param>
        /// <param name="namespaceName">The plugin namespace.</param>
        internal UiBuilder(Dalamud dalamud, string namespaceName) {
            this.namespaceName = namespaceName;

            this.dalamud = dalamud;
            this.dalamud.InterfaceManager.OnDraw += OnDraw;
            this.stopwatch = new System.Diagnostics.Stopwatch();
        }

        /// <summary>
        /// Unregister the UiBuilder. Do not call this in plugin code.
        /// </summary>
        public void Dispose() {
            this.dalamud.InterfaceManager.OnDraw -= OnDraw;
        }

        /// <summary>
        /// Loads an image from the specified file.
        /// </summary>
        /// <param name="filePath">The full filepath to the image.</param>
        /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image()</returns>
        public TextureWrap LoadImage(string filePath) =>
            this.dalamud.InterfaceManager.LoadImage(filePath);

        /// <summary>
        /// Loads an image from a byte stream, such as a png downloaded into memory.
        /// </summary>
        /// <param name="imageData">A byte array containing the raw image data.</param>
        /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image()</returns>
        public TextureWrap LoadImage(byte[] imageData) =>
            this.dalamud.InterfaceManager.LoadImage(imageData);

        /// <summary>
        /// Loads an image from raw unformatted pixel data, with no type or header information.  To load formatted data, use <see cref="LoadImage(byte[])"/>.
        /// </summary>
        /// <param name="imageData">A byte array containing the raw pixel data.</param>
        /// <param name="width">The width of the image contained in <paramref name="imageData"/>.</param>
        /// <param name="height">The height of the image contained in <paramref name="imageData"/>.</param>
        /// <param name="numChannels">The number of channels (bytes per pixel) of the image contained in <paramref name="imageData"/>.  This should usually be 4.</param>
        /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image()</returns>
        public TextureWrap LoadImageRaw(byte[] imageData, int width, int height, int numChannels) =>
            this.dalamud.InterfaceManager.LoadImageRaw(imageData, width, height, numChannels);

        /// <summary>
        /// An event that is called any time ImGui fonts need to be rebuilt.<br/>
        /// Any ImFontPtr objects that you store <strong>can be invalidated</strong> when fonts are rebuilt
        /// (at any time), so you should both reload your custom fonts and restore those
        /// pointers inside this handler.<br/>
        /// <strong>PLEASE remove this handler inside Dispose, or when you no longer need your fonts!</strong>
        /// </summary>
        public Action OnBuildFonts
        {
            get { return this.dalamud.InterfaceManager.OnBuildFonts; }
            set { this.dalamud.InterfaceManager.OnBuildFonts = value; }
        }

        /// <summary>
        /// Call this to queue a rebuild of the font atlas.<br/>
        /// This will invoke any <see cref="OnBuildFonts"/> handlers and ensure that any loaded fonts are
        /// ready to be used on the next UI frame.
        /// </summary>
        public void RebuildFonts()
        {
            Log.Verbose("[FONT] {0} plugin is initiating FONT REBUILD", this.namespaceName);
            this.dalamud.InterfaceManager.RebuildFonts();
        }

        /// <summary>
        /// Event that is fired when the plugin should open its configuration interface.
        /// </summary>
        public EventHandler OnOpenConfigUi;

        private bool hasErrorWindow;

        private void OnDraw() {

            if (this.dalamud.Framework.Gui.GameUiHidden && this.dalamud.Configuration.ToggleUiHide && !(DisableUserUiHide || DisableAutomaticUiHide) ||
                CutsceneActive && this.dalamud.Configuration.ToggleUiHideDuringCutscenes && !(DisableCutsceneUiHide || DisableAutomaticUiHide) ||
                GposeActive && this.dalamud.Configuration.ToggleUiHideDuringGpose && !(DisableGposeUiHide || DisableAutomaticUiHide))
                return;

            ImGui.PushID(this.namespaceName);
            if (DoStats) {
                this.stopwatch.Restart();
            }

            if (this.hasErrorWindow && ImGui.Begin(string.Format("{0} Error", this.namespaceName), ref this.hasErrorWindow,
                                             ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize)) {
                ImGui.Text(string.Format("The plugin {0} ran into an error.\nContact the plugin developer for support.\n\nPlease try restarting your game.", this.namespaceName));
                ImGui.Spacing();

                if (ImGui.Button("OK")) {
                    this.hasErrorWindow = false;
                }

                ImGui.End();
            }

            try {
                OnBuildUi?.Invoke();
            } catch (Exception ex) {
                Log.Error(ex, "[{0}] UiBuilder OnBuildUi caught exception", this.namespaceName);
                OnBuildUi = null;
                OnOpenConfigUi = null;

                this.hasErrorWindow = true;
            }
            
            if (DoStats) {
                this.stopwatch.Stop();
                this.lastDrawTime = this.stopwatch.ElapsedTicks;
                this.maxDrawTime = Math.Max(this.lastDrawTime, this.maxDrawTime);
                this.drawTimeHistory.Add(lastDrawTime);
                while (drawTimeHistory.Count > 100) drawTimeHistory.RemoveAt(0);
            }
            ImGui.PopID();
        }
    }
}

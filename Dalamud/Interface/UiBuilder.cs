using System;
using System.Collections.Generic;
using System.Diagnostics;

using Dalamud.Game.ClientState;
using Dalamud.Interface.Internal;
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
    public sealed class UiBuilder : IDisposable
    {
        private readonly Dalamud dalamud;
        private readonly Stopwatch stopwatch;
        private readonly string namespaceName;

        private bool hasErrorWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="UiBuilder"/> class and registers it.
        /// You do not have to call this manually.
        /// </summary>
        /// <param name="dalamud">The dalamud to register on.</param>
        /// <param name="namespaceName">The plugin namespace.</param>
        internal UiBuilder(Dalamud dalamud, string namespaceName)
        {
            this.dalamud = dalamud;
            this.stopwatch = new Stopwatch();
            this.namespaceName = namespaceName;

            this.dalamud.InterfaceManager.OnDraw += this.OnDraw;
        }

        /// <summary>
        /// The delegate that gets called when Dalamud is ready to draw your windows or overlays.
        /// When it is called, you can use static ImGui calls.
        /// </summary>
        public event RawDX11Scene.BuildUIDelegate OnBuildUi;

        /// <summary>
        /// Event that is fired when the plugin should open its configuration interface.
        /// </summary>
        public event EventHandler OnOpenConfigUi;

        /// <summary>
        /// Gets the default Dalamud font based on Noto Sans CJK Medium in 17pt - supporting all game languages and icons.
        /// </summary>
        public static ImFontPtr DefaultFont => InterfaceManager.DefaultFont;

        /// <summary>
        /// Gets the default Dalamud icon font based on FontAwesome 5 Free solid in 17pt.
        /// </summary>
        public static ImFontPtr IconFont => InterfaceManager.IconFont;

        /// <summary>
        /// Gets the game's active Direct3D device.
        /// </summary>
        public Device Device => this.dalamud.InterfaceManager.Device;

        /// <summary>
        /// Gets the game's main window handle.
        /// </summary>
        public IntPtr WindowHandlePtr => this.dalamud.InterfaceManager.WindowHandlePtr;

        /// <summary>
        /// Gets or sets a value indicating whether this plugin should hide its UI automatically when the game's UI is hidden.
        /// </summary>
        public bool DisableAutomaticUiHide { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this plugin should hide its UI automatically when the user toggles the UI.
        /// </summary>
        public bool DisableUserUiHide { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this plugin should hide its UI automatically during cutscenes.
        /// </summary>
        public bool DisableCutsceneUiHide { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this plugin should hide its UI automatically while gpose is active.
        /// </summary>
        public bool DisableGposeUiHide { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether or not the game's cursor should be overridden with the ImGui cursor.
        /// </summary>
        public bool OverrideGameCursor
        {
            get => this.dalamud.InterfaceManager.OverrideGameCursor;
            set => this.dalamud.InterfaceManager.OverrideGameCursor = value;
        }

        /// <summary>
        /// Gets or sets an action that is called any time ImGui fonts need to be rebuilt.<br/>
        /// Any ImFontPtr objects that you store <strong>can be invalidated</strong> when fonts are rebuilt
        /// (at any time), so you should both reload your custom fonts and restore those
        /// pointers inside this handler.<br/>
        /// <strong>PLEASE remove this handler inside Dispose, or when you no longer need your fonts!</strong>
        /// </summary>
        public Action OnBuildFonts
        {
            get => this.dalamud.InterfaceManager.OnBuildFonts;
            set => this.dalamud.InterfaceManager.OnBuildFonts = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether statistics about UI draw time should be collected.
        /// </summary>
#if DEBUG
        internal static bool DoStats { get; set; } = true;
#else
        internal static bool DoStats { get; set; } = false;
#endif

        /// <summary>
        /// Gets a value indicating whether this UiBuilder has a configuration UI registered.
        /// </summary>
        internal bool HasConfigUi => this.OnOpenConfigUi != null;

        /// <summary>
        /// Gets or sets the time this plugin took to draw on the last frame.
        /// </summary>
        internal long LastDrawTime { get; set; } = -1;

        /// <summary>
        /// Gets or sets the longest amount of time this plugin ever took to draw.
        /// </summary>
        internal long MaxDrawTime { get; set; } = -1;

        /// <summary>
        /// Gets or sets a history of the last draw times, used to calculate an average.
        /// </summary>
        internal List<long> DrawTimeHistory { get; set; } = new List<long>();

        private bool CutsceneActive => this.dalamud.ClientState != null &&
                                       (this.dalamud.ClientState.Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
                                        this.dalamud.ClientState.Condition[ConditionFlag.WatchingCutscene78]);

        private bool GposeActive => this.dalamud.ClientState != null &&
                                    this.dalamud.ClientState.Condition[ConditionFlag.WatchingCutscene];

        /// <summary>
        /// Loads an image from the specified file.
        /// </summary>
        /// <param name="filePath">The full filepath to the image.</param>
        /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image().</returns>
        public TextureWrap LoadImage(string filePath) =>
            this.dalamud.InterfaceManager.LoadImage(filePath);

        /// <summary>
        /// Loads an image from a byte stream, such as a png downloaded into memory.
        /// </summary>
        /// <param name="imageData">A byte array containing the raw image data.</param>
        /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image().</returns>
        public TextureWrap LoadImage(byte[] imageData) =>
            this.dalamud.InterfaceManager.LoadImage(imageData);

        /// <summary>
        /// Loads an image from raw unformatted pixel data, with no type or header information.  To load formatted data, use <see cref="LoadImage(byte[])"/>.
        /// </summary>
        /// <param name="imageData">A byte array containing the raw pixel data.</param>
        /// <param name="width">The width of the image contained in <paramref name="imageData"/>.</param>
        /// <param name="height">The height of the image contained in <paramref name="imageData"/>.</param>
        /// <param name="numChannels">The number of channels (bytes per pixel) of the image contained in <paramref name="imageData"/>.  This should usually be 4.</param>
        /// <returns>A <see cref="TextureWrap"/> object wrapping the created image.  Use <see cref="TextureWrap.ImGuiHandle"/> inside ImGui.Image().</returns>
        public TextureWrap LoadImageRaw(byte[] imageData, int width, int height, int numChannels) =>
            this.dalamud.InterfaceManager.LoadImageRaw(imageData, width, height, numChannels);

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
        /// Unregister the UiBuilder. Do not call this in plugin code.
        /// </summary>
        public void Dispose()
        {
            this.dalamud.InterfaceManager.OnDraw -= this.OnDraw;
        }

        /// <summary>
        /// Open the registered configuration UI, if it exists.
        /// </summary>
        internal void OpenConfigUi()
        {
            this.OnOpenConfigUi?.Invoke(this, null);
        }

        private void OnDraw()
        {
            if ((this.dalamud.Framework.Gui.GameUiHidden && this.dalamud.Configuration.ToggleUiHide && !(this.DisableUserUiHide || this.DisableAutomaticUiHide)) ||
                (this.CutsceneActive && this.dalamud.Configuration.ToggleUiHideDuringCutscenes && !(this.DisableCutsceneUiHide || this.DisableAutomaticUiHide)) ||
                (this.GposeActive && this.dalamud.Configuration.ToggleUiHideDuringGpose && !(this.DisableGposeUiHide || this.DisableAutomaticUiHide)))
                return;

            if (!this.dalamud.InterfaceManager.FontsReady)
                return;

            ImGui.PushID(this.namespaceName);
            if (DoStats)
            {
                this.stopwatch.Restart();
            }

            if (this.hasErrorWindow && ImGui.Begin($"{this.namespaceName} Error", ref this.hasErrorWindow, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize))
            {
                ImGui.Text($"The plugin {this.namespaceName} ran into an error.\nContact the plugin developer for support.\n\nPlease try restarting your game.");
                ImGui.Spacing();

                if (ImGui.Button("OK"))
                {
                    this.hasErrorWindow = false;
                }

                ImGui.End();
            }

            try
            {
                this.OnBuildUi?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[{0}] UiBuilder OnBuildUi caught exception", this.namespaceName);
                this.OnBuildUi = null;
                this.OnOpenConfigUi = null;

                this.hasErrorWindow = true;
            }

            if (DoStats)
            {
                this.stopwatch.Stop();
                this.LastDrawTime = this.stopwatch.ElapsedTicks;
                this.MaxDrawTime = Math.Max(this.LastDrawTime, this.MaxDrawTime);
                this.DrawTimeHistory.Add(this.LastDrawTime);
                while (this.DrawTimeHistory.Count > 100) this.DrawTimeHistory.RemoveAt(0);
            }

            ImGui.PopID();
        }
    }
}

using System.Numerics;

using Dalamud.Configuration.Internal;
using Dalamud.Game.ClientState.Keys;
using ImGuiNET;

namespace Dalamud.Interface.Windowing
{
    /// <summary>
    /// Base class you can use to implement an ImGui window for use with the built-in <see cref="WindowSystem"/>.
    /// </summary>
    public abstract class Window
    {
        private static bool wasEscPressedLastFrame = false;

        private bool internalLastIsOpen = false;
        private bool internalIsOpen = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        /// <param name="name">The name/ID of this window.
        /// If you have multiple windows with the same name, you will need to
        /// append an unique ID to it by specifying it after "###" behind the window title.
        /// </param>
        /// <param name="flags">The <see cref="ImGuiWindowFlags"/> of this window.</param>
        /// <param name="forceMainWindow">Whether or not this window should be limited to the main game window.</param>
        protected Window(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false)
        {
            this.WindowName = name;
            this.Flags = flags;
            this.ForceMainWindow = forceMainWindow;
        }

        /// <summary>
        /// Gets or sets the namespace of the window.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Gets or sets the name of the window.
        /// If you have multiple windows with the same name, you will need to
        /// append an unique ID to it by specifying it after "###" behind the window title.
        /// </summary>
        public string WindowName { get; set; }

        /// <summary>
        /// Gets a value indicating whether the window is focused.
        /// </summary>
        public bool IsFocused { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this window is to be closed with a hotkey, like Escape, and keep game addons open in turn if it is closed.
        /// </summary>
        public bool RespectCloseHotkey { get; set; } = true;

        /// <summary>
        /// Gets or sets the position of this window.
        /// </summary>
        public Vector2? Position { get; set; }

        /// <summary>
        /// Gets or sets the condition that defines when the position of this window is set.
        /// </summary>
        public ImGuiCond PositionCondition { get; set; }

        /// <summary>
        /// Gets or sets the size of the window.
        /// </summary>
        public Vector2? Size { get; set; }

        /// <summary>
        /// Gets or sets the condition that defines when the size of this window is set.
        /// </summary>
        public ImGuiCond SizeCondition { get; set; }

        /// <summary>
        /// Gets or sets the size constraints of the window.
        /// </summary>
        public WindowSizeConstraints? SizeConstraints { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this window is collapsed.
        /// </summary>
        public bool? Collapsed { get; set; }

        /// <summary>
        /// Gets or sets the condition that defines when the collapsed state of this window is set.
        /// </summary>
        public ImGuiCond CollapsedCondition { get; set; }

        /// <summary>
        /// Gets or sets the window flags.
        /// </summary>
        public ImGuiWindowFlags Flags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this ImGui window will be forced to stay inside the main game window.
        /// </summary>
        public bool ForceMainWindow { get; set; }

        /// <summary>
        /// Gets or sets this window's background alpha value.
        /// </summary>
        public float? BgAlpha { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this window will stay open.
        /// </summary>
        public bool IsOpen
        {
            get => this.internalIsOpen;
            set => this.internalIsOpen = value;
        }

        /// <summary>
        /// Toggle window is open state.
        /// </summary>
        public void Toggle()
        {
            this.IsOpen ^= true;
        }

        /// <summary>
        /// Code to be executed before conditionals are applied and the window is drawn.
        /// </summary>
        public virtual void PreDraw()
        {
        }

        /// <summary>
        /// Code to be executed after the window is drawn.
        /// </summary>
        public virtual void PostDraw()
        {
        }

        /// <summary>
        /// Code to be executed every time the window renders.
        /// </summary>
        /// <remarks>
        /// In this method, implement your drawing code.
        /// You do NOT need to ImGui.Begin your window.
        /// </remarks>
        public abstract void Draw();

        /// <summary>
        /// Code to be executed when the window is opened.
        /// </summary>
        public virtual void OnOpen()
        {
        }

        /// <summary>
        /// Code to be executed when the window is closed.
        /// </summary>
        public virtual void OnClose()
        {
        }

        /// <summary>
        /// Code to be executed every frame, even when the window is collapsed.
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// Draw the window via ImGui.
        /// </summary>
        internal void DrawInternal()
        {
            if (!this.IsOpen)
            {
                if (this.internalIsOpen != this.internalLastIsOpen)
                {
                    this.internalLastIsOpen = this.internalIsOpen;
                    this.OnClose();

                    this.IsFocused = false;
                }

                return;
            }

            this.Update();

            var hasNamespace = !string.IsNullOrEmpty(this.Namespace);

            if (hasNamespace)
                ImGui.PushID(this.Namespace);

            this.PreDraw();
            this.ApplyConditionals();

            if (this.ForceMainWindow)
                ImGuiHelpers.ForceNextWindowMainViewport();

            if (this.internalLastIsOpen != this.internalIsOpen && this.internalIsOpen)
            {
                this.internalLastIsOpen = this.internalIsOpen;
                this.OnOpen();
            }

            var wasFocused = this.IsFocused;
            if (wasFocused)
            {
                var style = ImGui.GetStyle();
                var focusedHeaderColor = style.Colors[(int)ImGuiCol.TitleBgActive];
                ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, focusedHeaderColor);
            }

            if (ImGui.Begin(this.WindowName, ref this.internalIsOpen, this.Flags))
            {
                // Draw the actual window contents
                this.Draw();
            }

            if (wasFocused)
            {
                ImGui.PopStyleColor();
            }

            this.IsFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

            var escapeDown = Service<KeyState>.Get()[VirtualKey.ESCAPE];
            var isAllowed = Service<DalamudConfiguration>.Get().IsFocusManagementEnabled;
            if (escapeDown && this.IsFocused && isAllowed && !wasEscPressedLastFrame && this.RespectCloseHotkey)
            {
                this.IsOpen = false;
                wasEscPressedLastFrame = true;
            }
            else if (!escapeDown && wasEscPressedLastFrame)
            {
                wasEscPressedLastFrame = false;
            }

            ImGui.End();

            this.PostDraw();

            if (hasNamespace)
                ImGui.PopID();
        }

        // private void CheckState()
        // {
        //     if (this.internalLastIsOpen != this.internalIsOpen)
        //     {
        //         if (this.internalIsOpen)
        //         {
        //             this.OnOpen();
        //         }
        //         else
        //         {
        //             this.OnClose();
        //         }
        //     }
        // }

        private void ApplyConditionals()
        {
            if (this.Position.HasValue)
            {
                var pos = this.Position.Value;

                if (this.ForceMainWindow)
                    pos += ImGuiHelpers.MainViewport.Pos;

                ImGui.SetNextWindowPos(pos, this.PositionCondition);
            }

            if (this.Size.HasValue)
            {
                ImGui.SetNextWindowSize(this.Size.Value * ImGuiHelpers.GlobalScale, this.SizeCondition);
            }

            if (this.Collapsed.HasValue)
            {
                ImGui.SetNextWindowCollapsed(this.Collapsed.Value, this.CollapsedCondition);
            }

            if (this.SizeConstraints.HasValue)
            {
                ImGui.SetNextWindowSizeConstraints(this.SizeConstraints.Value.MinimumSize * ImGuiHelpers.GlobalScale, this.SizeConstraints.Value.MaximumSize * ImGuiHelpers.GlobalScale);
            }

            if (this.BgAlpha.HasValue)
            {
                ImGui.SetNextWindowBgAlpha(this.BgAlpha.Value);
            }
        }

        /// <summary>
        /// Structure detailing the size constraints of a window.
        /// </summary>
        public struct WindowSizeConstraints
        {
            /// <summary>
            /// Gets or sets the minimum size of the window.
            /// </summary>
            public Vector2 MinimumSize { get; set; }

            /// <summary>
            /// Gets or sets the maximum size of the window.
            /// </summary>
            public Vector2 MaximumSize { get; set; }
        }
    }
}

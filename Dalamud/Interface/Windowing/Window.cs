using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Logging.Internal;

using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using PInvoke;

namespace Dalamud.Interface.Windowing;

/// <summary>
/// Base class you can use to implement an ImGui window for use with the built-in <see cref="WindowSystem"/>.
/// </summary>
public abstract class Window
{
    private static readonly ModuleLog Log = new("WindowSystem");

    private static bool wasEscPressedLastFrame = false;
    
    private bool internalLastIsOpen = false;
    private bool internalIsOpen = false;
    private bool internalIsPinned = false;
    private bool internalIsClickthrough = false;
    private bool didPushInternalAlpha = false;
    private float? internalAlpha = null;
    private bool nextFrameBringToFront = false;

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
    /// Gets or sets a value indicating whether this window should not generate sound effects when opening and closing.
    /// </summary>
    public bool DisableWindowSounds { get; set; } = false;

    /// <summary>
    /// Gets or sets a value representing the sound effect id to be played when the window is opened.
    /// </summary>
    public uint OnOpenSfxId { get; set; } = 23u;

    /// <summary>
    /// Gets or sets a value representing the sound effect id to be played when the window is closed.
    /// </summary>
    public uint OnCloseSfxId { get; set; } = 24u;
    
    /// <summary>
    /// Gets or sets the position of this window.
    /// </summary>
    public Vector2? Position { get; set; }

    /// <summary>
    /// Gets or sets the condition that defines when the position of this window is set.
    /// </summary>
    public ImGuiCond PositionCondition { get; set; }

    /// <summary>
    /// Gets or sets the size of the window. The size provided will be scaled by the global scale.
    /// </summary>
    public Vector2? Size { get; set; }

    /// <summary>
    /// Gets or sets the condition that defines when the size of this window is set.
    /// </summary>
    public ImGuiCond SizeCondition { get; set; }

    /// <summary>
    /// Gets or sets the size constraints of the window. The size constraints provided will be scaled by the global scale.
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
    /// Gets or sets a value indicating whether or not this ImGui window should display a close button in the title bar.
    /// </summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not this window should offer to be pinned via the window's titlebar context menu.
    /// </summary>
    public bool AllowPinning { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not this window should offer to be made click-through via the window's titlebar context menu.
    /// </summary>
    public bool AllowClickthrough { get; set; } = true;

    /// <summary>
    /// Gets or sets a list of available title bar buttons.
    /// 
    /// If <see cref="AllowPinning"/> or <see cref="AllowClickthrough"/> are set to true, and this features is not
    /// disabled globally by the user, an internal title bar button to manage these is added when drawing, but it will
    /// not appear in this collection. If you wish to remove this button, set both of these values to false.
    /// </summary>
    public List<TitleBarButton> TitleBarButtons { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether or not this window will stay open.
    /// </summary>
    public bool IsOpen
    {
        get => this.internalIsOpen;
        set => this.internalIsOpen = value;
    }
    
    private bool CanShowCloseButton => this.ShowCloseButton && !this.internalIsClickthrough;

    /// <summary>
    /// Toggle window is open state.
    /// </summary>
    public void Toggle()
    {
        this.IsOpen ^= true;
    }

    /// <summary>
    /// Bring this window to the front.
    /// </summary>
    public void BringToFront()
    {
        if (!this.IsOpen)
            return;

        this.nextFrameBringToFront = true;
    }

    /// <summary>
    /// Code to always be executed before the open-state of the window is checked.
    /// </summary>
    public virtual void PreOpenCheck()
    {
    }

    /// <summary>
    /// Additional conditions for the window to be drawn, regardless of its open-state.
    /// </summary>
    /// <returns>
    /// True if the window should be drawn, false otherwise.
    /// </returns>
    /// <remarks>
    /// Not being drawn due to failing this condition will not change focus or trigger OnClose.
    /// This is checked before PreDraw, but after Update.
    /// </remarks>
    public virtual bool DrawConditions()
    {
        return true;
    }

    /// <summary>
    /// Code to be executed before conditionals are applied and the window is drawn.
    /// </summary>
    public virtual void PreDraw()
    {
        if (this.internalAlpha.HasValue)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, this.internalAlpha.Value);
            this.didPushInternalAlpha = true;
        }
    }

    /// <summary>
    /// Code to be executed after the window is drawn.
    /// </summary>
    public virtual void PostDraw()
    {
        if (this.didPushInternalAlpha)
        {
            ImGui.PopStyleVar();
            this.didPushInternalAlpha = false;
        }
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
    /// <param name="configuration">Configuration instance used to check if certain window management features should be enabled.</param>
    internal void DrawInternal(DalamudConfiguration? configuration)
    {
        this.PreOpenCheck();

        var doSoundEffects = configuration?.EnablePluginUISoundEffects ?? false;

        if (!this.IsOpen)
        {
            if (this.internalIsOpen != this.internalLastIsOpen)
            {
                this.internalLastIsOpen = this.internalIsOpen;
                this.OnClose();

                this.IsFocused = false;
                
                if (doSoundEffects && !this.DisableWindowSounds) UIModule.PlaySound(this.OnCloseSfxId, 0, 0, 0);
            }

            return;
        }

        this.Update();
        if (!this.DrawConditions())
            return;

        var hasNamespace = !string.IsNullOrEmpty(this.Namespace);

        if (hasNamespace)
            ImGui.PushID(this.Namespace);
        
        if (this.internalLastIsOpen != this.internalIsOpen && this.internalIsOpen)
        {
            this.internalLastIsOpen = this.internalIsOpen;
            this.OnOpen();

            if (doSoundEffects && !this.DisableWindowSounds) UIModule.PlaySound(this.OnOpenSfxId, 0, 0, 0);
        }

        this.PreDraw();
        this.ApplyConditionals();

        if (this.ForceMainWindow)
            ImGuiHelpers.ForceNextWindowMainViewport();

        var wasFocused = this.IsFocused;
        if (wasFocused)
        {
            var style = ImGui.GetStyle();
            var focusedHeaderColor = style.Colors[(int)ImGuiCol.TitleBgActive];
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, focusedHeaderColor);
        }

        if (this.nextFrameBringToFront)
        {
            ImGui.SetNextWindowFocus();
            this.nextFrameBringToFront = false;
        }

        var flags = this.Flags;

        if (this.internalIsPinned || this.internalIsClickthrough)
            flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;

        if (this.internalIsClickthrough)
            flags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoMouseInputs;

        if (this.CanShowCloseButton ? ImGui.Begin(this.WindowName, ref this.internalIsOpen, flags) : ImGui.Begin(this.WindowName, flags))
        {
            // Draw the actual window contents
            try
            {
                this.Draw();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error during Draw(): {this.WindowName}");
            }
        }

        var additionsPopupName = "WindowSystemContextActions";
        var flagsApplicableForTitleBarIcons = !flags.HasFlag(ImGuiWindowFlags.NoDecoration) &&
                                              !flags.HasFlag(ImGuiWindowFlags.NoTitleBar);
        var showAdditions = (this.AllowPinning || this.AllowClickthrough) &&
                            (configuration?.EnablePluginUiAdditionalOptions ?? true) &&
                            flagsApplicableForTitleBarIcons;
        if (showAdditions)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);

            if (ImGui.BeginPopup(additionsPopupName, ImGuiWindowFlags.NoMove))
            {
                var isAvailable = ImGuiHelpers.CheckIsWindowOnMainViewport();
                
                if (!isAvailable)
                    ImGui.BeginDisabled();
                
                if (this.internalIsClickthrough)
                    ImGui.BeginDisabled();

                if (this.AllowPinning)
                {
                    var showAsPinned = this.internalIsPinned || this.internalIsClickthrough;
                    if (ImGui.Checkbox(Loc.Localize("WindowSystemContextActionPin", "Pin Window"), ref showAsPinned))
                        this.internalIsPinned = showAsPinned;
                }

                if (this.internalIsClickthrough)
                    ImGui.EndDisabled();

                if (this.AllowClickthrough)
                    ImGui.Checkbox(Loc.Localize("WindowSystemContextActionClickthrough", "Make clickthrough"), ref this.internalIsClickthrough);

                var alpha = (this.internalAlpha ?? ImGui.GetStyle().Alpha) * 100f;
                if (ImGui.SliderFloat(Loc.Localize("WindowSystemContextActionAlpha", "Opacity"), ref alpha, 20f,
                                      100f))
                {
                    this.internalAlpha = alpha / 100f;
                }

                ImGui.SameLine();
                if (ImGui.Button(Loc.Localize("WindowSystemContextActionReset", "Reset")))
                {
                    this.internalAlpha = null;
                }

                if (isAvailable)
                {
                    ImGui.TextColored(ImGuiColors.DalamudGrey,
                                      Loc.Localize("WindowSystemContextActionClickthroughDisclaimer",
                                                   "Open this menu again to disable clickthrough."));
                    ImGui.TextColored(ImGuiColors.DalamudGrey,
                                      Loc.Localize("WindowSystemContextActionDisclaimer",
                                                   "These options may not work for all plugins at the moment."));
                }
                else
                {
                    ImGui.TextColored(ImGuiColors.DalamudGrey,
                                      Loc.Localize("WindowSystemContextActionViewportDisclaimer",
                                                   "These features are only available if this window is inside the game window."));
                }

                if (!isAvailable)
                    ImGui.EndDisabled();
                
                ImGui.EndPopup();
            }

            ImGui.PopStyleVar();
        }

        var titleBarRect = Vector4.Zero;
        unsafe
        {
            var window = ImGuiNativeAdditions.igGetCurrentWindow();
            ImGuiNativeAdditions.ImGuiWindow_TitleBarRect(&titleBarRect, window);

            var additionsButton = new TitleBarButton
            {
                Icon = FontAwesomeIcon.Bars,
                IconOffset = new Vector2(2.5f, 1),
                Click = _ =>
                {
                    this.internalIsClickthrough = false;
                    ImGui.OpenPopup(additionsPopupName);
                },
                Priority = int.MinValue,
                AvailableClickthrough = true,
            };

            if (flagsApplicableForTitleBarIcons)
            {
                this.DrawTitleBarButtons(window, flags, titleBarRect,
                                         showAdditions
                                             ? this.TitleBarButtons.Append(additionsButton)
                                             : this.TitleBarButtons);
            }
        }

        if (wasFocused)
        {
            ImGui.PopStyleColor();
        }

        this.IsFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

        var isAllowed = configuration?.IsFocusManagementEnabled ?? false;
        if (isAllowed)
        {
            var escapeDown = Service<KeyState>.Get()[VirtualKey.ESCAPE];
            if (escapeDown && this.IsFocused && !wasEscPressedLastFrame && this.RespectCloseHotkey)
            {
                this.IsOpen = false;
                wasEscPressedLastFrame = true;
            }
            else if (!escapeDown && wasEscPressedLastFrame)
            {
                wasEscPressedLastFrame = false;
            }
        }

        ImGui.End();

        this.PostDraw();

        if (hasNamespace)
            ImGui.PopID();
    }

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
        
        // Manually set alpha takes precedence, if devs don't want that, they should turn it off
        if (this.internalAlpha.HasValue)
        {
            ImGui.SetNextWindowBgAlpha(this.internalAlpha.Value);
        }
    }

    private unsafe void DrawTitleBarButtons(void* window, ImGuiWindowFlags flags, Vector4 titleBarRect, IEnumerable<TitleBarButton> buttons)
    {
        ImGui.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), false);
        
        var style = ImGui.GetStyle();
        var fontSize = ImGui.GetFontSize();
        var drawList = ImGui.GetWindowDrawList();
        
        var padR = 0f;
        var buttonSize = ImGui.GetFontSize();

        var numNativeButtons = 0;
        if (this.CanShowCloseButton)
            numNativeButtons++;
        
        if (!flags.HasFlag(ImGuiWindowFlags.NoCollapse) && style.WindowMenuButtonPosition == ImGuiDir.Right)
            numNativeButtons++;

        // If there are no native buttons, pad from the right to make some space
        if (numNativeButtons == 0)
            padR += style.FramePadding.X;

        // Pad to the left, to get out of the way of the native buttons
        padR += numNativeButtons * (buttonSize + style.ItemInnerSpacing.X);
        
        Vector2 GetCenter(Vector4 rect) => new((rect.X + rect.Z) * 0.5f, (rect.Y + rect.W) * 0.5f); 

        var numButtons = 0;
        bool DrawButton(TitleBarButton button, Vector2 pos)
        {
            var id = ImGui.GetID($"###CustomTbButton{numButtons}");
            numButtons++;
            
            var min = pos;
            var max = pos + new Vector2(fontSize, fontSize);
            Vector4 bb = new(min.X, min.Y, max.X, max.Y);
            var isClipped = !ImGuiNativeAdditions.igItemAdd(bb, id, null, 0);
            bool hovered, held;
            var pressed = false;

            if (this.internalIsClickthrough)
            {
                hovered = false;
                held = false;
                
                // ButtonBehavior does not function if the window is clickthrough, so we have to do it ourselves
                if (ImGui.IsMouseHoveringRect(min, max))
                {
                    hovered = true;
                    
                    // We can't use ImGui native functions here, because they don't work with clickthrough
                    if ((User32.GetKeyState((int)VirtualKey.LBUTTON) & 0x8000) != 0)
                    {
                        held = true;
                        pressed = true;
                    }
                }
            }
            else
            {
                pressed = ImGuiNativeAdditions.igButtonBehavior(bb, id, &hovered, &held, ImGuiButtonFlags.None);
            }
                
            if (isClipped)
                return pressed;

            // Render
            var bgCol = ImGui.GetColorU32((held && hovered) ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button);
            var textCol = ImGui.GetColorU32(ImGuiCol.Text);
            if (hovered || held)
                drawList.AddCircleFilled(GetCenter(bb) + new Vector2(0.0f, -0.5f), (fontSize * 0.5f) + 1.0f, bgCol);
            
            var offset = button.IconOffset * ImGuiHelpers.GlobalScale;
            drawList.AddText(InterfaceManager.IconFont, (float)(fontSize * 0.8),  new Vector2(bb.X + offset.X, bb.Y + offset.Y), textCol, button.Icon.ToIconString());
            
            if (hovered)
                button.ShowTooltip?.Invoke();

            // Switch to moving the window after mouse is moved beyond the initial drag threshold
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left) && !this.internalIsClickthrough)
                ImGuiNativeAdditions.igStartMouseMovingWindow(window);

            return pressed;
        }

        foreach (var button in buttons.OrderBy(x => x.Priority))
        {
            if (this.internalIsClickthrough && !button.AvailableClickthrough)
                return;
            
            Vector2 position = new(titleBarRect.Z - padR - buttonSize, titleBarRect.Y + style.FramePadding.Y);
            padR += buttonSize + style.ItemInnerSpacing.X;
            
            if (DrawButton(button, position))
                button.Click?.Invoke(ImGuiMouseButton.Left);
        }
        
        ImGui.PopClipRect();
    }

    /// <summary>
    /// Structure detailing the size constraints of a window.
    /// </summary>
    public struct WindowSizeConstraints
    {
        private Vector2 internalMaxSize = new(float.MaxValue);
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowSizeConstraints"/> struct.
        /// </summary>
        public WindowSizeConstraints()
        {
        }

        /// <summary>
        /// Gets or sets the minimum size of the window.
        /// </summary>
        public Vector2 MinimumSize { get; set; } = new(0);
        
        /// <summary>
        /// Gets or sets the maximum size of the window.
        /// </summary>
        public Vector2 MaximumSize
        {
            get => this.GetSafeMaxSize();
            set => this.internalMaxSize = value;
        }
        
        private Vector2 GetSafeMaxSize()
        {
            var currentMin = this.MinimumSize;

            if (this.internalMaxSize.X < currentMin.X || this.internalMaxSize.Y < currentMin.Y) 
                return new Vector2(float.MaxValue);

            return this.internalMaxSize;
        }
    }

    /// <summary>
    /// Structure describing a title bar button.
    /// </summary>
    public class TitleBarButton
    {
        /// <summary>
        /// Gets or sets the icon of the button.
        /// </summary>
        public FontAwesomeIcon Icon { get; set; }
        
        /// <summary>
        /// Gets or sets a vector by which the position of the icon within the button shall be offset.
        /// Automatically scaled by the global font scale for you.
        /// </summary>
        public Vector2 IconOffset { get; set; }
        
        /// <summary>
        /// Gets or sets an action that is called when a tooltip shall be drawn.
        /// May be null if no tooltip shall be drawn.
        /// </summary>
        public Action? ShowTooltip { get; set; }
        
        /// <summary>
        /// Gets or sets an action that is called when the button is clicked.
        /// </summary>
        public Action<ImGuiMouseButton> Click { get; set; }
        
        /// <summary>
        /// Gets or sets the priority the button shall be shown in.
        /// Lower = closer to ImGui default buttons.
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether or not the button shall be clickable
        /// when the respective window is set to clickthrough.
        /// </summary>
        public bool AvailableClickthrough { get; set; }
    }
    
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "imports")]
    private static unsafe class ImGuiNativeAdditions
    {
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool igItemAdd(Vector4 bb, uint id, Vector4* navBb, uint flags);
        
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool igButtonBehavior(Vector4 bb, uint id, bool* outHovered, bool* outHeld, ImGuiButtonFlags flags);
        
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void* igGetCurrentWindow();
        
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igStartMouseMovingWindow(void* window);
    
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGuiWindow_TitleBarRect(Vector4* pOut, void* window);
    }
}

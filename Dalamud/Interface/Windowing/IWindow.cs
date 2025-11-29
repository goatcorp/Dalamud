using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Windowing;

/// <summary>
/// Represents a ImGui window for use with the built-in <see cref="WindowSystem"/>.
/// </summary>
public interface IWindow
{
    /// <summary>
    /// Gets or sets the namespace of the window.
    /// </summary>
    string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the name of the window.
    /// If you have multiple windows with the same name, you will need to
    /// append an unique ID to it by specifying it after "###" behind the window title.
    /// </summary>
    string WindowName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the window is focused.
    /// </summary>
    bool IsFocused { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this window is to be closed with a hotkey, like Escape, and keep game addons open in turn if it is closed.
    /// </summary>
    bool RespectCloseHotkey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this window should not generate sound effects when opening and closing.
    /// </summary>
    bool DisableWindowSounds { get; set; }

    /// <summary>
    /// Gets or sets a value representing the sound effect id to be played when the window is opened.
    /// </summary>
    uint OnOpenSfxId { get; set; }

    /// <summary>
    /// Gets or sets a value representing the sound effect id to be played when the window is closed.
    /// </summary>
    uint OnCloseSfxId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this window should not fade in and out, regardless of the users'
    /// preference.
    /// </summary>
    bool DisableFadeInFadeOut { get; set; }

    /// <summary>
    /// Gets or sets the position of this window.
    /// </summary>
    Vector2? Position { get; set; }

    /// <summary>
    /// Gets or sets the condition that defines when the position of this window is set.
    /// </summary>
    ImGuiCond PositionCondition { get; set; }

    /// <summary>
    /// Gets or sets the size of the window. The size provided will be scaled by the global scale.
    /// </summary>
    Vector2? Size { get; set; }

    /// <summary>
    /// Gets or sets the condition that defines when the size of this window is set.
    /// </summary>
    ImGuiCond SizeCondition { get; set; }

    /// <summary>
    /// Gets or sets the size constraints of the window. The size constraints provided will be scaled by the global scale.
    /// </summary>
    WindowSizeConstraints? SizeConstraints { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this window is collapsed.
    /// </summary>
    bool? Collapsed { get; set; }

    /// <summary>
    /// Gets or sets the condition that defines when the collapsed state of this window is set.
    /// </summary>
    ImGuiCond CollapsedCondition { get; set; }

    /// <summary>
    /// Gets or sets the window flags.
    /// </summary>
    ImGuiWindowFlags Flags { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this ImGui window will be forced to stay inside the main game window.
    /// </summary>
    bool ForceMainWindow { get; set; }

    /// <summary>
    /// Gets or sets this window's background alpha value.
    /// </summary>
    float? BgAlpha { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this ImGui window should display a close button in the title bar.
    /// </summary>
    bool ShowCloseButton { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this window should offer to be pinned via the window's titlebar context menu.
    /// </summary>
    bool AllowPinning { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this window should offer to be made click-through via the window's titlebar context menu.
    /// </summary>
    bool AllowClickthrough { get; set; }

    /// <summary>
    /// Gets a value indicating whether this window is pinned.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Gets a value indicating whether this window is click-through.
    /// </summary>
    public bool IsClickthrough { get; set; }

    /// <summary>
    /// Gets or sets a list of available title bar buttons.
    ///
    /// If <see cref="AllowPinning"/> or <see cref="AllowClickthrough"/> are set to true, and this features is not
    /// disabled globally by the user, an internal title bar button to manage these is added when drawing, but it will
    /// not appear in this collection. If you wish to remove this button, set both of these values to false.
    /// </summary>
    List<TitleBarButton> TitleBarButtons { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this window will stay open.
    /// </summary>
    bool IsOpen { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this window will request focus from the window system next frame.
    /// </summary>
    public bool RequestFocus { get; set; }

    /// <summary>
    /// Toggle window is open state.
    /// </summary>
    void Toggle();

    /// <summary>
    /// Bring this window to the front.
    /// </summary>
    void BringToFront();

    /// <summary>
    /// Code to always be executed before the open-state of the window is checked.
    /// </summary>
    void PreOpenCheck();

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
    bool DrawConditions();

    /// <summary>
    /// Code to be executed before conditionals are applied and the window is drawn.
    /// </summary>
    void PreDraw();

    /// <summary>
    /// Code to be executed after the window is drawn.
    /// </summary>
    void PostDraw();

    /// <summary>
    /// Code to be executed every time the window renders.
    /// </summary>
    /// <remarks>
    /// In this method, implement your drawing code.
    /// You do NOT need to ImGui.Begin your window.
    /// </remarks>
    void Draw();

    /// <summary>
    /// Code to be executed when the window is opened.
    /// </summary>
    void OnOpen();

    /// <summary>
    /// Code to be executed when the window is closed.
    /// </summary>
    void OnClose();

    /// <summary>
    /// Code to be executed when the window is safe to be disposed or removed from the window system.
    /// Doing so in <see cref="IWindow.OnClose"/> may result in animations not playing correctly.
    /// </summary>
    void OnSafeToRemove();

    /// <summary>
    /// Code to be executed every frame, even when the window is collapsed.
    /// </summary>
    void Update();
}

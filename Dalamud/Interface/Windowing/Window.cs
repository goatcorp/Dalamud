using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Windowing;

/// <inheritdoc/>
public abstract class Window : IWindow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowHost"/> class.
    /// </summary>
    /// <param name="name">The name/ID of this window.
    /// If you have multiple windows with the same name, you will need to
    /// append a unique ID to it by specifying it after "###" behind the window title.
    /// </param>
    /// <param name="flags">The <see cref="ImGuiWindowFlags"/> of this window.</param>
    /// <param name="forceMainWindow">Whether this window should be limited to the main game window.</param>
    protected Window(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false)
    {
        this.WindowName = name;
        this.Flags = flags;
        this.ForceMainWindow = forceMainWindow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowHost"/> class.
    /// </summary>
    /// <param name="name">The name/ID of this window.
    /// If you have multiple windows with the same name, you will need to
    /// append a unique ID to it by specifying it after "###" behind the window title.
    /// </param>
    protected Window(string name)
        : this(name, ImGuiWindowFlags.None)
    {
    }


    /// <inheritdoc/>
    public string? Namespace { get; set; }

    /// <inheritdoc/>
    public string WindowName { get; set; }

    /// <inheritdoc/>
    public bool IsFocused { get; set; }

    /// <inheritdoc/>
    public bool RespectCloseHotkey { get; set; } = true;

    /// <inheritdoc/>
    public bool DisableWindowSounds { get; set; } = false;

    /// <inheritdoc/>
    public uint OnOpenSfxId { get; set; } = 23u;

    /// <inheritdoc/>
    public uint OnCloseSfxId { get; set; } = 24u;

    /// <inheritdoc/>
    public bool DisableFadeInFadeOut { get; set; } = false;

    /// <inheritdoc/>
    public Vector2? Position { get; set; }

    /// <inheritdoc/>
    public ImGuiCond PositionCondition { get; set; }

    /// <inheritdoc/>
    public Vector2? Size { get; set; }

    /// <inheritdoc/>
    public ImGuiCond SizeCondition { get; set; }

    /// <inheritdoc/>
    public WindowSizeConstraints? SizeConstraints { get; set; }

    /// <inheritdoc/>
    public bool? Collapsed { get; set; }

    /// <inheritdoc/>
    public ImGuiCond CollapsedCondition { get; set; }

    /// <inheritdoc/>
    public ImGuiWindowFlags Flags { get; set; }

    /// <inheritdoc/>
    public bool ForceMainWindow { get; set; }

    /// <inheritdoc/>
    public float? BgAlpha { get; set; }

    /// <inheritdoc/>
    public bool ShowCloseButton { get; set; } = true;

    /// <inheritdoc/>
    public bool AllowPinning { get; set; } = true;

    /// <inheritdoc/>
    public bool AllowClickthrough { get; set; } = true;

    /// <inheritdoc/>
    public List<TitleBarButton> TitleBarButtons { get; set; } = [];

    /// <inheritdoc/>
    public bool IsOpen { get; set; }

    /// <inheritdoc/>
    public bool RequestFocus { get; set; }

    /// <inheritdoc/>
    public void Toggle()
    {
        this.IsOpen ^= true;
    }

    /// <inheritdoc/>
    public void BringToFront()
    {
        if (!this.IsOpen)
        {
            return;
        }

        this.RequestFocus = true;
    }

    /// <inheritdoc/>
    public virtual void PreOpenCheck()
    {
    }

    /// <inheritdoc/>
    public virtual bool DrawConditions()
    {
        return true;
    }

    /// <inheritdoc/>
    public virtual void PreDraw()
    {
    }

    /// <inheritdoc/>
    public virtual void PostDraw()
    {
    }

    /// <inheritdoc/>
    public abstract void Draw();

    /// <inheritdoc/>
    public virtual void OnOpen()
    {
    }

    /// <inheritdoc/>
    public virtual void OnClose()
    {
    }

    /// <inheritdoc/>
    public virtual void OnSafeToRemove()
    {
    }

    /// <inheritdoc/>
    public virtual void Update()
    {
    }
}

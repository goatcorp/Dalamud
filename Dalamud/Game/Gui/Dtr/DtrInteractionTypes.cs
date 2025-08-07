namespace Dalamud.Game.Gui.Dtr;

/// <summary>
/// An enum representing the mouse click types.
/// </summary>
public enum MouseClickType
{
    /// <summary>
    /// A left click.
    /// </summary>
    Left,

    /// <summary>
    /// A right click.
    /// </summary>
    Right,
}

/// <summary>
/// Modifier keys that can be held during a mouse click event.
/// </summary>
[Flags]
public enum ClickModifierKeys
{
    /// <summary>
    /// The CTRL key was held.
    /// </summary>
    Ctrl = 1 << 0,

    /// <summary>
    /// The ALT key was held.
    /// </summary>
    Alt = 1 << 1,

    /// <summary>
    /// The SHIFT key was held.
    /// </summary>
    Shift = 1 << 2,
}

/// <summary>
/// Possible directions for scroll wheel events.
/// </summary>
public enum MouseScrollDirection
{
    /// <summary>
    /// No scrolling.
    /// </summary>
    None = 0,

    /// <summary>
    /// A scroll up event.
    /// </summary>
    Up = 1,

    /// <summary>
    /// A scroll down event.
    /// </summary>
    Down = -1,
}

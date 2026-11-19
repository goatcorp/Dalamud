namespace Dalamud.NativeUi.Enums;

/// <summary>
/// Enumeration of AtkResNode DrawFlags.
/// </summary>
[Flags]
internal enum DrawFlags : uint
{
    /// <summary>
    /// No flags active.
    /// </summary>
    None = 0,

    /// <summary>
    /// Triggers the game to re-evaluate this node on next update.
    /// </summary>
    IsDirty = 0x1,

    /// <summary>
    /// IsAnimating.
    /// </summary>
    IsAnimating = 0x2,

    /// <summary>
    /// CalculateTransformation.
    /// </summary>
    CalculateTransformation = 0x4,

    /// <summary>
    /// DisableRapidUp.
    /// </summary>
    DisableRapidUp = 0x10,

    /// <summary>
    /// DisableRapidDown.
    /// </summary>
    DisableRapidDown = 0x20,

    /// <summary>
    /// DisableRapidLeft.
    /// </summary>
    DisableRapidLeft = 0x40,

    /// <summary>
    /// DisableRapidRight.
    /// </summary>
    DisableRapidRight = 0x80,

    /// <summary>
    /// DisableTimelineLabel.
    /// </summary>
    DisableTimelineLabel = 0x100,

    /// <summary>
    /// Causes this node to change the games cursor to a finger pointer cursor when hovering this node.
    /// </summary>
    ClickableCursor = 0x100000,

    /// <summary>
    /// Forces this node to render over top of all other nodes, regardless of its position in the tree.
    /// </summary>
    RenderOnTop = 0x200000,

    /// <summary>
    /// Causes this node to change the game cursor to a text input syncom when hovering this node.
    /// </summary>
    TextInputCursor = 0x400000,

    /// <summary>
    /// Calculates collision for this node spherically based on its width/height.
    /// </summary>
    UseEllipticalCollision = 0x800000,

    /// <summary>
    /// Calculates collision for this node based on its actual size/scale/rotation transformation.
    /// </summary>
    UseTransformedCollision = 0x1000000,
}

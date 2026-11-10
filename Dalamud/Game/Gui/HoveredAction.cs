namespace Dalamud.Game.Gui;

/// <summary>
/// This class represents the hotbar action currently hovered over by the cursor.
/// </summary>
public class HoveredAction
{
    /// <summary>
    /// Gets or sets the base action ID.
    /// </summary>
    public uint BaseActionId { get; set; } = 0;

    /// <summary>
    /// Gets or sets the action ID accounting for automatic upgrades.
    /// </summary>
    public uint ActionId { get; set; } = 0;

    /// <summary>
    /// Gets or sets the type of action.
    /// </summary>
    public DetailKind DetailKind { get; set; } = DetailKind.None;
}

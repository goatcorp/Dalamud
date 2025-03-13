namespace Dalamud.Game.Text.Evaluator.Internal;

/// <summary>
/// An enum providing additional information about the sheet redirect.
/// </summary>
[Flags]
internal enum SheetRedirectFlags
{
    /// <summary>
    /// No flags.
    /// </summary>
    None = 0,

    /// <summary>
    /// Resolved to a sheet related with items.
    /// </summary>
    Item = 1,

    /// <summary>
    /// Resolved to the EventItem sheet.
    /// </summary>
    EventItem = 2,

    /// <summary>
    /// Resolved to a high quality item.
    /// </summary>
    /// <remarks>
    /// Append Addon#9.
    /// </remarks>
    HighQuality = 4,

    /// <summary>
    /// Resolved to a collectible item.
    /// </summary>
    /// <remarks>
    /// Append Addon#150.
    /// </remarks>
    Collectible = 8,

    /// <summary>
    /// Resolved to a sheet related with actions.
    /// </summary>
    Action = 16,

    /// <summary>
    /// Resolved to the Action sheet.
    /// </summary>
    ActionSheet = 32,
}

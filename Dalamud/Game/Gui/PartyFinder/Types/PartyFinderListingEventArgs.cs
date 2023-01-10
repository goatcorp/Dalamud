namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// This class represents additional arguments passed by the game.
/// </summary>
public class PartyFinderListingEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PartyFinderListingEventArgs"/> class.
    /// </summary>
    /// <param name="batchNumber">The batch number.</param>
    internal PartyFinderListingEventArgs(int batchNumber)
    {
        this.BatchNumber = batchNumber;
    }

    /// <summary>
    /// Gets the batch number.
    /// </summary>
    public int BatchNumber { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the listing is visible.
    /// </summary>
    public bool Visible { get; set; } = true;
}

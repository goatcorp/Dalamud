namespace Dalamud.Game.Gui.PartyFinder.Types;

/// <summary>
/// A interface representing  additional arguments passed by the game.
/// </summary>
public interface IPartyFinderListingEventArgs
{
    /// <summary>
    /// Gets the batch number.
    /// </summary>
    int BatchNumber { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the listing is visible.
    /// </summary>
    bool Visible { get; set; }
}

/// <summary>
/// This class represents additional arguments passed by the game.
/// </summary>
internal class PartyFinderListingEventArgs : IPartyFinderListingEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PartyFinderListingEventArgs"/> class.
    /// </summary>
    /// <param name="batchNumber">The batch number.</param>
    internal PartyFinderListingEventArgs(int batchNumber)
    {
        this.BatchNumber = batchNumber;
    }

    /// <inheritdoc/>
    public int BatchNumber { get; }

    /// <inheritdoc/>
    public bool Visible { get; set; } = true;
}

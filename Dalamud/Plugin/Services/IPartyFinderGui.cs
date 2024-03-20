using Dalamud.Game.Gui.PartyFinder.Types;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class handles interacting with the native PartyFinder window.
/// </summary>
public interface IPartyFinderGui
{
    /// <summary>
    /// Event type fired each time the game receives an individual Party Finder listing.
    /// Cannot modify listings but can hide them.
    /// </summary>
    /// <param name="listing">The listings received.</param>
    /// <param name="args">Additional arguments passed by the game.</param>
    public delegate void PartyFinderListingEventDelegate(PartyFinderListing listing, PartyFinderListingEventArgs args);
    
    /// <summary>
    /// Event fired each time the game receives an individual Party Finder listing.
    /// Cannot modify listings but can hide them.
    /// </summary>
    public event PartyFinderListingEventDelegate ReceiveListing;
}

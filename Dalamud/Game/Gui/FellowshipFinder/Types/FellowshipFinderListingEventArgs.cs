namespace Dalamud.Game.Gui.FellowshipFinder.Types
{
    /// <summary>
    /// This class represents additional arguments passed by the game.
    /// </summary>
    public class FellowshipFinderListingEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FellowshipFinderListingEventArgs"/> class.
        /// </summary>
        /// <param name="batchPart">The batch part.</param>
        internal FellowshipFinderListingEventArgs(uint batchPart)
        {
            this.BatchPart = batchPart;
        }

        /// <summary>
        /// <para>
        /// Gets the batch part.
        /// </para>
        /// <para>
        /// This starts at one for the first part of a new batch. The final part is indicated by this value being zero.
        /// </para>
        /// </summary>
        public uint BatchPart { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the listing is visible.
        /// </summary>
        public bool Visible { get; set; } = true;
    }
}

namespace Dalamud.Game.ClientState.Actors.Resolvers
{
    /// <summary>
    /// Base object resolver.
    /// </summary>
    public abstract class BaseResolver
    {
        private Dalamud dalamud;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseResolver"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal BaseResolver(Dalamud dalamud)
        {
            this.dalamud = dalamud;
        }

        /// <summary>
        /// Gets the Dalamud instance.
        /// </summary>
        internal Dalamud Dalamud => this.dalamud;
    }
}

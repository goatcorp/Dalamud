using System;

namespace Dalamud.Game.ClientState.Actors
{
    /// <summary>
    /// This class represents the members of your party.
    /// </summary>
    public sealed partial class PartyList
    {
#pragma warning disable IDE0052 // Remove unread private members
        // Pending rewrite
        private readonly Dalamud dalamud;
        private readonly ClientStateAddressResolver address;
#pragma warning restore IDE0052 // Remove unread private members

        /// <summary>
        /// Initializes a new instance of the <see cref="PartyList"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        /// <param name="addressResolver">The ClientStateAddressResolver instance.</param>
        internal PartyList(Dalamud dalamud, ClientStateAddressResolver addressResolver)
        {
            this.address = addressResolver;
            this.dalamud = dalamud;
        }
    }
}

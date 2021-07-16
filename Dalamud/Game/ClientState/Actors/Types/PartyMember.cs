using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

namespace Dalamud.Game.ClientState.Actors.Types
{
    /// <summary>
    /// This class represents a party member.
    /// </summary>
    public class PartyMember
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartyMember"/> class.
        /// </summary>
        /// <param name="table">The ActorTable instance.</param>
        /// <param name="rawData">The interop data struct.</param>
        public PartyMember(ActorTable table, Structs.PartyMember rawData)
        {
            this.CharacterName = MemoryHelper.ReadSeString(rawData.namePtr);
            this.Unknown = rawData.unknown;
            this.Actor = null;

            for (var i = 0; i < table.Length; i++)
            {
                if (table[i] != null && table[i].ActorId == rawData.actorId)
                {
                    this.Actor = table[i];
                    break;
                }
            }

            this.ObjectKind = rawData.objectKind;
        }

        /// <summary>
        /// Gets the name of the character.
        /// </summary>
        public SeString CharacterName { get; }

        /// <summary>
        /// Gets something unknown.
        /// </summary>
        public long Unknown { get; }

        /// <summary>
        /// Gets the actor object that corresponds to this party member.
        /// </summary>
        public Actor Actor { get; }

        /// <summary>
        /// Gets the kind or type of actor.
        /// </summary>
        public ObjectKind ObjectKind { get; }
    }
}

using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Actors.Types
{
    public class PartyMember
    {
        public string CharacterName;
        public long Unknown;
        public Actor Actor;
        public ObjectKind ObjectKind;

        public PartyMember(ActorTable table, Structs.PartyMember rawData)
        {
            this.CharacterName = Marshal.PtrToStringAnsi(rawData.namePtr);
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
    }
}

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
            CharacterName = Marshal.PtrToStringAnsi(rawData.namePtr);
            Unknown = rawData.unknown;
            Actor = null;
            for (var i = 0; i < table.Length; i++)
            {
                if (table[i] != null && table[i].ActorId == rawData.actorId)
                {
                    Actor = table[i];
                    break;
                }
            }
            ObjectKind = rawData.objectKind;
        }
    }
}

using Dalamud.Game.ClientState.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
            for (var i = 0; i < 244; i += 2)
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

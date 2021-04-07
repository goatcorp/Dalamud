using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Game.ClientState.Actors.Types
{
    public class PartyMember
    {
        private PartyMember()
        {
        }

        public string CharacterName { get; private set; }

        public Actor Actor { get; private set; }

        public IntPtr Address { get; private set; }

        internal static PartyMember RegularMember(ActorTable table, IntPtr memberAddress)
        {
            var member = new PartyMember
            {
                CharacterName = PtrToStringUtf8(memberAddress + 0x1C4),
                Actor = GetActorById(table, Marshal.ReadInt32(memberAddress, 0x1A8)),
                Address = memberAddress,
            };
            return member;
        }

        internal static PartyMember CrossRealmMember(ActorTable table, IntPtr crossMemberAddress)
        {
            var member = new PartyMember
            {
                CharacterName = PtrToStringUtf8(crossMemberAddress + 0x22),
                Actor = GetActorById(table, Marshal.ReadInt32(crossMemberAddress, 0x10)),
                Address = crossMemberAddress,
            };
            return member;
        }

        internal static PartyMember CompanionMember(ActorTable table, IntPtr companionMemberAddress)
        {
            var actor = GetActorById(table, Marshal.ReadInt32(companionMemberAddress, 0));
            var member = new PartyMember
            {
                Actor = actor,
                CharacterName = actor?.Name ?? string.Empty,
                Address = companionMemberAddress,
            };
            return member;
        }

        internal static PartyMember LocalPlayerMember(Dalamud dalamud)
        {
            var player = dalamud.ClientState.LocalPlayer;
            return new PartyMember()
            {
                Actor = player,
                CharacterName = player?.Name ?? string.Empty,
                Address = player?.Address ?? IntPtr.Zero,
            };
        }

        private static Actor GetActorById(ActorTable table, int id)
        {
            for (var i = 0; i < table.Length; i++)
            {
                var obj = table[i];
                if (obj != null && obj.ActorId == id)
                {
                    return obj;
                }
            }

            return null;
        }

        private static unsafe string PtrToStringUtf8(IntPtr address, int maxLen = 64)
        {
            if (address == IntPtr.Zero)
                return string.Empty;

            var buffer = (byte*)address;
            var len = 0;
            while (len <= maxLen && *(buffer + len) != 0)
                ++len;

            return len < 1 ? string.Empty : Encoding.UTF8.GetString(buffer, len);
        }
    }
}

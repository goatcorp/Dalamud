using System;

using Dalamud.Game.Internal;

namespace Dalamud.Game.ClientState
{
    public sealed class ClientStateAddressResolver : BaseAddressResolver
    {
        // Static offsets
        public IntPtr ActorTable { get; private set; }
        //public IntPtr ViewportActorTable { get; private set; }
        public IntPtr LocalContentId { get; private set; }
        public IntPtr JobGaugeData { get; private set; }
        public IntPtr KeyboardState { get; private set; }
        public IntPtr TargetManager { get; private set; }

        public IntPtr GroupManager { get; private set; }
        public IntPtr CrossRealmGroupManagerPtr { get; private set; }
        public IntPtr CompanionManagerPtr { get; private set; }

        // Functions
        public IntPtr SetupTerritoryType { get; private set; }
        //public IntPtr SomeActorTableAccess { get; private set; }

        public IntPtr ConditionFlags { get; private set; }

        public IntPtr GetCrossRealmMemberCount { get; private set; }
        public IntPtr GetCompanionMemberCount { get; private set; }
        public IntPtr GetCrossMemberByGrpIndex { get; private set; }

        protected override void Setup64Bit(SigScanner sig) {
            // We don't need those anymore, but maybe someone else will - let's leave them here for good measure
            //ViewportActorTable = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 85 ED", 0) + 0x148;
            //SomeActorTableAccess = sig.ScanText("E8 ?? ?? ?? ?? 48 8D 55 A0 48 8D 8E ?? ?? ?? ??");
            ActorTable = sig.GetStaticAddressFromSig("88 91 ?? ?? ?? ?? 48 8D 3D ?? ?? ?? ??");

            LocalContentId = sig.GetStaticAddressFromSig("48 0F 44 05 ?? ?? ?? ?? 48 39 07");
            JobGaugeData = sig.GetStaticAddressFromSig("E8 ?? ?? ?? ?? FF C6 48 8D 5B 0C", 0xB9) + 0x10;

            SetupTerritoryType = sig.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 66 89 91 ?? ?? ?? ??");

            // This resolves to a fixed offset only, without the base address added in, so GetStaticAddressFromSig() can't be used
            KeyboardState = sig.ScanText("48 8D 0C 85 ?? ?? ?? ?? 8B 04 31 85 C2 0F 85") + 0x4;
            
            ConditionFlags = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? BA ?? ?? ?? ?? E8 ?? ?? ?? ?? B0 01 48 83 C4 30");

            TargetManager = sig.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? FF 50 ?? 48 85 DB", 3);

            GroupManager = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 80 B8 ?? ?? ?? ?? ?? 76 50");
            CrossRealmGroupManagerPtr = sig.GetStaticAddressFromSig("77 71 48 8B 05", 2);
            CompanionManagerPtr = sig.GetStaticAddressFromSig("4C 8B 15 ?? ?? ?? ?? 4C 8B C9");
            GetCrossRealmMemberCount = sig.ScanText("E8 ?? ?? ?? ?? 3C 01 77 4B");
            GetCrossMemberByGrpIndex = sig.ScanText("E8 ?? ?? ?? ?? 44 89 7C 24 ?? 4C 8B C8");
            GetCompanionMemberCount = sig.ScanText("E8 ?? ?? ?? ?? 8B D3 85 C0");
        }
    }
}

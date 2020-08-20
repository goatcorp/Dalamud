using System;

namespace Dalamud.Game.Internal.Network {
    public sealed class GameNetworkAddressResolver : BaseAddressResolver {
        public IntPtr ProcessZonePacketDown { get; private set; }
        public IntPtr ProcessZonePacketUp { get; private set; }

        protected override void Setup64Bit(SigScanner sig) {
            //ProcessZonePacket = sig.ScanText("48 89 74 24 18 57 48 83  EC 50 8B F2 49 8B F8 41 0F B7 50 02 8B CE E8 ?? ?? 7A FF 0F B7 57 02 8D 42 89 3D 5F 02 00 00 0F 87 60 01 00 00 4C 8D 05");
            //ProcessZonePacket = sig.ScanText("48 89 74 24 18 57 48 83  EC 50 8B F2 49 8B F8 41 0F B7 50 02 8B CE E8 ?? ?? 73 FF 0F B7 57 02 8D 42 ?? 3D ?? ?? 00 00 0F 87 60 01 00 00 4C 8D 05");
            ProcessZonePacketDown = sig.ScanText("48 89 5C 24 ?? 56 48 83 EC 50 8B F2");
            ProcessZonePacketUp =
                sig.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 70 8B 81 ?? ?? ?? ??");
        }
    }
}

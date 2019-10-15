using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.Internal.File
{
    class ResourceManagerAddressResolver : BaseAddressResolver
    {
        public IntPtr GetResourceAsync { get; private set; }
        public IntPtr GetResourceSync { get; private set; }

        protected override void Setup64Bit(SigScanner sig) {
            GetResourceAsync = sig.ScanText("48 89 5C 24 08 48 89 54  24 10 57 48 83 EC 20 B8 03 00 00 00 48 8B F9 86  82 A1 00 00 00 48 8B 5C 24 38 B8 01 00 00 00 87  83 90 00 00 00 85 C0 74"); 
            GetResourceSync  = sig.ScanText("48 89 5C 24 08 48 89 6C  24 10 48 89 74 24 18 57 41 54 41 55 41 56 41 57  48 83 EC 30 48 8B F9 49 8B E9 48 83 C1 30 4D 8B  F0 4C 8B EA FF 15 CE F6"); 
                  //ReadResourceSync  = sig.ScanText("48 89 74 24 18 57 48 83  EC 50 8B F2 49 8B F8 41 0F B7 50 02 8B CE E8 ?? ?? 7A FF 0F B7 57 02 8D 42 89 3D 5F 02 00 00 0F 87 60 01 00 00 4C 8D 05");
        }
    }
}

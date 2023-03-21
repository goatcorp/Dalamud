using System;
using System.Numerics;

using Dalamud.Game;
using Dalamud.Hooking;

namespace Dalamud.Fools.Plugins
{
    public class GoodVibesPlugin : IFoolsPlugin
    {
        // Plugin
        
        public GoodVibesPlugin()
        {
            var addr = Service<SigScanner>.Get().ScanText("48 83 EC 08 8B 02");
            OffsetModelHook = Hook<OffsetModelDelegate>.FromAddress(addr, OffsetModelDetour);
            OffsetModelHook.Enable();
        }

        public void Dispose()
        {
            OffsetModelHook.Disable();
            OffsetModelHook.Dispose();
        }
        
        // brrrrrrrr
        
        private readonly Random Rng = new();

        private Vector3 GenRandVec()
        {
            var Pos = new float[3];
            for (var i = 0; i < 3; i++)
                Pos[i] = Rng.Next(-5, 5) / 1000f;
            return new Vector3(Pos);
        }

        private delegate nint OffsetModelDelegate(nint a1, nint a2);
        private Hook<OffsetModelDelegate> OffsetModelHook = null!;
        private unsafe nint OffsetModelDetour(nint a1, nint a2)
        {
            *(Vector3*)a2 += GenRandVec();
            return OffsetModelHook.Original(a1, a2);
        }
    }
}
